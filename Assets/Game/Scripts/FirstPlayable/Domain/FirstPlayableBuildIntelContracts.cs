namespace GameDesinger.FirstPlayable.Domain
{
    public readonly struct PigmentInventory
    {
        public PigmentInventory(int fire, int ice, int lightning)
        {
            Fire = fire < 0 ? 0 : fire;
            Ice = ice < 0 ? 0 : ice;
            Lightning = lightning < 0 ? 0 : lightning;
        }

        public int Fire { get; }
        public int Ice { get; }
        public int Lightning { get; }
        public int Get(ElementType element) { return element == ElementType.Fire ? Fire : element == ElementType.Ice ? Ice : Lightning; }
        public PigmentInventory Change(ElementType element, int delta)
        {
            int next = Get(element) + delta;
            if (next < 0) return this;
            return element == ElementType.Fire ? new PigmentInventory(next, Ice, Lightning)
                : element == ElementType.Ice ? new PigmentInventory(Fire, next, Lightning)
                : new PigmentInventory(Fire, Ice, next);
        }
    }

    public enum PigmentTransferRejection
    {
        None = 0,
        NotBuildPhase = 1,
        InvalidParticipants = 2,
        NotTeammates = 3,
        InvalidAmount = 4,
        RequestExpired = 5,
        InventoryChanged = 6,
    }

    public readonly struct PigmentTransferRequest
    {
        public PigmentTransferRequest(int requestId, ParticipantId requesterId, ParticipantId approverId, ElementType element, int amount, MatchPhase createdDuring)
        {
            RequestId = requestId;
            RequesterId = requesterId;
            ApproverId = approverId;
            Element = element;
            Amount = amount;
            CreatedDuring = createdDuring;
        }

        public int RequestId { get; }
        public ParticipantId RequesterId { get; }
        public ParticipantId ApproverId { get; }
        public ElementType Element { get; }
        public int Amount { get; }
        public MatchPhase CreatedDuring { get; }
    }

    public readonly struct PigmentTransferResult
    {
        public PigmentTransferResult(PigmentInventory requesterInventory, PigmentInventory approverInventory, PigmentTransferRejection rejection)
        {
            RequesterInventory = requesterInventory;
            ApproverInventory = approverInventory;
            Rejection = rejection;
        }

        public PigmentInventory RequesterInventory { get; }
        public PigmentInventory ApproverInventory { get; }
        public PigmentTransferRejection Rejection { get; }
        public bool IsSuccess { get { return Rejection == PigmentTransferRejection.None; } }
    }

    public static class PigmentTransferRules
    {
        public static PigmentTransferResult TryApprove(ParticipantRoster roster, MatchPhase phase, PigmentTransferRequest request, PigmentInventory requesterInventory, PigmentInventory approverInventory)
        {
            PigmentTransferRejection rejection = Validate(roster, phase, request);
            if (rejection != PigmentTransferRejection.None) return new PigmentTransferResult(requesterInventory, approverInventory, rejection);
            if (approverInventory.Get(request.Element) < request.Amount)
                return new PigmentTransferResult(requesterInventory, approverInventory, PigmentTransferRejection.InventoryChanged);

            return new PigmentTransferResult(requesterInventory.Change(request.Element, request.Amount), approverInventory.Change(request.Element, -request.Amount), PigmentTransferRejection.None);
        }

        private static PigmentTransferRejection Validate(ParticipantRoster roster, MatchPhase phase, PigmentTransferRequest request)
        {
            if (!MatchPhaseRules.IsBuildPhase(phase)) return PigmentTransferRejection.NotBuildPhase;
            if (request.CreatedDuring != phase) return PigmentTransferRejection.RequestExpired;
            if (request.RequestId <= 0 || !request.RequesterId.IsValid || !request.ApproverId.IsValid || request.RequesterId.Equals(request.ApproverId)) return PigmentTransferRejection.InvalidParticipants;
            if (request.Amount <= 0) return PigmentTransferRejection.InvalidAmount;
            if (roster == null || !roster.AreTeammates(request.RequesterId, request.ApproverId)) return PigmentTransferRejection.NotTeammates;
            return PigmentTransferRejection.None;
        }
    }

    public readonly struct ParticipantAttributeSnapshot
    {
        public ParticipantAttributeSnapshot(float baseHealth, float buildBonusHealth)
        {
            BaseHealth = baseHealth < 0f ? 0f : baseHealth;
            BuildBonusHealth = buildBonusHealth;
        }

        public float BaseHealth { get; }
        public float BuildBonusHealth { get; }
    }

    public readonly struct BuildIntelSnapshot
    {
        public BuildIntelSnapshot(ParticipantId participantId, ParticipantLifeState lifeState, TattooBuildState build, ParticipantAttributeSnapshot attributes, MatchAchievementSnapshot achievements)
        {
            ParticipantId = participantId;
            LifeState = lifeState;
            Build = build;
            Attributes = attributes;
            Achievements = achievements;
        }

        public ParticipantId ParticipantId { get; }
        public ParticipantLifeState LifeState { get; }
        public TattooBuildState Build { get; }
        public ParticipantAttributeSnapshot Attributes { get; }
        public MatchAchievementSnapshot Achievements { get; }
        public bool IsValidBoundaryState { get { return LifeState != ParticipantLifeState.Downed; } }
    }
}
