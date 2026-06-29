# 11-skill-governance — 设计

## 一、description 改写规范

### 1.1 模板

```
<一句话定位>。触发：<关键词1>、<关键词2>、<关键词3>...。❌ 不适用：<场景>，请用 <其他 SKILL>。
```

- **一句话定位**：动词开头，说清"做什么"，30-60 字符
- **触发词清单**：3-8 个，覆盖中英文同义词，用顿号分隔
- **不适用划界**：仅当存在重叠 SKILL 时加，否则省略
- **总长度上限 250 字符**：超过就证明在堆叠关键词

### 1.2 反模式

❌ `texture-art: 专注于游戏和电影制作领域PBR工作流、Substance系列工具、Quixel Mixer及手绘纹理技法的资深纹理艺术家。当提及以下关键词时可启用：texture artist、PBR textures、Substance Painter、Substance Designer、Quixel Mixer、normal map、roughness map...（共 30+ 关键词）`

→ 30 个关键词里大半互相覆盖（PBR ≈ Substance Painter ≈ roughness ≈ normal map），是噪声。

✅ `PBR 贴图制作。覆盖 Substance Painter/Designer、Quixel Mixer、手绘纹理、烘焙流程、贴图集与通道打包。触发：PBR、贴图、texture、Substance、Quixel、normal、roughness、albedo、贴图烘焙。`

## 二、8 个极短 SKILL 的改写方案

| SKILL | 旧（字符数） | 新 |
|---|---|---|
| k6 | "k6负载测试工具，用于性能测试。"(16) | "k6 负载测试与性能压测。覆盖 HTTP/WebSocket/gRPC 脚本、阶梯/峰值/spike 场景、阈值断言、Prometheus/Grafana 集成。触发：k6、负载测试、压测、load test、performance test、stress test、benchmark。" |
| rigging | "Rigging基础、骨骼设置与动画控制"(19) | "角色 rigging 基础。覆盖骨骼层级、IK/FK 控制器、权重绘制、面部 blend shape、控制器命名规范。触发：rigging、骨骼、骨架、bone、skeleton、IK、FK、控制器、权重、weight paint。" |
| combat-balancer | "难度缩放、CR计算、行动经济、自适应遭遇战设计"(23) | "战斗数值平衡。覆盖难度缩放、CR（挑战等级）计算、行动经济、自适应遭遇战、敌人血量/伤害曲线。触发：战斗平衡、combat balance、CR、挑战等级、难度缩放、遭遇战、encounter、敌人数值。" |
| game-art | "游戏美术原则：视觉风格选择、资源管线、动画工作流"(24) | "游戏美术总原则。覆盖视觉风格选型、美术资源管线、动画工作流、跨子专家协调入口。触发：美术风格、art style、美术管线、art pipeline、视觉方向。❌ 不适用：具体执行（UI/3D/动画/特效），请用 art-ui/art-3d/art-anim/art-vfx。" |
| setup-fastlane | "为iOS/macOS应用自动化配置Fastlane"(25) | "Fastlane 安装与首轮配置（iOS/macOS）。覆盖 Gemfile、Appfile/Matchfile、证书初始化、lane 模板、CI 接入。触发：Fastlane、setup、安装 Fastlane、iOS 自动化、证书管理、match。❌ 不适用：完整 CI 流水线，请用 mobile-cicd。" |
| redis-best-practices | "Redis开发最佳实践：缓存、数据结构与高性能键值操作"(27) | "Redis 入门最佳实践。覆盖键命名、TTL 策略、基本数据结构（String/Hash/List/Set/ZSet）、pipeline、批量操作。触发：Redis 入门、最佳实践、键命名、TTL、pipeline。❌ 不适用：分布式锁/pub-sub/限流，请用 redis-specialist。" |
| milestone-tracker | "里程碑跟踪专家。用于进度跟踪、关键路径、状态报告和挣值管理。"(30) | "项目里程碑跟踪。覆盖关键路径分析、状态周报、EVM 挣值管理、风险升级、燃尽图。触发：里程碑、milestone、关键路径、critical path、EVM、挣值、状态报告、周报、燃尽图、burn down。" |
| kafka-development | "Apache Kafka事件流与分布式消息传递的最佳实践和指南"(31) | "Apache Kafka 开发实战。覆盖 topic/partition 设计、Producer/Consumer 配置、消费组、offset 管理、exactly-once 语义、Schema Registry。触发：Kafka、kafka、消息队列、event streaming、topic、partition、consumer group、Schema Registry。" |

## 三、5 个极长 SKILL 的压缩方案

只列首尾两行，全文写入时按模板压缩到 ≤250 字符：

- **texture-art** 691→200：保留 PBR / Substance / Quixel / 烘焙 / 手绘 5 个核心词，删 normal/roughness/metallic/albedo 等同义堆叠
- **game-ui-design** 682→200：保留 game UI / HUD / 控制器导航 / diegetic 4 个核心，删 nintendo/death space 等文学修辞
- **vfx-realtime** 628→200：保留 particle / Niagara / VFX Graph / flipbook 4 个核心，删 60fps/AAA 等营销词
- **3d-modeling** 598→200：保留 mesh / topology / UV / retopo / LOD / FBX 6 个核心，删 maya/zbrush/houdini 重复枚举
- **animation-systems** 574→200：保留 state machine / blend tree / IK / root motion / retargeting 5 个核心，删 unity/unreal/godot 引擎枚举

## 四、6 组重叠 SKILL 的划界

| SKILL A | SKILL B | A 加注 | B 加注 |
|---|---|---|---|
| redis-best-practices | redis-specialist | ❌ 不适用：分布式锁/pub-sub/限流，请用 redis-specialist | ❌ 不适用：入门键命名/TTL/pipeline，请用 redis-best-practices |
| game-design-core | game-design-theory | ❌ 不适用：纯学术理论（MDA/8 类乐趣），请用 game-design-theory | ❌ 不适用：实操（循环/手感/平衡），请用 game-design-core |
| level-design（SKILL） | level-designer（agent） | ❌ 不适用：实际拆解关卡任务，请 delegate 给 level-designer agent | （agent 无 description，不动） |
| multiplayer-game | game-networking | ❌ 不适用：netcode/lag compensation 协议层，请用 game-networking | ❌ 不适用：游戏循环/匹配通用模式，请用 multiplayer-game |
| unity-foundations | unity-dev | ❌ 不适用：Editor REST API/配置表/UI 全流程，请用 unity-dev | ❌ 不适用：GameObject/Scene/Prefab 概念入门，请用 unity-foundations |
| texture-art | hytale-texture-artist | ❌ 不适用：Hytale 项目专属像素纹理，请用 hytale-texture-artist | ❌ 不适用：通用 PBR/手绘，请用 texture-art |

## 五、项目无关 SKILL 处置

不删，仅在 SKILLS_INDEX.md 末尾加「**候选淘汰区**」一节，列出：

- character-sprite — "为 Claude Office Visualizer Agent 生成精灵图"，与本项目无关
- agency-technical-artist — agency 系列模板
- agency-unity-shader-graph-artist — agency 系列模板
- hytale-texture-artist — Hytale 项目专属（除非未来做类 Hytale 项目）

下次清理周期前若无任何 agent 在 frontmatter.skills 中显式引用，则正式删除。

## 六、MCP 启用清单重排

```jsonc
// 当前 8 个全量
"enabledMcpjsonServers": [
  "skill4agent", "codebase-memory", "playwright",
  "blender", "godot", "frame-ronin", "atlassian", "codex-art-gen"
]

// 治理后
"enabledMcpjsonServers": [
  "skill4agent",      // 常驻 — SKILL 注册中心
  "codebase-memory",  // 常驻 — 代码结构索引（强烈优先）
  "codex-art-gen",    // 常驻 — 美术出图主入口
  "playwright"        // 高频 — Web E2E 测试，按需保留
  // 默认关闭（用户需要时手动启）：
  // "blender",       — 3D 资产生成，仅 art-3d agent 用
  // "godot",         — 跨引擎参考，使用频率极低
  // "frame-ronin",   — 帧/精灵处理，仅特定美术批次用
  // "atlassian"      — Jira/Confluence，需求来源不固定
]
```

风险：用户某次需要 blender/godot/frame-ronin/atlassian 时需手动加回。在 CLAUDE.md §八 加一行说明即可解决。

## 七、CLAUDE.md / SKILL_MATRIX.md 同步点

- CLAUDE.md §八：MCP 表格从 7 改成 8（codex-art-gen 漏写）；表格下方加「默认启用 4 个，其余按需开启」说明
- CLAUDE.md 顶部摘要：「7 个 MCP 工具」→「8 个 MCP 工具（默认启 4）」
- SKILL_MATRIX.md：末尾加「治理元规则」一节，把本次 description 模板沉淀进去，作为后续新 SKILL 的写作要求
