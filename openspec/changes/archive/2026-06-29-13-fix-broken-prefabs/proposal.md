## Why

12-core-ui-screens 把 10 张核心 UI 全部产出后归档，但 4 个 Prefab 实际验收不合格，**等于一交付即返工**：

| Prefab | mockup 出处 | 乱码（`�` 转义） | Sprite 未绑 | 素材目录 png 数 |
|---|---|---|---|---|
| `Settings.prefab` | **权威**：`openspec/changes/10-settings-form/art/mockups/SettingsForm.png` | 14 处 | 37 处 | 6（`Assets/Resources/Sprite/UI/SettingsForm/`） |
| `SelfTattoo.prefab` | `archive/2026-06-29-12-core-ui-screens/art/mockups/SelfTattooForm.png` | 5 处 | 24 处 | 7（`Assets/Resources/Sprite/UI/SelfTattooForm/`） |
| `ThreeChoice.prefab` | `archive/2026-06-29-12-core-ui-screens/art/mockups/ThreeChoiceForm.png` | 7 处 | 1 处 | 7（`Assets/Resources/Sprite/UI/ThreeChoiceForm/`） |
| `TattooEnchant.prefab` | `archive/2026-06-29-12-core-ui-screens/art/mockups/TattooEnchantForm.png` | 5 处 | 1 处 | 7（`Assets/Resources/Sprite/UI/TattooEnchantForm/`） |

**根因**：阶段 5 调用 `unity-skills` MCP（`http://localhost:8091/`）经过 Codex/Claude → CLI 胶水层时，Windows cp936 / argv 编码导致 UTF-8 中文字符被破坏，Unity Editor 接收到的字符串再被 YAML 序列化为 `"��..."` 转义形式；Image 节点未绑 Sprite 是自动建流程未覆盖完整节点列表，素材已实际落到 `Assets/Resources/Sprite/UI/<PageName>Form/` 子目录。

**spike 结论**（详见 `art/raw/mcp-spike-report.md`）：根因不在 unity-skills server / Python CLI / C# listener 任一环节代码内，需端到端复现实验，留下一期 `14-mcp-encoding-fix` 立项。本期**直接 Edit Prefab YAML**绕开 MCP 完成修复。

**不重做也不重新拆分素材**：素材已在 `Assets/Resources/Sprite/UI/<PageName>Form/` 落地齐全，导入参数由 `Assets/Editor/UISpriteImportProcessor.cs` 统一管控。只**原地修**这 4 个 Prefab 直到对齐 mockup。

**Sprite 未绑节点数 ≠ 应绑数**：部分布局节点（Panel/Frame/Mask/分割条等）本就无需 Sprite，按 mockup 视觉对应关系补绑即可，不强求 100% 全绑。

## What Changes

- **修 Prefab 文本乱码**：4 个 Prefab 中 `�` 全部替换为正确中文（按 mockup / design.md 文案）
- **补 Sprite 绑定**：4 个 Prefab 缺失的 `m_Sprite` GUID 全部按 mockup 节点名匹配到 `Assets/Resources/Sprite/UI/<PageName>/` 下对应素材
- **校正层级/布局偏差**：多余节点删，缺失节点补，与 mockup 视觉层级（不要求像素对齐，但层级和分组要一致）一致
- **MCP 编码 bug spike**：单独跑一轮 30 分钟时间盒调查 `unity-skills` MCP 写入中文的链路；只输出 `art/raw/mcp-spike-report.md`，本期不实现修复（除非根因极简单），用于后续治理 change 立项
- **PlayMode 联调验收**：4 个 Prefab 在 Unity Editor PlayMode 下打开并对照 mockup 截图比对，列出残余偏差并迭代到一致
- **不做**：
  - 不重写 UIForm C# 脚本（除非编译错或运行时报 NRE）
  - 不重新生成/拆分美术素材
  - 不动其它 6 个已通过验收的 Prefab（CharacterSelect / CombatHUD / MainMenu / PauseMenu / RunResult / Shop / TattooStudio）
  - 不在本期修 MCP 编码 bug（只做 spike 报告）
