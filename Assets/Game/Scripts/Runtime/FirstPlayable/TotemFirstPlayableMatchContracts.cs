using System;
using UnityEngine;

public static class TotemFirstPlayableRules
{
    public const string ConfigVersion = "six-player-pure-pvp-first-playable-v1";
    public const int ParticipantCount = 6;
    public const int TeamCount = 3;
    public const int TeamSize = 2;
    public const int HumanCount = 1;
    public const int BotCount = 5;
    public const int OpeningBuildSeconds = 60;
    public const int LaterBuildSeconds = 45;
    public const int NormalCombatSeconds = 180;
    public const int NormalShrinkSeconds = 30;
    public const int FastCombatSeconds = 60;
    public const int FastShrinkSeconds = 10;
}

public readonly struct TotemParticipantId : IEquatable<TotemParticipantId>
{
    public TotemParticipantId(int value)
    {
        Value = value;
    }

    public int Value { get; }
    public bool IsValid => Value > 0;

    public bool Equals(TotemParticipantId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is TotemParticipantId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => IsValid ? $"P{Value}" : "P.Invalid";
    public static bool operator ==(TotemParticipantId left, TotemParticipantId right) => left.Equals(right);
    public static bool operator !=(TotemParticipantId left, TotemParticipantId right) => !left.Equals(right);
}

public readonly struct TotemTeamId : IEquatable<TotemTeamId>
{
    public TotemTeamId(int value)
    {
        Value = value;
    }

    public int Value { get; }
    public bool IsValid => Value >= 0 && Value < TotemFirstPlayableRules.TeamCount;

    public bool Equals(TotemTeamId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is TotemTeamId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => IsValid ? $"T{Value + 1}" : "T.Invalid";
    public static bool operator ==(TotemTeamId left, TotemTeamId right) => left.Equals(right);
    public static bool operator !=(TotemTeamId left, TotemTeamId right) => !left.Equals(right);
}

public enum TotemFirstPlayableParticipantKind : byte
{
    Human = 0,
    Bot = 1,
}

public enum TotemFirstPlayableLifeState : byte
{
    Reserved = 0,
    Alive = 1,
    Downed = 2,
    Eliminated = 3,
    Disconnected = 4,
}

public readonly struct TotemRosterSlot
{
    public TotemRosterSlot(
        TotemParticipantId participantId,
        TotemTeamId teamId,
        TotemFirstPlayableParticipantKind participantKind,
        TotemFirstPlayableLifeState lifeState)
    {
        ParticipantId = participantId;
        TeamId = teamId;
        ParticipantKind = participantKind;
        LifeState = lifeState;
    }

    public TotemParticipantId ParticipantId { get; }
    public TotemTeamId TeamId { get; }
    public TotemFirstPlayableParticipantKind ParticipantKind { get; }
    public TotemFirstPlayableLifeState LifeState { get; }
    public bool CountsAsAlive => LifeState == TotemFirstPlayableLifeState.Alive || LifeState == TotemFirstPlayableLifeState.Downed;
}

public static class TotemRosterContract
{
    public static bool Validate(TotemRosterSlot[] slots, out string error)
    {
        if (slots == null || slots.Length != TotemFirstPlayableRules.ParticipantCount)
        {
            error = $"Roster must contain exactly {TotemFirstPlayableRules.ParticipantCount} participants.";
            return false;
        }

        int humans = 0;
        var teamSizes = new int[TotemFirstPlayableRules.TeamCount];
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].ParticipantId.IsValid || !slots[i].TeamId.IsValid)
            {
                error = $"Roster slot {i} has an invalid participant or team ID.";
                return false;
            }

            for (int j = 0; j < i; j++)
            {
                if (slots[j].ParticipantId == slots[i].ParticipantId)
                {
                    error = $"Duplicate participant ID {slots[i].ParticipantId}.";
                    return false;
                }
            }

            teamSizes[slots[i].TeamId.Value]++;
            if (slots[i].ParticipantKind == TotemFirstPlayableParticipantKind.Human)
            {
                humans++;
            }
        }

        if (humans != TotemFirstPlayableRules.HumanCount)
        {
            error = $"Roster must contain exactly {TotemFirstPlayableRules.HumanCount} human.";
            return false;
        }

        for (int i = 0; i < teamSizes.Length; i++)
        {
            if (teamSizes[i] != TotemFirstPlayableRules.TeamSize)
            {
                error = $"Team {i} must contain exactly {TotemFirstPlayableRules.TeamSize} participants.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}

public enum TotemMatchPhase : byte
{
    FrontEnd = 0,
    OpeningBuild = 1,
    Round1Combat = 2,
    Build2 = 3,
    Round2Combat = 4,
    Build3 = 5,
    Round3Combat = 6,
    Build4 = 7,
    Round4Combat = 8,
    Build5 = 9,
    Round5Combat = 10,
    Result = 11,
}

public static class TotemMatchPhaseContract
{
    public static bool IsBuild(TotemMatchPhase phase)
    {
        return phase == TotemMatchPhase.OpeningBuild
            || phase == TotemMatchPhase.Build2
            || phase == TotemMatchPhase.Build3
            || phase == TotemMatchPhase.Build4
            || phase == TotemMatchPhase.Build5;
    }

    public static bool IsCombat(TotemMatchPhase phase)
    {
        return phase == TotemMatchPhase.Round1Combat
            || phase == TotemMatchPhase.Round2Combat
            || phase == TotemMatchPhase.Round3Combat
            || phase == TotemMatchPhase.Round4Combat
            || phase == TotemMatchPhase.Round5Combat;
    }

    public static bool IsGameplaySuspended(TotemMatchPhase phase) => IsBuild(phase);

    public static bool CanTransition(TotemMatchPhase current, TotemMatchPhase next)
    {
        switch (current)
        {
            case TotemMatchPhase.FrontEnd: return next == TotemMatchPhase.OpeningBuild;
            case TotemMatchPhase.OpeningBuild: return next == TotemMatchPhase.Round1Combat;
            case TotemMatchPhase.Round1Combat: return next == TotemMatchPhase.Build2;
            case TotemMatchPhase.Build2: return next == TotemMatchPhase.Round2Combat;
            case TotemMatchPhase.Round2Combat: return next == TotemMatchPhase.Build3;
            case TotemMatchPhase.Build3: return next == TotemMatchPhase.Round3Combat;
            case TotemMatchPhase.Round3Combat: return next == TotemMatchPhase.Build4;
            case TotemMatchPhase.Build4: return next == TotemMatchPhase.Round4Combat;
            case TotemMatchPhase.Round4Combat: return next == TotemMatchPhase.Build5;
            case TotemMatchPhase.Build5: return next == TotemMatchPhase.Round5Combat;
            case TotemMatchPhase.Round5Combat: return next == TotemMatchPhase.Result;
            case TotemMatchPhase.Result: return next == TotemMatchPhase.FrontEnd;
            default: return false;
        }
    }
}

public enum TotemMatchActivity : byte
{
    FrontEnd = 0,
    Build = 1,
    ZoneShrink = 2,
    Combat = 3,
    Result = 4,
}

[Serializable]
public sealed class TotemMatchTimingConfig
{
    public int openingBuildSeconds = TotemFirstPlayableRules.OpeningBuildSeconds;
    public int laterBuildSeconds = TotemFirstPlayableRules.LaterBuildSeconds;
    public int normalCombatSeconds = TotemFirstPlayableRules.NormalCombatSeconds;
    public int normalShrinkSeconds = TotemFirstPlayableRules.NormalShrinkSeconds;
    public int fastCombatSeconds = TotemFirstPlayableRules.FastCombatSeconds;
    public int fastShrinkSeconds = TotemFirstPlayableRules.FastShrinkSeconds;

    public int ResolveBuildSeconds(TotemMatchPhase phase)
    {
        return phase == TotemMatchPhase.OpeningBuild ? openingBuildSeconds : laterBuildSeconds;
    }

    public int ResolveCombatSeconds(bool fastMode) => fastMode ? fastCombatSeconds : normalCombatSeconds;
    public int ResolveShrinkSeconds(bool fastMode) => fastMode ? fastShrinkSeconds : normalShrinkSeconds;
}

public sealed class TotemMatchClockAccumulator
{
    public float WorldTime { get; private set; }
    public float UiTime { get; private set; }
    public bool IsWorldActive { get; private set; }

    public void Activate()
    {
        WorldTime = 0f;
        UiTime = 0f;
        IsWorldActive = true;
    }

    public void Deactivate()
    {
        IsWorldActive = false;
    }

    public void Reset()
    {
        WorldTime = 0f;
        UiTime = 0f;
        IsWorldActive = false;
    }

    public void Advance(float gameplayDeltaTime, float unscaledUiDeltaTime, bool gameplaySuspended)
    {
        if (!IsWorldActive)
        {
            return;
        }

        UiTime += Mathf.Max(0f, unscaledUiDeltaTime);
        if (!gameplaySuspended)
        {
            WorldTime += Mathf.Max(0f, gameplayDeltaTime);
        }
    }

#if UNITY_EDITOR
    public void SetWorldTimeForDiagnostics(float worldTime)
    {
        WorldTime = Mathf.Max(0f, worldTime);
    }
#endif
}

public enum TotemGameplayCommandType : byte
{
    None = 0,
    Move = 1,
    Aim = 2,
    Fire = 3,
    Interact = 4,
    EquipTattoo = 5,
    RemoveTattoo = 6,
    BeginRevive = 7,
    CancelRevive = 8,
    RequestPigment = 9,
    ResolvePigmentRequest = 10,
    ReadyBuild = 11,
    Pause = 12,
}

public enum TotemGameplayCommandSource : byte
{
    HumanInput = 0,
    BotDecision = 1,
}

public readonly struct TotemGameplayCommand
{
    public TotemGameplayCommand(
        TotemParticipantId participantId,
        TotemGameplayCommandSource source,
        TotemGameplayCommandType type,
        int sequence,
        Vector3 worldValue,
        int intValue = 0)
    {
        ParticipantId = participantId;
        Source = source;
        Type = type;
        Sequence = sequence;
        WorldValue = worldValue;
        IntValue = intValue;
    }

    public TotemParticipantId ParticipantId { get; }
    public TotemGameplayCommandSource Source { get; }
    public TotemGameplayCommandType Type { get; }
    public int Sequence { get; }
    public Vector3 WorldValue { get; }
    public int IntValue { get; }
    public bool IsValid => ParticipantId.IsValid && Type != TotemGameplayCommandType.None && Sequence >= 0;
}
