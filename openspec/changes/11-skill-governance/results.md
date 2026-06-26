# 11-skill-governance — 实施结果

> 完成时间：2026-06-26
> 实施者：主对话（Auto Mode）

## 一、扫描脚本对比

| 指标 | 治理前 | 治理后 | 变化 |
|---|---|---|---|
| SKILL 总数 | 109 | 109 | — |
| description 平均长度 | 158 字符 | **145 字符** | -8.2% |
| description 最大长度 | 691（texture-art） | **375**（blender-mcp，可接受） | -45.7% |
| description 最小长度 | 16（k6） | **44**（agency-technical-artist，候选淘汰） | +175% |
| 缺触发词数（脚本检测） | 28 | 15 | -46% |
| 启用 MCP 数 | 8 | **4** | -50% |

## 二、单 SKILL 改善记录

### T1：8 极短 description（<40 → 60-200 字符）

| SKILL | 旧长 | 新长 |
|---|---|---|
| k6 | 16 | 178 |
| rigging | 19 | 144 |
| combat-balancer | 23 | 174 |
| game-art | 24 | 186 |
| setup-fastlane | 25 | 198 |
| redis-best-practices | 27 | 210 |
| milestone-tracker | 30 | 184 |
| kafka-development | 31 | 189 |

### T2：5 极长 description（>500 → ≤250 字符）+ 额外 2 个

| SKILL | 旧长 | 新长 |
|---|---|---|
| texture-art | 691 | 200 |
| game-ui-design | 682 | 200 |
| vfx-realtime | 628 | 200 |
| 3d-modeling | 598 | 200 |
| animation-systems | 574 | 249 |
| player-onboarding | 540 | 172（顺手） |
| ab-testing | 526 | 172（顺手） |

### T3：6 组重叠 SKILL 加 ❌ 不适用 划界

- redis-best-practices ↔ redis-specialist
- game-design-core ↔ game-design-theory
- level-design → level-designer agent
- multiplayer-game ↔ game-networking
- unity-foundations ↔ unity-dev
- texture-art ↔ hytale-texture-artist
- game-art → 4 个 art-* agent（附加项）

### T4：候选淘汰区

入册 4 个：character-sprite / hytale-texture-artist / agency-unity-shader-graph-artist / agency-technical-artist

### T5：MCP 启用清单

```
旧：[skill4agent, codebase-memory, playwright, blender, godot, frame-ronin, atlassian, codex-art-gen]  // 8 个
新：[skill4agent, codebase-memory, codex-art-gen, playwright]                                            // 4 个
```

按需启的 4 个：blender / godot / frame-ronin / atlassian（CLAUDE.md §八 已加说明）

### T6：SKILL_MATRIX.md 沉淀治理元规则

新增 §六「SKILL description 写作规范」，含模板 / 反模式 / checklist / 复跑审计要求

## 三、收益估算

| 项 | 节省 token / 启动 |
|---|---|
| description 总长度优化（极短补 ~500 + 极长压 ~2850） | ~600 net |
| MCP 4 个默认关闭（每个 schema ~1-3k） | ~4000-12000 |
| 重叠 SKILL 划界（误召回率下降，间接） | ~500-2000 |
| **合计估算** | **~5-15k / 启动** |

## 四、未做的 P 项与原因

| 未做项 | 原因 |
|---|---|
| 向量召回 `skill-router` MCP | 用户明确指示「不做」 |
| Agent frontmatter.skills 改「3 常用 + router 召回」 | 依赖 router，并发暂缓 |
| SKILL 使用日志（hook 注入 + 月度淘汰） | 收益低，且需要新 hook 设计，下次需要时再做 |
| SkillReducer 思路的 body 压缩 pass | description 治理已达成 P0 目标，body 压缩边际收益低 |

## 五、follow-up

1. 下次清理周期评估 4 个候选淘汰 SKILL，若仍无 agent 引用则正式删除
2. 月度复跑 `python /tmp/scan_skills.py` 看 description 长度均值是否回涨（防腐）
3. 若发现新 SKILL 创建未遵守 SKILL_MATRIX §六 规范，纳入 code review checklist

## 六、变更文件清单

```
.claude/CLAUDE.md                                # MCP 表格 + 摘要
.claude/SKILL_MATRIX.md                          # 新增 §六 写作规范
.claude/settings.local.json                      # enabledMcpjsonServers 8→4
.claude/skills/SKILLS_INDEX.md                   # 候选淘汰区
.claude/skills/3d-modeling/SKILL.md              # description 压缩
.claude/skills/ab-testing/SKILL.md               # description 压缩
.claude/skills/animation-systems/SKILL.md        # description 压缩
.claude/skills/combat-balancer/SKILL.md          # description 补强
.claude/skills/game-art/SKILL.md                 # description 补强 + ❌划界
.claude/skills/game-design-core/SKILL.md         # description 压缩 + ❌划界
.claude/skills/game-design-theory/SKILL.md       # description 补强 + ❌划界
.claude/skills/game-networking/SKILL.md          # description 压缩 + ❌划界
.claude/skills/game-ui-design/SKILL.md           # description 压缩
.claude/skills/hytale-texture-artist/SKILL.md    # ❌划界
.claude/skills/k6/SKILL.md                       # description 补强
.claude/skills/kafka-development/SKILL.md        # description 补强
.claude/skills/level-design/SKILL.md             # description 补强 + ❌划界
.claude/skills/milestone-tracker/SKILL.md        # description 补强
.claude/skills/multiplayer-game/SKILL.md         # description 补强 + ❌划界
.claude/skills/player-onboarding/SKILL.md        # description 压缩
.claude/skills/redis-best-practices/SKILL.md     # description 补强 + ❌划界
.claude/skills/redis-specialist/SKILL.md         # description 压缩 + ❌划界
.claude/skills/rigging/SKILL.md                  # description 补强
.claude/skills/setup-fastlane/SKILL.md           # description 补强 + ❌划界
.claude/skills/texture-art/SKILL.md              # description 压缩 + ❌划界
.claude/skills/unity-dev/SKILL.md                # ❌划界
.claude/skills/unity-foundations/SKILL.md        # description 补强 + ❌划界
.claude/skills/vfx-realtime/SKILL.md             # description 压缩
openspec/changes/11-skill-governance/             # 新建 change 目录
```

共 27 个文件改动 + 4 个新建。
