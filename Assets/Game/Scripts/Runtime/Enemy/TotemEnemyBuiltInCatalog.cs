using System;

public static class TotemEnemyBuiltInCatalog
{
    public const int DefinitionCount = 15;

    public static TotemEnemyRuntimeDefinition[] CreateDefinitions()
    {
        return new[]
        {
            Enemy("enemy_common_hunter", "Common Hunter", "common", TotemEnemyTier.Light, 70f, 9f, "light_hunter",
                Ability("hunter_melee", TotemEnemyAbilityType.Melee, 2f, 0f, 1.1f, 0.25f, 0.1f, 0.35f, 1f)),
            Enemy("enemy_common_shooter", "Common Shooter", "common", TotemEnemyTier.Light, 55f, 8f, "light_shooter",
                Ability("shooter_projectile", TotemEnemyAbilityType.Projectile, 10f, 0f, 1.6f, 0.35f, 0.1f, 0.45f, 1f)),
            Enemy("enemy_common_guardian", "Common Guardian", "common", TotemEnemyTier.Elite, 180f, 11f, "elite_guardian",
                Shield("guardian_shield", 35f),
                Ability("guardian_pulse", TotemEnemyAbilityType.AreaPulse, 4f, 4f, 3f, 0.45f, 0.1f, 0.55f, 1.15f)),

            Enemy("enemy_ai_servo", "Ruins Servo", "ai_ruins", TotemEnemyTier.Light, 75f, 9f, "light_servo",
                Ability("servo_melee", TotemEnemyAbilityType.Melee, 2.2f, 0f, 1f, 0.2f, 0.1f, 0.3f, 1f)),
            Enemy("enemy_ai_arc_drone", "Arc Drone", "ai_ruins", TotemEnemyTier.Light, 60f, 8f, "light_strafe",
                Ability("arc_drone_beam", TotemEnemyAbilityType.Beam, 9f, 0f, 1.8f, 0.5f, 0.2f, 0.45f, 1.1f)),
            Enemy("enemy_ai_manager", "Ruins Manager", "ai_ruins", TotemEnemyTier.Elite, 210f, 10f, "elite_support",
                Shield("manager_shield", 45f),
                Ability("manager_emp", TotemEnemyAbilityType.AreaPulse, 5f, 5f, 3.4f, 0.6f, 0.1f, 0.6f, 0.9f),
                Summon("manager_summon", "enemy_ai_servo", 2, 7f)),
            Boss("boss_ai_core_zero", "Core Zero", "ai_ruins", 1300f, 16f, "boss_core_zero",
                Ability("core_zero_pulse", TotemEnemyAbilityType.AreaPulse, 7f, 7f, 4f, 0.6f, 0.2f, 0.7f, 1f),
                Ability("core_zero_beam", TotemEnemyAbilityType.Beam, 14f, 0f, 2.2f, 0.7f, 0.25f, 0.65f, 1.2f),
                Summon("core_zero_summon", "enemy_ai_arc_drone", 2, 9f),
                Ability("core_zero_overheat", TotemEnemyAbilityType.HazardZone, 12f, 5f, 5f, 0.8f, 0.1f, 0.8f, 0.8f, 2),
                PhaseAbility("core_zero_phase")),

            Enemy("enemy_alien_crawler", "Hive Crawler", "alien_hive", TotemEnemyTier.Light, 68f, 10f, "light_flanker",
                Leap("crawler_leap", 7f, 3.5f),
                Ability("crawler_melee", TotemEnemyAbilityType.Melee, 2f, 0f, 0.9f, 0.18f, 0.1f, 0.25f, 0.9f)),
            Enemy("enemy_alien_spitter", "Hive Spitter", "alien_hive", TotemEnemyTier.Light, 58f, 8f, "light_kiter",
                Ability("spitter_projectile", TotemEnemyAbilityType.Projectile, 11f, 0f, 1.5f, 0.35f, 0.1f, 0.45f, 0.9f),
                Ability("spitter_acid", TotemEnemyAbilityType.HazardZone, 10f, 3f, 4f, 0.6f, 0.1f, 0.55f, 0.7f)),
            Enemy("enemy_alien_guard", "Hive Guard", "alien_hive", TotemEnemyTier.Elite, 230f, 12f, "elite_area_denial",
                Cone("alien_guard_sweep", 6f, 55f),
                Summon("alien_guard_summon", "enemy_alien_crawler", 2, 8f),
                Ability("alien_guard_hazard", TotemEnemyAbilityType.HazardZone, 7f, 4f, 4.5f, 0.55f, 0.1f, 0.6f, 0.8f)),
            Boss("boss_alien_hive_mother", "Hive Mother", "alien_hive", 1450f, 17f, "boss_hive_mother",
                Cone("hive_mother_sweep", 8f, 65f),
                Summon("hive_mother_summon", "enemy_alien_crawler", 3, 7f),
                Ability("hive_mother_hazard", TotemEnemyAbilityType.HazardZone, 12f, 5f, 4f, 0.55f, 0.1f, 0.55f, 0.9f),
                PhaseAbility("hive_mother_phase")),

            Enemy("enemy_virus_mutant", "Swamp Mutant", "virus_swamp", TotemEnemyTier.Light, 90f, 11f, "light_berserker",
                Charge("mutant_charge", 8f, 5f)),
            Enemy("enemy_virus_spore_carrier", "Spore Carrier", "virus_swamp", TotemEnemyTier.Light, 52f, 8f, "light_death_burst",
                Ability("carrier_projectile", TotemEnemyAbilityType.Projectile, 9f, 0f, 1.7f, 0.3f, 0.1f, 0.4f, 0.8f),
                DeathBurst("carrier_death_burst", 4f),
                Ability("carrier_hazard", TotemEnemyAbilityType.HazardZone, 8f, 3f, 4f, 0.5f, 0.1f, 0.5f, 0.7f)),
            Enemy("enemy_virus_spore_host", "Spore Host", "virus_swamp", TotemEnemyTier.Elite, 250f, 10f, "elite_regenerator",
                Ability("spore_host_pulse", TotemEnemyAbilityType.AreaPulse, 5f, 5f, 3f, 0.45f, 0.1f, 0.5f, 0.9f),
                Regenerate("spore_host_regenerate", 35f),
                Summon("spore_host_summon", "enemy_virus_spore_carrier", 2, 8f)),
            Boss("boss_virus_terminus", "Virus Terminus", "virus_swamp", 1550f, 18f, "boss_virus_terminus",
                Charge("terminus_charge", 10f, 7f),
                Ability("terminus_hazard", TotemEnemyAbilityType.HazardZone, 13f, 6f, 4f, 0.6f, 0.1f, 0.65f, 1f),
                Summon("terminus_split", "enemy_virus_spore_carrier", 3, 7f),
                Regenerate("terminus_regenerate", 90f, 3),
                PhaseAbility("terminus_phase")),
        };
    }

    private static TotemEnemyRuntimeDefinition Enemy(
        string id,
        string name,
        string theme,
        TotemEnemyTier tier,
        float health,
        float damage,
        string behaviorId,
        params TotemEnemyAbilityRuntimeDefinition[] abilities)
    {
        return new TotemEnemyRuntimeDefinition
        {
            enemyId = id,
            displayName = name,
            themeId = theme,
            tier = tier,
            runtimeAssetKey = "enemy/" + theme + "/" + id,
            lootTableId = "loot_" + id,
            abilityIds = JoinAbilityIds(abilities),
            maxHealth = health,
            baseDamage = damage,
            behavior = Behavior(behaviorId, tier),
            abilities = abilities ?? Array.Empty<TotemEnemyAbilityRuntimeDefinition>(),
        };
    }

    private static TotemEnemyRuntimeDefinition Boss(
        string id,
        string name,
        string theme,
        float health,
        float damage,
        string behaviorId,
        params TotemEnemyAbilityRuntimeDefinition[] abilities)
    {
        TotemEnemyRuntimeDefinition definition = Enemy(id, name, theme, TotemEnemyTier.Boss, health, damage, behaviorId, abilities);
        definition.bossPhases = new[]
        {
            new TotemBossPhaseDefinition { phase = 1, enterHealthRatio = 1f, damageMultiplier = 1f },
            new TotemBossPhaseDefinition { phase = 2, enterHealthRatio = 0.6f, damageMultiplier = 1.2f, transitionSeconds = 0.6f, vfxId = theme + "_boss_phase_2", audioCueId = "boss_phase_2" },
            new TotemBossPhaseDefinition { phase = 3, enterHealthRatio = 0.3f, damageMultiplier = 1.45f, transitionSeconds = 0.8f, vfxId = theme + "_boss_phase_3", audioCueId = "boss_phase_3" },
        };
        return definition;
    }

    private static TotemEnemyBehaviorDefinition Behavior(string id, TotemEnemyTier tier)
    {
        return new TotemEnemyBehaviorDefinition
        {
            behaviorProfileId = id,
            detectRange = tier == TotemEnemyTier.Boss ? 24f : tier == TotemEnemyTier.Elite ? 18f : 14f,
            attackRange = tier == TotemEnemyTier.Boss ? 5f : tier == TotemEnemyTier.Elite ? 3f : 2f,
            leashRange = tier == TotemEnemyTier.Boss ? 45f : tier == TotemEnemyTier.Elite ? 32f : 24f,
            moveSpeed = tier == TotemEnemyTier.Boss ? 3.2f : tier == TotemEnemyTier.Elite ? 3.4f : 3.6f,
            groupAlertRadius = tier == TotemEnemyTier.Light ? 9f : 14f,
        };
    }

    private static TotemEnemyAbilityRuntimeDefinition Ability(
        string id,
        TotemEnemyAbilityType type,
        float range,
        float radius,
        float cooldown,
        float windup,
        float active,
        float recovery,
        float damageMultiplier,
        int minimumPhase = 1)
    {
        return new TotemEnemyAbilityRuntimeDefinition
        {
            abilityId = id,
            abilityType = type,
            range = range,
            radius = radius,
            cooldown = cooldown,
            windup = windup,
            active = active,
            recovery = recovery,
            damageMultiplier = damageMultiplier,
            minimumBossPhase = minimumPhase,
        };
    }

    private static TotemEnemyAbilityRuntimeDefinition Shield(string id, float amount)
    {
        TotemEnemyAbilityRuntimeDefinition ability = Ability(id, TotemEnemyAbilityType.Shield, 20f, 0f, 8f, 0.5f, 0.1f, 0.5f, 0f);
        ability.shieldAmount = amount;
        ability.score = 0.7f;
        return ability;
    }

    private static TotemEnemyAbilityRuntimeDefinition Summon(string id, string enemyId, int count, float cooldown)
    {
        TotemEnemyAbilityRuntimeDefinition ability = Ability(id, TotemEnemyAbilityType.Summon, 20f, 0f, cooldown, 0.7f, 0.1f, 0.6f, 0f);
        ability.summonEnemyId = enemyId;
        ability.summonCount = count;
        ability.score = 0.65f;
        return ability;
    }

    private static TotemEnemyAbilityRuntimeDefinition Leap(string id, float range, float distance)
    {
        TotemEnemyAbilityRuntimeDefinition ability = Ability(id, TotemEnemyAbilityType.Leap, range, 2.5f, 2.4f, 0.35f, 0.1f, 0.45f, 1.1f);
        ability.moveDistance = distance;
        return ability;
    }

    private static TotemEnemyAbilityRuntimeDefinition Charge(string id, float range, float distance)
    {
        TotemEnemyAbilityRuntimeDefinition ability = Ability(id, TotemEnemyAbilityType.Charge, range, 0f, 2.8f, 0.5f, 0.15f, 0.5f, 1.25f);
        ability.moveDistance = distance;
        return ability;
    }

    private static TotemEnemyAbilityRuntimeDefinition Cone(string id, float radius, float halfAngle)
    {
        TotemEnemyAbilityRuntimeDefinition ability = Ability(id, TotemEnemyAbilityType.ConeSweep, radius, radius, 2.6f, 0.45f, 0.2f, 0.5f, 1.15f);
        ability.coneHalfAngle = halfAngle;
        return ability;
    }

    private static TotemEnemyAbilityRuntimeDefinition Regenerate(string id, float amount, int minimumPhase = 1)
    {
        TotemEnemyAbilityRuntimeDefinition ability = Ability(id, TotemEnemyAbilityType.Regenerate, 20f, 0f, 9f, 0.9f, 0.1f, 0.7f, 0f, minimumPhase);
        ability.healAmount = amount;
        ability.score = 0.8f;
        ability.interruptible = true;
        return ability;
    }

    private static TotemEnemyAbilityRuntimeDefinition DeathBurst(string id, float radius)
    {
        return Ability(id, TotemEnemyAbilityType.DeathBurst, radius, radius, 0f, 0f, 0f, 0f, 1.2f);
    }

    private static TotemEnemyAbilityRuntimeDefinition PhaseAbility(string id)
    {
        return Ability(id, TotemEnemyAbilityType.PhaseTransition, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
    }

    private static string JoinAbilityIds(TotemEnemyAbilityRuntimeDefinition[] abilities)
    {
        if (abilities == null || abilities.Length == 0)
        {
            return string.Empty;
        }

        string[] ids = new string[abilities.Length];
        for (int i = 0; i < abilities.Length; i++)
        {
            ids[i] = abilities[i]?.abilityId ?? string.Empty;
        }

        return string.Join(",", ids);
    }
}
