using UnityEngine;

public sealed class TotemInteractionService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float DeathChestInteractRadius = 3f;
    public const float MapEventInteractRadius = 3f;

    private TotemGameFlowService flowService;
    private TotemInputService inputService;
    private TotemMapService mapService;
    private TotemActorService actorService;
    private TotemEconomyService economyService;
    private TotemWeaponService weaponService;
    private TotemChestService chestService;
    private TotemNpcService npcService;
    private TotemChoiceService choiceService;
    private TotemUIService uiService;
    private TotemActorModel currentDeathChestActor;
    private TotemWeaponPickupModel currentWeaponPickup;
    private TotemChestModel currentChest;
    private TotemNpcModel currentNpc;
    private TotemMapAnchor currentMapEventAnchor;
    private string currentPrompt = string.Empty;
    private string lastInteraction = string.Empty;

    public override string ServiceName => "Interaction";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        inputService = runtime.GetService<TotemInputService>();
        mapService = runtime.GetService<TotemMapService>();
        actorService = runtime.GetService<TotemActorService>();
        economyService = runtime.GetService<TotemEconomyService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        chestService = runtime.GetService<TotemChestService>();
        npcService = runtime.GetService<TotemNpcService>();
        choiceService = runtime.GetService<TotemChoiceService>();
        uiService = runtime.GetService<TotemUIService>();
    }

    protected override void OnShutdown()
    {
        flowService = null;
        inputService = null;
        mapService = null;
        actorService = null;
        economyService = null;
        weaponService = null;
        chestService = null;
        npcService = null;
        choiceService = null;
        uiService = null;
        currentDeathChestActor = null;
        currentWeaponPickup = null;
        currentChest = null;
        currentNpc = null;
        currentMapEventAnchor = null;
        currentPrompt = string.Empty;
        lastInteraction = string.Empty;
    }

    public void Tick(float deltaTime)
    {
        if (flowService?.CurrentState != TotemGameFlowState.CombatHud || actorService?.Player == null)
        {
            SetCurrentFocus(null, null, null, null, null);
            return;
        }

        var deathChest = FindNearestDeathChest(actorService.Player.Position);
        var weaponPickup = deathChest == null ? weaponService?.FindNearestPickup(actorService.Player.Position, TotemWeaponService.PickupInteractRadius) : null;
        var chest = deathChest == null && weaponPickup == null ? chestService?.FindNearestClosedChest(actorService.Player.Position, TotemChestService.ChestInteractRadius) : null;
        var npc = deathChest == null && weaponPickup == null && chest == null ? npcService?.FindNearestInteractable(actorService.Player.Position) : null;
        var mapEvent = deathChest == null && weaponPickup == null && chest == null ? FindNearestMapEventAnchor(actorService.Player.Position) : null;
        ResolveNpcMapEventFocus(actorService.Player.Position, ref npc, ref mapEvent);
        SetCurrentFocus(deathChest, weaponPickup, chest, npc, mapEvent);
        var input = inputService?.Current ?? TotemInputSnapshot.Empty;
        if (input.interactPressed)
        {
            TryInteractCurrent();
        }
    }

    public bool TryInteractCurrent()
    {
        if (currentDeathChestActor != null)
        {
            return LootDeathChest(currentDeathChestActor);
        }

        if (currentWeaponPickup != null)
        {
            return PickupWeapon(currentWeaponPickup);
        }

        if (currentChest != null)
        {
            return OpenChest(currentChest);
        }

        if (currentNpc != null)
        {
            OpenInteraction(currentNpc);
            return true;
        }

        if (currentMapEventAnchor != null)
        {
            return OpenMapEvent(currentMapEventAnchor);
        }

        return false;
    }

    public TotemInteractionSnapshot CaptureSnapshot()
    {
        var choice = choiceService?.Current;
        return new TotemInteractionSnapshot
        {
            hasNpc = currentNpc != null,
            npcId = currentNpc?.NpcId ?? string.Empty,
            npcType = currentNpc == null ? string.Empty : currentNpc.Type.ToString(),
            hasDeathChest = currentDeathChestActor != null,
            deathChestActorId = currentDeathChestActor?.ActorId ?? 0,
            hasWeaponPickup = currentWeaponPickup != null,
            weaponPickupInstanceId = currentWeaponPickup?.InstanceId ?? 0,
            weaponPickupWeaponId = currentWeaponPickup?.WeaponId ?? string.Empty,
            hasChest = currentChest != null,
            chestInstanceId = currentChest?.InstanceId ?? 0,
            chestId = currentChest?.ChestId ?? string.Empty,
            hasMapEvent = currentMapEventAnchor != null,
            mapEventAnchorId = currentMapEventAnchor?.AnchorId ?? string.Empty,
            mapEventId = GetMapEventId(currentMapEventAnchor),
            prompt = currentPrompt,
            lastInteraction = lastInteraction,
            choiceEventId = choice?.EventId ?? string.Empty,
            choiceCount = choice?.Options?.Length ?? 0,
        };
    }

    public static string BuildPrompt(TotemNpcModel npc)
    {
        if (npc == null)
        {
            return string.Empty;
        }

        return npc.Type == TotemNpcType.Merchant
            ? $"F: Shop with {npc.NpcId}"
            : $"F: Tattoo with {npc.NpcId}";
    }

    public static string BuildDeathChestPrompt(TotemActorModel deadActor)
    {
        return deadActor == null ? string.Empty : $"F: Loot Death Chest {deadActor.ActorId}";
    }

    public static string BuildWeaponPickupPrompt(TotemWeaponPickupModel pickup)
    {
        return pickup == null ? string.Empty : $"F: Pick Up {pickup.WeaponId}";
    }

    public static string BuildChestPrompt(TotemChestModel chest)
    {
        return chest == null ? string.Empty : $"F: Open {chest.ChestId}";
    }

    public static string BuildMapEventPrompt(TotemMapAnchor anchor)
    {
        string eventId = GetMapEventId(anchor);
        return string.IsNullOrWhiteSpace(eventId) ? string.Empty : $"F: Inspect {eventId}";
    }

    public static string BuildChoiceEventId(TotemNpcModel npc)
    {
        if (npc == null)
        {
            return "interaction_choice";
        }

        string prefix = npc.Type == TotemNpcType.Merchant ? "shop" : "tattoo";
        return $"{prefix}_{npc.NpcId}";
    }

    private void SetCurrentFocus(TotemActorModel deathChestActor, TotemWeaponPickupModel weaponPickup, TotemChestModel chest, TotemNpcModel npc, TotemMapAnchor mapEventAnchor)
    {
        if (ReferenceEquals(currentDeathChestActor, deathChestActor) && ReferenceEquals(currentWeaponPickup, weaponPickup) && ReferenceEquals(currentChest, chest) && ReferenceEquals(currentNpc, npc) && ReferenceEquals(currentMapEventAnchor, mapEventAnchor))
        {
            return;
        }

        currentDeathChestActor = deathChestActor;
        currentWeaponPickup = weaponPickup;
        currentChest = chest;
        currentNpc = npc;
        currentMapEventAnchor = mapEventAnchor;
        currentPrompt = currentDeathChestActor != null
            ? BuildDeathChestPrompt(currentDeathChestActor)
            : currentWeaponPickup != null
                ? BuildWeaponPickupPrompt(currentWeaponPickup)
                : currentChest != null
                    ? BuildChestPrompt(currentChest)
                    : currentNpc != null
                        ? BuildPrompt(currentNpc)
                        : BuildMapEventPrompt(currentMapEventAnchor);
        GFTrace.Info("TotemInteraction", "FocusChanged", null, GFTrace.Data(
            "deathChestActorId", (currentDeathChestActor?.ActorId ?? 0).ToString(),
            "weaponPickupId", (currentWeaponPickup?.InstanceId ?? 0).ToString(),
            "weaponId", currentWeaponPickup?.WeaponId ?? string.Empty,
            "chestId", currentChest?.ChestId ?? string.Empty,
            "chestInstanceId", (currentChest?.InstanceId ?? 0).ToString(),
            "npcId", currentNpc?.NpcId ?? string.Empty,
            "mapEventAnchorId", currentMapEventAnchor?.AnchorId ?? string.Empty,
            "mapEventId", GetMapEventId(currentMapEventAnchor),
            "prompt", currentPrompt));
    }

    private TotemActorModel FindNearestDeathChest(Vector3 position)
    {
        if (actorService?.Actors == null || economyService == null)
        {
            return null;
        }

        float bestDistance = DeathChestInteractRadius * DeathChestInteractRadius;
        TotemActorModel bestActor = null;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor == null || actor.IsAlive || !economyService.HasPendingDeathChest(actor))
            {
                continue;
            }

            float distance = (actor.Position - position).sqrMagnitude;
            if (distance > bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestActor = actor;
        }

        return bestActor;
    }

    private bool LootDeathChest(TotemActorModel deadActor)
    {
        economyService ??= Runtime.GetService<TotemEconomyService>();
        actorService ??= Runtime.GetService<TotemActorService>();
        if (economyService == null || actorService?.Player == null || deadActor == null)
        {
            return false;
        }

        if (!economyService.TryLootDeathChest(actorService.Player, deadActor, out var snapshot))
        {
            return false;
        }

        lastInteraction = $"death_chest_{deadActor.ActorId}";
        GFTrace.Success("TotemInteraction", "DeathChestLooted", null, GFTrace.Data(
            "deadActorId", deadActor.ActorId.ToString(),
            "coins", snapshot.coins.ToString(),
            "ink", snapshot.inkBottleCount.ToString(),
            "recipes", snapshot.recipeCopyCount.ToString(),
            "equipment", snapshot.equipmentCount.ToString()));
        RefreshFocusAfterInteraction();
        return true;
    }

    private bool PickupWeapon(TotemWeaponPickupModel pickup)
    {
        weaponService ??= Runtime.GetService<TotemWeaponService>();
        actorService ??= Runtime.GetService<TotemActorService>();
        if (weaponService == null || actorService?.Player == null || pickup == null)
        {
            return false;
        }

        if (!weaponService.TryPickupWeapon(actorService.Player, pickup, out var result))
        {
            return false;
        }

        lastInteraction = $"weapon_pickup_{result.pickupInstanceId}";
        GFTrace.Success("TotemInteraction", "WeaponPicked", null, GFTrace.Data(
            "pickupId", result.pickupInstanceId.ToString(),
            "weaponId", result.weaponId,
            "level", result.weaponLevel.ToString(),
            "convertedGold", result.convertedGold.ToString()));
        RefreshFocusAfterInteraction();
        return true;
    }

    private bool OpenChest(TotemChestModel chest)
    {
        chestService ??= Runtime.GetService<TotemChestService>();
        actorService ??= Runtime.GetService<TotemActorService>();
        if (chestService == null || actorService?.Player == null || chest == null)
        {
            return false;
        }

        int seed = ComputeStableSeed($"{chest.ChestId}_{chest.InstanceId}", actorService.Player.ActorId);
        if (!chestService.TryOpenChest(actorService.Player, chest, seed, out var result))
        {
            return false;
        }

        lastInteraction = $"chest_{result.chestInstanceId}";
        GFTrace.Success("TotemInteraction", "ChestOpened", null, GFTrace.Data(
            "chestId", result.chestId,
            "instanceId", result.chestInstanceId.ToString(),
            "rewardType", result.rewardType.ToString(),
            "rewardId", result.rewardId,
            "coins", result.coinsAdded.ToString(),
            "heal", result.healAmount.ToString("F1"),
            "weaponPickupId", result.spawnedWeaponPickupId.ToString()));
        RefreshFocusAfterInteraction();
        return true;
    }

    private bool OpenMapEvent(TotemMapAnchor anchor)
    {
        choiceService ??= Runtime.GetService<TotemChoiceService>();
        uiService ??= Runtime.GetService<TotemUIService>();
        actorService ??= Runtime.GetService<TotemActorService>();
        mapService ??= Runtime.GetService<TotemMapService>();
        if (choiceService == null || actorService?.Player == null || anchor == null || anchor.Kind != TotemMapAnchorKind.Event)
        {
            return false;
        }

        int seed = ComputeStableSeed(anchor.AnchorId, actorService.Player.ActorId);
        if (mapService?.CurrentMap != null)
        {
            seed = unchecked(seed + mapService.CurrentMap.Seed * 31);
        }

        var choice = choiceService.RollAnchorChoice(anchor, seed);
        if (choice == null)
        {
            return false;
        }

        uiService?.OpenThreeChoice(choice);
        lastInteraction = $"map_event_{anchor.AnchorId}";
        GFTrace.Success("TotemInteraction", "MapEventOpened", null, GFTrace.Data(
            "anchorId", anchor.AnchorId ?? string.Empty,
            "eventId", choice.EventId ?? string.Empty,
            "choiceCount", (choice.Options?.Length ?? 0).ToString()));
        RefreshFocusAfterInteraction();
        return true;
    }

    private void OpenInteraction(TotemNpcModel npc)
    {
        if (npc == null)
        {
            return;
        }

        lastInteraction = BuildChoiceEventId(npc);
        uiService ??= Runtime.GetService<TotemUIService>();
        if (npc.Type == TotemNpcType.Merchant)
        {
            uiService?.OpenShop(npc);
            GFTrace.Success("TotemInteraction", "ShopOpened", null, GFTrace.Data("eventId", lastInteraction, "npcId", npc.NpcId));
            return;
        }

        var choice = RollInteractionChoice(npc);
        uiService?.OpenTattooStudio(npc, choice);
        GFTrace.Success("TotemInteraction", "TattooStudioOpened", null, GFTrace.Data(
            "eventId", lastInteraction,
            "choiceCount", (choice?.Options?.Length ?? 0).ToString()));
    }

    private TotemChoiceSnapshot RollInteractionChoice(TotemNpcModel npc)
    {
        if (choiceService == null)
        {
            return null;
        }

        string eventId = BuildChoiceEventId(npc);
        int seed = ComputeStableSeed(eventId, actorService.Player.ActorId);
        var choice = choiceService.RollThreeChoices(eventId, seed);
        lastInteraction = eventId;
        GFTrace.Success("TotemInteraction", "ChoiceRolled", null, GFTrace.Data(
            "eventId", eventId,
            "choiceCount", (choice?.Options?.Length ?? 0).ToString()));
        return choice;
    }

    private void RefreshFocusAfterInteraction()
    {
        var player = actorService?.Player;
        if (player == null)
        {
            SetCurrentFocus(null, null, null, null, null);
            return;
        }

        var weaponPickup = weaponService?.FindNearestPickup(player.Position, TotemWeaponService.PickupInteractRadius);
        var chest = weaponPickup == null ? chestService?.FindNearestClosedChest(player.Position, TotemChestService.ChestInteractRadius) : null;
        var npc = weaponPickup == null && chest == null ? npcService?.FindNearestInteractable(player.Position) : null;
        var mapEvent = weaponPickup == null && chest == null ? FindNearestMapEventAnchor(player.Position) : null;
        ResolveNpcMapEventFocus(player.Position, ref npc, ref mapEvent);
        SetCurrentFocus(null, weaponPickup, chest, npc, mapEvent);
    }

    private static void ResolveNpcMapEventFocus(Vector3 playerPosition, ref TotemNpcModel npc, ref TotemMapAnchor mapEvent)
    {
        if (npc == null || mapEvent == null)
        {
            return;
        }

        float npcDistance = FlatDistanceSq(playerPosition, npc.Position);
        float eventDistance = FlatDistanceSq(playerPosition, mapEvent.Position);
        if (eventDistance + 0.0001f < npcDistance)
        {
            npc = null;
        }
        else
        {
            mapEvent = null;
        }
    }

    private static float FlatDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private TotemMapAnchor FindNearestMapEventAnchor(Vector3 position)
    {
        if (choiceService?.ChoiceState == TotemChoiceRuntimeState.Showing)
        {
            return null;
        }

        var anchors = TotemMapService.FindAnchors(mapService?.CurrentMap, TotemMapAnchorKind.Event);
        float bestDistance = MapEventInteractRadius * MapEventInteractRadius;
        TotemMapAnchor best = null;
        for (int i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            float dx = anchor.Position.x - position.x;
            float dz = anchor.Position.z - position.z;
            float distance = dx * dx + dz * dz;
            if (distance > bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = anchor;
        }

        return best;
    }

    private static string GetMapEventId(TotemMapAnchor anchor)
    {
        if (anchor == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(anchor.PayloadId) ? anchor.AnchorId ?? string.Empty : anchor.PayloadId;
    }

    public static int ComputeStableSeed(string text, int actorId)
    {
        text ??= string.Empty;
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return hash * 31 + actorId;
        }
    }
}
