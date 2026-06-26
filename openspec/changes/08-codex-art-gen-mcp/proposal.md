# 08-codex-art-gen-mcp — Codex CLI 生图工作流封装为 MCP 服务

> 状态：阶段 B 实施中
> 创建：2026-06-26
> 触发：v2.1 实测 codex 用量异常（详见 `06-v21-implementation/art/codex_cli_image_workflow_refactor_plan.md`），用户决定把这套优化方案固化为常驻 MCP 服务

## 一、为什么做

v2.1 美术批处理实测发现，原 `.codex-batch.sh` 在项目根 cwd 启动 codex exec，每次单图消耗 ~47K token（正常应 3K，多 15 倍）。三类浪费占 99.5%：
- 加载 12 个坏 SKILL.md frontmatter + 40 个坏 agent toml 报错文本入 system context（21%）
- image_gen 工具被反复重试调用 7.6 次/图（63%）
- 逐图独立 cold start 无 session 复用（13%）

实测验证：cwd 切到 `/tmp/codex-clean-cwd` 后单次降到 3,261 token（省 93%）。

继续临时改 `.codex-batch.sh` 不稳定——每次新 change 需手抄一份。**做成 MCP 服务**则固化最佳实践，未来所有 change 直接用。

## 二、目标

1. **Claude 做脑，Codex 做手**：MCP 服务把"解析—分桶—调用 codex—验证—落盘—写记录"固化为 5 个工具，Claude 调用 MCP 不再写脚本
2. **token 砍 90%+**：cwd 隔离 + L2 合图 + 限 1 次 imagegen + ephemeral session 全部内建
3. **通用化**：任何 `openspec/changes/<NN>/art/` 的 prompts.md 都能用，参数化 change_name / art_dir
4. **删除** `.codex-batch.sh`（grill A 阶段用户已确认）

## 三、关键决策（grill 阶段 A 共识）

| 项 | 选择 | 备选理由 |
|---|---|---|
| 技术栈 | **Python stdio** | 复用 `.venv/`，frame-ronin 已用 Python MCP，标准 SDK 成熟 |
| 工具粒度 | **5 个细粒度** | parse / bucket / dispatch_l1 / dispatch_l2 / write_record。Claude 控制每一步，分桶失败可中途调整 |
| L2 切图 | **dispatch_l2 内部直接调** | image_cut.py 本地脚本几乎不会失败，并入工具省一步 |
| 通用性 | **全项目参数化** | change_name / art_dir 入参，未来 v2.2 直接用 |
| 错误处理 | **失败继续不重试** | 符合 refactor plan §6 L1 策略，不浪费额度 |
| 并发 | **2 路并发** codex exec | 墙钟时间砍半，额度总量不变 |
| 旧脚本 | **直接删** | grill A 阶段用户已确认 |

## 四、不做

- OpenAI Images API 直调（refactor plan §11 优先级 6，后续可独立改造）
- Codex 内重试机制（明确禁止）
- L2 切图作为独立工具暴露（并入 dispatch_l2）
- 美术 prompts.md 的内容生成（仍由用户/Claude 写）
- 美术资源 ID 与 ResourceConfig 对接（属于其他 change）

## 五、验收标准

- [x] grill A 5 条全清，本 proposal 写完
- [ ] `tools/codex-art-gen-mcp/server.py` 实现 5 个 MCP 工具
- [ ] `.mcp.json` 注册 + `.claude/settings.local.json` enabledMcpjsonServers 加 `codex-art-gen`
- [ ] cwd 隔离生效：实测 token < 5K/调用
- [ ] L1 dispatch：6-8 张大图 1 次 codex exec
- [ ] L2 dispatch：1 张 1024 合图 1 次 codex exec + 自动切图入 raw/
- [ ] 失败 item 标 `failed` 不中断整批
- [ ] 2 路并发 + 日志独立文件不交叠
- [ ] 用 06-v21 剩 22 张美术验证全套（等 codex 额度恢复）
- [ ] `.codex-batch.sh` 删除
- [ ] INDEX.md 加入口

## 六、关键约束

- 沿用 `codex_cli_image_workflow_refactor_plan.md` 全部 §9 安全约束
- MCP 服务必须用 `cwd=/tmp/codex-art-gen-runner` 隔离调 codex（不删项目级 `.codex/agents` 和 `.agents/skills`）
- writable_roots 必须显式授权到 `art/raw/` 防 sandbox 阻塞
- `--ephemeral` 必加，避免污染 session 历史
- env_floor/wall 自动 `transparent=false`
- imagegen 强制 1 次/item，不重试

## 七、影响范围

| 类型 | 文件 |
|---|---|
| 新增 | `tools/codex-art-gen-mcp/server.py` / `requirements.txt` / `README.md` |
| 修改 | `.mcp.json` / `.claude/settings.local.json` / `项目知识库（AI自行维护）/INDEX.md` |
| 删除 | `openspec/changes/06-v21-implementation/art/.codex-batch.sh`（不可逆，grill A 已确认） |
| 影响 | 后续所有 openspec change 的美术批处理走 MCP，不再手写脚本 |
