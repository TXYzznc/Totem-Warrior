from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
KB_DIR = ROOT / "项目知识库（AI自行维护）"
WIKI_DIR = KB_DIR / "wiki"
MANIFEST_DIR = WIKI_DIR / "manifests"
ACTIVE_SCRIPTS_DIR = ROOT / "Assets" / "Scripts"
LEGACY_SCRIPTS_DIR = ROOT / "LegacyProjectArchive" / "Assets" / "Scripts"
SCRIPTS_DIR = ACTIVE_SCRIPTS_DIR if ACTIVE_SCRIPTS_DIR.exists() else LEGACY_SCRIPTS_DIR
MODULES_DIR = SCRIPTS_DIR / "Modules"
ACTIVE_DATA_JSON_DIR = ROOT / "Assets" / "Resources" / "DataTable"
LEGACY_DATA_JSON_DIR = ROOT / "LegacyProjectArchive" / "Assets" / "Resources" / "DataTable"
DATA_JSON_DIR = ACTIVE_DATA_JSON_DIR if ACTIVE_DATA_JSON_DIR.exists() else LEGACY_DATA_JSON_DIR
DATA_CS_DIR = SCRIPTS_DIR / "DataTable"
AI_DATATABLE_DIR = ROOT / "GameData" / "AIData" / "DataTables"
DATATABLE_EXCEL_DIR = ROOT / "GameData" / "DataTables"
RESOURCES_DIR = ROOT / "Assets" / "Resources"
GAME_DIR = ROOT / "Assets" / "Game"
OPENSPEC_DIR = ROOT / "openspec"
RUNTIME_ASSET_CATALOG_PATH = ROOT / "GameData" / "AIData" / "GameplayCatalogs" / "totem_runtime_assets.json"
UI_FORM_CONFIG_PATH = ROOT / "GameData" / "AIData" / "DataTables" / "Business" / "UIFormConfig.json"
TODAY = date.today().isoformat()
DIAGNOSTICS_COMMAND = "python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8092"
ART_EXTENSIONS = {
    ".anim",
    ".asset",
    ".controller",
    ".fbx",
    ".jpeg",
    ".jpg",
    ".mat",
    ".mp3",
    ".ogg",
    ".otf",
    ".png",
    ".prefab",
    ".psd",
    ".shader",
    ".tga",
    ".ttf",
    ".wav",
}
UNITY_GUID_PATTERN = re.compile(r"guid:\s*([0-9a-fA-F]{32})")
BUSINESS_SCHEMA_EXTENSION_FIELDS: dict[str, tuple[str, ...]] = {
    "BossPhaseConfig": ("AbilityIds",),
    "BotConfig": (
        "Personality",
        "ReadingTargetWeight",
        "RiskTolerance",
        "ShopPreference",
        "TargetBossWeight",
        "TargetHumanoidAiWeight",
        "TargetPlayerWeight",
        "TargetResourceWeight",
    ),
    "EnemyConfig": (
        "AbilityIds",
        "BehaviorProfileId",
        "FallbackRuntimeAssetKey",
        "LeashRange",
        "RuntimeAssetKey",
        "SpawnCost",
    ),
}
OBSOLETE_ART_PREFIXES = (
    "assets/resources/character/",
    "assets/resources/characters/",
    "assets/resources/environments/",
    "assets/resources/recipes/",
    "assets/resources/tattoo/",
)
PLACEHOLDER_UI_ART_PREFIX = "assets/resources/sprite/ui/"
REVIEW_POLICIES = (
    {
        "review_state": "obsolete",
        "path_prefixes": [
            "Assets/Resources/Character/",
            "Assets/Resources/Characters/",
            "Assets/Resources/Environments/",
            "Assets/Resources/Recipes/",
            "Assets/Resources/Tattoo/",
        ],
        "note": "Confirmed by user on 2026-07-08: these folders are discarded legacy art and must not be selected for new production runtime content.",
    },
    {
        "review_state": "placeholder",
        "path_prefixes": ["Assets/Resources/Sprite/UI/"],
        "note": "Confirmed by user on 2026-07-08: old Sprite/UI art can be used temporarily in this phase but should be regenerated later.",
    },
)


@dataclass(frozen=True)
class ModuleMeta:
    purpose: str
    owner: str
    gdd_module: str | None = None
    gdd_systems: tuple[str, ...] = ()
    datatables: tuple[str, ...] = ()
    resources: tuple[str, ...] = ()
    specs: tuple[str, ...] = ()
    notes: tuple[str, ...] = ()


MODULE_META: dict[str, ModuleMeta] = {
    "Audio": ModuleMeta(
        "音效、BGM、事件驱动的一次性播放与运行时音频桥接。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/13-AudioModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/14-音效与环境音.md",),
        resources=("Assets/Resources/Audio",),
    ),
    "Bot": ModuleMeta(
        "AI 对手控制、Bot 配置、构筑预设与战斗行为入口。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/16-BotControllerModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/12-数值平衡与曲线.md",),
        ("BotConfig", "BotBuildPreset"),
        specs=("openspec/specs/playtest-driver/spec.md",),
    ),
    "Camera": ModuleMeta(
        "2.5D 正交相机、LateUpdate 跟随、边界 clamp、震动整合。",
        "client-unity",
        specs=("openspec/specs/camera-system/spec.md",),
        notes=("依赖 GameTickDriver 的 ILateTickable；避免在 Update/LateUpdate 中分配 GC。",),
    ),
    "Combat": ModuleMeta(
        "战斗意图、命中、伤害、攻击事件与玩家/敌人战斗流程。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/02-CombatModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/02-战斗手感.md",),
        ("ProjectileConfig", "WeaponConfig", "SkillConfig"),
        specs=("openspec/specs/player-attack-system/spec.md",),
    ),
    "DataTable": ModuleMeta(
        "配置表加载、注册表消费、JSON 到强类型表的运行时入口。",
        "client-unity",
        datatables=tuple(),
        notes=(
            "旧表结构源已归档为证据；当前业务数据先改 GameData/AIData/DataTables/Business/*.json，再逆向生成 GameData/DataTables/Business/*.xlsx 和 runtime catalog。",
        ),
    ),
    "Economy": ModuleMeta(
        "货币、资源、商店库存、宝箱奖励与经济消耗。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/11-EconomyModule.md",
        (
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/08-宝箱与探财节奏.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/09-纹身师与商人NPC.md",
        ),
        ("ResourceConfig", "ItemConfig", "ChestConfig", "MerchantConfig", "ShopStockConfig"),
    ),
    "Enemy": ModuleMeta(
        "敌人、Boss、怪物属性、死亡与相关战斗接入。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/08-EnemyModule+BossModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/11-怪物与Boss.md",),
        ("EnemyConfig", "BossPhaseConfig"),
        resources=("Assets/Resources/Prefab/Enemy", "Assets/Resources/Sprite/Characters/Enemies"),
    ),
    "Event": ModuleMeta(
        "三选一事件、事件配置与事件 UI/奖励流程。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/10-EventModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/10-事件与三选一.md",),
        ("EventConfig", "ThreeChoiceOptionConfig"),
    ),
    "Flow": ModuleMeta(
        "流程编排、启动/运行阶段切换和模块间流程上下文。",
        "client-lead",
        specs=("openspec/specs/main-menu-flow/spec.md",),
        notes=("流程层只编排，不承载具体业务规则。",),
    ),
    "GameState": ModuleMeta(
        "游戏状态机、RunStarted/GameOver 等状态转换事件来源。",
        "client-unity",
        specs=("openspec/specs/main-menu-flow/spec.md",),
    ),
    "Input": ModuleMeta(
        "玩家输入、测试输入注入与所有按键入口。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/05-InputModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/05-闪避与身法.md",),
        specs=("openspec/specs/playtest-driver/spec.md",),
        notes=("所有按键输入必须走 TotemInputService / ITotemInputProvider，不允许业务代码直接读 Input。",),
    ),
    "MapGen": ModuleMeta(
        "地图生成/加载、缩圈、地形与交互物布点。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/07-MapGenModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/07-地图生成.md",),
        ("MapTemplateConfig", "ZoneShrinkConfig"),
        specs=(
            "openspec/changes/26-fixed-map-three-themes/specs/map-fixed-terrain/spec.md",
            "openspec/changes/26-fixed-map-three-themes/specs/map-interactive-spawn/spec.md",
        ),
    ),
    "NPC": ModuleMeta(
        "纹身师、商人等 NPC 的生成、交互与 UI 接入。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/09-NPCModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/09-纹身师与商人NPC.md",),
        ("NPCConfig", "MerchantConfig", "ShopStockConfig"),
        resources=("Assets/Resources/Prefab/NPC", "Assets/Resources/Sprite/Characters/NPCs"),
    ),
    "Resource": ModuleMeta(
        "资源定义和轻量资源查询入口。",
        "client-unity",
        datatables=("ResourceConfig",),
        resources=("Assets/Resources",),
    ),
    "Save": ModuleMeta(
        "存档、序列化、运行记录保存与恢复。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/14-SaveModule.md",
        specs=("openspec/specs/workflow/spec.md",),
    ),
    "Scene": ModuleMeta(
        "GF_X Launch 场景入口、场景切换和旧场景证据边界。",
        "client-unity",
        specs=("openspec/specs/main-menu-flow/spec.md",),
        resources=("Assets/Game/Scene", "LegacyProjectArchive/Assets/Scenes"),
    ),
    "Settings": ModuleMeta(
        "设置项、设置 UI 数据接入与运行时选项。",
        "client-unity",
        specs=("openspec/specs/settings/spec.md",),
        resources=("Assets/Resources/Prefab/UI/Settings.prefab",),
    ),
    "Skill": ModuleMeta(
        "主动技能配置、释放、命中效果与技能 UI 数据。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/04-SkillModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/04-主动技能.md",),
        ("SkillConfig",),
        resources=("Assets/Resources/Sprite/Skills",),
    ),
    "Spawner": ModuleMeta(
        "玩家、敌人、Bot、掉落物等运行时生成入口。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/06-SpawnerModule.md",
        datatables=("EnemyConfig", "WeaponDropConfig", "ChestConfig"),
        resources=("Assets/Resources/Prefab",),
    ),
    "Status": ModuleMeta(
        "状态效果、DoT、控制、叠层与状态图标事件来源。",
        "client-unity",
        specs=("openspec/specs/player-attack-system/spec.md",),
    ),
    "Tattoo": ModuleMeta(
        "纹身构筑、部位/颜色/元素/形状、附魔、读条与构筑策略。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/01-TattooModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/01-纹身构筑系统.md",),
        (
            "TattooColorConfig",
            "TattooElementConfig",
            "TattooPartConfig",
            "TattooPatternConfig",
            "TattooShapeConfig",
            "TattooReadingTimeConfig",
            "TattooEnchantAffixConfig",
            "TattooEnchantRecipeConfig",
        ),
        ("Assets/Resources/Sprite/Tattoo",),
        ("openspec/specs/tattoo/spec.md",),
    ),
    "UI": ModuleMeta(
        "UGUI Form、HUD、菜单、运行结果、商店、纹身界面与 UI 数据绑定。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/12-UIModule+各UIForm.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/13-UI与HUD.md",),
        ("UIFormConfig",),
        ("Assets/Resources/Prefab/UI", "Assets/Resources/Sprite/UI"),
        ("openspec/specs/ui-workflow/spec.md", "openspec/specs/core-ui-screens/spec.md"),
        notes=("新 UI 必须走结构先行 6 阶段流程。",),
    ),
    "VFX": ModuleMeta(
        "命中特效、粒子、镜头抖动、战斗视觉反馈。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/15-VFXModule.md",
        specs=("openspec/specs/visual-polish/spec.md",),
        resources=("Assets/Resources/Effect", "Assets/Resources/Sprite/Effects"),
    ),
    "Weapon": ModuleMeta(
        "武器配置、攻击、拾取、升级、特性和弹道接入。",
        "client-unity",
        "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/03-WeaponModule.md",
        ("项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/03-武器系统.md",),
        ("WeaponConfig", "WeaponDropConfig", "WeaponTraitConfig", "ProjectileConfig"),
        ("Assets/Resources/Prefab/Weapon", "Assets/Resources/Sprite/Weapons"),
        ("openspec/specs/weapon-pickup/spec.md", "openspec/specs/player-attack-system/spec.md"),
    ),
}

FEATURE_SLICE_DEFINITIONS: tuple[dict[str, Any], ...] = (
    {
        "id": "ui_entry_flow",
        "name": "主菜单 -> 角色选择 -> 启动选择 -> 战斗 HUD",
        "status": "covered_with_manual_visual_boundary",
        "product_goal": "首屏启动链路干净进入当前 GF_X 工作区，旧 UI 只作为视觉/需求证据。",
        "modules": ["Flow", "Scene", "UI", "Input", "GameState"],
        "business_tables": ["UIFormConfig"],
        "runtime_services": ["TotemGameFlowService", "TotemUIService", "TotemInputService"],
        "ui_forms": ["MainMenu", "CharacterSelect", "StartupSelect", "CombatHUD"],
        "runtime_asset_keys": [
            "ui.character.card.unlocked",
        ],
        "docs": [
            "openspec/specs/main-menu-flow/spec.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/12-UIModule+各UIForm.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/Startup/Launch To Totem Runtime Smoke",
            "Scenario/BusinessRuntime/Totem First Slice UI",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
        ],
        "discipline_handoff": {
            "design": "确认首轮流程状态、按钮语义、角色选择和启动选择的可见规则。",
            "art": "按 runtime_usages 检查角色头像、卡框和 UI 占位图，后续替换仍保持相同 runtime key。",
            "program": "流程只走 TotemGameFlowService/TotemUIService/TotemInputService，不回接旧 GameApp/UIModule。",
            "qa": "用 First Slice UI 和 Causality Smoke 证明状态流、输入流和 HUD 进入点。",
        },
    },
    {
        "id": "first_round_population",
        "name": "首轮 50 名参赛者",
        "status": "covered",
        "product_goal": "复现 1 玩家 + 20 Smart AI + 29 Light AI 的 50 名参赛者规模；NPC 敌人不占参赛者名额。",
        "modules": ["Spawner", "Bot", "Enemy", "Combat", "GameState"],
        "business_tables": ["BotConfig", "BotBuildPreset", "EnemyConfig"],
        "runtime_services": ["TotemActorService", "TotemAIService", "TotemGameFlowService"],
        "ui_forms": ["CombatHUD"],
        "runtime_asset_keys": ["actor.player", "actor.smartAi", "actor.lightAi"],
        "docs": [
            "openspec/changes/gf-x-business-runtime-refactor/REQUIREMENTS_INVENTORY.md",
            "openspec/changes/gf-x-business-runtime-refactor/SMART_AI_PERSONALITY_DRAFT.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem First Round Contract",
            "Scenario/BusinessRuntime/Totem AI Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
        ],
        "discipline_handoff": {
            "design": "维护 Smart/Light 数量、个性分配、参赛者身份和最后存活者胜利规则。",
            "art": "检查 actor.* runtime key 对应的临时参赛者视觉，后续替换保持 key 不漂移。",
            "program": "人口创建、状态和 AI 决策归 TotemActorService/TotemAIService，不挂旧 ModuleRunner。",
            "qa": "用 First Round Contract 和 AI Runtime 证明数量、状态、性格和首轮模型。",
        },
    },
    {
        "id": "combat_weapon_skill",
        "name": "武器、技能、弹道与状态",
        "status": "covered_with_tuning_boundary",
        "product_goal": "GF_X 原生重写攻击、命中、技能、武器冷却、状态概率和伤害结算。",
        "modules": ["Combat", "Weapon", "Skill", "Status", "VFX"],
        "business_tables": ["WeaponConfig", "WeaponTraitConfig", "WeaponDropConfig", "ProjectileConfig", "SkillConfig"],
        "runtime_services": ["TotemCombatService", "TotemCombatRelationshipService", "TotemWeaponService", "TotemSkillService", "TotemStatusService", "TotemVfxService"],
        "ui_forms": ["CombatHUD", "SkillAcquire"],
        "runtime_asset_keys": [
            "weapon.knife_basic",
            "weapon.hammer_heavy",
            "weapon.pistol_basic",
            "weapon.bow_charge",
            "weapon.energy_fist",
            "skill.skill_fireball_01",
            "skill.skill_phase_dash",
            "effect.attack.hit",
            "effect.projectile.bullet_pistol",
            "effect.projectile.arrow_bow",
        ],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/02-CombatModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/03-WeaponModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/04-SkillModule.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Gameplay Runtime",
            "Scenario/BusinessRuntime/Totem Extended Gameplay",
            "Scenario/BusinessRuntime/Totem Balance Envelope",
            "Scenario/BusinessRuntime/Totem VFX Runtime",
        ],
        "discipline_handoff": {
            "design": "数值先改 Business JSON，再同步 xlsx 和 runtime catalog；最终手感仍是 T9f7 后续调试边界。",
            "art": "武器、技能、命中特效通过 runtime asset key 管理；替换前先查 art_assets.runtime_usages。",
            "program": "攻击/技能命中必须通过 TotemCombatService 和关联服务，状态概率用当前 StatusChance helper。",
            "qa": "用 Gameplay Runtime、Extended Gameplay、Balance Envelope 和 VFX Runtime 验证非 UI 效果。",
        },
    },
    {
        "id": "tattoo_builds",
        "name": "336 纹身组合、自纹身与附魔",
        "status": "covered_with_placeholder_art_boundary",
        "product_goal": "复现部位/颜色/元素/图案/形状/读条/附魔的 GF_X 原生构筑链路。",
        "modules": ["Tattoo", "NPC", "Economy", "UI", "Status"],
        "business_tables": [
            "TattooColorConfig",
            "TattooElementConfig",
            "TattooPartConfig",
            "TattooPatternConfig",
            "TattooShapeConfig",
            "TattooReadingTimeConfig",
            "TattooEnchantAffixConfig",
            "TattooEnchantRecipeConfig",
        ],
        "runtime_services": ["TotemTattooService", "TotemEconomyService", "TotemNpcService", "TotemStatusService"],
        "ui_forms": ["SelfTattoo", "TattooEnchant", "TattooStudio", "CombatHUD"],
        "runtime_asset_keys": [],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/01-TattooModule.md",
            "openspec/specs/tattoo/spec.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Gameplay Catalog",
            "Scenario/BusinessRuntime/Totem Gameplay Runtime",
            "Scenario/BusinessRuntime/Totem Extended Gameplay",
        ],
        "discipline_handoff": {
            "design": "维护 336 组合、读条、中断扣费、AfterDodge、StatusChance 与附魔规则。",
            "art": "当前纹身图标/部位/图案多为占位；正式替换时优先保持 tattoo.* runtime key。",
            "program": "自纹身、附魔、触发和状态效果集中在 TotemTattooService，经济扣费通过 TotemEconomyService。",
            "qa": "用 Gameplay Catalog/Runtime/Extended Gameplay 证明组合数、触发、附魔和中断经济。",
        },
    },
    {
        "id": "smart_ai_roster",
        "name": "Smart AI 五性格与 Light AI",
        "status": "covered_with_tuning_boundary",
        "product_goal": "20 Smart AI 采用激进、保守、资源获取、Boss 优先、玩家优先五性格，29 Light AI 保持轻量压力。",
        "modules": ["Bot", "Combat", "Weapon", "Tattoo", "Enemy"],
        "business_tables": ["BotConfig", "BotBuildPreset", "WeaponConfig", "SkillConfig", "TattooPartConfig"],
        "runtime_services": ["TotemAIService", "TotemActorService", "TotemWeaponService", "TotemTattooService"],
        "ui_forms": ["CombatHUD"],
        "runtime_asset_keys": ["actor.smartAi", "actor.lightAi", "weapon.knife_basic", "weapon.pistol_basic"],
        "docs": [
            "openspec/changes/gf-x-business-runtime-refactor/SMART_AI_PERSONALITY_DRAFT.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/16-BotControllerModule.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem AI Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
            "Scenario/BusinessRuntime/Totem Balance Envelope",
        ],
        "discipline_handoff": {
            "design": "调整性格数量、权重、Boss/玩家/资源优先级时先改 BotConfig/BotBuildPreset。",
            "art": "AI 视觉当前复用 actor.* 临时资源，正式角色生产前保持 runtime key 稳定。",
            "program": "AI 行为差异应来自数据和 TotemAIService 决策，不写散落硬编码性格。",
            "qa": "用 AI Runtime、Causality Smoke 和 Balance Envelope 验证决策输出和压力边界。",
        },
    },
    {
        "id": "economy_shop_chest",
        "name": "金币、宝箱、商店与掉落",
        "status": "covered_with_tuning_boundary",
        "product_goal": "复现战斗内经济、死亡宝箱、普通/稀有宝箱、商店库存和购买消耗。",
        "modules": ["Economy", "Spawner", "NPC", "Weapon", "UI"],
        "business_tables": ["ResourceConfig", "ItemConfig", "ChestConfig", "MerchantConfig", "ShopStockConfig", "WeaponDropConfig"],
        "runtime_services": ["TotemEconomyService", "TotemChestService", "TotemNpcService", "TotemWeaponService"],
        "ui_forms": ["Shop", "CombatHUD"],
        "runtime_asset_keys": ["chest.chest_common", "chest.chest_rare", "npc.merchant"],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/11-EconomyModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/08-宝箱与探财节奏.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/09-纹身师与商人NPC.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Gameplay Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
            "Scenario/BusinessRuntime/Totem Runtime Catalog Binding",
        ],
        "discipline_handoff": {
            "design": "经济数值、库存、掉落范围和价格优先改 Business JSON，再同步 xlsx。",
            "art": "宝箱、商人视觉通过 chest.* 和 npc.merchant key 查询和替换。",
            "program": "交易、掉落和宝箱逻辑归 Economy/Chest/Npc 服务，UI 只展示和提交请求。",
            "qa": "用 Gameplay Runtime、Causality Smoke 和 Catalog Binding 验证购买、奖励和表绑定。",
        },
    },
    {
        "id": "npc_interactions",
        "name": "纹身师、商人与交互",
        "status": "covered",
        "product_goal": "NPC 生成、交互、纹身附魔交易、商店入口都走 GF_X 生命周期。",
        "modules": ["NPC", "Tattoo", "Economy", "UI"],
        "business_tables": ["NPCConfig", "MerchantConfig", "ShopStockConfig", "TattooEnchantRecipeConfig", "TattooEnchantAffixConfig"],
        "runtime_services": ["TotemNpcService", "TotemInteractionService", "TotemTattooService", "TotemEconomyService", "TotemUIService"],
        "ui_forms": ["Shop", "TattooEnchant", "TattooStudio"],
        "runtime_asset_keys": ["npc.tattooist", "npc.merchant"],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/09-NPCModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/09-纹身师与商人NPC.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Gameplay Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
        ],
        "discipline_handoff": {
            "design": "确认 NPC 类型、交互范围、交易成本和拒绝原因文案。",
            "art": "NPC 临时视觉由 npc.* key 管理，替换时同步 art_assets 和 runtime catalog。",
            "program": "交互请求进入 TotemNpcService，交易扣费必须经过 Economy 服务。",
            "qa": "用 Runtime/Causality 证明 NPC 生成、商店购买和纹身附魔交易。",
        },
    },
    {
        "id": "three_choice_events",
        "name": "事件与三选一",
        "status": "covered",
        "product_goal": "复现三选一事件选项、奖励、反误触和 UI 数据绑定。",
        "modules": ["Event", "UI", "Economy", "Tattoo", "Weapon"],
        "business_tables": ["EventConfig", "ThreeChoiceOptionConfig", "ItemConfig", "WeaponConfig", "TattooEnchantAffixConfig"],
        "runtime_services": ["TotemChoiceService", "TotemUIService", "TotemEconomyService", "TotemTattooService"],
        "ui_forms": ["ThreeChoice"],
        "runtime_asset_keys": [],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/10-EventModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/10-事件与三选一.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Choice Runtime",
            "Scenario/BusinessRuntime/Totem First Slice UI",
        ],
        "discipline_handoff": {
            "design": "三选一内容、权重、奖励和反误触时间优先由表和 ChoiceService 规则表达。",
            "art": "当前没有专属 runtime key；新增图标或背景时先补 runtime asset catalog 再替换 UI。",
            "program": "选择结算集中在 TotemChoiceService，UI 表单不直接发奖励。",
            "qa": "用 Choice Runtime 和 First Slice UI 验证选项、奖励和反误触窗口。",
        },
    },
    {
        "id": "map_zone",
        "name": "地图主题、地形与缩圈",
        "status": "covered_with_future_map_art_boundary",
        "product_goal": "当前以表驱动地图模板和缩圈伤害，后续地图/地形美术再生产。",
        "modules": ["MapGen", "Spawner", "Camera", "Combat"],
        "business_tables": ["MapTemplateConfig", "ZoneShrinkConfig", "ChestConfig", "NPCConfig"],
        "runtime_services": ["TotemMapService", "TotemZoneService", "TotemCameraService", "TotemActorService"],
        "ui_forms": ["CombatHUD"],
        "runtime_asset_keys": [],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/07-MapGenModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/07-地图生成.md",
            "openspec/changes/26-fixed-map-three-themes/specs/map-fixed-terrain/spec.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Gameplay Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
            "Scenario/BusinessRuntime/Totem Balance Envelope",
        ],
        "discipline_handoff": {
            "design": "地图模板、缩圈阶段、伤害和资源分布从 MapTemplate/ZoneShrink 表维护。",
            "art": "当前 map.* key 是临时地形纹理入口；正式三主题地形需要扩展 runtime asset catalog。",
            "program": "地图、缩圈和相机边界归 Map/Zone/Camera 服务，不在 UI 或 Actor 里散写。",
            "qa": "用 Gameplay Runtime、Causality Smoke 和 Balance Envelope 验证缩圈和地图表绑定。",
        },
    },
    {
        "id": "boss_phase",
        "name": "Boss 阶段与掉落",
        "status": "covered_with_tuning_boundary",
        "product_goal": "Boss 生成、阶段切换、技能、死亡掉落和 AI Boss 优先策略都可被诊断追踪。",
        "modules": ["Enemy", "Bot", "Combat", "Skill", "Economy", "VFX"],
        "business_tables": ["EnemyConfig", "EnemyAbilityConfig", "EncounterSpawnConfig", "EnemyLootConfig", "BossPhaseConfig", "BotConfig"],
        "runtime_services": ["TotemEnemyService", "TotemEnemyWorldService", "TotemEnemyLootService", "TotemAIService", "TotemCombatService", "TotemVfxService"],
        "ui_forms": ["CombatHUD"],
        "runtime_asset_keys": ["enemy.boss_ai_core_zero", "enemy.boss_alien_hive_mother", "enemy.boss_virus_terminus", "enemy.fallback.ai_ruins.boss", "enemy.fallback.alien_hive.boss", "enemy.fallback.virus_swamp.boss"],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/08-EnemyModule+BossModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/11-怪物与Boss.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem AI Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
            "Scenario/BusinessRuntime/Totem Runtime Catalog Binding",
        ],
        "discipline_handoff": {
            "design": "Boss 阶段阈值、倍率、技能、掉落和 BossPriority 权重优先由表维护。",
            "art": "Boss 视觉通过 enemy.<enemyId> 与 enemy.fallback.<theme>.boss key 管理；阶段特效和音频由 BossPhaseConfig cue 驱动。",
            "program": "Boss 是 Enemy 域中的 Boss tier，由 TotemEnemyService 管理阶段、能力和死亡事件，TotemEnemyWorldService 负责世界表现。",
            "qa": "用 AI Runtime、Causality Smoke 和 Catalog Binding 验证阶段、技能和优先策略。",
        },
    },
    {
        "id": "native_enemy_domain",
        "name": "独立 NPC 敌人、遭遇与掉落",
        "status": "covered_with_placeholder_art_boundary",
        "product_goal": "NPC 敌人作为独立怪物域运行，拥有基础 FSM、仇恨索敌、数据驱动能力、Light/Elite/Boss 策略、PCG 遭遇刷新和公开掉落。",
        "modules": ["Enemy", "Combat", "Spawner", "MapGen", "Economy"],
        "business_tables": ["EnemyConfig", "EnemyAbilityConfig", "EncounterSpawnConfig", "EnemyLootConfig", "BossPhaseConfig"],
        "runtime_services": [
            "TotemMatchClockService",
            "TotemCombatRelationshipService",
            "TotemParticipantReadinessService",
            "TotemEnemyWorldService",
            "TotemEnemyService",
            "TotemEnemyLootService",
            "TotemActorService",
            "TotemCombatService"
        ],
        "ui_forms": ["CombatHUD"],
        "runtime_asset_keys": [
            "enemy.boss_ai_core_zero",
            "enemy.boss_alien_hive_mother",
            "enemy.boss_virus_terminus",
            "enemy.fallback.ai_ruins.boss",
            "enemy.fallback.alien_hive.boss",
            "enemy.fallback.virus_swamp.boss"
        ],
        "docs": [
            "openspec/changes/native-enemy-domain-rebuild/design.md",
            "openspec/changes/native-enemy-domain-rebuild/specs/enemy-ai-runtime/spec.md",
            "openspec/changes/native-enemy-domain-rebuild/specs/enemy-encounter-spawning/spec.md",
            "openspec/changes/native-enemy-domain-rebuild/specs/enemy-loot-progression/spec.md"
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Enemy Pure Logic",
            "Scenario/BusinessRuntime/Totem Enemy Domain Runtime",
            "Scenario/BusinessRuntime/Totem Enemy Status Fast"
        ],
        "discipline_handoff": {
            "design": "敌人身份、能力、遭遇预算、掉落和 Boss 阶段从 Business JSON 维护；参赛者与怪物不可再次合并。",
            "art": "当前敌人使用 enemy.* 与 enemy.fallback.* 占位资源；正式替换保持 runtime key 和单敌人生命周期稳定。",
            "program": "EnemyService 负责怪物逻辑，EnemyWorldService 负责世界/表现桥接，EnemyLootService 负责公开掉落；所有伤害先过关系服务。",
            "qa": "用 Enemy Pure Logic、Enemy Domain Runtime、Enemy Status 和 PlayMode smoke 验证 FSM、能力、刷新、掉落、清理与性能。"
        },
    },
    {
        "id": "audio_vfx_feedback",
        "name": "战斗反馈、VFX 与音频",
        "status": "covered_with_external_audio_noise_boundary",
        "product_goal": "命中特效、伤害飘字、Boss/技能反馈和音频 cue 都能通过 GF_X 诊断定位。",
        "modules": ["VFX", "Audio", "Combat", "Skill", "Enemy"],
        "business_tables": ["SkillConfig", "BossPhaseConfig"],
        "runtime_services": ["TotemVfxService", "TotemAudioService", "TotemCombatService", "TotemEnemyService", "TotemEnemyWorldService"],
        "ui_forms": ["CombatHUD"],
        "runtime_asset_keys": [
            "effect.attack.hit",
            "effect.projectile.bullet_pistol",
            "effect.projectile.arrow_bow",
            "effect.skill.burst",
            "effect.boss.bolt",
        ],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/15-VFXModule.md",
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/13-AudioModule.md",
            ".claude/skills/playtest-driver/SKILL.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem VFX Runtime",
            "Scenario/BusinessRuntime/Totem Audio Runtime",
            "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
        ],
        "discipline_handoff": {
            "design": "反馈节奏和音频触发先用表/服务事件表达，最终手感留给后续 playtest 调整。",
            "art": "VFX 占位资源通过 effect.* key 管理；替换时检查残留清理和 runtime_usages。",
            "program": "临时对象必须归 TotemVfxService 生命周期管理，音频噪声按 ExternalAudioDeviceNoise 分类。",
            "qa": "用 VFX Runtime、Audio Runtime、Causality Smoke 和 Console 过滤验证反馈链路。",
        },
    },
    {
        "id": "save_meta_settings",
        "name": "新存档、Meta 与设置",
        "status": "covered_new_save_only",
        "product_goal": "不兼容旧存档；当前只维护 GF_X 新运行记录、Meta 状态和设置入口。",
        "modules": ["Save", "Settings", "GameState", "UI"],
        "business_tables": ["UIFormConfig"],
        "runtime_services": ["TotemRunStatsService", "TotemSettingsService", "TotemMetaProgressService", "TotemUIService"],
        "ui_forms": ["Settings", "RunResult"],
        "runtime_asset_keys": [],
        "docs": [
            "项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/14-SaveModule.md",
            "openspec/specs/settings/spec.md",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Meta Progress",
            "Scenario/BusinessRuntime/Totem First Slice UI",
        ],
        "discipline_handoff": {
            "design": "旧存档不兼容是已确认边界；新增持久化字段前先定义新存档语义。",
            "art": "Settings/RunResult 仍使用当前 UI 占位资源，正式 UI 生产时补 runtime key。",
            "program": "存档和设置写入由对应服务集中处理，UI 只提交命令。",
            "qa": "用 Meta Progress 和 First Slice UI 验证新存档/设置状态，不测旧存档迁移。",
        },
    },
    {
        "id": "data_resource_contracts",
        "name": "业务数据与资源生命周期契约",
        "status": "covered",
        "product_goal": "把旧 DataTable/Resource 模块的职责收束到 AI 友好 Business 表、runtime catalog、资源索引和 GF_X 服务生命周期里。",
        "modules": ["DataTable", "Resource"],
        "business_tables": [
            "BossPhaseConfig",
            "BotBuildPreset",
            "BotConfig",
            "ChestConfig",
            "EnemyConfig",
            "EventConfig",
            "ItemConfig",
            "MapTemplateConfig",
            "MerchantConfig",
            "NPCConfig",
            "ProjectileConfig",
            "ResourceConfig",
            "ShopStockConfig",
            "SkillConfig",
            "TattooColorConfig",
            "TattooElementConfig",
            "TattooEnchantAffixConfig",
            "TattooEnchantRecipeConfig",
            "TattooPartConfig",
            "TattooPatternConfig",
            "TattooReadingTimeConfig",
            "TattooShapeConfig",
            "ThreeChoiceOptionConfig",
            "UIFormConfig",
            "WeaponConfig",
            "WeaponDropConfig",
            "WeaponTraitConfig",
            "ZoneShrinkConfig",
        ],
        "runtime_services": ["TotemDataService", "TotemAssetService"],
        "ui_forms": [],
        "runtime_asset_keys": [
            "weapon.knife_basic",
        ],
        "docs": [
            "openspec/changes/gf-x-business-runtime-refactor/DATATABLE_MIGRATION_MANIFEST.md",
            "openspec/changes/gf-x-business-runtime-refactor/GAMEPLAY_RUNTIME_SLICE.md",
            "项目知识库（AI自行维护）/wiki/manifests/datatables.json",
            "项目知识库（AI自行维护）/wiki/manifests/art_assets.json",
        ],
        "diagnostic_scenarios": [
            "Scenario/BusinessRuntime/Totem Gameplay Catalog",
            "Scenario/BusinessRuntime/Totem Runtime Catalog Binding",
            "Scenario/BusinessRuntime/Totem Runtime Assets",
            "Scenario/BusinessRuntime/GF_X Rewrite Inventory Contract",
        ],
        "discipline_handoff": {
            "design": "业务配置先改 Business JSON，策划可读 xlsx 由逆向导表同步，runtime catalog 由生成器产出。",
            "art": "资源使用先查 art_assets.runtime_usages，替换资源时保持或同步 runtime asset key。",
            "program": "数据入口归 TotemDataService，资源入口归 TotemAssetService，不恢复旧 DataTableModule/ResourceModule。",
            "qa": "用 Gameplay Catalog、Runtime Catalog Binding、Runtime Assets 和 Rewrite Inventory 证明表、资源和索引没有漂移。",
        },
    },
)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def exists_rel(path: str | None) -> bool:
    if not path:
        return False
    return (ROOT / path).exists()


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def json_dump(data: Any) -> str:
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def count_files(path: Path, patterns: tuple[str, ...]) -> int:
    if not path.exists():
        return 0
    total = 0
    for pattern in patterns:
        total += sum(1 for p in path.rglob(pattern) if p.is_file())
    return total


def registry_entries() -> dict[str, str]:
    registry_path = DATA_CS_DIR / "DataTableRegistry.cs"
    if not registry_path.exists():
        return {}
    text = read_text(registry_path)
    entries: dict[str, str] = {}
    for file_name, type_name in re.findall(r'new DataTableEntry\("([^"]+)",\s*typeof\(([^)]+)\)\)', text):
        entries[file_name] = type_name
    return entries


def business_table_schema_bridge(table_name: str, legacy_fields: list[dict[str, Any]]) -> dict[str, Any]:
    business_path = AI_DATATABLE_DIR / "Business" / f"{table_name}.json"
    legacy_field_names = [str(field.get("name") or "") for field in legacy_fields if field.get("name")]
    if not business_path.exists():
        return {
            "business_json": rel(business_path),
            "business_exists": False,
            "legacy_field_count": len(legacy_field_names),
            "business_column_count": 0,
            "business_data_field_count": 0,
            "business_row_count": 0,
            "id_column_role": None,
            "legacy_fields_preserved_count": 0,
            "missing_legacy_fields": legacy_field_names,
            "added_business_fields": [],
            "valid": False,
        }

    raw = json.loads(read_text(business_path))
    columns = raw.get("columns", [])
    rows = raw.get("rows", [])
    business_field_names = [
        str(column.get("key") or "")
        for column in columns
        if column.get("key") and column.get("role") != "comment"
    ]
    business_data_field_names = [
        str(column.get("key") or "")
        for column in columns
        if column.get("key") and column.get("role") == "data"
    ]
    id_column_role = next(
        (
            str(column.get("role") or "")
            for column in columns
            if column.get("key") == "Id"
        ),
        None,
    )
    missing_legacy_fields = sorted(field for field in legacy_field_names if field not in business_field_names)
    expected_extension_fields = BUSINESS_SCHEMA_EXTENSION_FIELDS.get(table_name, ())
    added_business_fields = sorted(
        field
        for field in expected_extension_fields
        if field in business_data_field_names
    )

    return {
        "business_json": rel(business_path),
        "business_exists": True,
        "legacy_field_count": len(legacy_field_names),
        "business_column_count": len(business_field_names),
        "business_data_field_count": len(business_data_field_names),
        "business_row_count": len(rows),
        "id_column_role": id_column_role,
        "legacy_fields_preserved_count": len(legacy_field_names) - len(missing_legacy_fields),
        "missing_legacy_fields": missing_legacy_fields,
        "added_business_fields": added_business_fields,
        "valid": not missing_legacy_fields,
    }


def business_table_field_map(table_name: str) -> dict[str, dict[str, str]]:
    business_path = AI_DATATABLE_DIR / "Business" / f"{table_name}.json"
    if not business_path.exists():
        return {}

    raw = json.loads(read_text(business_path))
    columns = raw.get("columns", [])
    result: dict[str, dict[str, str]] = {}
    for column in columns:
        if not isinstance(column, dict) or column.get("role") == "comment":
            continue

        key = str(column.get("key") or "")
        if not key:
            continue

        result[key] = {
            "type": str(column.get("type") or ""),
            "desc": str(column.get("comment") or ""),
        }

    return result


def build_datatables_manifest() -> dict[str, Any]:
    entries = registry_entries()
    tables: list[dict[str, Any]] = []
    warnings: list[str] = []
    non_standard_primary_keys: list[str] = []
    schema_bridge_missing_fields: dict[str, list[str]] = {}
    schema_bridge_added_fields: dict[str, list[str]] = {}

    for json_path in sorted(DATA_JSON_DIR.glob("*.json")):
        raw = json.loads(read_text(json_path))
        table = raw.get("table")
        fields = raw.get("fields", [])
        rows = raw.get("rows", [])
        file_name = json_path.stem
        csharp_type = entries.get(file_name)
        csharp_path = DATA_CS_DIR / f"{csharp_type or file_name}.cs"
        primary_field = fields[0] if fields else None
        schema_bridge = business_table_schema_bridge(file_name, fields)
        business_fields = business_table_field_map(file_name)
        if schema_bridge["missing_legacy_fields"]:
            schema_bridge_missing_fields[file_name] = list(schema_bridge["missing_legacy_fields"])
        if schema_bridge["added_business_fields"]:
            schema_bridge_added_fields[file_name] = list(schema_bridge["added_business_fields"])

        if table != file_name:
            warnings.append(f"{rel(json_path)} table 字段为 {table!r}，与文件名不一致")
        if not fields:
            warnings.append(f"{rel(json_path)} 缺少 fields 定义")
        elif primary_field.get("name") != "Id" or primary_field.get("type") != "int":
            non_standard_primary_keys.append(f"{file_name}.{primary_field.get('name')}:{primary_field.get('type')}")
        if file_name not in entries:
            warnings.append(f"{rel(json_path)} 未出现在 DataTableRegistry")
        if csharp_type and not csharp_path.exists():
            warnings.append(f"{file_name} 注册到 {csharp_type}，但缺少 {rel(csharp_path)}")

        tables.append(
            {
                "name": file_name,
                "json": rel(json_path),
                "table_field": table,
                "registered": file_name in entries,
                "csharp_type": csharp_type,
                "csharp": rel(csharp_path) if csharp_path.exists() else None,
                "primary_field": {
                    "name": primary_field.get("name"),
                    "type": business_fields.get(primary_field.get("name"), {}).get("type") or primary_field.get("type"),
                    "desc": business_fields.get(primary_field.get("name"), {}).get("desc") or primary_field.get("desc"),
                }
                if primary_field
                else None,
                "uses_standard_id_primary_key": bool(
                    primary_field and primary_field.get("name") == "Id" and primary_field.get("type") == "int"
                ),
                "field_count": len(fields),
                "row_count": len(rows),
                "business_schema_bridge": schema_bridge,
                "fields": [
                    {
                        "name": field.get("name"),
                        "type": business_fields.get(field.get("name"), {}).get("type") or field.get("type"),
                        "desc": business_fields.get(field.get("name"), {}).get("desc") or field.get("desc"),
                        "desc_source": "Business AI DataTable" if field.get("name") in business_fields else "legacy evidence",
                    }
                    for field in fields
                ],
            }
        )

    json_names = {p.stem for p in DATA_JSON_DIR.glob("*.json")}
    for file_name in sorted(set(entries) - json_names):
        warnings.append(f"DataTableRegistry 注册了 {file_name}，但缺少对应 JSON")
    if non_standard_primary_keys:
        warnings.append(
            "旧版证据表使用业务主键而不是 Id:int；当前 Business AI DataTables 已补 GF_X Id:int。只有重跑旧生成器或改旧证据表时才需要处理这些主键: "
            + ", ".join(non_standard_primary_keys)
        )

    business_ai_jsons = sorted((AI_DATATABLE_DIR / "Business").glob("*.json"))
    business_xlsx_files = sorted((DATATABLE_EXCEL_DIR / "Business").glob("*.xlsx"))
    legacy_names = {table["name"] for table in tables}
    business_ai_names = {path.stem for path in business_ai_jsons}
    business_xlsx_names = {path.stem for path in business_xlsx_files}

    missing_business_ai_json = sorted(legacy_names - business_ai_names)
    missing_business_xlsx = sorted(legacy_names - business_xlsx_names)
    if missing_business_ai_json:
        warnings.append("Missing Business AI DataTable JSON: " + ", ".join(missing_business_ai_json))
    if missing_business_xlsx:
        warnings.append("Missing Business DataTable xlsx: " + ", ".join(missing_business_xlsx))

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "registry": rel(DATA_CS_DIR / "DataTableRegistry.cs"),
        "count": len(tables),
        "ai_datatable_bridge": {
            "target_workflow": "28 legacy business tables -> GameData/AIData/DataTables/Business JSON -> GameData/DataTables/Business xlsx -> GF_X DataTable export",
            "legacy_business_count": len(tables),
            "business_ai_json_count": len(business_ai_jsons),
            "business_xlsx_count": len(business_xlsx_files),
            "schema_bridge_table_count": len(tables),
            "schema_bridge_valid_table_count": sum(1 for table in tables if table["business_schema_bridge"]["valid"]),
            "schema_bridge_missing_legacy_field_table_count": len(schema_bridge_missing_fields),
            "schema_bridge_added_business_field_table_count": len(schema_bridge_added_fields),
            "schema_bridge_missing_legacy_fields": schema_bridge_missing_fields,
            "schema_bridge_added_business_fields": schema_bridge_added_fields,
            "business_ai_jsons": [rel(path) for path in business_ai_jsons],
            "business_xlsx": [rel(path) for path in business_xlsx_files],
            "missing_business_ai_json": missing_business_ai_json,
            "missing_business_xlsx": missing_business_xlsx,
        },
        "tables": tables,
        "warnings": warnings,
    }


def build_modules_manifest(datatable_names: set[str]) -> dict[str, Any]:
    modules: list[dict[str, Any]] = []
    warnings: list[str] = []

    for module_dir in sorted(p for p in MODULES_DIR.iterdir() if p.is_dir()):
        name = module_dir.name
        meta = MODULE_META.get(name)
        cs_files = sorted(p for p in module_dir.rglob("*.cs") if p.is_file())
        module_datatables = list(meta.datatables if meta else ())
        missing_tables = [name for name in module_datatables if name not in datatable_names]
        if missing_tables:
            warnings.append(f"{name} MODULE_META 引用了不存在的配置表: {', '.join(missing_tables)}")
        if meta is None:
            warnings.append(f"{name} 缺少 MODULE_META 描述")

        modules.append(
            {
                "name": name,
                "path": rel(module_dir),
                "owner": meta.owner if meta else "client-unity",
                "purpose": meta.purpose if meta else "未登记职责，请补 MODULE_META。",
                "script_count": len(cs_files),
                "main_scripts": [rel(p) for p in cs_files if p.parent == module_dir],
                "module_card": rel(module_dir / "MODULE.md"),
                "gdd_module": meta.gdd_module if meta else None,
                "gdd_module_exists": exists_rel(meta.gdd_module) if meta else False,
                "gdd_systems": list(meta.gdd_systems if meta else ()),
                "datatables": module_datatables,
                "resources": list(meta.resources if meta else ()),
                "specs": list(meta.specs if meta else ()),
                "notes": list(meta.notes if meta else ()),
            }
        )

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "module_root": rel(MODULES_DIR),
        "count": len(modules),
        "modules": modules,
        "warnings": warnings,
    }


def bucket_asset_counts(base: Path) -> dict[str, Any]:
    if not base.exists():
        return {"path": rel(base), "exists": False, "files": 0, "children": []}

    children: list[dict[str, Any]] = []
    for child in sorted(p for p in base.iterdir() if p.is_dir()):
        children.append(
            {
                "name": child.name,
                "path": rel(child),
                "files": count_files(child, ("*.*",)),
                "png": count_files(child, ("*.png",)),
                "prefab": count_files(child, ("*.prefab",)),
                "json": count_files(child, ("*.json",)),
                "asset": count_files(child, ("*.asset",)),
            }
        )
    return {
        "path": rel(base),
        "exists": True,
        "files": count_files(base, ("*.*",)),
        "children": children,
    }


def build_assets_manifest() -> dict[str, Any]:
    buckets = []
    for name in ("DataTable", "Prefab", "Sprite", "Audio", "Anim", "Animation", "Effect", "Font", "Material", "Model", "Texture"):
        bucket_path = DATA_JSON_DIR if name == "DataTable" else RESOURCES_DIR / name
        buckets.append(bucket_asset_counts(bucket_path))

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "root": rel(RESOURCES_DIR),
        "buckets": buckets,
    }


def art_asset_type(path: Path) -> str:
    suffix = path.suffix.lower()
    if suffix == ".prefab":
        return "prefab"
    if suffix in {".png", ".jpg", ".jpeg", ".tga", ".psd"}:
        return "sprite_or_texture"
    if suffix == ".fbx":
        return "model"
    if suffix in {".anim", ".controller"}:
        return "animation"
    if suffix in {".mat", ".shader"}:
        return "material_or_shader"
    if suffix in {".wav", ".mp3", ".ogg"}:
        return "audio"
    if suffix in {".ttf", ".otf"}:
        return "font"
    return "unity_asset"


def infer_art_system(path: Path) -> str:
    normalized = rel(path).lower()
    if "/examples/" in normalized:
        return "GF_XExample"
    if "/scriptsbuiltin/editor/" in normalized:
        return "GF_XCore"
    if normalized in {
        "assets/resources/appsettings.asset",
        "assets/resources/dotweensettings.asset",
        "assets/resources/newtonsoft.json-for-unity.converters.asset",
        "assets/game/dotweensettings.asset",
        "assets/game/newtonsoft.json-for-unity.converters.asset",
    }:
        return "GF_XCore"
    if "/sprite/pcg/" in normalized:
        return "PCGMap"
    if "/prefab/ui/" in normalized or "/prefabs/ui/" in normalized or "/sprites/ui/" in normalized or "/sprite/ui/" in normalized:
        return "UI"
    if "/prefabs/entity/actors/" in normalized or "/sprite/actors/" in normalized or "/sprite/npc/" in normalized or "/character" in normalized or "/characters/" in normalized or "/player" in normalized or "/boss" in normalized:
        return "Character"
    if "/weapon" in normalized or "/weapons/" in normalized or "/projectile" in normalized:
        return "Weapon"
    if "/sprite/skills/" in normalized:
        return "Skill"
    if "/sprite/tattoo/" in normalized or "/sprite/paints/" in normalized or "/sprite/affixes/" in normalized or "/sprite/recipes/" in normalized:
        return "Tattoo"
    if "/sprite/pcg/" in normalized or "/sprite/environments/" in normalized or "/environment/" in normalized or "/environments/" in normalized:
        return "Map"
    if "/sprite/items/" in normalized or "/sprite/consumables/" in normalized:
        return "Economy"
    if "/effect" in normalized or "/effects/" in normalized or "/vfx" in normalized:
        return "VFX"
    if "/audio/" in normalized:
        return "Audio"
    if "/font/" in normalized:
        return "Font"
    if "/model" in normalized or "/models/" in normalized:
        return "Model"
    if "/shader/" in normalized or "/material" in normalized or "/materials/" in normalized or "/texture" in normalized or "/textures/" in normalized:
        return "Material"
    if "/anim/" in normalized or "/animation/" in normalized:
        return "Animation"
    if "/prefabs/core/" in normalized or "/scriptableassets/core/" in normalized:
        return "GF_XCore"
    return "Unclassified"


def infer_art_role(path: Path, system: str, asset_type: str) -> str:
    name = path.stem.lower()
    if system == "GF_XExample":
        return "framework example/reference asset, not part of clean startup runtime"
    if system == "GF_XCore":
        return "GF_X core/runtime support asset"
    if system == "PCGMap":
        return "PCG terrain, object, POI, route or overlay visual selected by the PCG catalogs"
    if system == "UI":
        if asset_type == "prefab":
            return "UI screen/form prefab candidate for GF_X UIForm rewrite"
        return "UI visual element or font/material used by UI"
    if system == "Character":
        if "boss" in name:
            return "boss character visual/animation resource"
        if "player" in name:
            return "player character visual/animation resource"
        return "character visual/animation resource"
    if system == "Weapon":
        if "projectile" in name or "bullet" in name:
            return "projectile visual/runtime prefab resource"
        return "weapon visual/runtime prefab resource"
    if system == "Skill":
        return "skill icon/effect visual resource"
    if system == "Tattoo":
        if "affix" in name:
            return "tattoo enchant affix icon resource"
        if "paint" in name:
            return "tattoo paint/color resource"
        if "recipe" in name:
            return "tattoo recipe/pattern scroll resource"
        return "tattoo part, pattern, color or enchant visual resource"
    if system == "Map":
        return "map/environment tile, wall, floor or landmark visual resource"
    if system == "Economy":
        return "item, consumable, chest or economy UI visual resource"
    if system == "VFX":
        return "combat or feedback effect resource"
    if system == "Audio":
        return "audio cue resource"
    if system == "Font":
        return "font or TMP font asset"
    if system in {"Model", "Animation", "Material"}:
        return f"{system.lower()} support resource"
    return "art/runtime resource pending manual classification"


def lifecycle_policy(path: Path, system: str) -> str:
    review_state, _, review_lifecycle = art_review_state(path)
    if review_state and review_lifecycle:
        return review_lifecycle

    if system == "PCGMap":
        return "runtime-bound through PCG catalogs and the GF_X PCG map lifecycle; do not require one runtime asset key per source image"

    normalized = rel(path).lower()
    if "/examples/" in normalized:
        return "keep isolated as GF_X example/reference; do not include in clean startup flow"
    if normalized.startswith("assets/resources/"):
        return "reuse asset content, rewrite loading and lifecycle through GF_X"
    if normalized.startswith("assets/game/"):
        return "GF_X project/framework asset; keep under GF_X lifecycle"
    return "pending"


def art_review_state(path: Path) -> tuple[str | None, str | None, str | None]:
    normalized = rel(path).lower()
    if any(normalized.startswith(prefix) for prefix in OBSOLETE_ART_PREFIXES):
        return (
            "obsolete",
            "User confirmed this folder is obsolete and should not be reused as production art.",
            "obsolete; do not use for new GF_X runtime content; keep only as archived/reference material if present",
        )

    if normalized.startswith(PLACEHOLDER_UI_ART_PREFIX):
        return (
            "placeholder",
            "User confirmed Sprite/UI is temporary placeholder art for this phase and should be regenerated later.",
            "temporary placeholder UI art; usable during current GF_X rewrite, regenerate or replace before production polish",
        )

    return None, None, None


def resource_key(path: Path) -> str | None:
    if not path.is_relative_to(RESOURCES_DIR):
        return None
    without_ext = path.relative_to(RESOURCES_DIR).with_suffix("")
    return without_ext.as_posix()


def normalize_project_path(value: Any) -> str:
    if not isinstance(value, str):
        return ""
    return value.strip().replace("\\", "/")


def runtime_usage_record(entry: dict[str, Any], source: str) -> dict[str, Any]:
    return {
        "key": str(entry.get("key") or ""),
        "asset_kind": str(entry.get("assetKind") or ""),
        "role": str(entry.get("role") or ""),
        "source": source,
        "load_mode": str(entry.get("loadMode") or ""),
        "fallback_primitive": str(entry.get("fallbackPrimitive") or ""),
        "notes": str(entry.get("notes") or ""),
    }


def build_unity_guid_index(files: list[Path]) -> dict[str, Path]:
    """Map Unity .meta GUIDs to art assets so prefab/controller dependencies stay traceable."""
    guid_index: dict[str, Path] = {}
    for path in files:
        meta_path = path.with_name(f"{path.name}.meta")
        if not meta_path.exists():
            continue
        try:
            match = UNITY_GUID_PATTERN.search(read_text(meta_path))
        except (OSError, UnicodeDecodeError):
            continue
        if match:
            guid_index.setdefault(match.group(1).lower(), path)
    return guid_index


def unity_referenced_art_paths(path: Path, guid_index: dict[str, Path]) -> list[Path]:
    """Return indexed art assets referenced by a text-serialized Unity asset."""
    if path.suffix.lower() not in {".prefab", ".controller", ".anim", ".asset", ".mat"}:
        return []
    try:
        guids = UNITY_GUID_PATTERN.findall(read_text(path))
    except (OSError, UnicodeDecodeError):
        return []
    return [guid_index[guid.lower()] for guid in guids if guid.lower() in guid_index]


def load_runtime_asset_catalog_entries() -> tuple[dict[str, Any], list[dict[str, Any]]]:
    summary: dict[str, Any] = {
        "path": rel(RUNTIME_ASSET_CATALOG_PATH),
        "exists": RUNTIME_ASSET_CATALOG_PATH.exists(),
        "schema_version": None,
        "source": None,
        "entry_count": 0,
        "parse_error": "",
    }
    if not RUNTIME_ASSET_CATALOG_PATH.exists():
        return summary, []

    try:
        raw = json.loads(read_text(RUNTIME_ASSET_CATALOG_PATH))
    except json.JSONDecodeError as exc:
        summary["parse_error"] = str(exc)
        return summary, []

    entries = raw.get("entries", [])
    if not isinstance(entries, list):
        summary["parse_error"] = "entries is not a list"
        return summary, []

    typed_entries = [entry for entry in entries if isinstance(entry, dict)]
    summary.update(
        {
            "schema_version": raw.get("schemaVersion"),
            "source": raw.get("source"),
            "entry_count": len(typed_entries),
        }
    )
    return summary, typed_entries


def build_runtime_asset_usage_index(
    files: list[Path],
) -> tuple[dict[str, list[dict[str, Any]]], dict[str, Any]]:
    summary, entries = load_runtime_asset_catalog_entries()
    indexed_paths = {rel(path).lower() for path in files}
    indexed_path_map = {rel(path).lower(): path for path in files}
    guid_index = build_unity_guid_index(files)
    usage_index: dict[str, list[dict[str, Any]]] = {}
    active_usage_count = 0
    legacy_source_usage_count = 0
    indexed_active_usage_count = 0
    indexed_legacy_source_usage_count = 0
    active_asset_paths: set[str] = set()
    legacy_source_paths: set[str] = set()
    missing_active_asset_paths: set[str] = set()
    missing_legacy_source_paths: set[str] = set()
    dependency_usage_count = 0

    def add_usage(path_key: str, entry: dict[str, Any], source: str) -> None:
        nonlocal dependency_usage_count
        usage_index.setdefault(path_key, []).append(runtime_usage_record(entry, source))
        if source.endswith("Dependency"):
            dependency_usage_count += 1

    def add_dependency_usages(root_key: str, entry: dict[str, Any], source: str) -> None:
        root = indexed_path_map.get(root_key)
        if root is None:
            return
        pending = [root]
        visited = {root_key}
        while pending:
            current = pending.pop()
            for dependency in unity_referenced_art_paths(current, guid_index):
                dependency_key = rel(dependency).lower()
                if dependency_key in visited:
                    continue
                visited.add(dependency_key)
                add_usage(dependency_key, entry, source)
                pending.append(dependency)

    for entry in entries:
        active_path = normalize_project_path(entry.get("activeAssetPath"))
        legacy_path = normalize_project_path(entry.get("legacySourcePath"))

        if active_path:
            active_usage_count += 1
            active_asset_paths.add(active_path)
            active_key = active_path.lower()
            if active_key in indexed_paths:
                indexed_active_usage_count += 1
                add_usage(active_key, entry, "activeAssetPath")
                add_dependency_usages(active_key, entry, "activeAssetPathDependency")
            else:
                missing_active_asset_paths.add(active_path)

        if legacy_path:
            legacy_source_usage_count += 1
            legacy_source_paths.add(legacy_path)
            legacy_key = legacy_path.lower()
            if legacy_key in indexed_paths:
                indexed_legacy_source_usage_count += 1
                add_usage(legacy_key, entry, "legacySourcePath")
                add_dependency_usages(legacy_key, entry, "legacySourcePathDependency")
            else:
                missing_legacy_source_paths.add(legacy_path)

    summary.update(
        {
            "active_usage_count": active_usage_count,
            "legacy_source_usage_count": legacy_source_usage_count,
            "indexed_active_usage_count": indexed_active_usage_count,
            "indexed_legacy_source_usage_count": indexed_legacy_source_usage_count,
            "active_asset_path_count": len(active_asset_paths),
            "legacy_source_path_count": len(legacy_source_paths),
            "missing_active_asset_paths": sorted(missing_active_asset_paths),
            "missing_legacy_source_paths": sorted(missing_legacy_source_paths),
            "dependency_usage_count": dependency_usage_count,
        }
    )
    return usage_index, summary


def int_or_none(value: Any) -> int | None:
    try:
        return int(str(value).strip())
    except (TypeError, ValueError):
        return None


def bool_or_none(value: Any) -> bool | None:
    if isinstance(value, bool):
        return value
    text = str(value).strip().lower()
    if text in {"true", "1", "yes", "y"}:
        return True
    if text in {"false", "0", "no", "n"}:
        return False
    return None


def ui_form_asset_path(prefab_path: str) -> str:
    normalized = normalize_project_path(prefab_path).strip("/")
    if not normalized:
        return ""
    if normalized.lower().endswith(".prefab"):
        asset_path = GAME_DIR / "Prefabs" / normalized
    else:
        asset_path = GAME_DIR / "Prefabs" / f"{normalized}.prefab"
    return rel(asset_path)


def ui_form_usage_record(row: dict[str, Any], values: dict[str, Any], asset_path: str) -> dict[str, Any]:
    return {
        "id": str(values.get("Id") or ""),
        "form_name": str(values.get("FormName") or ""),
        "prefab_path": normalize_project_path(values.get("PrefabPath")),
        "asset_path": asset_path,
        "sort_order": int_or_none(values.get("SortOrder")),
        "is_exclusive": bool_or_none(values.get("IsExclusive")),
        "source": rel(UI_FORM_CONFIG_PATH),
        "row": row.get("row"),
        "runtime_chain": "UIViews -> UITable.UIPrefab -> UtilityBuiltin.AssetsPath.GetUIFormPath -> UIComponent.OpenUIForm",
    }


def load_ui_form_config_rows() -> tuple[dict[str, Any], list[dict[str, Any]]]:
    summary: dict[str, Any] = {
        "path": rel(UI_FORM_CONFIG_PATH),
        "exists": UI_FORM_CONFIG_PATH.exists(),
        "schema_version": None,
        "table_name": None,
        "row_count": 0,
        "enabled_row_count": 0,
        "parse_error": "",
        "runtime_chain": "UIViews -> UITable.UIPrefab -> UtilityBuiltin.AssetsPath.GetUIFormPath -> UIComponent.OpenUIForm",
    }
    if not UI_FORM_CONFIG_PATH.exists():
        return summary, []

    try:
        raw = json.loads(read_text(UI_FORM_CONFIG_PATH))
    except json.JSONDecodeError as exc:
        summary["parse_error"] = str(exc)
        return summary, []

    rows = raw.get("rows", [])
    if not isinstance(rows, list):
        summary["parse_error"] = "rows is not a list"
        return summary, []

    typed_rows = [row for row in rows if isinstance(row, dict)]
    enabled_rows = [row for row in typed_rows if row.get("enabled", True)]
    summary.update(
        {
            "schema_version": raw.get("schemaVersion"),
            "table_name": raw.get("tableName"),
            "row_count": len(typed_rows),
            "enabled_row_count": len(enabled_rows),
        }
    )
    return summary, enabled_rows


def build_ui_form_usage_index(
    indexed_paths: set[str],
) -> tuple[dict[str, list[dict[str, Any]]], dict[str, Any]]:
    summary, rows = load_ui_form_config_rows()
    usage_index: dict[str, list[dict[str, Any]]] = {}
    prefab_paths: set[str] = set()
    active_asset_paths: set[str] = set()
    indexed_asset_paths: set[str] = set()
    missing_active_asset_paths: set[str] = set()
    indexed_usage_count = 0

    for row in rows:
        values = row.get("values", {})
        if not isinstance(values, dict):
            continue

        prefab_path = normalize_project_path(values.get("PrefabPath"))
        asset_path = ui_form_asset_path(prefab_path)
        if not prefab_path or not asset_path:
            continue

        prefab_paths.add(prefab_path)
        active_asset_paths.add(asset_path)
        active_key = asset_path.lower()
        if active_key in indexed_paths:
            indexed_usage_count += 1
            indexed_asset_paths.add(asset_path)
            usage_index.setdefault(active_key, []).append(ui_form_usage_record(row, values, asset_path))
        else:
            missing_active_asset_paths.add(asset_path)

    summary.update(
        {
            "prefab_path_count": len(prefab_paths),
            "active_asset_path_count": len(active_asset_paths),
            "indexed_asset_path_count": len(indexed_asset_paths),
            "indexed_usage_count": indexed_usage_count,
            "missing_active_asset_paths": sorted(missing_active_asset_paths),
        }
    )
    return usage_index, summary


def art_usage_guidance(
    path: Path,
    system: str,
    review_state: str | None,
    usages: list[dict[str, Any]],
    ui_form_usages: list[dict[str, Any]],
) -> str:
    if review_state == "obsolete":
        return "已确认废弃；不要作为新的 GF_X 运行时美术资源使用。"
    if system == "PCGMap":
        return "PCG 地图运行资源；由 Terrain/WorldObject/Zone 等 PCG catalog 选择，并由 GF_X PCG 地图生命周期加载。"
    if usages:
        keys = ", ".join(usage["key"] for usage in usages[:6] if usage.get("key"))
        suffix = "..." if len(usages) > 6 else ""
        return f"运行时已绑定：{keys}{suffix}；用途以 runtime_usages.role 为准，代码通过 TotemAssetService/runtime asset catalog 使用。"
    if ui_form_usages:
        forms = ", ".join(usage["form_name"] for usage in ui_form_usages[:6] if usage.get("form_name"))
        suffix = "..." if len(ui_form_usages) > 6 else ""
        return f"GF_X UI form bound: {forms}{suffix}; lifecycle is driven by UIFormConfig/UITable -> UIExtension.OpenUIForm, not by runtime asset catalog."
    if review_state == "placeholder":
        return "临时 UI 占位资源；当前可短期复用，正式美术阶段需要替换或重生成。"
    if system == "GF_XExample":
        return "GF_X 示例/参考资源；不得进入当前干净启动运行流程。"
    return "当前未被 runtime asset catalog 绑定；用于新功能前先确认用途并补充 runtime asset catalog。"


def art_usage_status(
    system: str,
    review_state: str | None,
    duplicate_name: bool,
    usages: list[dict[str, Any]],
    ui_form_usages: list[dict[str, Any]],
) -> str:
    if review_state == "obsolete":
        return "obsolete"
    if system == "PCGMap":
        return "pcg_catalog_bound"
    if usages and review_state == "placeholder":
        return "runtime_bound_placeholder"
    if usages:
        return "runtime_bound"
    if ui_form_usages and review_state == "placeholder":
        return "ui_form_bound_placeholder"
    if ui_form_usages:
        return "ui_form_bound"
    if review_state == "placeholder":
        return "placeholder"
    if system == "GF_XExample":
        return "example_reference"
    if duplicate_name:
        return "duplicate_name_review"
    if system == "Unclassified":
        return "classification_needed"
    if system == "GF_XCore":
        return "gf_x_core_support"
    return "reusable_candidate"


def art_usage_status_reason(status: str) -> str:
    reasons = {
        "obsolete": "User-confirmed obsolete resource. Do not use for new GF_X runtime content.",
        "runtime_bound_placeholder": "Runtime-bound but still placeholder art. Keep key stable and replace in art polish.",
        "runtime_bound": "Referenced by the runtime asset catalog and safe to inspect through runtime_usages.",
        "ui_form_bound_placeholder": "GF_X UI form prefab binding exists, but the art is still a temporary placeholder.",
        "ui_form_bound": "Referenced by UIFormConfig/UITable and opened through the GF_X UI form lifecycle.",
        "placeholder": "Temporary placeholder art that can be used in this phase but should be regenerated later.",
        "example_reference": "GF_X example/reference asset. Keep isolated from the clean startup runtime.",
        "duplicate_name_review": "Shares a filename with another asset. Confirm exact path before use.",
        "classification_needed": "Path/name inference is not enough. Ask for confirmation or add runtime catalog context before use.",
        "gf_x_core_support": "GF_X support asset. Treat as framework/runtime support rather than game content.",
        "reusable_candidate": "Inferred as reusable project art, but not currently runtime-bound.",
        "pcg_catalog_bound": "PCG map source art selected by PCG catalogs and consumed by the GF_X PCG runtime lifecycle.",
    }
    return reasons.get(status, "Unknown generated status; inspect asset manually before use.")


def iter_art_files() -> list[Path]:
    roots = [RESOURCES_DIR, GAME_DIR]
    files: list[Path] = []
    for root in roots:
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.is_file() and path.suffix.lower() in ART_EXTENSIONS and not path.name.endswith(".meta"):
                files.append(path)
    return sorted(files, key=lambda p: rel(p).lower())


def build_art_assets_manifest() -> dict[str, Any]:
    files = iter_art_files()
    indexed_paths = {rel(path).lower() for path in files}
    runtime_usage_index, runtime_asset_summary = build_runtime_asset_usage_index(files)
    ui_form_usage_index, ui_form_usage_summary = build_ui_form_usage_index(indexed_paths)
    name_counts: dict[str, int] = {}
    for path in files:
        key = path.stem.lower()
        name_counts[key] = name_counts.get(key, 0) + 1

    assets: list[dict[str, Any]] = []
    system_counts: dict[str, int] = {}
    type_counts: dict[str, int] = {}
    review_state_counts: dict[str, int] = {"obsolete": 0, "placeholder": 0}
    usage_status_counts: dict[str, int] = {}
    review_count = 0

    for path in files:
        system = infer_art_system(path)
        asset_type = art_asset_type(path)
        duplicate_name = name_counts.get(path.stem.lower(), 0) > 1
        review_state, review_note, _ = art_review_state(path)
        runtime_usages = sorted(runtime_usage_index.get(rel(path).lower(), []), key=lambda item: (item["key"], item["source"]))
        ui_form_usages = sorted(ui_form_usage_index.get(rel(path).lower(), []), key=lambda item: (item["prefab_path"], item["form_name"]))
        usage_status = art_usage_status(system, review_state, duplicate_name, runtime_usages, ui_form_usages)
        needs_review = system in {"Unclassified", "GF_XExample"} or duplicate_name or review_state is not None
        if needs_review:
            review_count += 1
        if review_state:
            review_state_counts[review_state] = review_state_counts.get(review_state, 0) + 1
        usage_status_counts[usage_status] = usage_status_counts.get(usage_status, 0) + 1
        system_counts[system] = system_counts.get(system, 0) + 1
        type_counts[asset_type] = type_counts.get(asset_type, 0) + 1
        asset = {
            "path": rel(path),
            "resource_key": resource_key(path),
            "name": path.name,
            "extension": path.suffix.lower(),
            "asset_type": asset_type,
            "inferred_system": system,
            "inferred_role": infer_art_role(path, system, asset_type),
            "lifecycle_policy": lifecycle_policy(path, system),
            "source_scope": "GF_X example" if "/examples/" in rel(path).lower() else "project",
            "runtime_usage_count": len(runtime_usages),
            "runtime_keys": sorted({usage["key"] for usage in runtime_usages if usage.get("key")}),
            "runtime_usages": runtime_usages,
            "ui_form_usage_count": len(ui_form_usages),
            "ui_form_names": sorted({usage["form_name"] for usage in ui_form_usages if usage.get("form_name")}),
            "ui_form_prefab_paths": sorted({usage["prefab_path"] for usage in ui_form_usages if usage.get("prefab_path")}),
            "ui_form_usages": ui_form_usages,
            "usage_guidance": art_usage_guidance(path, system, review_state, runtime_usages, ui_form_usages),
            "usage_status": usage_status,
            "usage_status_reason": art_usage_status_reason(usage_status),
            "duplicate_name": duplicate_name,
            "needs_review": needs_review,
            "size_bytes": path.stat().st_size,
        }
        if review_state:
            asset["review_state"] = review_state
        if review_note:
            asset["review_note"] = review_note
        assets.append(asset)

    runtime_bound_asset_count = sum(1 for asset in assets if asset["runtime_usage_count"] > 0)
    ui_form_bound_asset_count = sum(1 for asset in assets if asset["ui_form_usage_count"] > 0)
    runtime_asset_summary["runtime_bound_asset_count"] = runtime_bound_asset_count
    ui_form_usage_summary["ui_form_bound_asset_count"] = ui_form_bound_asset_count

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "roots": [rel(RESOURCES_DIR), rel(GAME_DIR)],
        "count": len(assets),
        "review_count": review_count,
        "runtime_bound_asset_count": runtime_bound_asset_count,
        "ui_form_bound_asset_count": ui_form_bound_asset_count,
        "runtime_asset_catalog": runtime_asset_summary,
        "ui_form_usage_summary": ui_form_usage_summary,
        "review_state_counts": dict(sorted(review_state_counts.items())),
        "usage_status_counts": dict(sorted(usage_status_counts.items())),
        "usage_status_legend": {
            status: art_usage_status_reason(status)
            for status in sorted(usage_status_counts)
        },
        "review_policies": list(REVIEW_POLICIES),
        "system_counts": dict(sorted(system_counts.items())),
        "type_counts": dict(sorted(type_counts.items())),
        "assets": assets,
        "notes": [
            "Purpose fields are inferred from path/name and must be corrected when the user confirms a resource is obsolete, duplicated, or reserved.",
            "GF_X DemoGame example assets are removed from the active project; do not recreate Assets/Game/Examples for runtime work.",
            "Assets under Assets/Resources are reuse candidates, but loading/lifecycle must be rewritten through GF_X.",
            "runtime_usages links an asset back to GameData/AIData/GameplayCatalogs/totem_runtime_assets.json so AI can see where the asset is used before editing or replacing it.",
            "ui_form_usages links UI prefabs back to GameData/AIData/DataTables/Business/UIFormConfig.json and the GF_X UI form lifecycle.",
        ],
    }


def runtime_asset_catalog_keys() -> set[str]:
    _, entries = load_runtime_asset_catalog_entries()
    return {str(entry.get("key") or "") for entry in entries if entry.get("key")}


def active_runtime_service_names() -> set[str]:
    runtime_path = GAME_DIR / "Scripts" / "Runtime" / "TotemGameRuntime.cs"
    if not runtime_path.exists():
        return set()

    return set(re.findall(r"RegisterService\(new\s+(Totem\w+Service)\s*\(", read_text(runtime_path)))


def build_feature_slices_manifest(modules: dict[str, Any], datatables: dict[str, Any]) -> dict[str, Any]:
    module_names = {module["name"] for module in modules["modules"]}
    table_names = {table["name"] for table in datatables["tables"]}
    table_names.update(path.stem for path in (AI_DATATABLE_DIR / "Business").glob("*.json"))
    runtime_keys = runtime_asset_catalog_keys()
    runtime_service_names = active_runtime_service_names()
    required_disciplines = ("design", "art", "program", "qa")
    slices: list[dict[str, Any]] = []
    missing_modules: dict[str, list[str]] = {}
    missing_tables: dict[str, list[str]] = {}
    missing_runtime_services: dict[str, list[str]] = {}
    missing_runtime_asset_keys: dict[str, list[str]] = {}
    missing_required_fields: dict[str, list[str]] = {}

    for definition in FEATURE_SLICE_DEFINITIONS:
        feature = dict(definition)
        feature_modules = list(feature.get("modules", []))
        feature_tables = list(feature.get("business_tables", []))
        feature_runtime_services = list(feature.get("runtime_services", []))
        feature_runtime_keys = list(feature.get("runtime_asset_keys", []))
        feature_handoff = dict(feature.get("discipline_handoff", {}))
        feature_id = str(feature.get("id") or "")

        feature_missing_modules = sorted(module for module in feature_modules if module not in module_names)
        feature_missing_tables = sorted(table for table in feature_tables if table not in table_names)
        feature_missing_services = sorted(service for service in feature_runtime_services if service not in runtime_service_names)
        feature_missing_keys = sorted(key for key in feature_runtime_keys if key not in runtime_keys)
        feature_missing_fields: list[str] = []
        for field in ("id", "name", "status", "product_goal", "modules", "business_tables", "runtime_services", "diagnostic_scenarios", "discipline_handoff"):
            if not feature.get(field):
                feature_missing_fields.append(field)
        for discipline in required_disciplines:
            if not feature_handoff.get(discipline):
                feature_missing_fields.append(f"discipline_handoff.{discipline}")

        if feature_missing_modules:
            missing_modules[feature_id] = feature_missing_modules
        if feature_missing_tables:
            missing_tables[feature_id] = feature_missing_tables
        if feature_missing_services:
            missing_runtime_services[feature_id] = feature_missing_services
        if feature_missing_keys:
            missing_runtime_asset_keys[feature_id] = feature_missing_keys
        if feature_missing_fields:
            missing_required_fields[feature_id] = feature_missing_fields

        feature["business_json_paths"] = [f"GameData/AIData/DataTables/Business/{table}.json" for table in feature_tables]
        feature["business_xlsx_paths"] = [f"GameData/DataTables/Business/{table}.xlsx" for table in feature_tables]
        feature["art_lookup"] = {
            "manifest": "项目知识库（AI自行维护）/wiki/manifests/art_assets.json",
            "query": "Find assets whose runtime_keys contains any runtime_asset_keys listed by this feature.",
        }
        feature["runtime_asset_catalog"] = rel(RUNTIME_ASSET_CATALOG_PATH)
        feature["validation"] = {
            "missing_modules": feature_missing_modules,
            "missing_business_tables": feature_missing_tables,
            "missing_runtime_services": feature_missing_services,
            "missing_runtime_asset_keys": feature_missing_keys,
            "missing_required_fields": feature_missing_fields,
            "valid": not (feature_missing_modules or feature_missing_tables or feature_missing_services or feature_missing_keys or feature_missing_fields),
        }
        slices.append(feature)

    all_feature_tables = sorted({table for feature in slices for table in feature.get("business_tables", [])})
    all_feature_modules = sorted({module for feature in slices for module in feature.get("modules", [])})
    all_feature_runtime_services = sorted({service for feature in slices for service in feature.get("runtime_services", [])})
    all_feature_runtime_keys = sorted({key for feature in slices for key in feature.get("runtime_asset_keys", [])})
    all_diagnostics = sorted({item for feature in slices for item in feature.get("diagnostic_scenarios", [])})
    uncovered_legacy_modules = sorted(module for module in module_names if module not in all_feature_modules)
    uncovered_runtime_services = sorted(service for service in runtime_service_names if service not in all_feature_runtime_services)
    validation = {
        "missing_modules": missing_modules,
        "uncovered_legacy_modules": uncovered_legacy_modules,
        "missing_business_tables": missing_tables,
        "missing_runtime_services": missing_runtime_services,
        "uncovered_runtime_services": uncovered_runtime_services,
        "missing_runtime_asset_keys": missing_runtime_asset_keys,
        "missing_required_fields": missing_required_fields,
        "valid": not (missing_modules or uncovered_legacy_modules or missing_tables or missing_runtime_services or uncovered_runtime_services or missing_runtime_asset_keys or missing_required_fields),
    }

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "purpose": "Feature-slice index for design/art/program/QA handoffs during the GF_X rewrite.",
        "count": len(slices),
        "discipline_columns": list(required_disciplines),
        "legacy_module_coverage_count": len(all_feature_modules),
        "business_table_coverage_count": len(all_feature_tables),
        "runtime_service_coverage_count": len(all_feature_runtime_services),
        "runtime_asset_key_coverage_count": len(all_feature_runtime_keys),
        "diagnostic_scenario_coverage_count": len(all_diagnostics),
        "modules": all_feature_modules,
        "business_tables": all_feature_tables,
        "runtime_services": all_feature_runtime_services,
        "runtime_asset_keys": all_feature_runtime_keys,
        "diagnostic_scenarios": all_diagnostics,
        "validation": validation,
        "slices": slices,
        "notes": [
            "Use this manifest before feature work to identify which Business JSON/xlsx, runtime service, UI form, art runtime key, and diagnostic evidence belong together.",
            "Art replacement should preserve runtime_asset_keys or UI form bindings, or update both the source config and art_assets.json through build_ai_manifests.py.",
            "Design changes should start from Business AI JSON, then synchronize xlsx and regenerate runtime catalogs before GF_X diagnostics.",
        ],
    }


def build_diagnostic_triage_manifest(feature_slices: dict[str, Any]) -> dict[str, Any]:
    records_by_scenario: dict[str, dict[str, Any]] = {}
    for feature in feature_slices["slices"]:
        for scenario in feature.get("diagnostic_scenarios", []):
            record = records_by_scenario.setdefault(
                scenario,
                {
                    "diagnostic_scenario": scenario,
                    "feature_slices": [],
                    "feature_ids": [],
                    "business_tables": set(),
                    "runtime_services": set(),
                    "ui_forms": set(),
                    "runtime_asset_keys": set(),
                    "docs": set(),
                    "triage_steps": [
                        "Open the failing report item details and timeline first.",
                        "Read every feature slice listed in feature_slices.",
                        "Check the linked Business JSON/xlsx tables before changing runtime constants.",
                        "Check runtime_services and UI forms for code-side regressions.",
                        "For visual failures, query art_assets.json by runtime_asset_keys and preserve or update the runtime asset catalog.",
                    ],
                },
            )
            record["feature_slices"].append(
                {
                    "id": feature["id"],
                    "name": feature["name"],
                    "status": feature["status"],
                    "discipline_handoff": feature["discipline_handoff"],
                }
            )
            record["feature_ids"].append(feature["id"])
            record["business_tables"].update(feature.get("business_tables", []))
            record["runtime_services"].update(feature.get("runtime_services", []))
            record["ui_forms"].update(feature.get("ui_forms", []))
            record["runtime_asset_keys"].update(feature.get("runtime_asset_keys", []))
            record["docs"].update(feature.get("docs", []))

    records: list[dict[str, Any]] = []
    for scenario, record in sorted(records_by_scenario.items()):
        records.append(
            {
                "diagnostic_scenario": scenario,
                "feature_ids": sorted(set(record["feature_ids"])),
                "feature_slices": sorted(record["feature_slices"], key=lambda item: item["id"]),
                "business_tables": sorted(record["business_tables"]),
                "runtime_services": sorted(record["runtime_services"]),
                "ui_forms": sorted(record["ui_forms"]),
                "runtime_asset_keys": sorted(record["runtime_asset_keys"]),
                "docs": sorted(record["docs"]),
                "triage_steps": record["triage_steps"],
            }
        )

    missing_feature_links = [record["diagnostic_scenario"] for record in records if not record["feature_ids"]]

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "purpose": "Reverse index from GF_X diagnostic scenarios to feature slices and likely design/art/program/QA investigation surfaces.",
        "count": len(records),
        "feature_link_count": sum(len(record["feature_ids"]) for record in records),
        "validation": {
            "missing_feature_links": missing_feature_links,
            "valid": len(missing_feature_links) == 0,
        },
        "records": records,
        "notes": [
            "When a GF_X diagnostic fails, search diagnostic_scenario in this file before editing code.",
            "This file is generated from feature_slices.json so feature ownership and diagnostic triage stay aligned.",
        ],
    }


def active_changes() -> list[dict[str, Any]]:
    changes_dir = OPENSPEC_DIR / "changes"
    if not changes_dir.exists():
        return []
    result = []
    for path in sorted(p for p in changes_dir.iterdir() if p.is_dir() and p.name != "archive"):
        artifacts = sorted(p.name for p in path.iterdir() if p.is_file())
        result.append(
            {
                "id": path.name,
                "path": rel(path),
                "has_proposal": (path / "proposal.md").exists(),
                "has_design": (path / "design.md").exists(),
                "has_tasks": (path / "tasks.md").exists(),
                "artifacts": artifacts,
            }
        )
    return result


def build_tests_manifest() -> dict[str, Any]:
    playtest_reports_dir = ROOT / "tools" / "playtest" / "reports"
    reports = []
    if playtest_reports_dir.exists():
        reports = [rel(p) for p in sorted(playtest_reports_dir.rglob("*.md"))]

    unity_tests = []
    tests_root = ROOT / "Assets" / "Tests"
    if tests_root.exists():
        unity_tests = [rel(p) for p in sorted(tests_root.rglob("*")) if p.is_file() and not p.name.endswith(".meta")]

    specs_dir = OPENSPEC_DIR / "specs"
    specs = [rel(p) for p in sorted(specs_dir.rglob("spec.md"))] if specs_dir.exists() else []

    changes = active_changes()
    warnings = [
        f"{change['id']} 缺少 proposal/design/tasks 中的一个或多个 artifact"
        for change in changes
        if not (change["has_proposal"] and change["has_design"] and change["has_tasks"])
    ]

    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "unity_tests": unity_tests,
        "playtest_reports": reports,
        "openspec_specs": specs,
        "active_changes": changes,
        "warnings": warnings,
    }


def build_health(
    modules: dict[str, Any],
    datatables: dict[str, Any],
    tests: dict[str, Any],
) -> dict[str, Any]:
    warnings = []
    warnings.extend(modules.get("warnings", []))
    warnings.extend(datatables.get("warnings", []))
    warnings.extend(tests.get("warnings", []))
    return {
        "generated_at": TODAY,
        "source": "tools/ai_index/build_ai_manifests.py",
        "status": "warning" if warnings else "ok",
        "warning_count": len(warnings),
        "warnings": warnings,
    }


def module_card(module: dict[str, Any]) -> str:
    def bullet(items: list[str]) -> str:
        if not items:
            return "- 无\n"
        return "".join(f"- `{item}`\n" for item in items)

    gdd_items = []
    if module.get("gdd_module"):
        gdd_items.append(module["gdd_module"])
    gdd_items.extend(module.get("gdd_systems", []))

    return f"""---
module: {module["name"]}
owner: {module["owner"]}
generated_at: {TODAY}
source: tools/ai_index/build_ai_manifests.py
---

# {module["name"]} Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

{module["purpose"]}

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

{bullet(gdd_items)}
## 关联 OpenSpec

{bullet(module.get("specs", []))}
## 关联 DataTable

{bullet(module.get("datatables", []))}
## 关联资源

{bullet(module.get("resources", []))}
## 主要脚本

{bullet(module.get("main_scripts", []))}
## 注意事项

{bullet(module.get("notes", []))}
"""


def unity_text_meta_for(asset_path: Path) -> str:
    guid = hashlib.md5(f"ai-index:{rel(asset_path)}".encode("utf-8")).hexdigest()
    return f"""fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def project_map(
    modules: dict[str, Any],
    datatables: dict[str, Any],
    tests: dict[str, Any],
    feature_slices: dict[str, Any],
    diagnostic_triage: dict[str, Any],
) -> str:
    module_root = rel(MODULES_DIR)
    data_cs_root = rel(DATA_CS_DIR)
    datatable_runtime = f"{module_root}/DataTable/DataTableModule.cs"
    datatable_registry = f"{data_cs_root}/DataTableRegistry.cs"
    active_runtime_root = "Assets/Game/Scripts"
    framework_runtime_root = "Assets/Game/ScriptsBuiltin"
    business_ai_json_root = "GameData/AIData/DataTables/Business"
    business_xlsx_root = "GameData/DataTables/Business"
    runtime_catalog_path = "GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json"
    module_lines = "\n".join(
        f"| `{m['name']}` | {m['purpose']} | {', '.join(m['datatables']) or '无'} |"
        for m in modules["modules"]
    )
    active_lines = "\n".join(
        f"| `{c['id']}` | `{c['path']}` | proposal={c['has_proposal']} / design={c['has_design']} / tasks={c['has_tasks']} |"
        for c in tests["active_changes"]
    ) or "| 无 | - | - |"

    return f"""# AI 项目地图

> 由 `tools/ai_index/build_ai_manifests.py` 生成。项目级任务先读本文件，再进入具体模块。
>
> 生成日期：{TODAY}

## 1. 读取入口

```text
AGENTS.md
→ 项目知识库（AI自行维护）/wiki/INDEX.md
→ 项目知识库（AI自行维护）/wiki/PROJECT_MAP.md
→ 项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md
→ 项目知识库（AI自行维护）/wiki/manifests/*.json
→ {active_runtime_root}/Runtime 或 {active_runtime_root}/UI（当前 GF_X 业务代码）
→ {module_root}/<Module>/MODULE.md（仅作为旧行为证据）
```

## 2. 信息分层

| 层级 | 路径 | 作用 |
|---|---|---|
| AI 行为入口 | `AGENTS.md` / `.claude/` / `.codex/` | agent 路由、skill 白名单、grill-me、openspec 工作流 |
| 项目知识库 | `项目知识库（AI自行维护）/` | GDD、wiki、历史决策、AI 自维护知识 |
| 变更记录 | `openspec/changes/` / `openspec/specs/` | 中大型变更的 proposal、design、tasks、spec、测试和归档 |
| 当前 GF_X 业务代码 | `{active_runtime_root}/` | 当前可编译、可启动的业务运行时；新增业务脚本优先进入这里 |
| GF_X/工具核心 | `{framework_runtime_root}/` | GF_X runtime/editor/diagnostics/tooling 边界，修改前需要更谨慎 |
| 旧程序模块证据 | `{module_root}/` | 旧业务模块证据，每个模块有 `MODULE.md`；归档后不再作为 Unity 编译入口 |
| 当前业务配置 | `{business_ai_json_root}/` + `{business_xlsx_root}/` | AI 友好 JSON 与策划可读 xlsx；runtime catalog 由 Business JSON 生成 |
| 旧配置证据 | `{rel(DATA_JSON_DIR)}/` + `{data_cs_root}/` | 旧 JSON 与旧生成 C# 只作为字段/行为证据 |
| 功能切片索引 | `项目知识库（AI自行维护）/wiki/manifests/feature_slices.json` | 按功能串联策划表、美术 runtime key、程序服务、UI 和诊断证据 |
| 诊断定位索引 | `项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json` | 从失败的 GF_X 诊断反查功能切片、表、服务、UI 和资源 key |
| 美术资源 | `Assets/Resources/Prefab/` / `Sprite/` / `Audio/` / `Effect/` + `Assets/Game/Prefabs/` | 复用资源内容，但加载和生命周期必须走 GF_X；先查 `manifests/art_assets.json` 的 `usage_status`、`runtime_usages`、`ui_form_usages` 反链 |
| 测试证据 | `GameData/Diagnostics/Reports/` / `tools/playtest/reports/` / `LegacyProjectArchive/Assets/Tests/` | GF_X 自动诊断、人工 playtest、旧测试证据 |

## 3. 模块总览

| 模块 | 职责 | 关联配置表 |
|---|---|---|
{module_lines}

## 4. 配置表入口

- 当前 AI JSON：`{business_ai_json_root}/`
- 当前策划 xlsx：`{business_xlsx_root}/`
- 当前 runtime catalog：`{runtime_catalog_path}`
- 旧配置 JSON 证据：`{rel(DATA_JSON_DIR)}/`
- 旧生成 C# 证据：`{data_cs_root}/`
- 旧运行时加载证据：`{datatable_runtime}`
- 旧注册表证据：`{datatable_registry}`
- 业务配置表数量：{datatables["count"]}
- 跨岗位功能切片数量：{feature_slices["count"]}
- 功能切片覆盖旧模块数量：{feature_slices["legacy_module_coverage_count"]}/{modules["count"]}
- 功能切片覆盖运行时服务数量：{feature_slices["runtime_service_coverage_count"]}
- 功能切片入口：`项目知识库（AI自行维护）/wiki/manifests/feature_slices.json`
- 诊断定位入口：`项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json`（{diagnostic_triage["count"]} 个诊断场景）

## 5. 活跃 OpenSpec

| Change | 路径 | Artifact 状态 |
|---|---|---|
{active_lines}

## 6. AI 修改任务的推荐流程

1. 先读 `ACTIVE_CONTEXT.md`，确认 active change、禁改区和当前风险。
2. 功能改动先查 `manifests/feature_slices.json`，确认对应策划表、美术 key、程序服务、UI 和诊断。
3. 再查 `{active_runtime_root}` 中的当前 GF_X 服务/UI/Procedure；需要旧效果证据时再读 `{module_root}/<Module>/MODULE.md`。
4. 配置改动优先改 `{business_ai_json_root}`，再用导表/逆向导表流程同步 xlsx 和 runtime catalog。
5. 小改直接修改并验证；中大型改动先创建/推进 openspec change。
6. 改完优先运行 `{DIAGNOSTICS_COMMAND}` 生成 GF_X 诊断报告。
7. 若诊断失败，先用 `manifests/diagnostic_triage.json` 从失败场景反查功能切片和改动面。
8. 至少运行 `python tools/ai_index/build_ai_manifests.py --check` 确认索引未过期。
"""


def active_context(
    tests: dict[str, Any],
    health: dict[str, Any],
    feature_slices: dict[str, Any],
    diagnostic_triage: dict[str, Any],
) -> str:
    active_lines = "\n".join(
        f"- `{c['id']}`：{c['path']}（proposal={c['has_proposal']}，design={c['has_design']}，tasks={c['has_tasks']}）"
        for c in tests["active_changes"]
    ) or "- 无"
    warning_lines = "\n".join(f"- {w}" for w in health["warnings"]) or "- 无"

    return f"""# AI 当前上下文

> 由 `tools/ai_index/build_ai_manifests.py` 生成。用于提醒 AI 当前项目状态和任务前检查项。
>
> 生成日期：{TODAY}

## 1. 活跃 OpenSpec Change

{active_lines}

## 2. 任务前检查

- 先读 `AGENTS.md`，确认是否触发 grill-me / openspec / agent 路由。
- 涉及功能改动时，先读 `manifests/feature_slices.json`，按切片确认策划/美术/程序/QA 交接点。
- 涉及 Unity 代码时，优先读 `Assets/Game/Scripts` 当前 GF_X 业务代码；旧 `MODULE.md` 只作为旧行为证据。
- 涉及配置时，先读 `manifests/datatables.json`、`GameData/AIData/DataTables/Business/*.json` 和 `totem_gameplay_catalog.json`。
- 涉及美术时，先读 `manifests/art_assets.json` 的 `usage_status` / `runtime_usages` / `ui_form_usages` / `usage_guidance`，再对照 runtime asset catalog 或 UIFormConfig。
- 涉及测试或诊断失败时，先读 `manifests/diagnostic_triage.json`，再读 `manifests/tests.json`、最近 `GameData/Diagnostics/Reports/gf-diagnostics-run-all_*.json` 和最近 playtest 报告；需要刷新 GF_X 全量诊断时运行 `{DIAGNOSTICS_COMMAND}`。

## 3. 禁改区 / 谨慎区

- 不直接修改 `.codex/agents/*.toml`，源文件是 `.claude/agents/*.md`。
- 不直接修改 `项目知识库（AI自行维护）/raw/`。
- 不在没有 openspec 的情况下大改 GF_X 框架核心（例如 `Assets/Game/ScriptsBuiltin/`）。
- 不让业务代码绕过 `TotemInputService` / `ITotemInputProvider` 读取按键输入。
- 不在 `Update` / `LateUpdate` 热路径中引入 GC 分配。

## 4. 当前索引健康

状态：`{health["status"]}`，warning 数：{health["warning_count"]}，功能切片数：{feature_slices["count"]}，诊断定位场景数：{diagnostic_triage["count"]}

{warning_lines}
"""


def expected_outputs() -> dict[Path, str]:
    datatables = build_datatables_manifest()
    modules = build_modules_manifest({t["name"] for t in datatables["tables"]})
    feature_slices = build_feature_slices_manifest(modules, datatables)
    diagnostic_triage = build_diagnostic_triage_manifest(feature_slices)
    assets = build_assets_manifest()
    tests = build_tests_manifest()
    health = build_health(modules, datatables, tests)

    outputs: dict[Path, str] = {
        MANIFEST_DIR / "datatables.json": json_dump(datatables),
        MANIFEST_DIR / "modules.json": json_dump(modules),
        MANIFEST_DIR / "assets.json": json_dump(assets),
        MANIFEST_DIR / "art_assets.json": json_dump(build_art_assets_manifest()),
        MANIFEST_DIR / "feature_slices.json": json_dump(feature_slices),
        MANIFEST_DIR / "diagnostic_triage.json": json_dump(diagnostic_triage),
        MANIFEST_DIR / "tests.json": json_dump(tests),
        MANIFEST_DIR / "health.json": json_dump(health),
        WIKI_DIR / "PROJECT_MAP.md": project_map(modules, datatables, tests, feature_slices, diagnostic_triage),
        WIKI_DIR / "ACTIVE_CONTEXT.md": active_context(tests, health, feature_slices, diagnostic_triage),
    }

    for module in modules["modules"]:
        module_path = ROOT / module["path"] / "MODULE.md"
        outputs[module_path] = module_card(module)
        outputs[module_path.with_suffix(module_path.suffix + ".meta")] = unity_text_meta_for(module_path)

    return outputs


def write_outputs(outputs: dict[Path, str]) -> None:
    for path, content in outputs.items():
        write_text(path, content)


def check_outputs(outputs: dict[Path, str]) -> int:
    stale: list[str] = []
    for path, content in outputs.items():
        if not path.exists() or read_text(path) != content:
            stale.append(rel(path))

    if stale:
        print("AI manifest 需要重新生成：")
        for item in stale:
            print(f"- {item}")
        return 1

    print("AI manifest 已是最新。")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="生成/校验 AI 项目信息 manifest。")
    parser.add_argument("--check", action="store_true", help="只校验生成物是否最新，不写文件。")
    args = parser.parse_args()

    outputs = expected_outputs()
    if args.check:
        return check_outputs(outputs)

    write_outputs(outputs)
    print(f"已生成 {len(outputs)} 个 AI 信息索引文件。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
