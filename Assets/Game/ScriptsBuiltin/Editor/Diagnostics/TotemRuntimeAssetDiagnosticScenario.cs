#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemRuntimeAssetDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Runtime Assets";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckRuntimeAssetCatalog(context);
            CheckMigratedEntityPrefabs(context);
            CheckAssetServiceInstantiation(context);
            CheckAssetServiceVisualLoads(context);
            CheckAssetServiceCaching(context);
            context.Pass("Totem runtime asset contract is ready.");
        }

        private static void CheckRuntimeAssetCatalog(GFDiagnosticScenarioContext context)
        {
            string path = TotemAssetService.GetRuntimeAssetCatalogPath();
            context.Detail("runtimeAssetCatalog.path", path);
            context.Assert(File.Exists(path), $"Runtime asset catalog file must exist: {path}");
            context.Assert(TotemAssetService.TryLoadRuntimeAssetCatalogFromFile(path, out var catalog, out string error), $"Runtime asset catalog should parse: {error}");

            var errors = new List<string>();
            context.Assert(TotemRuntimeAssetCatalogValidator.Validate(catalog, errors), $"Runtime asset catalog validation failed: {string.Join("; ", errors)}");
            context.Detail("runtimeAssetCatalog.source", catalog.source);
            context.Detail("runtimeAssetCatalog.entryCount", catalog.entries.Length);
            context.Detail("runtimeAssetCatalog.prefabCount", catalog.entries.Count(item => string.Equals(item.assetKind, "Prefab", System.StringComparison.OrdinalIgnoreCase)));
            context.Detail("runtimeAssetCatalog.textureCount", catalog.entries.Count(item => string.Equals(item.assetKind, "Texture", System.StringComparison.OrdinalIgnoreCase)));
            context.Detail("runtimeAssetCatalog.spriteCount", catalog.entries.Count(item => string.Equals(item.assetKind, "Sprite", System.StringComparison.OrdinalIgnoreCase)));
            context.Assert(catalog.entries.Length >= 49, "Runtime asset catalog should include actor, selected-character actor, CharacterSelect UI, NPC, chest, map, weapon, all skill, VFX, and tattoo visual entries.");
            context.Assert(catalog.TryGetEntry("actor.player.1", out _), "Runtime asset catalog should include selected character actor.player.1.");
            context.Assert(catalog.TryGetEntry("actor.player.2", out _), "Runtime asset catalog should include selected character actor.player.2.");
            context.Assert(catalog.TryGetEntry("actor.player.3", out _), "Runtime asset catalog should include selected character actor.player.3.");
            context.Assert(catalog.TryGetEntry("tattoo.part.head", out _), "Runtime asset catalog should include tattoo.part.head.");
            context.Assert(catalog.TryGetEntry("tattoo.pattern.beast", out _), "Runtime asset catalog should include tattoo.pattern.beast.");
            context.Assert(catalog.TryGetEntry("ui.character.1", out _), "Runtime asset catalog should include CharacterSelect portrait ui.character.1.");
            context.Assert(catalog.TryGetEntry("ui.character.card.unlocked", out _), "Runtime asset catalog should include CharacterSelect unlocked card frame.");
            context.Assert(catalog.TryGetEntry("skill.skill_phase_dash", out _), "Runtime asset catalog should include SkillAcquire icon skill.skill_phase_dash.");
            context.Assert(catalog.TryGetEntry("skill.skill_ink_shield", out _), "Runtime asset catalog should include SkillAcquire icon skill.skill_ink_shield.");
            CheckCatalogSourceFiles(context, catalog);
        }

        private static void CheckMigratedEntityPrefabs(GFDiagnosticScenarioContext context)
        {
            var rules = TotemRuntimeAssetMigrator.GetRules();
            context.Detail("runtimePrefab.ruleCount", rules.Length);
            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                context.Assert(File.Exists(rule.SourcePath), $"Legacy visual source must exist: {rule.SourcePath}");
                context.Assert(File.Exists(rule.TargetPath), $"Active GF_X runtime prefab must exist: {rule.TargetPath}");
                if (File.Exists(rule.TargetPath))
                {
                    CheckPrefabClean(context, rule.Key, rule.TargetPath);
                }
            }
        }

        private static void CheckPrefabClean(GFDiagnosticScenarioContext context, string key, string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                int behaviourCount = root.GetComponentsInChildren<MonoBehaviour>(true).Count(item => item != null);
                int spriteRendererCount = root.GetComponentsInChildren<SpriteRenderer>(true).Length;
                var animator = root.GetComponentInChildren<Animator>(true);
                context.Detail($"{key}.missingScriptCount", missingScriptCount);
                context.Detail($"{key}.monoBehaviourCount", behaviourCount);
                context.Detail($"{key}.spriteRendererCount", spriteRendererCount);
                context.Detail($"{key}.hasAnimator", animator != null);
                context.AssertEqual(0, missingScriptCount, $"{key}.missingScriptCount");
                context.AssertEqual(0, behaviourCount, $"{key}.monoBehaviourCount");
                context.Assert(spriteRendererCount > 0, $"{key} should keep visual SpriteRenderer components.");
                if (key.StartsWith("actor.", System.StringComparison.Ordinal))
                {
                    CheckActorAnimatorParameters(context, key, animator);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CheckActorAnimatorParameters(GFDiagnosticScenarioContext context, string key, Animator animator)
        {
            context.Assert(animator != null, $"{key} should keep an Animator.");
            context.Assert(animator != null && animator.runtimeAnimatorController != null, $"{key} should keep an AnimatorController.");
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            var parameters = animator.parameters;
            context.Assert(parameters.Any(item => item.name == "IsMoving" && item.type == AnimatorControllerParameterType.Bool), $"{key} Animator should expose bool IsMoving.");
            context.Assert(parameters.Any(item => item.name == "Direction" && item.type == AnimatorControllerParameterType.Int), $"{key} Animator should expose int Direction.");
            context.Assert(parameters.Any(item => item.name == "AttackTrigger" && item.type == AnimatorControllerParameterType.Trigger), $"{key} Animator should expose trigger AttackTrigger.");
            context.Assert(parameters.Any(item => item.name == "Die" && item.type == AnimatorControllerParameterType.Trigger), $"{key} Animator should expose trigger Die.");
            context.Assert(parameters.Any(item => item.name == "Dead" && item.type == AnimatorControllerParameterType.Bool), $"{key} Animator should expose bool Dead.");
        }

        private static void CheckAssetServiceInstantiation(GFDiagnosticScenarioContext context)
        {
            var service = new TotemAssetService();
            service.ReloadRuntimeAssetCatalog();
            context.Assert(service.RuntimeAssetCatalogLoadedFromFile, $"Runtime asset catalog should load from file: {service.RuntimeAssetCatalogMessage}");
            string[] actorKeys =
            {
                "actor.player",
                "actor.player.1",
                "actor.player.2",
                "actor.player.3",
            };
            for (int i = 0; i < actorKeys.Length; i++)
            {
                context.Assert(service.TryInstantiateGameObject(actorKeys[i], null, Vector3.zero, Vector3.one, out var instance), $"Asset service should instantiate {actorKeys[i]} in editor.");
                context.Assert(instance != null, $"Instantiated {actorKeys[i]} should not be null.");
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            DetailFallbackCounters(context, service, "runtimeAsset.instantiate");
            context.AssertEqual(0, service.MissingEntryCount, "runtimeAsset.instantiate.missingEntryCount");
            context.AssertEqual(0, service.FallbackRequiredCount, "runtimeAsset.instantiate.fallbackRequiredCount");
        }

        private static void CheckAssetServiceVisualLoads(GFDiagnosticScenarioContext context)
        {
            var service = new TotemAssetService();
            service.ReloadRuntimeAssetCatalog();
            context.Assert(service.RuntimeAssetCatalogLoadedFromFile, $"Runtime asset catalog should load from file: {service.RuntimeAssetCatalogMessage}");
            context.Assert(service.TryLoadTexture("map.floor.ruins", out var floorTexture), "Asset service should load map.floor.ruins texture.");
            context.Assert(floorTexture != null, "Loaded map.floor.ruins texture should not be null.");
            context.Assert(service.TryLoadSprite("weapon.knife_basic", out var weaponSprite), "Asset service should load weapon.knife_basic sprite.");
            context.Assert(weaponSprite != null, "Loaded weapon.knife_basic sprite should not be null.");
            context.Assert(service.TryLoadSprite("skill.skill_fireball_01", out var skillSprite), "Asset service should load skill.skill_fireball_01 sprite.");
            context.Assert(skillSprite != null, "Loaded skill.skill_fireball_01 sprite should not be null.");
            string[] characterUiKeys =
            {
                "ui.character.1",
                "ui.character.2",
                "ui.character.3",
                "ui.character.card.unlocked",
            };
            for (int i = 0; i < characterUiKeys.Length; i++)
            {
                context.Assert(service.TryLoadSprite(characterUiKeys[i], out var characterSprite), $"Asset service should load {characterUiKeys[i]} sprite.");
                context.Assert(characterSprite != null, $"Loaded {characterUiKeys[i]} sprite should not be null.");
            }

            string[] skillIconKeys =
            {
                "skill.skill_frost_field_01",
                "skill.skill_chain_lightning_01",
                "skill.skill_heal_aura_01",
                "skill.skill_shield_01",
                "skill.skill_stealth_01",
                "skill.skill_summon_01",
                "skill.skill_time_slow_01",
                "skill.skill_phase_dash",
                "skill.skill_ink_shield",
                "skill.skill_stomp",
                "skill.skill_beam",
                "skill.skill_summon",
                "skill.skill_enrage_aoe",
            };
            for (int i = 0; i < skillIconKeys.Length; i++)
            {
                context.Assert(service.TryLoadSprite(skillIconKeys[i], out var extraSkillSprite), $"Asset service should load {skillIconKeys[i]} sprite.");
                context.Assert(extraSkillSprite != null, $"Loaded {skillIconKeys[i]} sprite should not be null.");
            }

            context.Assert(service.TryLoadSprite("chest.chest_common", out var commonChestSprite), "Asset service should load chest.chest_common sprite.");
            context.Assert(commonChestSprite != null, "Loaded chest.chest_common sprite should not be null.");
            context.Assert(service.TryLoadSprite("chest.chest_rare", out var rareChestSprite), "Asset service should load chest.chest_rare sprite.");
            context.Assert(rareChestSprite != null, "Loaded chest.chest_rare sprite should not be null.");
            context.Assert(service.TryLoadSprite("tattoo.part.head", out var tattooPartSprite), "Asset service should load tattoo.part.head sprite.");
            context.Assert(tattooPartSprite != null, "Loaded tattoo.part.head sprite should not be null.");
            context.Assert(service.TryLoadSprite("tattoo.pattern.line", out var tattooPatternSprite), "Asset service should load tattoo.pattern.line sprite.");
            context.Assert(tattooPatternSprite != null, "Loaded tattoo.pattern.line sprite should not be null.");
            context.Assert(service.TryCreateTexturedMaterial("map.wall.ruins", Color.gray, out var material), "Asset service should create map.wall.ruins material.");
            context.Assert(material != null, "Created map.wall.ruins material should not be null.");
            DetailFallbackCounters(context, service, "runtimeAsset.visual");
            context.AssertEqual(0, service.MissingEntryCount, "runtimeAsset.visual.missingEntryCount");
            context.AssertEqual(0, service.FallbackRequiredCount, "runtimeAsset.visual.fallbackRequiredCount");
            // Keep the temporary editor material alive for the current editor repaint.
            // DestroyImmediate here can make UIElements touch a destroyed Material on the next frame.
        }

        private static void CheckAssetServiceCaching(GFDiagnosticScenarioContext context)
        {
            var service = new TotemAssetService();
            service.ReloadRuntimeAssetCatalog();
            context.AssertEqual(0, service.CachedAssetCount, "runtimeAsset.cache.initialCount");
            context.AssertEqual(0, service.CacheHitCount, "runtimeAsset.cache.initialHits");
            context.AssertEqual(0, service.CacheMissCount, "runtimeAsset.cache.initialMisses");

            context.Assert(service.TryLoadSprite("weapon.knife_basic", out var firstSprite), "Asset cache diagnostic should load weapon.knife_basic sprite first.");
            context.Assert(service.TryLoadSprite("weapon.knife_basic", out var secondSprite), "Asset cache diagnostic should load weapon.knife_basic sprite second.");
            context.Assert(firstSprite == secondSprite, "Second sprite load should reuse the cached Sprite instance.");
            context.AssertEqual(1, service.CachedAssetCount, "runtimeAsset.cache.afterSprite.cachedCount");
            context.AssertEqual(1, service.CacheHitCount, "runtimeAsset.cache.afterSprite.hits");
            context.AssertEqual(1, service.CacheMissCount, "runtimeAsset.cache.afterSprite.misses");
            context.AssertEqual("weapon.knife_basic", service.LastCacheKey, "runtimeAsset.cache.afterSprite.lastKey");
            context.AssertEqual("Sprite", service.LastCacheKind, "runtimeAsset.cache.afterSprite.lastKind");

            context.Assert(service.TryLoadTexture("map.floor.ruins", out var firstTexture), "Asset cache diagnostic should load map.floor.ruins texture first.");
            context.Assert(service.TryLoadTexture("map.floor.ruins", out var secondTexture), "Asset cache diagnostic should load map.floor.ruins texture second.");
            context.Assert(firstTexture == secondTexture, "Second texture load should reuse the cached Texture2D instance.");
            context.AssertEqual(2, service.CachedAssetCount, "runtimeAsset.cache.afterTexture.cachedCount");
            context.AssertEqual(2, service.CacheHitCount, "runtimeAsset.cache.afterTexture.hits");
            context.AssertEqual(2, service.CacheMissCount, "runtimeAsset.cache.afterTexture.misses");

            context.Assert(service.TryInstantiateGameObject("actor.player.1", null, Vector3.zero, Vector3.one, out var firstInstance), "Asset cache diagnostic should instantiate actor.player.1 first.");
            context.Assert(service.TryInstantiateGameObject("actor.player.1", null, Vector3.zero, Vector3.one, out var secondInstance), "Asset cache diagnostic should instantiate actor.player.1 second.");
            context.Assert(firstInstance != null && secondInstance != null && firstInstance != secondInstance, "Prefab cache should reuse the loaded prefab while creating distinct instances.");
            context.AssertEqual(3, service.CachedAssetCount, "runtimeAsset.cache.afterPrefab.cachedCount");
            context.AssertEqual(3, service.CacheHitCount, "runtimeAsset.cache.afterPrefab.hits");
            context.AssertEqual(3, service.CacheMissCount, "runtimeAsset.cache.afterPrefab.misses");
            context.AssertEqual("actor.player.1", service.LastCacheKey, "runtimeAsset.cache.afterPrefab.lastKey");
            context.AssertEqual("Prefab", service.LastCacheKind, "runtimeAsset.cache.afterPrefab.lastKind");

            if (firstInstance != null)
            {
                Object.DestroyImmediate(firstInstance);
            }

            if (secondInstance != null)
            {
                Object.DestroyImmediate(secondInstance);
            }

            service.ReloadRuntimeAssetCatalog();
            context.AssertEqual(0, service.CachedAssetCount, "runtimeAsset.cache.afterReload.cachedCount");
            context.AssertEqual(0, service.CacheHitCount, "runtimeAsset.cache.afterReload.hits");
            context.AssertEqual(0, service.CacheMissCount, "runtimeAsset.cache.afterReload.misses");
        }

        private static void CheckCatalogSourceFiles(GFDiagnosticScenarioContext context, TotemRuntimeAssetCatalog catalog)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            for (int i = 0; i < catalog.entries.Length; i++)
            {
                var entry = catalog.entries[i];
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, entry.activeAssetPath));
                context.Assert(File.Exists(fullPath), $"{entry.key} active asset should exist: {entry.activeAssetPath}");
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                long size = new FileInfo(fullPath).Length;
                context.Detail($"{entry.key}.activeAssetBytes", size);
                context.Assert(size > 128, $"{entry.key} active asset should not be empty.");

                if (string.Equals(entry.assetKind, "Texture", System.StringComparison.OrdinalIgnoreCase))
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.activeAssetPath);
                    context.Assert(texture != null, $"{entry.key} should load as Texture2D.");
                }
                else if (string.Equals(entry.assetKind, "Sprite", System.StringComparison.OrdinalIgnoreCase))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.activeAssetPath);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.activeAssetPath);
                    context.Assert(sprite != null || texture != null, $"{entry.key} should load as Sprite or Texture2D.");
                    context.Detail($"{entry.key}.spriteLoaded", sprite != null);
                }
            }
        }

        private static void DetailFallbackCounters(GFDiagnosticScenarioContext context, TotemAssetService service, string prefix)
        {
            context.Detail($"{prefix}.missingEntryCount", service.MissingEntryCount);
            context.Detail($"{prefix}.fallbackRequiredCount", service.FallbackRequiredCount);
            context.Detail($"{prefix}.lastFallbackKey", service.LastFallbackKey);
            context.Detail($"{prefix}.lastFallbackReason", service.LastFallbackReason);
        }
    }
}
#endif
