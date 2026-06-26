# Proposal — 10-settings-form

> **范围**：新增「设置面板（SettingsForm）」UI，覆盖音量调节 / 画质档位 / 按键重绑定 / 保存取消。
> **决策日期**：2026-06-26
> **决策方式**：用户在 prompt 中已锁定主体方案，阶段 1 启动前完成 5 项关键决策（见 design.md §决策记录）。
> **驱动**：游戏目前无系统设置入口，玩家无法调音量、改画质、重映射按键。

## 为什么做

当前框架已有：
- `InputModule`（项目硬约束：所有按键输入必须走它）
- `AudioModule`（背景音乐 + 音效双通道，目前音量固定）
- URP 渲染管线（多档 `UniversalRenderPipelineAsset` 待挂载）
- `UIModule` + `UIFormConfig` 配置驱动的表单系统

缺少把这些能力暴露给玩家的入口。本次新增一个 UI 表单 + 一个轻量 `SettingsModule` 承载持久化与运行时应用。

## 目标（DoD）

- [ ] 玩家从主菜单 / 暂停菜单可打开 `SettingsForm`
- [ ] 音量：两条滑动条（BGM / SFX），**拖动即时生效**
- [ ] 画质：三档单选（低 / 中 / 高），切换即时套用 URP RP Asset
- [ ] 按键重绑定：移动（4 键合并 or WASD 单组）/ 攻击 / 暂停 共 3 个绑定槽，使用 InputModule 提供的 RebindingOperation API
- [ ] 保存按钮：把当前值写入存档，关闭面板
- [ ] 取消按钮：**回滚到打开面板时的快照**（音量恢复、画质恢复、重绑定撤销），关闭面板
- [ ] 重绑定冲突：玩家把目标键绑到一个已被占用的功能上时，**弹提示拒绝**，不写入
- [ ] 设置持久化到 `SettingsSave`（与音量画质同档，PersistentDataPath）
- [ ] 下次启动自动应用上次保存的设置
- [ ] 编译通过 + 至少 1 个 EditMode 单元测试（重绑定冲突检测）

## 非目标（明确不做）

- ❌ 多语言切换（Q4 决策范围外）
- ❌ 手柄 / 触屏支持（PC 键鼠优先）
- ❌ 灵敏度 / 反转轴 / 死区调节
- ❌ 画质细分参数（仅切 RP Asset 三档，不暴露阴影/抗锯齿/后处理粒度）
- ❌ 移动端适配
- ❌ 设置项搜索 / 分类多 Tab（单一 Form 一屏铺开）
- ❌ Cloud Save / Steam Cloud 同步

## 阶段拆分（UI 5 阶段子流程）

| 阶段 | 内容 | 主导 Agent |
|---|---|---|
| **1 需求设计** | proposal + design + tasks + spec + art/requirements.md 三表 | 主对话（producer / gd-system / ai-art） |
| **2 效果图设计** | art/prompts.md 单页效果图提示词 | art-ui |
| **3 效果图生成** | art/mockups/SettingsForm.png + 生成记录 | codex-image-gen |
| **4 Prefab + 代码** | 标注稿 ∥ Prefab + SettingsModule + SettingsForm 脚本 | art-ui ∥ client-unity（Fan-Out） |
| **5 联调微调** | 运行时截图 vs mockups 对比 + 偏差修复 | client-unity + 用户 |

每阶段产出后停下来等用户确认才进下一阶段。

## 风险

| 风险 | 缓解 |
|---|---|
| InputModule 的 RebindingOperation API 现状未知，可能需要扩展 | 阶段 4 前由 client-unity 先做 spike，缺什么补什么并通知用户 |
| URP RP Asset 三档资产尚未创建 | 阶段 4 由 client-ta / client-unity 创建占位 RP Asset，命名固定 `URP-Low/Med/High.asset` |
| 取消回滚需要打开时快照所有可变状态 | design.md 中明确"快照模型 + 应用 + 提交"三态分离 |
| 重绑定冲突检测漏判（如同一物理键不同 modifier） | MVP 只查 Path 字符串相等，不做 modifier 组合 |

## 引用

- [.claude/CLAUDE.md §六 工作流系统](../../../.claude/CLAUDE.md) UI 制作子流程强制时序
- [.claude/skills/ai-art/references/drawing-prompt-UI.md](../../../.claude/skills/ai-art/references/drawing-prompt-UI.md) UI 出图前置三表
- [Assets/Scripts/Modules/InputModule](../../../Assets/Scripts/Modules/) InputModule 实现入口（待 client-unity 阶段 4 调研）
