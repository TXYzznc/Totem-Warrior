using System;
using UnityEngine;

internal sealed class TotemEnemyStatusRuntime
{
    public const int DefaultCapacity = 8;

    private readonly Entry[] _entries;
    private int _count;

    public TotemEnemyStatusRuntime(int capacity = DefaultCapacity)
    {
        _entries = new Entry[Mathf.Max(1, capacity)];
    }

    public int Count => _count;

    public bool TryApply(
        in TotemEnemyStatusDefinition definition,
        TotemCombatantModel source,
        string sourceReason,
        float worldTime,
        out TotemEnemyStatusApplyResult result)
    {
        if (!definition.IsValid)
        {
            result = TotemEnemyStatusApplyResult.InvalidDefinition;
            return false;
        }

        int sourceId = source?.CombatantId ?? 0;
        int existingIndex = FindIndex(definition.StatusId, sourceId);
        if (existingIndex >= 0)
        {
            ref Entry existing = ref _entries[existingIndex];
            if (existing.ExpiresAt <= worldTime)
            {
                existing = new Entry
                {
                    Definition = definition,
                    Source = source,
                    SourceId = sourceId,
                    ExpiresAt = worldTime + definition.Duration,
                    NextTickAt = worldTime + definition.TickInterval,
                    DamageReason = BuildDamageReason(definition.StatusId, sourceReason),
                };
                result = TotemEnemyStatusApplyResult.Applied;
                return true;
            }

            existing.Definition = Merge(existing.Definition, definition);
            existing.Source = source ?? existing.Source;
            existing.ExpiresAt = Mathf.Max(existing.ExpiresAt, worldTime + definition.Duration);
            existing.NextTickAt = Mathf.Min(existing.NextTickAt, worldTime + existing.Definition.TickInterval);
            if (!string.IsNullOrWhiteSpace(sourceReason))
            {
                existing.DamageReason = BuildDamageReason(definition.StatusId, sourceReason);
            }

            result = TotemEnemyStatusApplyResult.Refreshed;
            return true;
        }

        if (_count >= _entries.Length)
        {
            result = TotemEnemyStatusApplyResult.CapacityReached;
            return false;
        }

        _entries[_count++] = new Entry
        {
            Definition = definition,
            Source = source,
            SourceId = sourceId,
            ExpiresAt = worldTime + definition.Duration,
            NextTickAt = worldTime + definition.TickInterval,
            DamageReason = BuildDamageReason(definition.StatusId, sourceReason),
        };
        result = TotemEnemyStatusApplyResult.Applied;
        return true;
    }

    public void Tick(int enemyCombatantId, float worldTime, ITotemEnemyStatusTickSink sink)
    {
        for (int i = _count - 1; i >= 0; i--)
        {
            ref Entry entry = ref _entries[i];
            if (entry.Definition.Kind == TotemEnemyStatusKind.DamageOverTime && entry.Definition.Power > 0f)
            {
                float lastTickAt = Mathf.Min(worldTime, entry.ExpiresAt);
                while (entry.NextTickAt <= lastTickAt + 0.0001f)
                {
                    var tick = new TotemEnemyStatusTick(
                        entry.Source,
                        entry.Definition.Power * entry.Definition.TickInterval,
                        entry.DamageReason,
                        entry.NextTickAt,
                        entry.Definition.CanHitEnemies,
                        entry.Definition.WorldDamageAffectsEnemies);
                    entry.NextTickAt += entry.Definition.TickInterval;
                    if (sink != null && !sink.ApplyStatusTick(enemyCombatantId, tick))
                    {
                        Clear();
                        return;
                    }
                }
            }

            if (worldTime + 0.0001f >= entry.ExpiresAt)
            {
                RemoveAt(i);
            }
        }
    }

    public bool HasStatus(string statusId, float worldTime)
    {
        if (string.IsNullOrWhiteSpace(statusId))
        {
            return false;
        }

        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].ExpiresAt > worldTime
                && string.Equals(_entries[i].Definition.StatusId, statusId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsStunned(float worldTime)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].ExpiresAt > worldTime
                && _entries[i].Definition.Kind == TotemEnemyStatusKind.Stun)
            {
                return true;
            }
        }

        return false;
    }

    public float GetMoveSpeedMultiplier(float worldTime)
    {
        float multiplier = 1f;
        for (int i = 0; i < _count; i++)
        {
            ref Entry entry = ref _entries[i];
            if (entry.ExpiresAt <= worldTime)
            {
                continue;
            }

            if (entry.Definition.Kind == TotemEnemyStatusKind.Stun)
            {
                return 0f;
            }

            if (entry.Definition.Kind == TotemEnemyStatusKind.Slow)
            {
                multiplier = Mathf.Min(multiplier, entry.Definition.MoveSpeedMultiplier);
            }
        }

        return multiplier;
    }

    public bool TryGetRemaining(string statusId, float worldTime, out float remaining)
    {
        remaining = 0f;
        for (int i = 0; i < _count; i++)
        {
            ref Entry entry = ref _entries[i];
            if (!string.Equals(entry.Definition.StatusId, statusId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            remaining = Mathf.Max(remaining, entry.ExpiresAt - worldTime);
        }

        return remaining > 0f;
    }

    public void Clear()
    {
        for (int i = 0; i < _count; i++)
        {
            _entries[i] = default;
        }

        _count = 0;
    }

    private int FindIndex(string statusId, int sourceId)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].SourceId == sourceId
                && string.Equals(_entries[i].Definition.StatusId, statusId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveAt(int index)
    {
        int last = --_count;
        _entries[index] = _entries[last];
        _entries[last] = default;
    }

    private static TotemEnemyStatusDefinition Merge(
        in TotemEnemyStatusDefinition current,
        in TotemEnemyStatusDefinition incoming)
    {
        return new TotemEnemyStatusDefinition(
            current.StatusId,
            current.Kind,
            Mathf.Max(current.Duration, incoming.Duration),
            Mathf.Max(current.Power, incoming.Power),
            Mathf.Min(current.TickInterval, incoming.TickInterval),
            Mathf.Min(current.MoveSpeedMultiplier, incoming.MoveSpeedMultiplier),
            current.CanHitEnemies || incoming.CanHitEnemies,
            current.WorldDamageAffectsEnemies || incoming.WorldDamageAffectsEnemies);
    }

    private static string BuildDamageReason(string statusId, string sourceReason)
    {
        return string.IsNullOrWhiteSpace(sourceReason)
            ? "Status:" + statusId
            : "Status:" + statusId + ":" + sourceReason;
    }

    private struct Entry
    {
        public TotemEnemyStatusDefinition Definition;
        public TotemCombatantModel Source;
        public int SourceId;
        public float ExpiresAt;
        public float NextTickAt;
        public string DamageReason;
    }
}
