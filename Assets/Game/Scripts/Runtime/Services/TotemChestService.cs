using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemChestService : TotemRuntimeServiceBase
{
    public const float ChestInteractRadius = 1.8f;
    public const float PotionHealPerStack = 25f;

    private readonly List<TotemChestModel> activeChests = new List<TotemChestModel>(8);
    private TotemGameFlowService flowService;
    private TotemAssetService assetService;
    private TotemMapService mapService;
    private TotemWeaponService weaponService;
    private TotemEconomyService economyService;
    private TotemChestRewardDefinition[] rewardCatalog = Array.Empty<TotemChestRewardDefinition>();
    private GameObject chestRoot;
    private int nextInstanceId = 1;
    private string lastOpenedChestId = string.Empty;
    private string lastRewardType = string.Empty;

    public override string ServiceName => "Chest";

    public IReadOnlyList<TotemChestModel> ActiveChests => activeChests;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        assetService = runtime.GetService<TotemAssetService>();
        mapService = runtime.GetService<TotemMapService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        economyService = runtime.GetService<TotemEconomyService>();
        var catalog = runtime.GetService<TotemDataService>()?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
        rewardCatalog = catalog.CreateChestRewardDefinitions();
        if (rewardCatalog.Length <= 0)
        {
            rewardCatalog = TotemDataService.LoadGameplayCatalogOrDefault().CreateChestRewardDefinitions();
        }

        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        DestroyChests();
        assetService = null;
        mapService = null;
        weaponService = null;
        economyService = null;
        rewardCatalog = Array.Empty<TotemChestRewardDefinition>();
        nextInstanceId = 1;
        lastOpenedChestId = string.Empty;
        lastRewardType = string.Empty;
    }

    public IReadOnlyList<TotemChestRewardDefinition> GetRuntimeRewardCatalog()
    {
        return rewardCatalog;
    }

    public TotemChestSnapshot CaptureSnapshot()
    {
        int opened = 0;
        int common = 0;
        int rare = 0;
        for (int i = 0; i < activeChests.Count; i++)
        {
            var chest = activeChests[i];
            if (chest == null)
            {
                continue;
            }

            if (chest.Opened)
            {
                opened++;
            }

            if (string.Equals(chest.ChestId, "chest_common", StringComparison.Ordinal))
            {
                common++;
            }
            else if (string.Equals(chest.ChestId, "chest_rare", StringComparison.Ordinal))
            {
                rare++;
            }
        }

        return new TotemChestSnapshot
        {
            activeChestCount = activeChests.Count,
            openedChestCount = opened,
            commonChestCount = common,
            rareChestCount = rare,
            lastOpenedChestId = lastOpenedChestId,
            lastRewardType = lastRewardType,
        };
    }

    public void SpawnChests(TotemMapSnapshot map, bool createObjects)
    {
        DestroyChests();
        if (map == null)
        {
            map = TotemMapService.BuildLayout(1, 1);
        }

        if (createObjects)
        {
            chestRoot = new GameObject("[TotemChests]");
        }

        var chestAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Chest);
        if (chestAnchors.Length > 0)
        {
            for (int i = 0; i < chestAnchors.Length; i++)
            {
                var anchor = chestAnchors[i];
                SpawnChest(string.IsNullOrWhiteSpace(anchor.PayloadId) ? "chest_common" : anchor.PayloadId, anchor.Position, createObjects);
            }
        }
        else
        {
            SpawnChest("chest_common", FindRoomCenter(map, TotemRoomType.SpawnRoom, new Vector3(82f, 0f, 82f)) + new Vector3(6f, 0.5f, 5f), createObjects);
            SpawnChest("chest_common", FindRoomCenter(map, TotemRoomType.TattooStudio, new Vector3(94f, 0f, 314f)) + new Vector3(-5f, 0.5f, -5f), createObjects);
            SpawnChest("chest_rare", FindRoomCenter(map, TotemRoomType.Merchant, new Vector3(308f, 0f, 300f)) + new Vector3(5f, 0.5f, -5f), createObjects);
            SpawnChest("chest_rare", FindRoomCenter(map, TotemRoomType.BossRoom, new Vector3(324f, 0f, 82f)) + new Vector3(-6f, 0.5f, 6f), createObjects);
        }

        GFTrace.Success("TotemChest", "Chests.Spawned", null, GFTrace.Data(
            "count", activeChests.Count.ToString(),
            "common", CaptureSnapshot().commonChestCount.ToString(),
            "rare", CaptureSnapshot().rareChestCount.ToString()));
    }

    public TotemChestModel SpawnChest(string chestId, Vector3 position, bool createObject)
    {
        var chest = new TotemChestModel
        {
            InstanceId = nextInstanceId++,
            ChestId = string.IsNullOrWhiteSpace(chestId) ? "chest_common" : chestId,
            Position = position,
            Opened = false,
        };

        if (createObject)
        {
            chest.GameObject = CreateChestObject(chest);
        }

        activeChests.Add(chest);
        return chest;
    }

    public TotemChestModel FindNearestClosedChest(Vector3 position, float radius)
    {
        float maxSqr = radius * radius;
        float bestSqr = float.MaxValue;
        TotemChestModel best = null;
        for (int i = 0; i < activeChests.Count; i++)
        {
            var chest = activeChests[i];
            if (chest == null || chest.Opened)
            {
                continue;
            }

            float sqr = (chest.Position - position).sqrMagnitude;
            if (sqr > maxSqr || sqr >= bestSqr)
            {
                continue;
            }

            bestSqr = sqr;
            best = chest;
        }

        return best;
    }

    public bool TryOpenNearestChest(TotemActorModel opener, float radius, int seed, out TotemChestOpenResult result)
    {
        result = null;
        if (opener == null)
        {
            result = BuildOpenResult(false, "NoOpener", null, null);
            return false;
        }

        var chest = FindNearestClosedChest(opener.Position, radius);
        if (chest == null)
        {
            result = BuildOpenResult(false, "NoChestInRange", null, null);
            return false;
        }

        return TryOpenChest(opener, chest, seed, out result);
    }

    public bool TryOpenChest(TotemActorModel opener, TotemChestModel chest, int seed, out TotemChestOpenResult result)
    {
        result = null;
        if (opener == null || chest == null || !activeChests.Contains(chest))
        {
            result = BuildOpenResult(false, "InvalidChest", chest, null);
            return false;
        }

        if (chest.Opened)
        {
            result = BuildOpenResult(false, "AlreadyOpened", chest, null);
            return false;
        }

        if (!TrySelectReward(chest.ChestId, seed, out var reward))
        {
            result = BuildOpenResult(false, "NoReward", chest, null);
            return false;
        }

        chest.Opened = true;
        ApplyOpenedVisual(chest);
        result = ApplyReward(opener, chest, reward, seed);
        lastOpenedChestId = chest.ChestId;
        lastRewardType = reward.RewardType.ToString();
        GFTrace.Success("TotemChest", "Chest.Opened", null, GFTrace.Data(
            "opener", opener.Name,
            "chestId", chest.ChestId,
            "instanceId", chest.InstanceId.ToString(),
            "rewardType", reward.RewardType.ToString(),
            "rewardId", reward.RewardId ?? string.Empty,
            "amount", reward.RewardAmount.ToString()));
        return result.opened;
    }

    public bool TrySelectReward(string chestId, int seed, out TotemChestRewardDefinition reward)
    {
        reward = null;
        int totalProbability = 0;
        for (int i = 0; i < rewardCatalog.Length; i++)
        {
            var candidate = rewardCatalog[i];
            if (!IsRewardCandidate(candidate, chestId))
            {
                continue;
            }

            totalProbability += Mathf.Max(0, candidate.Probability);
        }

        if (totalProbability <= 0)
        {
            return false;
        }

        int roll = (int)(Math.Abs((long)seed) % totalProbability);
        int cursor = 0;
        for (int i = 0; i < rewardCatalog.Length; i++)
        {
            var candidate = rewardCatalog[i];
            if (!IsRewardCandidate(candidate, chestId))
            {
                continue;
            }

            cursor += Mathf.Max(0, candidate.Probability);
            if (roll < cursor)
            {
                reward = candidate;
                return true;
            }
        }

        return false;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            SpawnChests(mapService?.CurrentMap ?? TotemMapService.BuildLayout(1, 1), createObjects: true);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            DestroyChests();
            nextInstanceId = 1;
            lastOpenedChestId = string.Empty;
            lastRewardType = string.Empty;
            GFTrace.Info("TotemChest", "Chests.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private TotemChestOpenResult ApplyReward(TotemActorModel opener, TotemChestModel chest, TotemChestRewardDefinition reward, int seed)
    {
        var result = BuildOpenResult(true, "Opened", chest, reward);
        switch (reward.RewardType)
        {
            case TotemChestRewardType.Weapon:
                ApplyWeaponReward(chest, reward, seed, result);
                break;
            case TotemChestRewardType.Gold:
                int coins = Mathf.Max(0, reward.RewardAmount);
                economyService?.AddCoins(opener, coins);
                result.coinsAdded = coins;
                break;
            case TotemChestRewardType.Potion:
                int stacks = Mathf.Max(1, reward.RewardAmount);
                result.healAmount = opener.Heal(stacks * PotionHealPerStack);
                break;
        }

        return result;
    }

    private void ApplyWeaponReward(TotemChestModel chest, TotemChestRewardDefinition reward, int seed, TotemChestOpenResult result)
    {
        if (weaponService == null)
        {
            result.reason = "WeaponServiceMissing";
            return;
        }

        Vector3 spawnPosition = chest.Position + Vector3.up * 0.5f;
        TotemWeaponPickupModel pickup = null;
        if (string.IsNullOrWhiteSpace(reward.RewardId))
        {
            int roomIndex = ResolveRoomIndex(chest.Position);
            weaponService.SpawnWeightedWeaponPickup("Chest", roomIndex, spawnPosition, seed * 37 + chest.InstanceId, out pickup);
        }
        else
        {
            pickup = weaponService.SpawnWeaponPickup(reward.RewardId, "Chest", spawnPosition);
        }

        if (pickup == null)
        {
            result.reason = "WeaponDropFailed";
            return;
        }

        result.spawnedWeaponPickupId = pickup.InstanceId;
        result.rewardId = pickup.WeaponId;
    }

    private int ResolveRoomIndex(Vector3 position)
    {
        var rooms = mapService?.CurrentMap?.Rooms;
        if (rooms == null || rooms.Length <= 0)
        {
            return 1;
        }

        int nearestRoomId = rooms[0].RoomId;
        float bestSqr = float.MaxValue;
        var point = new Vector2(position.x, position.z);
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room == null)
            {
                continue;
            }

            if (room.Bounds.Contains(point))
            {
                return Mathf.Max(1, room.RoomId + 1);
            }

            Vector3 delta = room.CenterWorld - position;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearestRoomId = room.RoomId;
            }
        }

        return Mathf.Max(1, nearestRoomId + 1);
    }

    private GameObject CreateChestObject(TotemChestModel chest)
    {
        string assetKey = $"chest.{chest.ChestId}";
        if (assetService != null && assetService.TryLoadSprite(assetKey, out var sprite) && sprite != null)
        {
            var spriteGo = new GameObject($"TotemChest_{chest.InstanceId}_{chest.ChestId}");
            spriteGo.transform.SetParent(chestRoot == null ? null : chestRoot.transform, false);
            spriteGo.transform.position = chest.Position;
            spriteGo.transform.localScale = Vector3.one * 1.1f;
            var renderer = spriteGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = GetChestColor(chest.ChestId);
            renderer.sortingOrder = 2;
            return spriteGo;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(chestRoot == null ? null : chestRoot.transform, false);
        go.transform.position = chest.Position;
        go.transform.localScale = new Vector3(1.1f, 0.8f, 0.8f);
        SetColor(go, GetChestColor(chest.ChestId));
        go.name = $"TotemChest_{chest.InstanceId}_{chest.ChestId}";
        return go;
    }

    private void ApplyOpenedVisual(TotemChestModel chest)
    {
        if (chest?.GameObject == null)
        {
            return;
        }

        SetColor(chest.GameObject, new Color(0.35f, 0.35f, 0.35f));
        chest.GameObject.transform.localScale = new Vector3(1.1f, 0.35f, 0.8f);
    }

    private static Color GetChestColor(string chestId)
    {
        return string.Equals(chestId, "chest_rare", StringComparison.Ordinal)
            ? new Color(0.95f, 0.65f, 0.15f)
            : new Color(0.55f, 0.32f, 0.18f);
    }

    private void DestroyChests()
    {
        for (int i = activeChests.Count - 1; i >= 0; i--)
        {
            var chest = activeChests[i];
            if (chest != null)
            {
                DestroyObject(chest.GameObject);
                chest.GameObject = null;
            }
        }

        activeChests.Clear();
        DestroyObject(chestRoot);
        chestRoot = null;
    }

    private static bool IsRewardCandidate(TotemChestRewardDefinition reward, string chestId)
    {
        return reward != null
            && reward.Probability > 0
            && reward.RewardType != TotemChestRewardType.Unknown
            && string.Equals(reward.ChestId, chestId, StringComparison.Ordinal);
    }

    private static TotemChestOpenResult BuildOpenResult(bool opened, string reason, TotemChestModel chest, TotemChestRewardDefinition reward)
    {
        return new TotemChestOpenResult
        {
            opened = opened,
            reason = reason,
            chestInstanceId = chest?.InstanceId ?? 0,
            chestId = chest?.ChestId ?? string.Empty,
            rewardType = reward?.RewardType ?? TotemChestRewardType.Unknown,
            rewardId = reward?.RewardId ?? string.Empty,
            rewardAmount = reward?.RewardAmount ?? 0,
        };
    }

    private static Vector3 FindRoomCenter(TotemMapSnapshot map, TotemRoomType roomType, Vector3 fallback)
    {
        var rooms = map?.Rooms;
        if (rooms == null)
        {
            return fallback;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null && rooms[i].RoomType == roomType)
            {
                return rooms[i].CenterWorld;
            }
        }

        return fallback;
    }

    private static void SetColor(GameObject go, Color color)
    {
        var renderer = go == null ? null : go.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        renderer.material = material;
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
