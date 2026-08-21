using System;

namespace GameDesinger.FirstPlayable.Domain
{
    public enum ElementStrength
    {
        None = 0,
        Weak = 1,
        Standard = 2,
        Strong = 3,
    }

    public enum ElementTraitKind
    {
        FireBurn = 0,
        IceMoveSlow = 1,
        LightningDischarge = 2,
    }

    public enum ElementReactionType
    {
        None = 0,
        ThermalShock = 1,
        Overload = 2,
        Stasis = 3,
    }

    public readonly struct ElementLayerSource
    {
        public ElementLayerSource(ParticipantId participantId, long applicationSequence)
        {
            ParticipantId = participantId;
            ApplicationSequence = applicationSequence;
        }

        public ParticipantId ParticipantId { get; }
        public long ApplicationSequence { get; }
    }

    public readonly struct ElementReactionDefinition
    {
        public ElementReactionDefinition(ElementReactionType type, float centerDamageMultiplier, float secondaryDamageMultiplier, float radiusMeters, float durationSeconds, float directDamageReduction)
        {
            Type = type;
            CenterDamageMultiplier = centerDamageMultiplier;
            SecondaryDamageMultiplier = secondaryDamageMultiplier;
            RadiusMeters = radiusMeters;
            DurationSeconds = durationSeconds;
            DirectDamageReduction = directDamageReduction;
        }

        public ElementReactionType Type { get; }
        public float CenterDamageMultiplier { get; }
        public float SecondaryDamageMultiplier { get; }
        public float RadiusMeters { get; }
        public float DurationSeconds { get; }
        public float DirectDamageReduction { get; }
    }

    public readonly struct ElementReactionOccurrence
    {
        public ElementReactionOccurrence(ElementReactionDefinition definition, ParticipantId triggerParticipantId, ElementLayerSource assistingLayer)
        {
            Definition = definition;
            TriggerParticipantId = triggerParticipantId;
            AssistingLayer = assistingLayer;
        }

        public ElementReactionDefinition Definition { get; }
        public ParticipantId TriggerParticipantId { get; }
        public ElementLayerSource AssistingLayer { get; }
    }

    public readonly struct ElementApplicationResult
    {
        public ElementApplicationResult(ElementAttachment attachment, bool appliedLayer, ElementReactionOccurrence reaction)
        {
            Attachment = attachment;
            AppliedLayer = appliedLayer;
            Reaction = reaction;
        }

        public ElementAttachment Attachment { get; }
        public bool AppliedLayer { get; }
        public ElementReactionOccurrence Reaction { get; }
        public bool HasReaction { get { return Reaction.Definition.Type != ElementReactionType.None; } }
    }

    public readonly struct ElementAttachment
    {
        public const float DecayIntervalSeconds = 3f;
        private readonly ElementLayerSource[] sources;

        private ElementAttachment(ElementType element, ElementLayerSource[] sources, float remainingToDecaySeconds)
        {
            Element = element;
            this.sources = sources;
            RemainingToDecaySeconds = remainingToDecaySeconds;
        }

        public bool HasElement { get { return sources != null && sources.Length > 0; } }
        public ElementType Element { get; }
        public ElementStrength Strength { get { return HasElement ? (ElementStrength)sources.Length : ElementStrength.None; } }
        public float RemainingToDecaySeconds { get; }
        public int LayerCount { get { return HasElement ? sources.Length : 0; } }

        public ElementLayerSource GetLayerSource(int index)
        {
            if (!HasElement || index < 0 || index >= sources.Length) throw new ArgumentOutOfRangeException("index");
            return sources[index];
        }

        public ElementApplicationResult Apply(ElementType incomingElement, ParticipantId source, long applicationSequence)
        {
            if (!source.IsValid) throw new ArgumentException("Element source must be a valid participant.", "source");
            var incoming = new ElementLayerSource(source, applicationSequence);
            if (!HasElement)
            {
                return new ElementApplicationResult(new ElementAttachment(incomingElement, new[] { incoming }, DecayIntervalSeconds), true, default(ElementReactionOccurrence));
            }

            if (Element == incomingElement)
            {
                if (sources.Length == (int)ElementStrength.Strong)
                {
                    return new ElementApplicationResult(new ElementAttachment(Element, CopySources(), DecayIntervalSeconds), false, default(ElementReactionOccurrence));
                }

                var nextSources = new ElementLayerSource[sources.Length + 1];
                Array.Copy(sources, nextSources, sources.Length);
                nextSources[nextSources.Length - 1] = incoming;
                return new ElementApplicationResult(new ElementAttachment(Element, nextSources, DecayIntervalSeconds), true, default(ElementReactionOccurrence));
            }

            ElementLayerSource consumed = sources[0];
            ElementLayerSource[] remaining = RemoveOldestLayer();
            var attachment = remaining.Length == 0 ? default(ElementAttachment) : new ElementAttachment(Element, remaining, RemainingToDecaySeconds);
            var reaction = new ElementReactionOccurrence(ElementReactionRules.Get(Element, incomingElement), source, consumed);
            return new ElementApplicationResult(attachment, false, reaction);
        }

        public ElementAttachment AdvanceTime(float seconds)
        {
            if (!HasElement || seconds <= 0f) return this;
            float remaining = RemainingToDecaySeconds - seconds;
            ElementLayerSource[] nextSources = CopySources();
            while (nextSources.Length > 0 && remaining <= 0f)
            {
                nextSources = RemoveOldest(nextSources);
                remaining += DecayIntervalSeconds;
            }

            return nextSources.Length == 0 ? default(ElementAttachment) : new ElementAttachment(Element, nextSources, remaining);
        }

        private ElementLayerSource[] CopySources()
        {
            var copy = new ElementLayerSource[sources.Length];
            Array.Copy(sources, copy, sources.Length);
            return copy;
        }

        private ElementLayerSource[] RemoveOldestLayer() { return RemoveOldest(sources); }

        private static ElementLayerSource[] RemoveOldest(ElementLayerSource[] current)
        {
            if (current.Length <= 1) return Array.Empty<ElementLayerSource>();
            var next = new ElementLayerSource[current.Length - 1];
            Array.Copy(current, 1, next, 0, next.Length);
            return next;
        }
    }

    public static class ElementReactionRules
    {
        public static ElementReactionDefinition Get(ElementType first, ElementType second)
        {
            if ((first == ElementType.Fire && second == ElementType.Ice) || (first == ElementType.Ice && second == ElementType.Fire))
                return new ElementReactionDefinition(ElementReactionType.ThermalShock, 0.6f, 0f, 0f, 0f, 0f);
            if ((first == ElementType.Fire && second == ElementType.Lightning) || (first == ElementType.Lightning && second == ElementType.Fire))
                return new ElementReactionDefinition(ElementReactionType.Overload, 0.35f, 0.25f, 3f, 0f, 0f);
            return new ElementReactionDefinition(ElementReactionType.Stasis, 0f, 0f, 0f, 2f, 0.2f);
        }

        public static ElementTraitKind GetTrait(ElementType element)
        {
            return element == ElementType.Fire ? ElementTraitKind.FireBurn : element == ElementType.Ice ? ElementTraitKind.IceMoveSlow : ElementTraitKind.LightningDischarge;
        }

        public static float GetTraitStrength(ElementType element, ElementStrength strength)
        {
            int index = (int)strength - 1;
            if (index < 0 || index > 2) return 0f;
            if (element == ElementType.Fire) return new[] { 1f, 1.25f, 1.5f }[index];
            if (element == ElementType.Ice) return new[] { 0.12f, 0.20f, 0.28f }[index];
            return 0.5f;
        }
    }
}
