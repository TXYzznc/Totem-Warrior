# M14 OasisCity 权威运行时验收证据

日期：2026-08-12

## 场景与资源合同

- `OasisCity.unity` 包含 47 个正式 `TotemMapAnchorAuthoring` 引用：20 个 PlayerSpawn、20 个 MapResource、7 个 Extraction。
- 场景内嵌 `TotemMapAnchorAuthoring` MonoScript 数量为 0。
- Unity 场景校验：0 issue；当前场景缺失脚本：0。
- `Assets/Game/Sprites/Weapons/weapon_rifle_placeholder.png` 不属于第一版合同；武器图目录按确认保持清理状态。
- `weapon.rifle.patrol.v1` 的运行时资源项允许 `activeAssetPath` 为空，并由非空 primitive fallback 保证可运行。

## 自动验证

| 门禁 | 结果 | 证据 |
|---|---:|---|
| EditMode 全量 | 253/253 通过 | `artifacts/test-results/editmode-full-fontfix.xml` |
| PlayMode 全量 | 8/8 通过 | `artifacts/test-results/playmode-full-fontfix-no-domain.xml` |
| 正常 Domain Reload 下机器人 20 局 smoke | 1/1 通过 | UnitySkills job `c0a10813` |
| GF_X 全量诊断 | 23/23 通过，0 warning | `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260812_162442.json` |
| OpenSpec strict validate | 通过 | `openspec validate rebuild-six-player-pvpve-first-playable --strict` |

## 测试框架说明

- Unity 2022.3.62f3c1 的 PlayMode Test Runner 在完整 Domain Reload 下会丢失临时场景的 `PlaymodeTestsController`；全量回归因此在临时关闭 Domain Reload 的隔离环境中由 Unity 官方命令行 Test Framework 执行并直接生成 XML。
- 正常 Domain Reload 环境下另行执行机器人 20 局 smoke，结果 1/1 通过，用于交叉验证正式运行配置。
- `GFBuiltin`、`GF`、`GameEntry` 的销毁与静态缓存清理已覆盖重复 Launch 场景加载；PlayMode 全量包含重复加载生命周期测试并通过。
- UnitySkills 动态 CJK FontAsset 的 Material/atlas 生命周期已修复，避免编辑器 UI Toolkit 的 `MissingReferenceException` 污染 Test Runner 结果。
- `ProjectSettings/EditorSettings.asset` 已恢复项目原值：`m_EnterPlayModeOptionsEnabled: 0`、`m_EnterPlayModeOptions: 3`。
