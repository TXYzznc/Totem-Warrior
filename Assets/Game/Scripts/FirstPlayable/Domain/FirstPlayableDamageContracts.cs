namespace GameDesinger.FirstPlayable.Domain
{
    public enum HitRegion
    {
        Body = 0,
        Weakpoint = 1,
    }

    public enum DamageKind
    {
        Direct = 0,
        IndirectTattoo = 1,
        IndirectElement = 2,
    }

    public enum DamageRejectionReason
    {
        None = 0,
        MissingAttacker = 1,
        MissingTarget = 2,
        SameTeam = 3,
        TargetNotDamageable = 4,
        NonPositiveAmount = 5,
    }

    public readonly struct DamageIntent
    {
        public DamageIntent(ParticipantId attackerId, ParticipantId targetId, DamageKind kind, HitRegion hitRegion, float amount)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Kind = kind;
            HitRegion = hitRegion;
            Amount = amount;
        }

        public ParticipantId AttackerId { get; }
        public ParticipantId TargetId { get; }
        public DamageKind Kind { get; }
        public HitRegion HitRegion { get; }
        public float Amount { get; }
    }

    public readonly struct CombatantVitals
    {
        public CombatantVitals(float maxHealth, float currentHealth, float shield, ParticipantLifeState lifeState)
        {
            MaxHealth = maxHealth < 0f ? 0f : maxHealth;
            CurrentHealth = currentHealth < 0f ? 0f : (currentHealth > maxHealth ? maxHealth : currentHealth);
            Shield = shield < 0f ? 0f : shield;
            LifeState = lifeState;
        }

        public float MaxHealth { get; }
        public float CurrentHealth { get; }
        public float Shield { get; }
        public ParticipantLifeState LifeState { get; }
        public bool CanReceiveDamage { get { return LifeState == ParticipantLifeState.Alive || LifeState == ParticipantLifeState.Downed; } }
    }

    public readonly struct DamageApplication
    {
        public DamageApplication(DamageIntent intent, DamageRejectionReason rejectionReason, float shieldDamage, float healthDamage, CombatantVitals targetAfter)
        {
            Intent = intent;
            RejectionReason = rejectionReason;
            ShieldDamage = shieldDamage;
            HealthDamage = healthDamage;
            TargetAfter = targetAfter;
        }

        public DamageIntent Intent { get; }
        public DamageRejectionReason RejectionReason { get; }
        public float ShieldDamage { get; }
        public float HealthDamage { get; }
        public CombatantVitals TargetAfter { get; }
        public float EffectiveAmount { get { return ShieldDamage + HealthDamage; } }
        public bool IsAccepted { get { return RejectionReason == DamageRejectionReason.None; } }
        public bool IsEffectiveDirectDamage { get { return Intent.Kind == DamageKind.Direct && EffectiveAmount > 0f; } }
    }

    public static class DamageResolver
    {
        public static DamageApplication Resolve(ParticipantRoster roster, DamageIntent intent, CombatantVitals target)
        {
            if (roster == null || !roster.TryGetParticipant(intent.AttackerId, out _))
            {
                return Reject(intent, DamageRejectionReason.MissingAttacker, target);
            }

            if (!roster.TryGetParticipant(intent.TargetId, out _))
            {
                return Reject(intent, DamageRejectionReason.MissingTarget, target);
            }

            if (roster.AreTeammates(intent.AttackerId, intent.TargetId))
            {
                return Reject(intent, DamageRejectionReason.SameTeam, target);
            }

            if (!target.CanReceiveDamage)
            {
                return Reject(intent, DamageRejectionReason.TargetNotDamageable, target);
            }

            if (intent.Amount <= 0f)
            {
                return Reject(intent, DamageRejectionReason.NonPositiveAmount, target);
            }

            float shieldDamage = intent.Amount > target.Shield ? target.Shield : intent.Amount;
            float remaining = intent.Amount - shieldDamage;
            float healthDamage = remaining > target.CurrentHealth ? target.CurrentHealth : remaining;
            float nextHealth = target.CurrentHealth - healthDamage;
            ParticipantLifeState nextState = target.LifeState;
            if (target.LifeState == ParticipantLifeState.Alive && nextHealth <= 0f && healthDamage > 0f)
            {
                nextState = ParticipantLifeState.Downed;
            }

            var targetAfter = new CombatantVitals(target.MaxHealth, nextHealth, target.Shield - shieldDamage, nextState);
            return new DamageApplication(intent, DamageRejectionReason.None, shieldDamage, healthDamage, targetAfter);
        }

        private static DamageApplication Reject(DamageIntent intent, DamageRejectionReason reason, CombatantVitals target)
        {
            return new DamageApplication(intent, reason, 0f, 0f, target);
        }
    }
}
