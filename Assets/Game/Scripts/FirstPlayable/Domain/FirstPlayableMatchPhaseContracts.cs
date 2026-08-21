namespace GameDesinger.FirstPlayable.Domain
{
    public enum MatchPhase
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

    public readonly struct PhaseEpoch
    {
        public static readonly PhaseEpoch Invalid = new PhaseEpoch(0);
        public PhaseEpoch(long value) { Value = value; }
        public long Value { get; }
        public bool IsValid { get { return Value > 0; } }
    }

    public static class MatchPhaseRules
    {
        public static bool IsBuildPhase(MatchPhase phase)
        {
            return phase == MatchPhase.OpeningBuild || phase == MatchPhase.Build2 || phase == MatchPhase.Build3 || phase == MatchPhase.Build4 || phase == MatchPhase.Build5;
        }

        public static bool IsCombatPhase(MatchPhase phase)
        {
            return phase == MatchPhase.Round1Combat || phase == MatchPhase.Round2Combat || phase == MatchPhase.Round3Combat || phase == MatchPhase.Round4Combat || phase == MatchPhase.Round5Combat;
        }

        public static bool CanTransition(MatchPhase from, MatchPhase to)
        {
            if (to == MatchPhase.Result)
            {
                return IsCombatPhase(from);
            }

            switch (from)
            {
                case MatchPhase.FrontEnd: return to == MatchPhase.OpeningBuild;
                case MatchPhase.OpeningBuild: return to == MatchPhase.Round1Combat;
                case MatchPhase.Round1Combat: return to == MatchPhase.Build2;
                case MatchPhase.Build2: return to == MatchPhase.Round2Combat;
                case MatchPhase.Round2Combat: return to == MatchPhase.Build3;
                case MatchPhase.Build3: return to == MatchPhase.Round3Combat;
                case MatchPhase.Round3Combat: return to == MatchPhase.Build4;
                case MatchPhase.Build4: return to == MatchPhase.Round4Combat;
                case MatchPhase.Round4Combat: return to == MatchPhase.Build5;
                case MatchPhase.Build5: return to == MatchPhase.Round5Combat;
                case MatchPhase.Result: return to == MatchPhase.FrontEnd;
                default: return false;
            }
        }
    }

    public sealed class MatchPhaseCursor
    {
        private long lastIssuedEpoch;

        public MatchPhaseCursor()
        {
            CurrentPhase = MatchPhase.FrontEnd;
            CurrentEpoch = PhaseEpoch.Invalid;
        }

        public MatchPhase CurrentPhase { get; private set; }
        public PhaseEpoch CurrentEpoch { get; private set; }

        public bool TryTransition(MatchPhase next, bool atomicResolutionInProgress)
        {
            if (!MatchPhaseRules.CanTransition(CurrentPhase, next))
            {
                return false;
            }

            if (MatchPhaseRules.IsCombatPhase(CurrentPhase) && !MatchPhaseRules.IsCombatPhase(next) && atomicResolutionInProgress)
            {
                return false;
            }

            CurrentPhase = next;
            if (MatchPhaseRules.IsCombatPhase(next))
            {
                lastIssuedEpoch++;
                CurrentEpoch = new PhaseEpoch(lastIssuedEpoch);
            }
            else
            {
                CurrentEpoch = PhaseEpoch.Invalid;
            }

            return true;
        }

        public bool CanApplyDelayedWork(PhaseEpoch createdEpoch)
        {
            return MatchPhaseRules.IsCombatPhase(CurrentPhase)
                && createdEpoch.IsValid
                && createdEpoch.Value == CurrentEpoch.Value;
        }
    }
}
