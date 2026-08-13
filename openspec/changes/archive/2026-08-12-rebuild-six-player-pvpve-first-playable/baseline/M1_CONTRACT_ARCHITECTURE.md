# M1 公共合同与数据骨架

## 项目层级与选择

- 项目层级：需要持续迭代的小型游戏，不建立新的通用框架。
- 候选 A：直接扩写现有 `TotemGameplayModels` 可变 class。优点是文件少；缺点是旧 50 人语义与新合同继续混杂，纯逻辑边界不清。
- 候选 B：为第一阶段新建完整程序集和 DI 容器。优点是隔离最强；缺点是迁移成本、程序集依赖和启动复杂度超出单人 + AI 第一版需要。
- 候选 C：在现有 `Hotfix` 程序集内增加 `Runtime/FirstPlayable`，使用不可变 runtime struct、可序列化配置 DTO 和薄服务桥。优点是可渐进接入、可纯逻辑测试、无需新增运行依赖；缺点是迁移期间新旧合同并存。
- 决策：选择 C。M1 只建立合同，不把旧运行流程强切到新合同；M2 起按 feature slice 迁移。

## 模块边界

| 模块 | 职责 | 数据所有权 |
|---|---|---|
| MatchContracts | 6 人/3 队 roster、ID、阶段、时长、共享 gameplay command | runtime 使用只读 struct；时长由可序列化配置持有 |
| CombatContracts | 单枪械命中、有效直接伤害、元素来源、反应归因、效果事件和 resolution identity | 命中与队列热路径使用只读 struct，不持有 GameObject |
| SocialContracts | 倒地/救援/观战、构筑快照、精确成果、颜料请求/原子转移 | 快照 DTO 可序列化；事务合同只表达意图与版本，不直接改库存 |
| AssetContracts | 稳定 form/slot/asset/VFX key 与 fallback | 引用 `rebaseline-pvpve-art-resources` 的交付 ID，不复制视觉判断 |
| Validator/Diagnostics | 配置完整性、优先级、容量、handoff 和 fallback 检查 | Editor/测试侧分配允许；不进入常规帧热路径 |

## 场景与装配

- `TotemGameRuntime` 继续作为唯一运行时装配入口；M1 不添加第二 Bootstrap。
- 后续服务从配置构造纯 C# 合同，再由现有 MonoBehaviour/Service 负责 Transform、Collider、Prefab 和 UI 映射。
- 不依赖 `Awake` 偶然顺序；阶段转换最终只由 match-flow coordinator 发布。

## 通信规则

- 真人设备输入仍只由 `TotemInputService` / `ITotemInputProvider` 读取，然后翻译为 `TotemGameplayCommand`。
- Bot 直接生产同一种 command，不模拟键盘鼠标，不调用设备 API。
- 规则服务接收 command 并统一执行阶段、队伍、资源与伤害校验。
- 表现只消费已结算 `TotemEffectPresentationInstruction`；延迟和 VFX 不反向改变模拟结果。

## 测试分层

- EditMode：roster、阶段转换、命令一致性、有效伤害、优先级/seed、归因、序列化、资源 fallback、颜料事务合同。
- PlayMode：旧 Launch/CombatHUD 链继续启动并明确退出；M2 起再验证 6 人场景生成。
- 全量诊断：新增 `Totem First Playable Contract` 场景，旧 50 人诊断暂保留，直至 M2 正式切换并更新断言。

## 当前不做

- 不实现第四/第五轮、Boss、撤离、线上协议或最终 UI。
- 不新建 DI/事件总线/ScriptableObject 数据库。
- 不把美术 VFX key 当成玩法事件，也不在配置中推断敌人弱点造型。

## Gate 证据

- `TotemFirstPlayableContractTests`：M1 完成时 `10/10 passed`；加入 M2 roster/友伤测试后扩展为 `12/12 passed`。
- `Totem First Playable Contract` 全量诊断项通过；报告 `gf-diagnostics-run-all_20260811_104916.json` 总计 `33 success / 5 failure / 36 warning`。
- 5 个 failure 与 M0 相同，属于资源分类、工作区、旧地图/宝箱断言与 player Prefab 组件，不是新合同回归。
- M1 仅新增未接入旧 runtime 的合同；M0 的核心 PlayMode smoke `ada99223` 已 `1/1 passed`。M2 更新后的六人版本又由任务 `5ed0f79f` 复验为 `1/1 passed`，证明合同接入后旧 Launch/CombatHUD 启动与退出链仍可用。
