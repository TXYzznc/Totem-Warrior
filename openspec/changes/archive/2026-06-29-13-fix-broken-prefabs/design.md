# 设计文档 — 13 修复 4 个问题 Prefab

## 1. 范围

仅修以下 4 个 Prefab：

| Prefab | 路径 | 权威 mockup | Sprite 素材目录 |
|---|---|---|---|
| Settings | `Assets/Resources/Prefab/UI/Settings.prefab` | `openspec/changes/10-settings-form/art/mockups/SettingsForm.png` | `Assets/Resources/Sprite/UI/SettingsForm/` (6 png) |
| SelfTattoo | `Assets/Resources/Prefab/UI/SelfTattoo.prefab` | `openspec/changes/archive/2026-06-29-12-core-ui-screens/art/mockups/SelfTattooForm.png` | `Assets/Resources/Sprite/UI/SelfTattooForm/` (7 png) |
| ThreeChoice | `Assets/Resources/Prefab/UI/ThreeChoice.prefab` | `openspec/changes/archive/2026-06-29-12-core-ui-screens/art/mockups/ThreeChoiceForm.png` | `Assets/Resources/Sprite/UI/ThreeChoiceForm/` (7 png) |
| TattooEnchant | `Assets/Resources/Prefab/UI/TattooEnchant.prefab` | `openspec/changes/archive/2026-06-29-12-core-ui-screens/art/mockups/TattooEnchantForm.png` | `Assets/Resources/Sprite/UI/TattooEnchantForm/` (7 png) |

## 2. 修复策略 — 三层逐 Prefab 流水线

每个 Prefab **顺序**走以下三层，**禁止跳层**：

### 层 1 — 文本乱码修复（Edit YAML 手工）

1. 用 Read 读 Prefab YAML，grep `\\uFFFD` 列出所有乱码行（**注意**：实际乱码是 YAML 转义形式 `"��..."`，不是 UTF-8 字节 0xEF 0xBF 0xBD，所以 grep 必须搜 `\\uFFFD` 字面，不能搜 `�` 字符）
2. 对每处乱码：参照同 Prefab 在 mockup 中对应位置（按 `m_Name:` 节点名定位）+ 10/12 change 的 design.md 文案表，确定正确中文
3. Edit 替换为正确 UTF-8 中文（如 `m_text: "����"` → `m_text: "音量设置"`）
4. **不能用 MCP 写中文回去**——MCP 仍有编码 bug，会再次写成 `�`
5. 替换长度参考：4 个 `�` ≈ 原 2 个 UTF-8 中文字（每字被破坏成 2 个 `�`），按节点名语义判断到底是 2 / 3 / 4 字

### 层 2 — Sprite 绑定补全

1. 读 Prefab YAML，列出所有 `Image` / `RawImage` 组件且 `m_Sprite: {fileID: 0}` 的节点
2. 按节点名匹配 `Assets/Resources/Sprite/UI/<PageName>Form/` 下素材文件（同 Prefab 子文件夹按 PageName 命名）
3. 读取目标 `.png.meta` 拿到 `guid`，回写到 Prefab 的 `m_Sprite` 字段
4. 多 sprite 的 `.png`（spritesheet）需取出对应 `fileID`（一般 `21300000+index`），按 mockup 状态变体匹配
5. **不要改 ImportProcessor 已设的导入参数**（textureType / pixelsPerUnit 等）

### 层 3 — 层级/布局校正

1. Prefab 当前层级 vs mockup 视觉层级对比，列出：
   - 多余节点 → 删
   - 缺失节点 → 用 Edit 直接在 YAML 里加（参考同 Prefab 已有节点的 transform/component 模板）
   - 错误命名节点 → rename
2. 不要求像素对齐，只要求**视觉分组与信息层级**一致
3. **不重做 UIForm 脚本**：如果新增节点需要脚本引用，记入 `tests/bugs.md` 留给下一期；本期只对齐视觉

## 3. MCP spike（30 分钟时间盒）

- 入口：`http://localhost:8091/`
- 目的：定位 UTF-8 中文 → Prefab YAML 写成 `�` 的具体环节
- 候选猜测：
  1. MCP server 端 JSON 解析时 `ensure_ascii=True` 误用
  2. C# 端 `string` 编码 fallback 到 GBK
  3. Unity YAML 序列化时强制 ASCII
- 产出：`openspec/changes/13-fix-broken-prefabs/art/raw/mcp-spike-report.md`，写 1 段根因猜测 + 1 段验证路径，供后续治理 change 立项
- **本期不修 MCP**——超出范围

## 4. 验收 — PlayMode 联调

1. Unity Editor 进 PlayMode，按 MainMenu → 每个表单的入口依次打开 4 个 Prefab
2. 用 Unity Recorder 或截图工具抓**运行时截图**
3. 把运行时截图与对应 mockup 并排贴入 `tests/results.md`
4. 列出残余偏差清单 → 迭代到「视觉分组与信息层级一致」
5. 偏差清单为空即验收通过

## 5. 风险与陷阱

| 风险 | 缓解 |
|---|---|
| Edit YAML 时 Prefab fileID 引用断链 | 改字段不改 fileID；删节点前 grep 是否被脚本引用 |
| Sprite GUID 找错 → 显示紫色 | 每个 GUID 替换后立刻在 Unity 里查看预览 |
| Settings.prefab 与 SettingsForm.png 差距过大无法原地修 | 阶段 A 共识允许 fallback：本 Prefab 单独重做（仍按 mockup 6 阶段子流程） |
| MCP spike 超 30 分钟仍无结论 | 直接收尾写「未定位，候选环节 X/Y/Z」入报告 |
| PlayMode 报 NRE | 只补缺失的 GameObject 引用，不重写脚本 |

## 6. 不在本期范围

- ❌ 重做美术素材（已落地）
- ❌ 重新生成 mockup（已确认）
- ❌ 修 unity-skills MCP 编码 bug（仅 spike）
- ❌ 重写 UIForm C# 脚本（除非编译错/NRE）
- ❌ 触碰其它 6 个验收通过的 Prefab
