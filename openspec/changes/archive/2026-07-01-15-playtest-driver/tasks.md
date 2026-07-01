# Tasks — 15-playtest-driver

> 进度。✅ = 完成；🟡 = 进行中；🔲 = 未开始。

## 阶段 1 — 文档（本次 commit）

- [✅] proposal.md — 范围、DoD、风险
- [✅] design.md — IInputSimulator / InputModule 改造 / SKILL SOP / 报告模板
- [✅] tasks.md — 本文件
- [🟡] specs/playtest-driver/spec.md — 验收契约

## 阶段 2 — InputModule 改造（client-unity / 主对话直接落地）

- [🔲] 新增 `Assets/Scripts/Modules/Input/IInputSimulator.cs`（条件编译）
- [🔲] 新增 `Assets/Scripts/Modules/Input/InputSimulator.cs`（条件编译）
- [🔲] 改 `Assets/Scripts/Modules/Input/InputModule.cs` 为 partial，在 11 个查询方法插入双源融合
- [🔲] 新增 `Assets/Scripts/Modules/Input/InputModule.Simulator.cs`（注入入口 partial + 生产 Stub）
- [🔲] Unity 编译通过（Editor + Player 两套配置）

## 阶段 3 — SKILL 落地

- [🔲] `.claude/skills/playtest-driver/SKILL.md`（含 frontmatter、触发词、SOP、PoC 示例）
- [🔲] description 长度 60-250 字符（SKILL_MATRIX §六规范）
- [🔲] 在 `.claude/SKILL_MATRIX.md` 把 `playtest-driver` 加入 `qa-engineer` 白名单
- [🔲] 在 `.claude/skills/SKILLS_INDEX.md` 增加索引行

## 阶段 4 — 报告模板

- [🔲] `tools/playtest/reports/` 目录建立 + `.gitkeep`
- [🔲] `tools/playtest/reports/_TEMPLATE.md` 模板
- [🔲] `tools/playtest/README.md` 说明报告格式

## 阶段 5 — PoC 验证

- [🔲] 启 Play Mode（unity-skills.editor_play）
- [🔲] uloop 装配 InputSimulator
- [🔲] uloop 注入 PressKey(KeyCode.E)
- [🔲] console_get_logs 抓到 SkillModule 响应或确认行为
- [🔲] editor_stop
- [🔲] 写第一份 `tools/playtest/reports/<ts>-poc-skill-trigger.md`

## 阶段 6 — 归档

- [🔲] `openspec validate 15-playtest-driver --strict`
- [🔲] `openspec archive-change 15-playtest-driver`
- [🔲] 更新 `项目知识库（AI自行维护）/INDEX.md`
