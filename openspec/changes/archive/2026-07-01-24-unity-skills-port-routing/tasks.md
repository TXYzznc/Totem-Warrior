---
change: 24-unity-skills-port-routing
---

# 任务：unity-skills 端口路由 — cwd 自动匹配

## 阶段 1：代码改造（`unity_skills.py`）

- [ ] 1.1 在 `get_registry_path()` 之后新增 `_load_registry() -> Dict[str, Any]`（复用给 `_find_port_by_target` 和 `_find_port_by_cwd`）
- [ ] 1.2 新增 `_find_port_by_cwd() -> Optional[Tuple[int, str]]`（cwd 反查 registry，返回 `(port, source_desc)`）
- [ ] 1.3 重构 `UnitySkills._find_port_by_target`：把内部 `open(reg_path)` 换成 `_load_registry()` 调用，保持行为一致
- [ ] 1.4 新增模块级 `_default_client_source: str = ""`（记录寻址来源，供 `health()` 输出）
- [ ] 1.5 新增 `_build_default_client() -> UnitySkills`：env → cwd → 8090 三级优先级；每层落地时写 `_default_client_source`；未匹配落 8090 时打 stderr warning
- [ ] 1.6 替换 `_default_client = UnitySkills()` 为 `_default_client = _build_default_client()`
- [ ] 1.7 改造 `get_skills()`：把 `UNITY_URL` 改为 `_default_client.url`
- [ ] 1.8 改造 `health()`：返回 `dict`（`{url, source, ok, [error]}`），内部用 `_default_client.url`
- [ ] 1.9 新增 `_pre_parse_routing()`：从 `sys.argv` 抽取 `--target=<x>` / `--target <x>` / `--port=<n>` / `--port <n>`，剔除后重建 `_default_client`，并更新 `_default_client_source` 为 `cli-port(...)` / `cli-target(...)`
- [ ] 1.10 在 `main()` 首行调用 `_pre_parse_routing()`，确保后续 argv 解析不受影响
- [ ] 1.11 更新 `main()` 的 usage help：加 `[--target=<name>] [--port=<num>]` 说明
- [ ] 1.12 `UNITY_URL` 常量保留（避免破坏可能存在的外部 import），但代码内部不再引用；加行内注释「legacy, 保留兼容」

## 阶段 2：SKILL 文档改造

- [ ] 2.1 `.claude/skills/unity-skills/SKILL.md` 新增「多项目路由（重要）」章节（放「中文 / CJK 参数调用约定」之前）：
  - 优先级链表（cli --port > --target > env > cwd > 8090）
  - registry.json schema 说明
  - `--target` 跨项目示例
  - `health` 排障示例（含预期输出格式）
- [ ] 2.2 `.claude/skills/playtest-driver/SKILL.md` 第 3 行描述从 `unity-skills MCP（REST localhost:8091）` 改为 `unity-skills CLI（端口自动路由）`
- [ ] 2.3 `.claude/skills/playtest-driver/SKILL.md` 所有 curl 示例统一改为 `python .claude/skills/unity-skills/scripts/unity_skills.py <skill> <params>`
- [ ] 2.4 `.claude/skills/unity-dev/references/unity-skills.md` 第 11 行 `Server endpoint: http://localhost:8090` 改为路由说明
- [ ] 2.5 `.claude/skills/unity-dev/references/unity-skills.md` 第 587 / 590 / 593 行 3 处 curl 改为 python CLI 调用

## 阶段 3：Grep 兜底扫描（无遗漏保证）

- [ ] 3.1 `Grep '8090|8091' --glob '.claude/**'` 结果全部处理完（改或标注 legacy 常量注释）
- [ ] 3.2 `Grep '8090|8091' --glob '.agents/**'`：由 `tools/sync-agents.py` 自动同步，此步为验证
- [ ] 3.3 `Grep '8090|8091' --glob 'tools/**'` 排查：确认 `tools/playtest/reports/**` 是历史 report 不改；`tools/ImageCompression_Tool/dist/**` 是打包产物不改
- [ ] 3.4 `Grep '8090|8091' --glob 'Assets/**'` 排查：确认 `.prefab` / `.controller` / `.asset` / `.wlt` 里的 8090/8091 是坐标 / GUID / 数值，无关；FastNoiseLite 是第三方库常量，无关
- [ ] 3.5 `.claude/CLAUDE.md` 全文 grep 一遍，无 8090/8091 硬编码

## 阶段 4：镜像同步

- [ ] 4.1 跑 `python tools/sync-agents.py` 把 `.claude/skills/**` 同步到 `.agents/skills/**`
- [ ] 4.2 抽查 `.agents/skills/unity-skills/scripts/unity_skills.py` 与 `.claude/` 版本 diff 为 0

## 阶段 5：验收（5 条 acceptance）

- [ ] 5.1 **cwd 命中**：从 `D:\unity\UnityProject\GameDesinger\` 或任意子目录跑 `python .claude/skills/unity-skills/scripts/unity_skills.py health`，输出 `source: "cwd-match(GameDesinger)"`，`url` 命中 registry 里该项目 port
- [ ] 5.2 **cwd 未命中兜底**：从 `C:\` 或 `~/tmp` 之类路径跑 `health`，输出 `source: "default"` + stderr warning + `url: http://localhost:8090`
- [ ] 5.3 **`--target` 显式覆盖**：从 `C:\tmp` 跑 `python <script> health --target=GameDesinger`，命中本项目 port，`source: "cli-target(GameDesinger)"`
- [ ] 5.4 **双 Unity 项目并行不串**：手工验证——用户开两个 Unity 项目 → 两个 Claude 会话分别在两项目根目录跑同一 skill → 各自落自己的 port（本条为人工验收，写入 `tests/results.md` 时手动打钩）
- [ ] 5.5 **硬编码归零**：`Grep '8090|8091'` 在 `.claude/skills/**` / `.agents/skills/**` / `.claude/CLAUDE.md` 里除代码内 `DEFAULT_PORT = 8090` 常量 + 注释外，无其它硬编码文本

## 阶段 6：openspec 验证 + 归档

- [ ] 6.1 `openspec validate 24-unity-skills-port-routing --strict` 通过
- [ ] 6.2 归档时 `项目知识库（AI自行维护）/INDEX.md` 追加本 change 摘要
- [ ] 6.3 `openspec archive 24-unity-skills-port-routing --yes`
