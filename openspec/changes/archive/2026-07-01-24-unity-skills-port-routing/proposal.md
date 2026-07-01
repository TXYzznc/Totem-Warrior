---
created: 2026-07-01
status: in-progress
depends-on: none
---

# 提案：unity-skills 端口路由 — cwd 自动匹配

## 一句话目标

修复 unity-skills 客户端端口硬编码问题：让 `_default_client` 根据当前工作目录自动查 `~/.unity_skills/registry.json` 反向匹配所属 Unity 项目的动态端口，同时清除项目内 `.claude/skills/**` / `.agents/skills/**` 里所有对 8090 / 8091 的固定端口文本，让多 Unity 项目并存时不再串项目。

## 为什么

真实痛点：用户同时打开多个 Unity 项目时，每个项目的 unity-skills 服务器绑定的是不同的**递增端口**（registry.json 已经在 per-project 记录 `{id, name, path, port, pid, ...}`），但客户端和文档全都写死 8090（甚至 playtest-driver SKILL 还写着 8091 错值），导致：

1. AI 从 A 项目根目录调 `unity_skills.py editor_execute_menu` 时，请求打到最早启动的项目（占 8090 那个），控制到别的项目去
2. `.claude/skills/playtest-driver/SKILL.md` 描述行 + curl 示例硬编码 `localhost:8091`，历史上就是错的（本项目实际是 8090），23-min-loop-round2 曾把这个偏差列为 DEFERRED
3. `.claude/skills/unity-dev/references/unity-skills.md` 有 3 处 curl 示例硬编码 `localhost:8090`，AI 读文档以为端口永远是 8090

根因不在服务端：registry.json 机制 + `_find_port_by_target()` + `UnitySkills(target=...)` 服务端能力已经在位，问题是客户端全局单例 `_default_client = UnitySkills()` 初始化时不带 target，就落 8090 兜底 + CLI `main()` 也不暴露 `--target` / `--port` 入参。

## 与用户的 5 条共识（grill-me 退出快照）

| 维度 | 决议 |
|---|---|
| 目标 | AI 无感知：跑什么项目自动落哪个 port；不再要求用户/AI 手动查端口 |
| 关键决策 A/B | (a) 方向 A：cwd 自动匹配 + 文档补章节（vs B 显式 target / D env 变量 / C 两者兼修）；(b) 保留 `UNITY_SKILLS_TARGET` env 层（作为 cwd 之上的显式覆盖，SSH / CI 兜底价值）；(c) 修复范围：**全项目 grep 一遍**（vs 只修 unity-skills + playtest-driver）；(d) playtest-driver curl 示例统一改成 `python unity_skills.py ...` 调用（一致性优先） |
| 边界 | (a) 不改 Unity 服务端（port 分配 / registry.json 写入是 unity-skills package 行为，本次只读不写）；(b) 不做进程健康检查（port 对应死进程时让上层看到明确 HTTP 错误，不拖启动时间）；(c) 不动 `openspec/changes/archive/**`；(d) 不动 `OutPackages/Unity-Skills-main/**` 上游源码；(e) 不动 Unity 二进制资源里 8090/8091 数值巧合（prefab / controller / SDF asset / FastNoise 常量）；(f) 不动 `tools/playtest/reports/**` 历史 playtest 报告文本 |
| 验收 | (a) 项目子目录跑 `python unity_skills.py health` 命中 registry 里本项目 port；(b) 项目外 cwd 落 8090 + stderr warning；(c) `--target=GameDesinger` 显式覆盖生效；(d) 双 Unity 项目并行 → 两个 Claude 会话各自不串项目；(e) grep `8090` / `8091` 硬编码在 `.claude/skills/**` / `.agents/skills/**` / `.claude/CLAUDE.md` 归零；(f) `openspec validate 24-unity-skills-port-routing --strict` 通过 |
| 约束 | 时间盒：下一个 openspec change（不阻塞当前，但优先于 10-settings-form）；向后兼容：旧调用姿势（不传 target / 不在 registry path 下）仍能落 8090 不抛；平台：Windows 主场景保证工作，mac/linux 代码至少不抛（commonpath + normcase 天然兼容）；不引新依赖，只用 stdlib（os / json） |

## 不做什么

- 不改 Unity 服务端 REST 实现（port 分配 / registry 写入是 unity-skills package 内部行为）
- 不做健康检查 / 心跳（port 对应死进程时让 HTTP 调用直接失败，不拖首次调用启动时间）
- 不改 `~/.unity_skills/registry.json` 的 schema（只读不写）
- 不动 `openspec/changes/archive/**` 归档历史（archive 是不可变的）
- 不动 `OutPackages/Unity-Skills-main/**` 上游源码（那是外部依赖）
- 不动 `tools/playtest/reports/**` 里历史 playtest 报告里出现的 8090/8091（时间戳快照，非配置）
- 不动 Unity 二进制资源（`.prefab` / `.controller` / `.asset` / `.wlt`）里 8090/8091 数字巧合（GUID hash / 坐标 / 数值）
- 不改 `tools/ImageCompression_Tool/dist/**` PyInstaller 打包产物里的编码表（无关字节数据）
- 不引入新 Python 依赖（只用 os / json / os.path）

## 验收

- [ ] `.claude/skills/unity-skills/scripts/unity_skills.py` 已加 cwd 自动匹配逻辑 + CLI `--target` / `--port` argv 解析
- [ ] `.claude/skills/unity-skills/scripts/unity_skills.py` `health` 输出目标端口和寻址来源（cwd / env / target / 兜底）
- [ ] `.claude/skills/unity-skills/SKILL.md` 已加「多项目路由（重要）」章节：优先级链 + registry.json 机制说明 + `--target` 跨项目示例 + `health` 排障
- [ ] `.claude/skills/playtest-driver/SKILL.md` 描述行 + 所有 curl 示例已从 `localhost:8091` 改为 `python unity_skills.py ...` 调用（或标注端口由路由自动解析）
- [ ] `.claude/skills/unity-dev/references/unity-skills.md` 3 处 curl 硬编码 `localhost:8090` 已改为路由说明
- [ ] `.agents/skills/unity-skills/**` 与 `.agents/skills/unity-dev/**` 通过 `tools/sync-agents.py` 同步（source of truth 是 `.claude/`）
- [ ] `openspec validate 24-unity-skills-port-routing --strict` 通过
- [ ] 归档时 `项目知识库（AI自行维护）/INDEX.md` 已更新（追加本 change 摘要）
