using System;
using System.Collections.Generic;

namespace GameDesinger.FirstPlayable.Domain
{
    public enum ParticipantControllerKind
    {
        Human = 0,
        Bot = 1,
    }

    public enum ParticipantLifeState
    {
        Alive = 0,
        Downed = 1,
        Eliminated = 2,
        Extracted = 3,
    }

    public enum GameplayCommandKind
    {
        None = 0,
        Move = 1,
        Fire = 2,
        Dodge = 3,
        Revive = 4,
        Interact = 5,
        BuildMutation = 6,
        PigmentRequest = 7,
        ActivateTattooSkill = 8,
    }

    public enum GameplayCommandSource
    {
        HumanInput = 0,
        BotDecision = 1,
    }

    public readonly struct ParticipantId : IEquatable<ParticipantId>
    {
        public ParticipantId(int value) { Value = value; }
        public int Value { get; }
        public bool IsValid { get { return Value > 0; } }
        public bool Equals(ParticipantId other) { return Value == other.Value; }
        public override bool Equals(object obj) { return obj is ParticipantId && Equals((ParticipantId)obj); }
        public override int GetHashCode() { return Value; }
    }

    public readonly struct TeamId : IEquatable<TeamId>
    {
        public TeamId(int value) { Value = value; }
        public int Value { get; }
        public bool IsValid { get { return Value > 0; } }
        public bool Equals(TeamId other) { return Value == other.Value; }
        public override bool Equals(object obj) { return obj is TeamId && Equals((TeamId)obj); }
        public override int GetHashCode() { return Value; }
    }

    public readonly struct ParticipantDefinition
    {
        public ParticipantDefinition(ParticipantId id, TeamId teamId, ParticipantControllerKind controllerKind)
        {
            Id = id;
            TeamId = teamId;
            ControllerKind = controllerKind;
        }

        public ParticipantId Id { get; }
        public TeamId TeamId { get; }
        public ParticipantControllerKind ControllerKind { get; }
        public bool IsValid { get { return Id.IsValid && TeamId.IsValid; } }
    }

    public sealed class ParticipantRoster
    {
        private const int RequiredParticipantCount = 6;
        private const int RequiredTeamCount = 3;
        private const int RequiredTeamSize = 2;
        private readonly ParticipantDefinition[] participants;

        private ParticipantRoster(ParticipantDefinition[] participants)
        {
            this.participants = participants;
        }

        public int Count { get { return participants.Length; } }

        public static bool TryCreate(IReadOnlyList<ParticipantDefinition> definitions, out ParticipantRoster roster, out string error)
        {
            roster = null;
            error = null;
            if (definitions == null || definitions.Count != RequiredParticipantCount)
            {
                error = "A match requires exactly six participants.";
                return false;
            }

            var copied = new ParticipantDefinition[RequiredParticipantCount];
            var teamSizes = new Dictionary<TeamId, int>();
            var participantIds = new HashSet<ParticipantId>();
            int humanCount = 0;
            for (int index = 0; index < definitions.Count; index++)
            {
                ParticipantDefinition definition = definitions[index];
                if (!definition.IsValid || !participantIds.Add(definition.Id))
                {
                    error = "Participant identities must be valid and unique.";
                    return false;
                }

                copied[index] = definition;
                if (definition.ControllerKind == ParticipantControllerKind.Human)
                {
                    humanCount++;
                }

                int currentSize;
                teamSizes.TryGetValue(definition.TeamId, out currentSize);
                teamSizes[definition.TeamId] = currentSize + 1;
            }

            if (humanCount != 1 || teamSizes.Count != RequiredTeamCount)
            {
                error = "A first playable match requires one human and three teams.";
                return false;
            }

            foreach (KeyValuePair<TeamId, int> team in teamSizes)
            {
                if (team.Value != RequiredTeamSize)
                {
                    error = "Each team requires exactly two participants.";
                    return false;
                }
            }

            roster = new ParticipantRoster(copied);
            return true;
        }

        public bool TryGetParticipant(ParticipantId id, out ParticipantDefinition participant)
        {
            for (int index = 0; index < participants.Length; index++)
            {
                if (participants[index].Id.Equals(id))
                {
                    participant = participants[index];
                    return true;
                }
            }

            participant = default(ParticipantDefinition);
            return false;
        }

        public bool AreTeammates(ParticipantId first, ParticipantId second)
        {
            ParticipantDefinition firstParticipant;
            ParticipantDefinition secondParticipant;
            return TryGetParticipant(first, out firstParticipant)
                && TryGetParticipant(second, out secondParticipant)
                && firstParticipant.TeamId.Equals(secondParticipant.TeamId);
        }
    }

    public readonly struct GameplayCommand
    {
        public GameplayCommand(ParticipantId participantId, GameplayCommandSource source, GameplayCommandKind kind)
        {
            ParticipantId = participantId;
            Source = source;
            Kind = kind;
        }

        public ParticipantId ParticipantId { get; }
        public GameplayCommandSource Source { get; }
        public GameplayCommandKind Kind { get; }
        public bool IsValid { get { return ParticipantId.IsValid && Kind != GameplayCommandKind.None; } }
    }

    public static class GameplayCommandRules
    {
        public static bool IsAllowed(ParticipantRoster roster, GameplayCommand command, MatchPhase phase, ParticipantLifeState actorState)
        {
            ParticipantDefinition participant;
            if (roster == null || !roster.TryGetParticipant(command.ParticipantId, out participant) || !MatchesController(participant, command.Source))
            {
                return false;
            }

            return IsAllowed(command, phase, actorState);
        }

        public static bool IsAllowed(GameplayCommand command, MatchPhase phase, ParticipantLifeState actorState)
        {
            if (!command.IsValid || actorState == ParticipantLifeState.Eliminated || actorState == ParticipantLifeState.Extracted)
            {
                return false;
            }

            if (MatchPhaseRules.IsBuildPhase(phase))
            {
                return command.Kind == GameplayCommandKind.BuildMutation || command.Kind == GameplayCommandKind.PigmentRequest;
            }

            if (!MatchPhaseRules.IsCombatPhase(phase) || actorState == ParticipantLifeState.Downed)
            {
                return false;
            }

            return command.Kind == GameplayCommandKind.Move
                || command.Kind == GameplayCommandKind.Fire
                || command.Kind == GameplayCommandKind.Dodge
                || command.Kind == GameplayCommandKind.Revive
                || command.Kind == GameplayCommandKind.Interact
                || command.Kind == GameplayCommandKind.ActivateTattooSkill;
        }

        private static bool MatchesController(ParticipantDefinition participant, GameplayCommandSource source)
        {
            return (participant.ControllerKind == ParticipantControllerKind.Human && source == GameplayCommandSource.HumanInput)
                || (participant.ControllerKind == ParticipantControllerKind.Bot && source == GameplayCommandSource.BotDecision);
        }
    }

    public readonly struct MatchAchievementSnapshot
    {
        public MatchAchievementSnapshot(
            float playerDamage,
            int playerKnockdowns,
            int playerEliminations,
            float pveDamage,
            int pveKills,
            float teammateHealing,
            float teammateShieldOrMitigation,
            int successfulRevives,
            int cleansesOrControlRemovals,
            float effectiveControl,
            float teammateDamageAmplification,
            int resourcesAcquired,
            int resourcesShared,
            int timesDowned)
        {
            PlayerDamage = playerDamage < 0f ? 0f : playerDamage;
            PlayerKnockdowns = playerKnockdowns < 0 ? 0 : playerKnockdowns;
            PlayerEliminations = playerEliminations < 0 ? 0 : playerEliminations;
            PveDamage = pveDamage < 0f ? 0f : pveDamage;
            PveKills = pveKills < 0 ? 0 : pveKills;
            TeammateHealing = teammateHealing < 0f ? 0f : teammateHealing;
            TeammateShieldOrMitigation = teammateShieldOrMitigation < 0f ? 0f : teammateShieldOrMitigation;
            SuccessfulRevives = successfulRevives < 0 ? 0 : successfulRevives;
            CleansesOrControlRemovals = cleansesOrControlRemovals < 0 ? 0 : cleansesOrControlRemovals;
            EffectiveControl = effectiveControl < 0f ? 0f : effectiveControl;
            TeammateDamageAmplification = teammateDamageAmplification < 0f ? 0f : teammateDamageAmplification;
            ResourcesAcquired = resourcesAcquired < 0 ? 0 : resourcesAcquired;
            ResourcesShared = resourcesShared < 0 ? 0 : resourcesShared;
            TimesDowned = timesDowned < 0 ? 0 : timesDowned;
        }

        public float PlayerDamage { get; }
        public int PlayerKnockdowns { get; }
        public int PlayerEliminations { get; }
        public float PveDamage { get; }
        public int PveKills { get; }
        public float TeammateHealing { get; }
        public float TeammateShieldOrMitigation { get; }
        public int SuccessfulRevives { get; }
        public int CleansesOrControlRemovals { get; }
        public float EffectiveControl { get; }
        public float TeammateDamageAmplification { get; }
        public int ResourcesAcquired { get; }
        public int ResourcesShared { get; }
        public int TimesDowned { get; }
    }
}
