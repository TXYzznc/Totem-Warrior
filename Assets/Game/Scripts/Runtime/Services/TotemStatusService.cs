using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemStatusService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float TickInterval = 0.5f;
    public const string BurnStatus = "Burn";
    public const string PoisonStatus = "Poison";
    public const string ShockStatus = "Shock";
    public const string StunStatus = "Stun";
    public const string SlowStatus = "Slow";

    private readonly Dictionary<int, List<TotemStatusInstance>> active = new Dictionary<int, List<TotemStatusInstance>>(64);
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private int appliedCount;
    private int expiredCount;
    private int tickDamageCount;
    private string lastStatusName = string.Empty;
    private string lastExpiredStatusName = string.Empty;

    public override string ServiceName => "Status";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
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

        actorService = null;
        ResetRunState();
    }

    private void ResetRunState()
    {
        active.Clear();
        appliedCount = 0;
        expiredCount = 0;
        tickDamageCount = 0;
        lastStatusName = string.Empty;
        lastExpiredStatusName = string.Empty;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || active.Count <= 0)
        {
            return;
        }

        foreach (var pair in active)
        {
            TickList(pair.Value, deltaTime);
        }
    }

    public void ApplyStatus(TotemActorModel target, string statusName, float dps, float duration, TotemCombatantModel source = null, string sourceReason = null)
    {
        if (target == null || string.IsNullOrWhiteSpace(statusName) || duration <= 0f)
        {
            return;
        }

        var list = GetOrCreateList(target);
        for (int i = 0; i < list.Count; i++)
        {
            var status = list[i];
            if (!string.Equals(status.StatusName, statusName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            status.DPS = Mathf.Max(status.DPS, dps);
            status.RemainingSec = Mathf.Max(status.RemainingSec, duration);
            if (source != null)
            {
                status.Source = source;
            }

            if (!string.IsNullOrWhiteSpace(sourceReason))
            {
                status.SourceReason = sourceReason;
            }

            appliedCount++;
            lastStatusName = status.StatusName;
            GFTrace.Info("TotemStatus", "Refresh", null, GFTrace.Data(
                "actorId", target.ActorId.ToString(),
                "status", status.StatusName,
                "remaining", status.RemainingSec.ToString("F1"),
                "dps", status.DPS.ToString("F1")));
            return;
        }

        list.Add(new TotemStatusInstance
        {
            Target = target,
            Source = source,
            StatusName = statusName,
            SourceReason = sourceReason,
            DPS = Mathf.Max(0f, dps),
            RemainingSec = duration,
            TickAccumulator = 0f,
        });
        appliedCount++;
        lastStatusName = statusName;
        GFTrace.Info("TotemStatus", "Apply", null, GFTrace.Data(
            "actorId", target.ActorId.ToString(),
            "status", statusName,
            "duration", duration.ToString("F1"),
            "dps", Mathf.Max(0f, dps).ToString("F1")));
    }

    public IReadOnlyList<TotemStatusInstance> GetActiveStatuses(TotemActorModel target)
    {
        if (target == null || !active.TryGetValue(target.ActorId, out var list))
        {
            return Array.Empty<TotemStatusInstance>();
        }

        return list;
    }

    public bool HasStatus(TotemActorModel target, string statusName)
    {
        if (target == null || string.IsNullOrWhiteSpace(statusName) || !active.TryGetValue(target.ActorId, out var list))
        {
            return false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var status = list[i];
            if (status.RemainingSec > 0f && string.Equals(status.StatusName, statusName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsStunned(TotemActorModel target)
    {
        return HasStatus(target, StunStatus);
    }

    public bool CanAct(TotemActorModel target)
    {
        return target != null && target.IsAlive && !IsStunned(target);
    }

    public bool CanMove(TotemActorModel target)
    {
        return CanAct(target) && GetMoveSpeedMultiplier(target) > 0f;
    }

    public float GetMoveSpeedMultiplier(TotemActorModel target)
    {
        if (target == null || !target.IsAlive)
        {
            return 0f;
        }

        if (!active.TryGetValue(target.ActorId, out var list) || list.Count <= 0)
        {
            return 1f;
        }

        float multiplier = 1f;
        for (int i = 0; i < list.Count; i++)
        {
            var status = list[i];
            if (status.RemainingSec <= 0f)
            {
                continue;
            }

            if (string.Equals(status.StatusName, StunStatus, StringComparison.OrdinalIgnoreCase))
            {
                return 0f;
            }

            if (string.Equals(status.StatusName, SlowStatus, StringComparison.OrdinalIgnoreCase))
            {
                float slowAmount = status.DPS > 0f ? status.DPS : 0.3f;
                multiplier = Mathf.Min(multiplier, Mathf.Clamp(1f - slowAmount, 0.15f, 1f));
            }
        }

        return multiplier;
    }

    public void ClearAllStatuses(TotemActorModel target)
    {
        if (target == null)
        {
            return;
        }

        if (active.TryGetValue(target.ActorId, out var list) && list.Count > 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                RecordExpired(list[i]?.StatusName);
            }
        }

        active.Remove(target.ActorId);
    }

    public TotemStatusSnapshot CaptureSnapshot(TotemActorModel target)
    {
        var snapshot = new TotemStatusSnapshot
        {
            actorId = target?.ActorId ?? 0,
            appliedCount = appliedCount,
            expiredCount = expiredCount,
            tickDamageCount = tickDamageCount,
            lastStatusName = lastStatusName,
            lastExpiredStatusName = lastExpiredStatusName,
        };

        if (target == null || !active.TryGetValue(target.ActorId, out var list) || list.Count <= 0)
        {
            snapshot.statusNames = Array.Empty<string>();
            snapshot.remainingSeconds = Array.Empty<float>();
            snapshot.summary = FormatStatusSummary(snapshot);
            return snapshot;
        }

        snapshot.activeCount = list.Count;
        snapshot.statusNames = new string[list.Count];
        snapshot.remainingSeconds = new float[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var status = list[i];
            snapshot.statusNames[i] = status.StatusName ?? string.Empty;
            snapshot.remainingSeconds[i] = Mathf.Max(0f, status.RemainingSec);
            snapshot.totalDps += Mathf.Max(0f, status.DPS);
        }

        snapshot.summary = FormatStatusSummary(snapshot);
        return snapshot;
    }

    public static string FormatStatusSummary(TotemStatusSnapshot snapshot)
    {
        if (snapshot == null || snapshot.activeCount <= 0 || snapshot.statusNames == null || snapshot.statusNames.Length <= 0)
        {
            return "Status: None";
        }

        var parts = new string[snapshot.statusNames.Length];
        for (int i = 0; i < snapshot.statusNames.Length; i++)
        {
            float remaining = snapshot.remainingSeconds != null && snapshot.remainingSeconds.Length > i
                ? Mathf.Max(0f, snapshot.remainingSeconds[i])
                : 0f;
            parts[i] = $"{snapshot.statusNames[i]} {remaining:F1}s";
        }

        return "Status: " + string.Join(", ", parts);
    }

    private List<TotemStatusInstance> GetOrCreateList(TotemActorModel target)
    {
        if (!active.TryGetValue(target.ActorId, out var list))
        {
            list = new List<TotemStatusInstance>(4);
            active[target.ActorId] = list;
        }

        return list;
    }

    private void TickList(List<TotemStatusInstance> list, float deltaTime)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var status = list[i];
            if (status.Target == null || !status.Target.IsAlive)
            {
                list.RemoveAt(i);
                continue;
            }

            status.RemainingSec -= deltaTime;
            if (status.RemainingSec <= 0f)
            {
                RecordExpired(status.StatusName);
                list.RemoveAt(i);
                continue;
            }

            status.TickAccumulator += deltaTime;
            while (status.TickAccumulator >= TickInterval)
            {
                status.TickAccumulator -= TickInterval;
                float tickDamage = ComputeTickDamage(status.StatusName, status.DPS);
                if (tickDamage <= 0f)
                {
                    continue;
                }

                string reason = string.IsNullOrWhiteSpace(status.SourceReason) ? $"Status:{status.StatusName}" : $"Status:{status.StatusName}:{status.SourceReason}";
                tickDamageCount++;
                bool killed = ApplyDamage(status.Target, tickDamage, status.Source, reason);
                if (killed)
                {
                    RecordExpired(status.StatusName);
                    list.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private bool ApplyDamage(TotemActorModel target, float damage, TotemCombatantModel source, string reason)
    {
        if (actorService != null)
        {
            return actorService.ApplyDamage(target, damage, source, reason);
        }

        if (target == null || damage <= 0f || !target.IsAlive)
        {
            return false;
        }

        target.ApplyDamage(damage);
        if (!target.IsAlive && target.GameObject != null)
        {
            target.GameObject.SetActive(false);
        }

        return !target.IsAlive;
    }

    public static float ComputeTickDamage(float dps)
    {
        return Mathf.Max(0f, dps) * TickInterval;
    }

    public static float ComputeTickDamage(string statusName, float dps)
    {
        return IsDamageStatus(statusName) ? ComputeTickDamage(dps) : 0f;
    }

    public static bool IsDamageStatus(string statusName)
    {
        return !string.Equals(statusName, StunStatus, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(statusName, SlowStatus, StringComparison.OrdinalIgnoreCase);
    }

    private void RecordExpired(string statusName)
    {
        expiredCount++;
        lastExpiredStatusName = statusName ?? string.Empty;
        GFTrace.Info("TotemStatus", "Expire", null, GFTrace.Data("status", lastExpiredStatusName));
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            GFTrace.Info("TotemStatus", "RunState.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }
}
