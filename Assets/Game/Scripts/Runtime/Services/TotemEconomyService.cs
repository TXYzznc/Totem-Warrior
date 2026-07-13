using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class TotemEconomyService : TotemRuntimeServiceBase
{
    public const int SelfTattooInterruptPenalty = 50;

    private readonly Dictionary<int, RuntimeInventory> inventories = new Dictionary<int, RuntimeInventory>(64);
    private readonly Dictionary<int, TotemDeathChestSnapshot> pendingDeathChests = new Dictionary<int, TotemDeathChestSnapshot>(32);
    private readonly Dictionary<int, TotemItemDefinition> itemDefinitions = new Dictionary<int, TotemItemDefinition>(64);
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemTattooService tattooService;
    private TotemDataService dataService;
    private int selfTattooInterruptPenaltyCount;
    private int lastSelfTattooInterruptPenaltyAmount;
    private int lastSelfTattooInterruptPenaltyActorId;
    private string lastSelfTattooInterruptPenaltyReason = string.Empty;

    public override string ServiceName => "Economy";

    public int PendingDeathChestCount => pendingDeathChests.Count;

    public int ItemDefinitionCount => itemDefinitions.Count;

    public int SelfTattooInterruptPenaltyCount => selfTattooInterruptPenaltyCount;

    public int LastSelfTattooInterruptPenaltyAmount => lastSelfTattooInterruptPenaltyAmount;

    public int LastSelfTattooInterruptPenaltyActorId => lastSelfTattooInterruptPenaltyActorId;

    public string LastSelfTattooInterruptPenaltyReason => lastSelfTattooInterruptPenaltyReason;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        tattooService = runtime.GetService<TotemTattooService>();
        dataService = runtime.GetService<TotemDataService>();
        ReloadItemDefinitions(dataService?.GameplayCatalog);
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

        if (actorService != null)
        {
            actorService.DamageResolved += OnDamageResolved;
        }

        if (tattooService != null)
        {
            tattooService.SelfTattooCancelled += OnSelfTattooCancelled;
        }
    }

    protected override void OnShutdown()
    {
        if (tattooService != null)
        {
            tattooService.SelfTattooCancelled -= OnSelfTattooCancelled;
            tattooService = null;
        }

        if (actorService != null)
        {
            actorService.DamageResolved -= OnDamageResolved;
        }

        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        actorService = null;
        dataService = null;
        ResetRunState();
        itemDefinitions.Clear();
    }

    public void ReloadItemDefinitions(TotemGameplayCatalog catalog)
    {
        itemDefinitions.Clear();
        var source = catalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
        source.Normalize();
        var definitions = source.CreateItemDefinitions();
        for (int i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i];
            if (definition != null && definition.ItemId > 0 && !itemDefinitions.ContainsKey(definition.ItemId))
            {
                itemDefinitions.Add(definition.ItemId, definition);
            }
        }
    }

    public bool TryGetItemDefinition(int itemId, out TotemItemDefinition definition)
    {
        return itemDefinitions.TryGetValue(itemId, out definition);
    }

    public int CalculateSellValue(int itemId, int count)
    {
        if (count <= 0 || !TryGetItemDefinition(itemId, out var definition))
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(definition.BasePrice * definition.SellRatio * count));
    }

    public void RegisterActor(TotemActorModel actor)
    {
        if (actor == null || inventories.ContainsKey(actor.ActorId))
        {
            return;
        }

        inventories[actor.ActorId] = new RuntimeInventory();
    }

    public void RegisterParticipant(TotemParticipantModel participant)
    {
        if (participant == null || inventories.ContainsKey(participant.ParticipantId))
        {
            return;
        }

        inventories[participant.ParticipantId] = new RuntimeInventory();
    }

    public TotemInventorySnapshot CaptureInventory(TotemActorModel actor)
    {
        return CaptureInventory(actor?.ActorId ?? 0);
    }

    public TotemInventorySnapshot CaptureInventory(TotemParticipantModel participant)
    {
        return CaptureInventory(participant?.ParticipantId ?? 0);
    }

    private TotemInventorySnapshot CaptureInventory(int participantId)
    {
        var inventory = GetOrCreateInventory(participantId);
        return new TotemInventorySnapshot
        {
            actorId = participantId,
            coins = inventory.Coins,
            inkBottleCount = inventory.InkBottleCount,
            recipeShardCount = inventory.RecipeShardCount,
            recipeUnlockCount = inventory.RecipeIds.Count,
            recipeIds = inventory.RecipeIds.ToArray(),
            equipmentCount = inventory.EquipmentCount,
            lootItems = CreateLootItemSnapshots(inventory),
        };
    }

    public void AddCoins(TotemActorModel actor, int delta)
    {
        var inventory = GetOrCreateInventory(actor);
        inventory.Coins = Mathf.Max(0, inventory.Coins + delta);
    }

    public bool SpendCoins(TotemActorModel actor, int amount)
    {
        var inventory = GetOrCreateInventory(actor);
        if (amount <= 0)
        {
            return true;
        }

        if (inventory.Coins < amount)
        {
            return false;
        }

        inventory.Coins -= amount;
        return true;
    }

    public void AddInk(TotemActorModel actor, int count)
    {
        var inventory = GetOrCreateInventory(actor);
        inventory.InkBottleCount += Mathf.Max(0, count);
    }

    public bool SpendInk(TotemActorModel actor, int count)
    {
        var inventory = GetOrCreateInventory(actor);
        if (count <= 0)
        {
            return true;
        }

        if (inventory.InkBottleCount < count)
        {
            return false;
        }

        inventory.InkBottleCount -= count;
        return true;
    }

    public void AddRecipeShards(TotemActorModel actor, int count)
    {
        var inventory = GetOrCreateInventory(actor);
        inventory.RecipeShardCount += Mathf.Max(0, count);
    }

    public bool AddConfiguredItem(TotemActorModel actor, int itemId, int count)
    {
        if (actor == null || count <= 0 || !TryGetItemDefinition(itemId, out var definition))
        {
            return false;
        }

        switch (definition.ItemType)
        {
            case TotemItemType.Coin:
                AddCoins(actor, count);
                return true;
            case TotemItemType.InkBottle:
                AddInk(actor, count);
                return true;
            case TotemItemType.RecipeShard:
                AddRecipeShards(actor, count);
                return true;
            case TotemItemType.RecipeFull:
                return UnlockRecipe(actor, $"recipe_item_{itemId}");
            case TotemItemType.Equipment:
                AddEquipment(actor, count);
                return true;
            default:
                return false;
        }
    }

    public bool TryAddEnemyLoot(
        TotemParticipantModel participant,
        TotemEnemyLootRewardType rewardType,
        string itemId,
        int count)
    {
        if (participant == null || count <= 0 || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        return TryAddEnemyLoot(GetOrCreateInventory(participant.ParticipantId), rewardType, itemId, count);
    }

    public bool TryAddEnemyLoot(
        TotemActorModel participant,
        TotemEnemyLootRewardType rewardType,
        string itemId,
        int count)
    {
        if (participant == null || count <= 0 || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        return TryAddEnemyLoot(GetOrCreateInventory(participant.ActorId), rewardType, itemId, count);
    }

    public bool UnlockRecipe(TotemActorModel actor, string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        var inventory = GetOrCreateInventory(actor);
        if (inventory.RecipeIds.Contains(recipeId))
        {
            return false;
        }

        inventory.RecipeIds.Add(recipeId);
        return true;
    }

    public bool HasRecipe(TotemActorModel actor, string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        return GetOrCreateInventory(actor).RecipeIds.Contains(recipeId);
    }

    public void AddEquipment(TotemActorModel actor, int count)
    {
        var inventory = GetOrCreateInventory(actor);
        inventory.EquipmentCount += Mathf.Max(0, count);
    }

    public TotemDeathChestSnapshot CalculateDeathChest(TotemActorModel actor)
    {
        var inventory = GetOrCreateInventory(actor);
        return new TotemDeathChestSnapshot
        {
            deadActorId = actor?.ActorId ?? 0,
            coins = Mathf.FloorToInt(inventory.Coins * 0.5f),
            inkBottleCount = Mathf.FloorToInt(inventory.InkBottleCount * 0.5f),
            recipeCopyCount = Mathf.FloorToInt(inventory.RecipeShardCount * 0.5f),
            equipmentCount = inventory.EquipmentCount,
        };
    }

    public bool TryGetPendingDeathChest(TotemActorModel deadActor, out TotemDeathChestSnapshot snapshot)
    {
        snapshot = null;
        if (deadActor == null || !pendingDeathChests.TryGetValue(deadActor.ActorId, out var value))
        {
            return false;
        }

        snapshot = CopyDeathChest(value);
        return true;
    }

    public bool HasPendingDeathChest(TotemActorModel deadActor)
    {
        return deadActor != null && pendingDeathChests.ContainsKey(deadActor.ActorId);
    }

    public int GetPendingDeathChestValue(TotemActorModel deadActor)
    {
        if (deadActor == null || !pendingDeathChests.TryGetValue(deadActor.ActorId, out var snapshot))
        {
            return 0;
        }

        return ComputeDeathChestValue(snapshot);
    }

    public bool TryLootDeathChest(TotemActorModel looter, TotemActorModel deadActor, out TotemDeathChestSnapshot snapshot)
    {
        snapshot = null;
        if (looter == null || deadActor == null || !pendingDeathChests.TryGetValue(deadActor.ActorId, out var value))
        {
            return false;
        }

        pendingDeathChests.Remove(deadActor.ActorId);
        snapshot = CopyDeathChest(value);
        AddCoins(looter, snapshot.coins);
        AddInk(looter, snapshot.inkBottleCount);
        AddRecipeShards(looter, snapshot.recipeCopyCount);
        AddEquipment(looter, snapshot.equipmentCount);
        GFTrace.Success("TotemEconomy", "DeathChest.Looted", null, GFTrace.Data(
            "looter", looter.Name,
            "deadActor", deadActor.Name,
            "coins", snapshot.coins.ToString(),
            "ink", snapshot.inkBottleCount.ToString(),
            "recipes", snapshot.recipeCopyCount.ToString(),
            "equipment", snapshot.equipmentCount.ToString()));
        return true;
    }

    private RuntimeInventory GetOrCreateInventory(TotemActorModel actor)
    {
        return GetOrCreateInventory(actor?.ActorId ?? 0);
    }

    private RuntimeInventory GetOrCreateInventory(int participantId)
    {
        if (!inventories.TryGetValue(participantId, out var inventory))
        {
            inventory = new RuntimeInventory();
            inventories[participantId] = inventory;
        }

        return inventory;
    }

    private static bool TryAddEnemyLoot(
        RuntimeInventory inventory,
        TotemEnemyLootRewardType rewardType,
        string itemId,
        int count)
    {
        if (inventory == null || count <= 0 || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (rewardType == TotemEnemyLootRewardType.Coin)
        {
            inventory.Coins = Mathf.Max(0, inventory.Coins + count);
            return true;
        }

        if (rewardType == TotemEnemyLootRewardType.Unknown || rewardType == TotemEnemyLootRewardType.Recipe)
        {
            return false;
        }

        string key = BuildLootStackKey(rewardType, itemId);
        inventory.LootStacks.TryGetValue(key, out int current);
        inventory.LootStacks[key] = current + count;
        return true;
    }

    private static TotemLootInventoryStackSnapshot[] CreateLootItemSnapshots(RuntimeInventory inventory)
    {
        if (inventory == null || inventory.LootStacks.Count <= 0)
        {
            return Array.Empty<TotemLootInventoryStackSnapshot>();
        }

        var keys = new List<string>(inventory.LootStacks.Keys);
        keys.Sort(StringComparer.Ordinal);
        var result = new TotemLootInventoryStackSnapshot[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            int separator = key.IndexOf(':');
            result[i] = new TotemLootInventoryStackSnapshot
            {
                rewardType = separator < 0 ? string.Empty : key.Substring(0, separator),
                itemId = separator < 0 || separator >= key.Length - 1 ? key : key.Substring(separator + 1),
                count = inventory.LootStacks[key],
            };
        }

        return result;
    }

    private static string BuildLootStackKey(TotemEnemyLootRewardType rewardType, string itemId)
    {
        return rewardType + ":" + itemId.Trim();
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            if (actorService == null)
            {
                return;
            }

            for (int i = 0; i < actorService.Actors.Count; i++)
            {
                RegisterActor(actorService.Actors[i]);
            }

            if (actorService.Player != null)
            {
                AddCoins(actorService.Player, 120);
                AddInk(actorService.Player, 6);
                AddRecipeShards(actorService.Player, 2);
                AddEquipment(actorService.Player, 1);
            }

            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            GFTrace.Info("TotemEconomy", "RunState.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void ResetRunState()
    {
        inventories.Clear();
        pendingDeathChests.Clear();
        selfTattooInterruptPenaltyCount = 0;
        lastSelfTattooInterruptPenaltyAmount = 0;
        lastSelfTattooInterruptPenaltyActorId = 0;
        lastSelfTattooInterruptPenaltyReason = string.Empty;
    }

    private void OnDamageResolved(TotemDamageRecord record)
    {
        if (!record.Killed || record.Target == null)
        {
            return;
        }

        CreateDeathChest(record.Target);
    }

    private void OnSelfTattooCancelled(TotemActorModel actor, string reason, int requestedPenalty)
    {
        int penalty = ResolveSelfTattooPenalty(reason, requestedPenalty);
        if (actor == null || penalty <= 0)
        {
            return;
        }

        var inventory = GetOrCreateInventory(actor);
        int amount = Mathf.Min(inventory.Coins, penalty);
        inventory.Coins -= amount;
        selfTattooInterruptPenaltyCount++;
        lastSelfTattooInterruptPenaltyAmount = amount;
        lastSelfTattooInterruptPenaltyActorId = actor.ActorId;
        lastSelfTattooInterruptPenaltyReason = string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason;
        GFTrace.Info("TotemEconomy", "SelfTattooInterrupt.Penalty", null, GFTrace.Data(
            "actor", actor.Name ?? string.Empty,
            "actorId", actor.ActorId.ToString(),
            "reason", lastSelfTattooInterruptPenaltyReason,
            "amount", amount.ToString(),
            "remainingCoins", inventory.Coins.ToString()));
    }

    private static int ResolveSelfTattooPenalty(string reason, int requestedPenalty)
    {
        if (string.Equals(reason, "Manual", System.StringComparison.Ordinal))
        {
            return Mathf.Max(0, requestedPenalty);
        }

        if (string.Equals(reason, "Moved", System.StringComparison.Ordinal)
            || string.Equals(reason, "Damaged", System.StringComparison.Ordinal)
            || string.Equals(reason, "Killed", System.StringComparison.Ordinal))
        {
            return SelfTattooInterruptPenalty;
        }

        return 0;
    }

    private bool CreateDeathChest(TotemActorModel deadActor)
    {
        if (deadActor == null || pendingDeathChests.ContainsKey(deadActor.ActorId))
        {
            return false;
        }

        var snapshot = CalculateDeathChest(deadActor);
        if (!HasDeathChestContent(snapshot))
        {
            return false;
        }

        pendingDeathChests[deadActor.ActorId] = snapshot;
        var inventory = GetOrCreateInventory(deadActor);
        inventory.Coins = Mathf.Max(0, inventory.Coins - snapshot.coins);
        inventory.InkBottleCount = Mathf.Max(0, inventory.InkBottleCount - snapshot.inkBottleCount);
        inventory.RecipeShardCount = Mathf.Max(0, inventory.RecipeShardCount - snapshot.recipeCopyCount);
        inventory.EquipmentCount = Mathf.Max(0, inventory.EquipmentCount - snapshot.equipmentCount);
        GFTrace.Success("TotemEconomy", "DeathChest.Spawned", null, GFTrace.Data(
            "deadActor", deadActor.Name,
            "coins", snapshot.coins.ToString(),
            "ink", snapshot.inkBottleCount.ToString(),
            "recipes", snapshot.recipeCopyCount.ToString(),
            "equipment", snapshot.equipmentCount.ToString()));
        return true;
    }

    private static bool HasDeathChestContent(TotemDeathChestSnapshot snapshot)
    {
        return snapshot != null
            && (snapshot.coins > 0
                || snapshot.inkBottleCount > 0
                || snapshot.recipeCopyCount > 0
                || snapshot.equipmentCount > 0);
    }

    private static int ComputeDeathChestValue(TotemDeathChestSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return 0;
        }

        return snapshot.coins
            + snapshot.inkBottleCount * 15
            + snapshot.recipeCopyCount * 30
            + snapshot.equipmentCount * 50;
    }

    private static TotemDeathChestSnapshot CopyDeathChest(TotemDeathChestSnapshot source)
    {
        if (source == null)
        {
            return null;
        }

        return new TotemDeathChestSnapshot
        {
            deadActorId = source.deadActorId,
            coins = source.coins,
            inkBottleCount = source.inkBottleCount,
            recipeCopyCount = source.recipeCopyCount,
            equipmentCount = source.equipmentCount,
        };
    }

    private sealed class RuntimeInventory
    {
        public readonly List<string> RecipeIds = new List<string>(8);
        public readonly Dictionary<string, int> LootStacks = new Dictionary<string, int>(16, StringComparer.Ordinal);
        public int Coins;
        public int InkBottleCount;
        public int RecipeShardCount;
        public int EquipmentCount;
    }
}
