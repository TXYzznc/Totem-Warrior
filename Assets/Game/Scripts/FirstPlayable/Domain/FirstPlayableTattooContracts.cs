using System;

namespace GameDesinger.FirstPlayable.Domain
{
    public enum TattooPatternId { None = 0, P01 = 1, P02 = 2 }
    public enum TattooBodyPart { Head = 0, Torso = 1, LeftArm = 2, RightArm = 3, LeftLeg = 4, RightLeg = 5 }
    public enum ElementType { Fire = 0, Ice = 1, Lightning = 2 }
    public enum TattooBuildRejection { None = 0, NotBuildPhase = 1, InvalidPattern = 2, InvalidSlot = 3, InsufficientPigment = 4 }

    public readonly struct TattooSlot
    {
        public TattooSlot(TattooPatternId pattern, ElementType element)
        {
            Pattern = pattern;
            Element = element;
        }

        public TattooPatternId Pattern { get; }
        public ElementType Element { get; }
        public bool IsEmpty { get { return Pattern == TattooPatternId.None; } }
    }

    public readonly struct TattooEffectDefinition
    {
        public TattooEffectDefinition(TattooPatternId pattern, TattooBodyPart bodyPart, float cooldownSeconds, string publicText)
        {
            Pattern = pattern;
            BodyPart = bodyPart;
            CooldownSeconds = cooldownSeconds;
            PublicText = publicText;
        }

        public TattooPatternId Pattern { get; }
        public TattooBodyPart BodyPart { get; }
        public float CooldownSeconds { get; }
        public string PublicText { get; }
    }

    public static class TattooEffectCatalog
    {
        public static bool TryGet(TattooPatternId pattern, TattooBodyPart bodyPart, out TattooEffectDefinition definition)
        {
            if (pattern != TattooPatternId.P01 && pattern != TattooPatternId.P02)
            {
                definition = default(TattooEffectDefinition);
                return false;
            }

            bool p01 = pattern == TattooPatternId.P01;
            float cooldown;
            switch (bodyPart)
            {
                case TattooBodyPart.Head: cooldown = p01 ? 6f : 10f; break;
                case TattooBodyPart.LeftArm: cooldown = 10f; break;
                case TattooBodyPart.RightArm: cooldown = p01 ? 2.5f : 6f; break;
                case TattooBodyPart.Torso: cooldown = p01 ? 6f : 9f; break;
                case TattooBodyPart.LeftLeg: cooldown = p01 ? 4f : 8f; break;
                case TattooBodyPart.RightLeg: cooldown = p01 ? 5f : 9f; break;
                default:
                    definition = default(TattooEffectDefinition);
                    return false;
            }

            definition = new TattooEffectDefinition(pattern, bodyPart, cooldown, p01 ? "对瞄准的单个敌方目标触发效果。" : "对以目标点为圆心、一定范围内的所有合法敌方目标触发效果。");
            return true;
        }
    }

    public sealed class TattooBuildState
    {
        public const int EquipCost = 10;
        public const int RemoveRefund = 6;
        private readonly TattooSlot[] slots;
        private readonly int[] pigments;

        private TattooBuildState(TattooSlot[] slots, int[] pigments)
        {
            this.slots = slots;
            this.pigments = pigments;
        }

        public static TattooBuildState CreateEmpty(int fire, int ice, int lightning)
        {
            return new TattooBuildState(new TattooSlot[6], new[] { ClampNonNegative(fire), ClampNonNegative(ice), ClampNonNegative(lightning) });
        }

        public TattooSlot GetSlot(TattooBodyPart bodyPart) { return slots[(int)bodyPart]; }
        public int GetPigment(ElementType element) { return pigments[(int)element]; }

        public TattooBuildMutationResult TryEquip(MatchPhase phase, TattooBodyPart bodyPart, TattooPatternId pattern, ElementType element)
        {
            TattooEffectDefinition ignored;
            if (!MatchPhaseRules.IsBuildPhase(phase)) return Reject(TattooBuildRejection.NotBuildPhase);
            if (!TattooEffectCatalog.TryGet(pattern, bodyPart, out ignored)) return Reject(TattooBuildRejection.InvalidPattern);

            TattooSlot oldSlot = slots[(int)bodyPart];
            int availableAfterRefund = pigments[(int)element] + (!oldSlot.IsEmpty && oldSlot.Element == element ? RemoveRefund : 0);
            if (availableAfterRefund < EquipCost) return Reject(TattooBuildRejection.InsufficientPigment);

            TattooSlot[] nextSlots = (TattooSlot[])slots.Clone();
            int[] nextPigments = (int[])pigments.Clone();
            if (!oldSlot.IsEmpty) nextPigments[(int)oldSlot.Element] += RemoveRefund;
            nextPigments[(int)element] -= EquipCost;
            nextSlots[(int)bodyPart] = new TattooSlot(pattern, element);
            return new TattooBuildMutationResult(new TattooBuildState(nextSlots, nextPigments), TattooBuildRejection.None);
        }

        public TattooBuildMutationResult TryRemove(MatchPhase phase, TattooBodyPart bodyPart)
        {
            if (!MatchPhaseRules.IsBuildPhase(phase)) return Reject(TattooBuildRejection.NotBuildPhase);
            TattooSlot oldSlot = slots[(int)bodyPart];
            if (oldSlot.IsEmpty) return new TattooBuildMutationResult(this, TattooBuildRejection.None);

            TattooSlot[] nextSlots = (TattooSlot[])slots.Clone();
            int[] nextPigments = (int[])pigments.Clone();
            nextSlots[(int)bodyPart] = default(TattooSlot);
            nextPigments[(int)oldSlot.Element] += RemoveRefund;
            return new TattooBuildMutationResult(new TattooBuildState(nextSlots, nextPigments), TattooBuildRejection.None);
        }

        private TattooBuildMutationResult Reject(TattooBuildRejection rejection) { return new TattooBuildMutationResult(this, rejection); }
        private static int ClampNonNegative(int value) { return value < 0 ? 0 : value; }
    }

    public readonly struct TattooBuildMutationResult
    {
        public TattooBuildMutationResult(TattooBuildState state, TattooBuildRejection rejection) { State = state; Rejection = rejection; }
        public TattooBuildState State { get; }
        public TattooBuildRejection Rejection { get; }
        public bool IsSuccess { get { return Rejection == TattooBuildRejection.None; } }
    }

    public sealed class TattooCooldownState
    {
        private readonly float[] remainingSeconds = new float[6];

        public float GetRemaining(TattooBodyPart bodyPart) { return remainingSeconds[(int)bodyPart]; }
        public bool TryStart(TattooBuildState build, TattooBodyPart bodyPart)
        {
            TattooSlot slot = build.GetSlot(bodyPart);
            TattooEffectDefinition definition;
            if (slot.IsEmpty || remainingSeconds[(int)bodyPart] > 0f || !TattooEffectCatalog.TryGet(slot.Pattern, bodyPart, out definition)) return false;
            remainingSeconds[(int)bodyPart] = definition.CooldownSeconds;
            return true;
        }

        public void AdvanceCombatTime(float seconds)
        {
            if (seconds <= 0f) return;
            for (int index = 0; index < remainingSeconds.Length; index++) remainingSeconds[index] = Math.Max(0f, remainingSeconds[index] - seconds);
        }

        public void RefreshForCombatStart(TattooBuildState build)
        {
            for (int index = 0; index < remainingSeconds.Length; index++) remainingSeconds[index] = 0f;
        }
    }
}
