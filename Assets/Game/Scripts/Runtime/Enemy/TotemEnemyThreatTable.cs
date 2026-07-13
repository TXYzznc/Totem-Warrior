using UnityEngine;

public sealed class TotemEnemyThreatTable
{
    public const int DefaultCapacity = 64;
    public const float TargetSwitchMultiplier = 1.25f;
    public const float RecentAttackerDuration = 3f;

    private readonly Entry[] _entries;
    private int _count;

    public TotemEnemyThreatTable(int capacity = DefaultCapacity)
    {
        _entries = new Entry[Mathf.Max(1, capacity)];
    }

    public int Capacity => _entries.Length;

    public int Count => _count;

    public void Clear()
    {
        for (int i = 0; i < _count; i++)
        {
            _entries[i] = default;
        }

        _count = 0;
    }

    public void Remove(int participantId)
    {
        int index = FindIndex(participantId);
        if (index < 0)
        {
            return;
        }

        int last = _count - 1;
        _entries[index] = _entries[last];
        _entries[last] = default;
        _count = last;
    }

    public void AddDamage(TotemActorModel participant, float damage, float worldTime)
    {
        if (participant == null || damage <= 0f)
        {
            return;
        }

        int index = GetOrCreateIndex(participant, worldTime);
        ref Entry entry = ref _entries[index];
        entry.DamageThreat += damage;
        entry.RecentAttackerBonus = Mathf.Max(entry.RecentAttackerBonus, damage * 0.5f + 5f);
        entry.LastAttackTime = worldTime;
        entry.LastTouchedTime = worldTime;
    }

    public void AddAlert(TotemActorModel participant, float amount, float worldTime)
    {
        if (participant == null || amount <= 0f)
        {
            return;
        }

        int index = GetOrCreateIndex(participant, worldTime);
        ref Entry entry = ref _entries[index];
        entry.DamageThreat += amount;
        entry.LastTouchedTime = worldTime;
    }

    public void SetAbilityModifier(TotemActorModel participant, float modifier, float worldTime)
    {
        if (participant == null)
        {
            return;
        }

        int index = GetOrCreateIndex(participant, worldTime);
        ref Entry entry = ref _entries[index];
        entry.AbilityTargetModifier = modifier;
        entry.LastTouchedTime = worldTime;
    }

    public float GetScore(TotemActorModel participant, float proximityThreat, float worldTime)
    {
        if (participant == null)
        {
            return float.MinValue;
        }

        int index = FindIndex(participant.ActorId);
        if (index < 0)
        {
            return Mathf.Max(0f, proximityThreat);
        }

        ref Entry entry = ref _entries[index];
        float recent = worldTime - entry.LastAttackTime <= RecentAttackerDuration
            ? entry.RecentAttackerBonus
            : 0f;
        return entry.DamageThreat + Mathf.Max(0f, proximityThreat) + recent + entry.AbilityTargetModifier;
    }

    public void PruneInvalid(ITotemEnemyParticipantSource participants, float worldTime, float staleSeconds = 30f)
    {
        for (int i = _count - 1; i >= 0; i--)
        {
            TotemActorModel participant = _entries[i].Participant;
            bool invalid = participant == null || participants == null || !participants.CanBeTargeted(participant);
            bool stale = worldTime - _entries[i].LastTouchedTime > staleSeconds && _entries[i].DamageThreat <= 0f;
            if (!invalid && !stale)
            {
                continue;
            }

            int last = _count - 1;
            _entries[i] = _entries[last];
            _entries[last] = default;
            _count = last;
        }
    }

    private int GetOrCreateIndex(TotemActorModel participant, float worldTime)
    {
        int index = FindIndex(participant.ActorId);
        if (index >= 0)
        {
            _entries[index].Participant = participant;
            return index;
        }

        if (_count < _entries.Length)
        {
            index = _count++;
            _entries[index] = new Entry
            {
                Participant = participant,
                ParticipantId = participant.ActorId,
                LastAttackTime = float.MinValue,
                LastTouchedTime = worldTime,
            };
            return index;
        }

        index = FindEvictionIndex(worldTime);
        _entries[index] = new Entry
        {
            Participant = participant,
            ParticipantId = participant.ActorId,
            LastAttackTime = float.MinValue,
            LastTouchedTime = worldTime,
        };
        return index;
    }

    private int FindIndex(int participantId)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].ParticipantId == participantId)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindEvictionIndex(float worldTime)
    {
        int bestIndex = 0;
        float bestScore = float.MaxValue;
        for (int i = 0; i < _entries.Length; i++)
        {
            ref Entry entry = ref _entries[i];
            float recent = worldTime - entry.LastAttackTime <= RecentAttackerDuration
                ? entry.RecentAttackerBonus
                : 0f;
            float score = entry.DamageThreat + recent + entry.AbilityTargetModifier;
            if (score < bestScore || (Mathf.Approximately(score, bestScore) && entry.ParticipantId > _entries[bestIndex].ParticipantId))
            {
                bestIndex = i;
                bestScore = score;
            }
        }

        return bestIndex;
    }

    private struct Entry
    {
        public int ParticipantId;
        public TotemActorModel Participant;
        public float DamageThreat;
        public float RecentAttackerBonus;
        public float AbilityTargetModifier;
        public float LastAttackTime;
        public float LastTouchedTime;
    }
}
