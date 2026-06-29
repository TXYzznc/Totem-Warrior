# UI 制作进度交接记录

更新时间：2026-06-29
用途：当前对话窗口因额度耗尽中断，供新对话窗口快速恢复上下文。

---

## 一、当前所处阶段

`CLAUDE.md §六 UI 制作子流程` 6 阶段中，已完成阶段 1-4（需求设计→效果图设计→效果图生成→素材拆分），目前在**阶段 5（Prefab + 代码）**，对 11 个 Form 做"重建 Prefab 层级 + 直连 SerializeField 字段 + 贴美术资源"。阶段 6（联调微调）尚未开始。

触发原因：用户在阶段 5 开始前要求先核查所有 Form 的 Prefab/Script 匹配情况，发现大面积结构性损坏（详见下方"根因"），于是从"贴新图"升级为"全部重建"。

## 二、根因（两类问题，已通过 grill 确认范围：全部 10+1 个 Form 都要按完整标准重做）

1. **Prefab 层级被误存成 PauseMenu 残留内容**：好几个 Form 的 prefab 实际打开后发现子节点是 `PauseBgPanel/PauseTitleText/ResumeBtn/QuitBtn`，与脚本完全不匹配，根节点甚至没挂对应 Form 组件。
2. **SerializeField 字段全是空引用**：即使根节点挂对了组件，字段也几乎全是 `fileID: 0`，没有真正连接，只能靠脚本里的 `FindChildByName` 运行时按名兜底——而且很多时候子节点命名（如 `ResumeBtn`）和兜底查找的字符串（`ResumeButton`）都不一致，兜底也会失效。

用户决策（已拍板，不要再问）：
- 全部 10 个既有 Form 都按"重建层级 + 接脚本 + 贴新图"的完整标准做，不再抽查分级处理。
- 子节点命名统一改成和脚本期望的字符串完全一致（而不是改脚本去适配现有命名）。

## 三、已完成：10/11 个 Form 的 Prefab 重建（全部验证通过）

通过 10 个并行 client-unity Agent（Unity-skills MCP REST API：`http://127.0.0.1:8090`，注意不是 localhost）完成，流程都是：`prefab_instantiate` → 改层级/贴素材/直连字段 → `prefab_apply` → 删除临时实例 → 读 YAML 验证 fileID 非 0。

| Form | Prefab 路径 | 处理方式 | 字段连接 | 已知缺口 |
|---|---|---|---|---|
| MainMenu | `Assets/Resources/Prefab/UI/MainMenu.prefab` | 清空 PauseMenu 残留重建 | `_startBtn`/`_settingsBtn` 直连验证通过 | hover 高亮态素材未接入（API 限制，需手动 Inspector 设 SpriteSwap）；mockup 退出按钮未做（脚本无字段） |
| PauseMenu | `Assets/Resources/Prefab/UI/PauseMenu.prefab` | `ResumeBtn`→`ResumeButton`、`QuitBtn`→`QuitButton` 改名，新增 `SettingsButton` | `_resumeBtn`/`_settingsBtn`/`_quitBtn` 直连验证通过 | 按钮按下态(pressed)贴图缺失 |
| CombatHUD | `Assets/Resources/Prefab/UI/CombatHUD.prefab` | 原骨架基本对，新建 4 个缺失节点（AmmoText/ZoneTimerText/MinimapImage/LogRowTemplate） | 12 个字段全部直连验证通过 | 5 张素材（buff图标×3/boss阶段图标/sidebar背景）因脚本无对应字段未接入，需脚本先扩字段 |
| CharacterSelect | `Assets/Resources/Prefab/UI/CharacterSelect.prefab` | 清空 PauseMenu 残留重建（脚本是空壳，无字段绑定需求） | N/A（空壳脚本） | 角色肖像专用立绘缺失，临时用 player idle 序列帧占位，效果不符 mockup |
| RunResult | `Assets/Resources/Prefab/UI/RunResult.prefab` | 清空 PauseMenu 残留重建（脚本是空壳） | N/A（空壳脚本） | 返回按钮/标题装饰无专属素材；**过程中误改了 Settings 临时实例的 TitleText 位置，已确认落盘（见下方四.2）** |
| TattooStudio | `Assets/Resources/Prefab/UI/TattooStudio.prefab` | 贴背景图+关闭按钮图标，新建 `BuildPreviewRoot` 容器 | `_canvasGroup`/`_closeBtn`/`_buildPreviewRoot` 直连验证通过 | **根节点 `m_LocalScale` 写盘异常，见下方四.1，需人工在 Editor 确认** |
| TattooEnchant | `Assets/Resources/Prefab/UI/TattooEnchant.prefab` | 清空 PauseMenu 残留重建（脚本是空壳） | N/A（空壳脚本） | 关闭按钮无专属图标素材，目前白块占位 |
| Shop | `Assets/Resources/Prefab/UI/Shop.prefab` | 占位骨架升级为完整层级，新建金币图标/文字、6 个商品格、刷新按钮 | `_canvasGroup`/`_coinText`/`_inventoryRoot`/`_closeBtn` 直连验证通过 | 刷新按钮脚本无对应字段，仅搭视觉；**发现项目级 CJK 字体缺口，见下方四.3** |
| ThreeChoice | `Assets/Resources/Prefab/UI/ThreeChoice.prefab` | 清空 PauseMenu 残留重建（脚本是空壳） | N/A（空壳脚本） | 寒冰卡选中态雪花图标、紫色卡数字角标贴图缺失 |
| Settings | `Assets/Resources/Prefab/UI/Settings.prefab` | 层级本身是对的，27 个字段（实际数比预估的21多）全是空引用 | 27 个字段逐项直连验证通过 | 无 mockup，跳过像素级对齐；TitleText 位置带有 RunResult Agent 的误改残留，留给阶段6统一处理 |

## 四、三个需要人工关注/后续处理的硬问题

### 1. TattooStudio.prefab 根节点 `m_LocalScale` 写盘异常
场景里读取正常是 `(1,1,1)`，但 `prefab_apply` 后磁盘 YAML 仍写 `(0,0,0)`。可能是 Unity 对 Canvas 根 RectTransform scale 序列化的特殊行为（运行时会被覆盖，可能无害），也可能是 unity-skills 工具链的局限。**需要人工在 Unity Editor 里打开 `Assets/Resources/Prefab/UI/TattooStudio.prefab` 检查 Inspector 里 Scale 字段实际显示值**，如果异常需手动改成 1 并保存。

### 2. 多 Agent 并行操作同一 Unity 实例的交叉污染（已发生，已确认低风险）
RunResult 的 Agent 在场景中操作时，因对象按名字查找（而非按 instanceId/entityId）误命中了 Settings Agent 的临时实例 `Settings_BUILD_001`，把其中 `TitleText` 的 RectTransform 从居中占位改成了顶部偏移布局。已用 Grep 核实这个改动确实落盘进了最终 `Settings.prefab`。**评估结论：不需要紧急回退**——因为 Settings 本身还没有 mockup，阶段 6 联调微调时反正要统一核对所有节点位置，到时候一并处理即可；且新位置（顶部居中）视觉上比原来的纯居中占位更合理。
**给后续多 Agent 并行任务的教训**：凡是涉及 unity-skills MCP 对同一个 Unity Editor 实例做并行编辑的场景，objet 查询/创建/reparent 一律用 `instanceId`/`entityId` 精确定位，不要用裸 `name` 字符串——多个 Agent 大概率会建出同名节点（如多个 Form 都有 `TitleText`/`SettingsButton`），按名字查找会跨实例串号。

### 3. 项目级 CJK 字体缺口（影响范围最大）
所有 TMP_Text 用的默认字体是 `LiberationSans SDF`，不含中文字形，导致**所有 Form 的中文文本运行时都会显示成方块 □**（不止 Shop，CombatHUD/RunResult/PauseMenu 等所有用了中文 TMP_Text 的地方全部受影响）。这是字体资源配置问题，不属于任何单个 Prefab 任务范围。
**已开后台任务跟踪**：`task_006bf2f7`「接入 CJK 字体修复中文显示方块问题」，**尚未启动**，需要用户点击启动或后续显式要求处理。建议方案：接入 Noto Sans SC / 思源黑体生成 TMP Font Asset（含中文字形子集），替换 TMP 默认字体配置，抽查 Shop/CombatHUD 等已搭好的 Prefab 确认显示正常。

## 五、进行中、被额度打断的任务：SelfTattooForm（第 11 个，全新功能）

这是唯一还没有脚本和 Prefab 的 Form（之前的功能未实现）。已派出 Agent **`agentId: a1bb804359db85df3`**（client-unity），任务内容：

1. 读 `Assets/Scripts/Modules/Input/InputModule.cs`，新增 `IsSelfTattooTogglePressed()`（绑定 Tab 键，命名风格与现有方法一致）—— 这是项目硬性约束："所有按键输入必须走 InputModule"，不能在 Form 里直接写 `Input.GetKeyDown`。
2. 新建 `Assets/Scripts/Modules/Tattoo/UI/SelfTattooForm.cs`（参考同目录 `TattooStudioForm.cs`/`ShopForm.cs` 的写法：实现 `IExclusiveUIForm`，`Awake` 时 `SetActive(false)`，`Start` 轮询等 `GameApp`/`ModuleRunner` 就绪后注册到 `UIModule`，`OnDestroy` 时反注册），`Update()` 里轮询 `IsSelfTattooTogglePressed()` 做开关切换。
3. 字段设计按 mockup（`openspec/changes/12-core-ui-screens/art/mockups/SelfTattooForm.png`）和素材目录（`Assets/Resources/Sprite/UI/SelfTattooForm/`）实际内容定，不超出 mockup 范围过度设计。
4. 明确要求：如果 `TattooModule` 没有现成的 `StartSelfTattoo(...)` 之类方法，**不要编造调用**，先用 codebase-memory MCP 查实际签名，没有就留 TODO 并如实告知。
5. 用 unity-skills MCP（同样是 `http://127.0.0.1:8090`）搭 `Assets/Resources/Prefab/UI/SelfTattoo.prefab`，所有字段直连验证。

DataTable 行已存在不需要再加：`Assets/Resources/DataTable/UIFormConfig.json` 里 `{ "Id": 11, "FormName": "SelfTattooForm", "PrefabPath": "UI/SelfTattoo", "SortOrder": 10, "IsExclusive": true }`。

该 Agent 跑了 60 次工具调用后撞到额度上限（重置时间 8:30am Asia/Shanghai），**没有给出最终结果汇报**，不确定它具体进行到哪一步、脚本/Prefab 有没有保存成功，**新窗口里第一件事应该是用 SendMessage 恢复这个 agentId 看它汇报进度**，而不是假设它已经完成或假设它什么都没做。

## 六、新对话窗口如何接续

1. 先尝试：`SendMessage(to: "a1bb804359db85df3", message: "额度已恢复，继续完成任务，不要中途询问，直接做完并总结")`，看能不能接回 SelfTattooForm 的 Agent。
   - 如果 agentId 在新窗口/新会话里失效（agent 不一定跨对话保留，需要实测），就直接重新派一个 client-unity Agent，把本文档"五、"里的任务说明原样给它。
2. SelfTattooForm 完成后，11 个 Form 的 Prefab+代码阶段就全部结束，进入 `CLAUDE.md §六` 的**阶段 6：联调微调**——把运行时截图和各 Form 的 mockup（`art/mockups/`）并排比对，列偏差清单后逐个修。Settings 因为没有 mockup，到这一步需要先确认是否要补出图。
3. 阶段 6 做完后，记得处理本文档"四、"列的 2 个遗留硬问题（TattooStudio scale 异常、CJK 字体），CJK 字体那个已经有后台任务 `task_006bf2f7` 等用户启动。
4. 全部完成后按 `CLAUDE.md §六` 收尾：`openspec archive-change 12-core-ui-screens` + 同步更新 `项目知识库（AI自行维护）/INDEX.md`。

## 七、关键路径速查

- openspec change：`openspec/changes/12-core-ui-screens/`
- mockups：`openspec/changes/12-core-ui-screens/art/mockups/<FormName>.png`
- 已拆分素材：`Assets/Resources/Sprite/UI/<FormName>/`
- Prefab：`Assets/Resources/Prefab/UI/<FormName 去掉Form后缀>.prefab`
- Form 脚本：`Assets/Scripts/Modules/{UI,NPC/UI,Event/UI,Tattoo/UI}/<FormName>.cs`
- Unity-skills MCP REST API：`http://127.0.0.1:8090`（不是 localhost，Host header 校验会拒绝）
- DataTable：`Assets/Resources/DataTable/UIFormConfig.json`
