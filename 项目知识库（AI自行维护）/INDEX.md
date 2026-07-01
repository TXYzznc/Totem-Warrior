# 项目知识库索引

> AI 自行维护的知识库入口。**任何 lead / system 层 agent 在做决策前必须先查阅本索引**。
>
> 本知识库与 `openspec/changes/` 一站式工作流配套：每次开发都会沉淀新条目。

---

## 一、目录结构

| 目录 | 说明 | 谁来写 |
|---|---|---|
| `outputs/` | AI 工作时输出的临时文件（草稿、中间结论） | AI 自动写 |
| `raw/` | 用户筛选并保留的原始资料（访谈、调研、PRD 草稿、初版 GDD） | 用户筛选后归档 |
| `raw/初版GDD-2026-06/` | 项目立项阶段产出的初版 GDD（9 份，约 95KB） | 已归档 |
| `wiki/` | AI 整理后的结构化知识维基（按系统分类） | AI 整理，用户校对 |

## 二、工作流

```
AI 输出 → outputs/ → 用户筛选 → raw/ → 用户指令 → AI 整理到 wiki/ → 更新本 INDEX.md
```

**规则**：

- AI **不要直接修改 `raw/`** 中的文件——那是用户认证后的原始素材。
- `wiki/` 由 AI 维护，但每次提交前回扫一次链接有效性。
- `outputs/` 中的文件是临时产物，定期清理。
- 大型决策结束后（grill-me → openspec → 落地完成），**必须**把决策摘要、关键 trade-off、被否定的备选方案写入 `wiki/`。

---

## 三、Wiki 目录（按系统）

### 3.1 设计层（owner: gd-lead / gd-system）

- **[GDD v2.1 全套设计文档（2026-06-25 升级）](GDD-v2/00-总策划案v2.md)** — 2026-06-25 — Phase A grill-me 5 条挖透 + **v2.1 grill 16 轮 24 项修订** 后产出。1 份总策划案 + 15 份系统 GDD（[systems/](GDD-v2/systems/)）+ 16 份模块详设（[modules/](GDD-v2/modules/)）+ 1 份全局契约（[CONTRACT.md](../openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md) 含 50+ 事件签名，v2.1 追加 5 个新事件）。**核心决策**：UGUI 轻量 MVP / 纹身师=词缀附魔工 / 玩家自纹身读条 3-8s / 死亡宝箱半颜料半配方拓本 / 10-15min 单局 / 2 技能槽 / 20+29+1 AI 配比 / Hades 精致 2.5D / 异能者身份 / 颜料三档 / 配方 4 来源 / 伪联机起步未来真联机零改动。详见 [openspec/changes/05-gdd-v2-full-design-docs/](../openspec/changes/05-gdd-v2-full-design-docs/)。
- **[初版 GDD 设计文档（2026-06 baseline）](wiki/初版GDD设计文档.md)** — 2026-06-25 — 项目立项阶段（2026-06-22~23）的初版 GDD，9 份文档涵盖玩法定位（Roguelike + BR）、纹身构筑系统（6×7×8 三层正交）、世界观（多元末日 + 实验体）、联机三层架构、技术选型。**已被 GDD v2 全面继承+扩展**。原始素材见 [raw/初版GDD-2026-06/](raw/初版GDD-2026-06/)。已派生 [01-tattoo-framework-rewrite](../openspec/changes/01-tattoo-framework-rewrite/) 落地。

### 3.2 客户端架构（owner: client-lead）

- **[Tattoo 系统重构（v1.0 → v2.0 框架化）](wiki/Tattoo系统重构.md)** — 2026-06-24 — 把原 Tattoo 业务（21 SO 子类 + Composer + CombatRunner + IMGUI）整体重写为 IGameModule 框架实现。详见 [openspec/changes/01-tattoo-framework-rewrite/](../openspec/changes/01-tattoo-framework-rewrite/)

### 3.3 服务端 / 网络（owner: net-lead）

- _尚无条目（项目当前为单机原型）_

### 3.4 美术（owner: art-director）

- **[UI 结构先行规范 v3](wiki/UI结构先行规范.md)** — 2026-07-01 — v2「三表」升级为 v3「结构先行」：单文件 `prefab-layout.md`（含 RectTransform 数据）同时喂养效果图长宽反哺 / 素材拆分节点树 / Prefab 层级搭建；6 阶段流程（结构设计→效果图设计→效果图生成→素材拆分→拼装实现→联调微调），无豁免；新建 `unity-rect-transform` SKILL 承担阶段 1 结构产出与阶段 5 Prefab 读取。详见 [openspec/changes/archive/2026-07-01-17-ui-structure-first/](../openspec/changes/archive/2026-07-01-17-ui-structure-first/)
- ~~[UI 先定表规范](wiki/UI先定表规范.md)~~ — 2026-06-24 — v2 三表规范，**已被上方 v3 结构先行取代**（2026-07-01）；仅保留作为历史记录。详见 [openspec/changes/archive/2026-06-29-04-ui-planning-first/](../openspec/changes/archive/2026-06-29-04-ui-planning-first/)
- **gameplay-character-art** — 2026-06-30 — 4 角色 × 4 方向 × 4 帧 sprite 系统（Player1 16 + Player2 4 + Player3 4 + Boss1 16 = 40 张）+ 自动化 Animator/Controller/Prefab 生成（`Tools/Character/Reimport Then Generate All`）+ PlayerAnimatorBridge 接 InputModule。**关键决策**：1536×1024 横向 4 帧 + PPU=256；禁用 flipX 走 4 方向真贴图；AnyState 用 Dead Bool 网关防 Death 抢回 Idle；Down=0/Up=1/Left=2/Right=3 与 `PlayerAnimatorBridge.ComputeDirection` 严格对齐。详见 [openspec/specs/gameplay-character-art/](../openspec/specs/gameplay-character-art/) + [archive/2026-06-30-17-gameplay-character-art/](../openspec/changes/archive/2026-06-30-17-gameplay-character-art/)。**已知占位**：Player1/Walk × 4 + Player1/Death/Down 因 codex 配额耗尽用 Idle/Right 复制顶替，待补真图；BUG-17-01 reimport 后首帧 Animator runtime 引用 null（绕过：等 1-2s 再 Play）。

### 3.5 工具链 / DevOps（owner: tools-engineer / devops-engineer）

- **[SKILL 路由系统统一（硬墙白名单 + gitnexus 清除）](wiki/SKILL路由统一.md)** — 2026-06-24 — 消除 `.claude/` 下两套 SKILL 路由机制的语义冲突；清除 gitnexus；19 个 agent 措辞统一为硬墙；7 个 agent 加共享 SKILL。详见 [openspec/changes/02-skill-routing-unification/](../openspec/changes/02-skill-routing-unification/)
- **[工作流迁移到 openspec/changes/ 一站式目录](wiki/工作流迁移.md)** — 2026-06-24 — 删除「工作/」整个目录，5 Phase 沉淀到 openspec change：proposal/design/tasks/specs（原生）+ brainstorm.md + CONTRACT.md + art/ + tests/。详见 [openspec/changes/03-workflow-on-openspec/](../openspec/changes/03-workflow-on-openspec/)
- **[Codex 批量出图协议（L1+L2）](wiki/Codex批量出图协议.md)** — 2026-06-25 — codex-image-gen SKILL 从「每张图一次 codex exec」改造为「L1 进程内并行（≤12 张/批）+ L2 合并画布（≤256×256 透明 icon 合并到 1024×1024 后 ImageCut 切割）」。9 张 demo 实测：imagegen 调用 9→1，节省 88.9%；tokens 节省 ~81%。详见 [openspec/changes/05-codex-batch-art-protocol/](../openspec/changes/05-codex-batch-art-protocol/)
- **[unity-skills SKILL CJK 编码修复](wiki/unity-skills-CJK编码修复.md)** — 2026-06-29 — `unity_skills.py` Python CLI 在 Windows cp936 下 argv 中文编码风险的四路防御：A. `GetCommandLineW` ctypes 加固；B. **`--stdin-json` 模式**（100% 编码安全唯一入口）；C. SKILL.md 强制调用约定；D. 字节链路回归 + live Editor round-trip 测试（20/20 + 7/7）。13 期 Prefab 残片根因在 agent → CLI 胶水层（不可见），server 端 UTF-8 全链路正确无需改动。详见 [openspec/changes/archive/2026-06-29-14-mcp-encoding-fix/](../openspec/changes/archive/2026-06-29-14-mcp-encoding-fix/) + spec [openspec/specs/mcp-encoding-fix/](../openspec/specs/mcp-encoding-fix/)。
- **unity-skills 多项目端口自动路由** — 2026-07-01 — server 端已按项目 hash 动态分配端口写入 `~/.unity_skills/registry.json`，但 `unity_skills.py` 客户端仍硬编码 `localhost:8090`，多项目并存时打到错误 Editor。修复：CLI 加寻址优先级链 `--port` > `--target` > env `UNITY_SKILLS_TARGET` > cwd 通过 `os.path.commonpath` 反查 registry > `DEFAULT_PORT=8090`；加 `health` 子命令报告 `source` + `url` 便于排障；default fallback warning 延迟到入口点 flush（避免 CLI `--target` 覆盖成功后误打）；SKILL.md / playtest-driver / unity-dev 文档所有硬编码端口改为「自动路由」说明 + `--stdin-json` 示例。sync 到 `.agents/` mirror byte-identical。详见 [openspec/changes/archive/2026-07-01-24-unity-skills-port-routing/](../openspec/changes/archive/2026-07-01-24-unity-skills-port-routing/) + spec [openspec/specs/unity-skills-routing/](../openspec/specs/unity-skills-routing/)。

### 3.6 已废弃 / 历史决策（owner: producer）

- _尚无条目_

---

## 四、当前活跃的 OpenSpec 变更

| ID | 标题 | 阶段 | 负责 agents |
|---|---|---|---|
| **10-settings-form** | SettingsForm 走完整 5 阶段视觉流程（效果图设计 → 生成 → 联调），展示 MVP UI 制作完整链路 | 阶段 1 进行中（proposal/design 草稿已就位，等用户确认；阶段 2-5 未开启） | art-ui / codex-image-gen / client-unity / qa-engineer |

> 已归档：
> - 05-codex-batch-art-protocol → `openspec/changes/archive/2026-06-25-05-codex-batch-art-protocol/`，详见 [wiki 条目](wiki/Codex批量出图协议.md)
> - **05-gdd-v2-full-design-docs** → `openspec/changes/archive/2026-06-25-05-gdd-v2-full-design-docs/`（2026-06-25 用户 review 通过），交付物 = 1 总策划 + 15 系统 GDD + 16 模块详设 + CONTRACT（v2.1 含 50+ 事件 / 模块依赖图 / IPlayerController / 50 actor 预算 / 12 个 v2.1 追加事件）。详见 [GDD v2.1 入口](GDD-v2/00-总策划案v2.md)
> - **08-codex-art-gen-mcp** → `openspec/changes/archive/2026-06-26-08-codex-art-gen-mcp/`（2026-06-26 归档）。把 Codex CLI 生图固化为常驻 MCP 服务（`dispatch_l1` / `dispatch_l2` / `write_record` 三件套 + cwd 隔离 + 并发 2 路 + L2 合图 + chroma_key 工作流去绿）。实测 v21 22 张资产单次跑完（L1 14 张 13.5 min + L2 8 张 1.5 min）。
> - **09-mcp-decouple** → `openspec/changes/archive/2026-06-26-09-mcp-decouple/`（2026-06-26 归档）。把 08 MCP 内的 v21 业务硬编码全部下沉到调用方：MCP 只剩跑 codex + 后处理，业务（STYLE_BASE / 分类 / 命名规则）写入 `tools/codex-art-gen-helper/expand_v21.py`。跨项目复用此 MCP 时只需新建一个 `expand_<project>.py`。同期修复两个关键 bug：subprocess 在 MCP stdio 下必须 `stdin=DEVNULL` 防协议管道死锁；prompt 不能给完整返回 JSON 占位，server.py 改用纯磁盘核验（防 codex 复读模板假装成功）。
> - **02-skill-routing-unification** → `openspec/changes/archive/2026-06-29-02-skill-routing-unification/`（2026-06-29 归档）。SKILL 路由白名单制硬墙化 + gitnexus 清除 + 共享 SKILL 显式登记 + 7 个 agent 措辞统一。spec 沉淀到 [openspec/specs/skill-routing/](../openspec/specs/skill-routing/)。
> - **03-workflow-on-openspec** → `openspec/changes/archive/2026-06-29-03-workflow-on-openspec/`（2026-06-29 归档）。工作/ 整目录删除 + 全职责沉淀到 openspec change 一站式目录树（proposal/design/tasks/specs + brainstorm + CONTRACT + art/ + tests/）。spec 沉淀到 [openspec/specs/workflow/](../openspec/specs/workflow/)。
> - **04-ui-planning-first** → `openspec/changes/archive/2026-06-29-04-ui-planning-first/`（2026-06-29 归档）。UI 类型素材出图前必须先定表（页面清单/复用组件清单/组件状态表）；ai-art 自动起草供用户审阅。spec 沉淀到 [openspec/specs/ui-planning/](../openspec/specs/ui-planning/)。**⚠️ 已被 17-ui-structure-first 取代**（2026-07-01）：ui-planning 的 2 条 Requirement 标 REMOVED，capability 废弃；新 UI 走 ui-workflow 6 阶段结构先行流程。
> - **11-skill-governance** → `openspec/changes/archive/2026-06-29-11-skill-governance/`（2026-06-29 归档）。109 SKILL description 全部 60-250 字符 + 触发词；6 组重叠 SKILL 加 ❌ 不适用 划界；MCP 启用从 8 降到 4（每次启动节省 5-15k token）；新增 audit_skills.py / audit_skill_usage.py 月度防腐。spec 沉淀到 [openspec/specs/skill-governance/](../openspec/specs/skill-governance/)。
> - **12-core-ui-screens** → `openspec/changes/archive/2026-06-29-12-core-ui-screens/`（2026-06-29 归档）。10 个核心 Form 走完整 UI 5 阶段流程；9 个 mockup 已确认；7 个 Form 视觉对齐；4 个 Prefab（Settings / SelfTattoo / ThreeChoice / TattooEnchant）因 MCP 中文编码 bug 移交 13-fix-broken-prefabs follow-up。spec 沉淀到 [openspec/specs/core-ui-screens/](../openspec/specs/core-ui-screens/)。
> - **13-fix-broken-prefabs** → `openspec/changes/archive/2026-06-29-13-fix-broken-prefabs/`（2026-06-29 归档）。修复 4 个 Prefab（Settings / SelfTattoo / ThreeChoice / TattooEnchant）—— unity-skills MCP（`http://localhost:8091/`）中文编码 bug 导致的乱码 + Sprite 未绑。Fan-Out 模式 1：4 个 client-unity agent 并行原地 Edit YAML（不走 MCP），4/4 Prefab `�` 残留清零 + 必要 Sprite 全部绑定 + 仅 4 个目标 Prefab mtime 落在本 session。MCP 编码 bug 根因 spike 完成（在 Codex/Claude → unity_skills.py CLI 胶水层 Windows cp936/argv 编码），本期不修，留下一期 **14-mcp-encoding-fix**。PlayMode 运行时联调（4 个 Form 与 mockup 视觉分组比对）交付用户人工执行。spec 沉淀到 [openspec/specs/fix-broken-prefabs/](../openspec/specs/fix-broken-prefabs/)。
> - **14-mcp-encoding-fix** → `openspec/changes/archive/2026-06-29-14-mcp-encoding-fix/`（2026-06-29 归档）。`unity_skills.py` Python CLI 端到端 CJK 编码加固：路径 A `GetCommandLineW + CommandLineToArgvW` argv 重写（Python 3.6+ 已是 no-op，但兜底未来版本回退）+ 路径 B `--stdin-json` 模式（**100% 安全唯一入口**，binary pipe 绕开 argv/cmd.exe）+ 路径 C SKILL.md 强制约定（CLAUDE.md §六.5 交叉引用）+ 路径 D 字节链路回归测试（4 脚本 × 5 case = 20/20 PASS）。Phase 5 live Editor `console_log` round-trip 7/7（含 emoji / ▶ / 200 字长串 / 13 期 ground truth）；13 归档的 4 个 Prefab mtime drift = 0.000s 零侵入校验。结论：13 残片根因在不可见的 agent → CLI 胶水层（Codex/Claude），server 端 UTF-8 全链路正确，无需改 C#。spec 沉淀到 [openspec/specs/mcp-encoding-fix/](../openspec/specs/mcp-encoding-fix/)。wiki：[unity-skills CJK 编码修复](wiki/unity-skills-CJK编码修复.md)。
> - **01-tattoo-framework-rewrite** → `openspec/changes/archive/2026-06-29-01-tattoo-framework-rewrite/`（2026-06-29 归档）。Tattoo 业务整体迁移到 IGameModule 框架：5 张 DataTable + 21 个 Strategy（6 Part / 7 Element / 8 Shape）+ TattooModule / SpawnerModule / CombatModule / VFXModule + UI Toolkit CombatHUD + Launch.unity + 21 张美术资源（17 codex-image-gen + 4 PIL 降级） + EditMode 143/144 + 336 穷举测试。Phase D 包含 ModuleRunner.StartAsync 退出条件 bug 修复。spec 沉淀到 [openspec/specs/tattoo/](../openspec/specs/tattoo/)。
> - **07-main-menu-flow** → `openspec/changes/archive/2026-06-29-07-main-menu-flow/`（2026-06-29 归档）。启动场景拆出 `Assets/Scenes/MainMenu.unity`（不挂 GameApp，零业务模块开销）；点开始游戏 `SceneManager.LoadScene("Launch")` 才挂 GameApp；UIModule 订阅 `OnGameReady` 从 UIFormConfig 动态实例化 9 个 Form Prefab 到 `UIRoot`（DontDestroyOnLoad）+ 强制矫正 RenderMode/RectTransform。Launch 场景 `_Temp` 实例清理完毕；PlayMode 实测 0 异常 + `[MainMenuLauncher] Action=Ready`。spec 沉淀到 [openspec/specs/main-menu-flow/](../openspec/specs/main-menu-flow/)。
> - **17-gameplay-character-art** → `openspec/changes/archive/2026-06-30-17-gameplay-character-art/`（2026-06-30 归档）。4 角色 sprite + Animator 系统全套：40 张 sprite（codex-art-gen 3 组并行 + 1 张补图）+ `Assets/Editor/Character/{AnimatorGenerator, CharacterSpriteImportProcessor}.cs`（4 个菜单一键生成 40 anim + 4 controller + 4 prefab，自动 PPU=256 + 4 帧均切 + Down=0/Up=1/Left=2/Right=3）+ `PlayerAnimatorBridge`（订阅 PlayerDiedEvent，`_isDead` 早退防 AnyState 抢回）+ SpawnerModule 改 Resources.Load prefab。Round 1 即 18/18 TC PASS（5 TC-Art + 13 #16 回归）+ 0 Console Errors。占位：Player1/Walk × 4（Idle 复制）+ Player1/Death/Down（Right 复制），codex 配额恢复后补真图。Follow-up BUG-17-01：reimport 后首帧 Animator runtime null（绕过：等 1-2s 再 Play）。spec 沉淀到 [openspec/specs/gameplay-character-art/](../openspec/specs/gameplay-character-art/)。
> - **20-player-attack-system** → `openspec/changes/archive/2026-06-30-20-player-attack-system/`（2026-07-01 归档）。玩家攻击体系骨架先行 + 8 路并行实装：9 种伤害源 D1~D9（武器普攻/蓄力 / 刺青形状/元素 DoT / Shape 多段链/AOE / 左腿延迟 / 技能 / 敌人打玩家 / 弹药耗尽降级）+ 3 大支柱（Pillar A 构筑可见性 / B 0.3s 手感 / C 暴击只来自 Head）+ StartupSelectForm 起手三选 + PlayerWeaponMounter（5 武器 Cube fallback）+ HumanPlayerController.GetAimTarget 鼠标地面投影 + StatusEffectModule（436 行单 accumulator + struct ActiveStatus）+ PlayerDamageReceiver 死亡防抖 + SkillHitResolver（8 EffectType 分支）+ WeaponTraitConfig.AimSpreadHalfDeg + balance.md（20× 链伤上限）。33 [Test] + 21 TC（qa-engineer 静态分析报 2 真 bug 全修：BUG-20-01 expired 帧丢 tick / BUG-20-02 EffectAppliedEvent.Target 路由 TODO）。骨架先行+并行填充模式 8/8 done。spec 沉淀到 [openspec/specs/player-attack-system/](../openspec/specs/player-attack-system/)。
> - **18-weapon-pickup-and-upgrade** → `openspec/changes/archive/2026-06-30-18-weapon-pickup-and-upgrade/`（2026-07-01 归档）。武器拾取与 3 级升级体系骨架先行 + 3 路 fan-out：WeaponSpawnerModule（精英掉落+宝箱+商人三路径）+ WeaponUpgradeModule（DamageMul=1.2^(L-1) / RangeAdd=0.5*(L-1) / CooldownMul=0.9^(L-1)，L3 满级转金币）+ WeaponPickupTrigger/ChestInteractTrigger/MerchantTrigger 3 个零-GetModule MonoBehaviour（runtime World Canvas + TMP）+ 3 张新 DataTable（WeaponDropConfig/ChestConfig/MerchantConfig）+ InputModule.IsPickupPressed(KeyCode.F)。**关键决策**：CombatModule 主动注入（CONTRACT §H 方案 A）—— ProcessController 查询 GetMultipliers 后调 WeaponModule.SetPendingMultipliers，FireWeapon 消费 pending 字典并应用 finalDamage = BaseDamage * mul.DamageMul（保持 FireWeapon 签名兼容 change 20）；EconomyModule.DeductGold 走反射 + fallback Warn 容错。4 个 [Test]（TC-Pickup-01~04 全 PASS）+ 4 个 KL 已知限制（SpawnElites no-op / DeductGold 反射 / MapGen 反射 / Trigger-Spawner 双路径，全 P3 非阻塞）。spec 沉淀到 [openspec/specs/weapon-pickup/](../openspec/specs/weapon-pickup/)。
> - **19-visual-polish** → `openspec/changes/archive/2026-06-30-19-visual-polish/`（2026-07-01 归档）。4 项视觉打磨并行 fan-out：TC-Polish-01 暴击数字飘字（白色 #FFFFFF 24 / 暴击红色 #FF2222 36 + Scale 弹出 + 对象池 8）+ TC-Polish-02 头顶状态图标（CanvasGroup DOFade + Scale 弹出 / TargetId 过滤 / burn/poison/stun 三 sprite）+ TC-Polish-03 hitspark 粒子 + 镜头抖动（DOShakePosition Kill 旧 tween 防叠加 / 普通 Burst 8 / 暴击 Burst 14）+ TC-Polish-04 HP<30% Vignette 闪烁（URP Volume + Vignette Override 三级 fallback：auto 创建 → 加 Override → 跳过 Warn）。4 个 KNOWN 待美术介入：飘字 Vector3.zero 默认位置 / StatusIconController 手动挂 Actor / Hitspark.prefab 用户创建 / Vignette fallback 已 OK。spec 沉淀到 [openspec/specs/visual-polish/](../openspec/specs/visual-polish/)。
> - **21-startup-select-form-landing** → `openspec/changes/archive/2026-07-01-21-startup-select-form-landing/`（2026-07-01 归档）。补齐 `MainMenu → CharacterSelect → StartupSelect → InGame` 起手流程：MainMenuForm.OnStartClicked 去掉 gs.StartGame() 直调 → 打开 CharacterSelectForm；CharacterSelectForm 从空壳→实体（3 张角色卡片动态生成 + Next 按钮 → 打开 StartupSelectForm）；StartupSelectForm.OnConfirm 末尾追加 gs.StartGame()；CharacterSelect.prefab (15772B) + StartupSelect.prefab (25399B) 由 unity-skills REST（端口 **8090**）通过 `Ch21_BuildAll` Editor 菜单一键生成。PlayMode 首战 9 步 + 二战 4 步全 PASS，0 Error / 1 无关 Warning（AudioModule MainMixer fallback）。测试工具沉淀：[PlaytestDriverEditor.cs](../Assets/Editor/Playtest/PlaytestDriverEditor.cs) 追加 `Ch21_TestFullFlow` / `Ch21_TestSecondRun` / `Ch21_Snap*` 4 个 MenuItem（DDOL UI Form 需 `FindObjectOfType(true)` + 反射私有 `_colorIds/_weaponIds/_patternIds` 才能模拟点击）。playtest 报告见 [tools/playtest/reports/2026-07-01-1215-startup-select-landing.md](../tools/playtest/reports/2026-07-01-1215-startup-select-landing.md)。
> - **22-gameplay-visual-polish** → `openspec/changes/archive/2026-07-01-22-gameplay-visual-polish/`（2026-07-01 归档）。4 子项串行 fan-out 打磨战斗视听：**A** [BotVisualBinder.cs](../Assets/Scripts/Modules/Bot/BotVisualBinder.cs) Smart 暖色 4 / Light 冷色 4 硬编码调色板 + 1.05x 黑 SpriteRenderer 描边（避免建 BotColorPresetConfig DataTable 打断 Auto Mode）；**B** VFXModule 加 3 个 `_bus.Subscribe<T>`：WeaponAttackHitEvent→SpawnSpark(+Crit Ring) / TargetKilledEvent→SpawnAOEBurst / PlayerDiedEvent→大 Burst+Ring；**C** [ActorShadowHelper.cs](../Assets/Scripts/Utils/ActorShadowHelper.cs) 运行时生成 64×64 径向渐变 Texture2D + Sprite.Create 懒缓存（不落 PNG，避免调 codex-image-gen），SpawnerModule 3 处挂（Player / Enemy loop / Boss）；**D** AudioModule 加 `PlayOneShot(string,Vector3,float)` + `PlayBgm(string,bool,float)` + `_bgmSource`（DontDestroyOnLoad）+ Clip 缓存 dict，新建 [AudioBridge.cs](../Assets/Scripts/Modules/Audio/AudioBridge.cs) IGameModule 订阅 4 事件（GameStateChanged→BGM 切换 / hit 按 WeaponConfig.Class Melee/Ranged/Special 分类硬编码路径 / kill / player_died）。0 Error / 1 无关 Warn（MainMixer fallback，DoD-5 显式推迟）。screenshot 可见 49 Bot 多色 + 脚下阴影。playtest 报告见 [tools/playtest/reports/2026-07-01-22-gameplay-visual-polish.md](../tools/playtest/reports/2026-07-01-22-gameplay-visual-polish.md)。**关键决策**：全部走硬编码/运行时生成绕过 DataTable schema 修改 + 美术资源出图，实现 Auto Mode 全流程 0 打断落地。**Post-archive 补丁（2026-07-01）**：子项 A 从"1.05x 复制 SpriteRenderer 黑描边"重构为 [Assets/Shader/SpriteTintOutline.shader](../Assets/Shader/SpriteTintOutline.shader)（URP 单 Pass，`_Tint × 纹理` + 4 邻域采样内/外双向描边 + `[PerRendererData] _MainTex` 保 SRP Batcher），BotVisualBinder 走 `MaterialPropertyBlock.SetColor("_Tint")` 各 Bot 独立染色 + 共享一个 sharedMaterial + 删除子对象方案，Batcher 合并友好 + 少一个 SpriteRenderer/Bot。
> - **17-ui-structure-first** → `openspec/changes/archive/2026-07-01-17-ui-structure-first/`（2026-07-01 归档）。UI 制作 v2→v3 重构：删「三表」（页面清单/复用组件清单/组件状态表）→ 换成单文件 **`prefab-layout.md`**（含 RectTransform 数据：anchor / pivot / sizeDelta / anchoredPosition / states），一份文档同时喂养阶段 2 效果图长宽反哺 / 阶段 4 素材拆分节点树 / 阶段 5 Prefab 层级搭建。**流程从 5 阶段升到 6 阶段**：1.结构设计（新增，art-ui 主导）→ 2.效果图设计 → 3.效果图生成 → 4.素材拆分 → 5.拼装实现（**单线 client-unity**，删阶段 5 art-ui 标注稿并行）→ 6.联调微调。**无豁免**（简单弹窗也走完整 6 阶段）；历史归档 UI 不回溯改造。新增 SKILL：**`unity-rect-transform`**（art-ui + client-unity 共享，含 UGUI 空间语言词典 + prefab-layout.md 模板 + 常见陷阱）；新增 spec capability **`ui-workflow`**（7 条 Requirement，覆盖 6 阶段每步的 GIVEN/WHEN/THEN），废弃 **`ui-planning`**（2 条 Requirement 标 REMOVED）。影响 14 个文件：CLAUDE.md §六 / conventions.md §八 / smoke-ui-workflow.md / art-ui.md / client-unity.md / SKILL_MATRIX.md / SKILLS_INDEX.md / ai-art SKILL + 2 references / ui-asset-splitting SKILL / ui-workflow spec / ui-planning spec REMOVED。wiki 条目：[UI 结构先行规范 v3](wiki/UI结构先行规范.md)。
> - **06-v21-implementation** → `openspec/changes/archive/2026-07-01-06-v21-implementation/`（2026-07-01 归档）。v2.1 GDD 全套落地：30+ 模块新增/重构（WeaponModule / SkillModule / NPCModule / MapGenModule / EnemyModule + BossModule / EventModule / EconomyModule / SaveModule / BotControllerModule / StatusEffectModule / …）+ 9 UGUI Form + 91 张美术 + 11 EditMode 测试 + DOTween Pro asmdef 接线。骨架先行模式（Phase 3-A 主对话生成公共骨架 = 事件总表 + 模块空壳 + IPlayerController 抽象 + CombatModule 消费意图重构 + 49 actor 占位）+ 12 子 agent 并行填充（Phase 3-B/C/D/E/F）。34 项 [x] + 4 项遗留（PlayMode 完整一局 / 帧率 profile / INDEX 同步 / archive 本身）——PlayMode 战斗流程实测已在 [16-min-game-loop-closure](../openspec/changes/archive/2026-07-01-16-min-game-loop-closure/) 与 [23-min-loop-round2](../openspec/changes/archive/2026-07-01-23-min-loop-round2/) 5 轮 22 TC / 21 PASS + 0 error 中充分覆盖；帧率 profile 转独立后续 change。见 [INTEGRATION_REPORT.md](../openspec/changes/archive/2026-07-01-06-v21-implementation/INTEGRATION_REPORT.md) §五「0 CS 错误 + EditMode 154/155 通过（99.4%）」。
> - **15-playtest-driver** → `openspec/changes/archive/2026-07-01-15-playtest-driver/`（2026-07-01 归档）。AI 协作 playtest 基础设施三件套：**A** [IInputSimulator](../Assets/Scripts/Modules/Input/IInputSimulator.cs) + [InputSimulator](../Assets/Scripts/Modules/Input/InputSimulator.cs) 测试专用注入器（Editor / Dev 编译进去，生产剥离）；**B** [InputModule](../Assets/Scripts/Modules/Input/InputModule.cs) 双源融合改造（优先 `_simulator`，fallback `Input.*`，业务调用零改动）；**C** [.claude/skills/playtest-driver/](../.claude/skills/playtest-driver/) SKILL SOP（启 Play → 装配 Simulator → 注入输入 → 读日志 → 写报告）+ [tools/playtest/reports/](../tools/playtest/reports/) 报告目录。实战验证：被 [16](../openspec/changes/archive/2026-07-01-16-min-game-loop-closure/) 与 [23](../openspec/changes/archive/2026-07-01-23-min-loop-round2/) 两次 loop 全流程依赖（Editor 菜单 `Tools/Playtest/*` + unity-skills REST `editor_execute_menu`）；PlaytestDriverEditor 目前累计 21+ 个菜单（Enable Simulator / Move / Press / Combat / Debug / Ch21 系列）。**关键决策**：uloop CLI 本机没装 → 全走 unity-skills REST + Editor 菜单（记 feedback memory）；Editor 脚本 namespace 用 `Playtest.EditorTools` 避 `UnityEditor.Editor` 冲突（CS0118）；生产 Stub 断开 Simulator 依赖。spec 沉淀到 [openspec/specs/playtest-driver/](../openspec/specs/playtest-driver/)。
> - **16-min-game-loop-closure** → `openspec/changes/archive/2026-07-01-16-min-game-loop-closure/`（2026-07-01 归档）。最小闭环 loop **首轮收敛**（B 方案：MainMenu → 战斗 → SelfTattoo → Pause → GameOver → MainMenu，不含 NPC / ThreeChoice）。baseline 9/13 PASS + 7 bug → **Round 1 13/13 PASS + 0 Console errors**。按根因分组 3 个修复批次：（1）Editor 焦点 freeze（Bug #6/#7）→ EnableSimulator 中加 `Application.runInBackground = true`；（2）CombatHUDForm RunStarted miss（Bug #3）→ [CombatHUDForm.cs:132](../Assets/Scripts/Modules/UI/CombatHUDForm.cs#L132) LateInit fallback 兜底订阅时序错过；（3）SelfTattooForm 双实例（Bug #4）→ Launch.unity 删残留 GameObject + SceneRoots 引用 + `asset_refresh` 强刷。剩 4 bug 判定为 DOC-ONLY / NOT-A-BUG（文档 typo、event_invoke 不能访 DDOL 属 MCP 限制）。同 bug 5 轮未解安全网未触发。后续被 [23-min-loop-round2](../openspec/changes/archive/2026-07-01-23-min-loop-round2/) 扩展到 22 TC 覆盖 change 18/20/22 新能力。
> - **23-min-loop-round2** → `openspec/changes/archive/2026-07-01-23-min-loop-round2/`（2026-07-01 归档）。最小完整流程 loop 收敛 Round 1→5：baseline 9/22 PASS → Round 5 **21/22 PASS + Console errors=0**。22 TC = 16 基线 13 TC + change 18/20/22 新能力 9 TC；报错口径严格 `console_get_stats.errors == 0`，Warning 不阻塞。loop 编排 = 骨架先行（qa-engineer 跑 22 TC → 主对话按根因分组）+ Fan-Out（模式 1，N 个 client-unity/tools-engineer 并行修）；终止安全网 = 同 bug# 连续 5 轮 OPEN → 交回用户（本 loop 未触发）。**11 个 bug 全 VERIFIED**：BUG-01/09 VFXModule.SpawnAOEBurst/SpawnSpark 及 7 处同根因 `ps.Stop(true,StopEmittingAndClear)` 前置；BUG-02/03 测试文档菜单路径修正；BUG-04/06/10 战斗自动化 null guard Warn+Vector3.zero 兜底继续 SpawnAOEBurst/SpawnSpark（原静默 return）；BUG-05 RunResultForm.OnGameStateChanged 补 GameOver 分支；BUG-07 新增 WeaponSpawnedEvent + WeaponSpawnerModule.SpawnDroppedWeapon 出口 Publish；BUG-08 PlaytestDriverEditor 重命名 `Publish AttackHit`→`Publish WeaponAttackHit (nearest bot)` 匹配 VFXModule 订阅类型；BUG-11 新增 `ForcePickupNearestWeapon` 绕开物理触发器（CombatModule 直接 `transform.position +=` 不走物理，SphereCollider 永不触发）。新增 5 个 debug 菜单（`Tools/Playtest/Combat/{ForceKillNearestBot, ForceSpawnWeaponPickup, ForcePickupNearestWeapon, Publish WeaponAttackHit, ForceRefillEnemies}`）。**唯一 BLOCKED**：TC-19 Bot 染色目视验证因 unity-skills `camera_screenshot` skill 返回 `GameObject not found`，属工具限制 NOT-A-BUG（转后续独立 change 处理，Bot 染色 shader 代码本身无问题）。**Warning 记录**：AudioModule PlayBgm/PlayOneShot 3 路资源缺失 + unity-skills SKILL doc 端口偏差 `:8091`→实际 `:8090`（`.claude/**` 不可动，DEFERRED）。

详见 [openspec/changes/](../openspec/changes/)。

---

## 五、新建 wiki 条目的规范

每个 wiki 文件头部要包含：

```yaml
---
title: <系统/决策名>
owner: <主负责 agent>
created: YYYY-MM-DD
last_updated: YYYY-MM-DD
status: active | superseded | archived
related_specs:
  - openspec/changes/...
related_skills:
  - <skill-name>
---
```

正文必须含：

1. **背景**：为什么有这个东西
2. **决策**：选了什么 + 为什么
3. **被否定的备选**：≥2 条 + 否定理由
4. **影响范围**：哪些代码 / 哪些 agent / 哪些 skill
5. **过时检查**：何时该 review / 何时该归档

---

## 六、与其他索引的关系

| 索引 | 作用 | 链接 |
|---|---|---|
| `.claude/CLAUDE.md` | AI 行为准则 + 路由 + 工作流主入口 | [→](../.claude/CLAUDE.md) |
| `.claude/SKILL_MATRIX.md` | agent ↔ skill 白名单 | [→](../.claude/SKILL_MATRIX.md) |
| `.claude/skills/SKILLS_INDEX.md` | 124 SKILL 分组索引 | [→](../.claude/skills/SKILLS_INDEX.md) |
| **本文件** | **项目自维护知识库** | — |
| `.claude/AGENTS.md` | 多 Agent 协作 5 模式 | [→](../.claude/AGENTS.md) |
| `openspec/changes/` | 活跃 / 已归档变更（含 art/ + tests/ + brainstorm.md） | [→](../openspec/changes/) |

---

*最后更新：2026-07-01（追加归档 24-unity-skills-port-routing：`unity_skills.py` 客户端端口自动路由 —— `--port` > `--target` > env > cwd registry 反查 > 8090 fallback，配 `health` 排障命令 + 延迟 warning + 所有 SKILL 文档去硬编码。追加归档 17-ui-structure-first：UI 制作 v2 三表 → v3 结构先行 `prefab-layout.md`，6 阶段无豁免，新增 `unity-rect-transform` SKILL 与 `ui-workflow` capability，废弃 `ui-planning`。批量归档 06 / 15 / 16 / 23：v2.1 GDD 全套落地 + playtest-driver 基础设施 + 最小闭环 loop 首轮 13/13 PASS + 最小完整流程 loop 5 轮 21/22 PASS。活跃仅剩 10-settings-form。）*
