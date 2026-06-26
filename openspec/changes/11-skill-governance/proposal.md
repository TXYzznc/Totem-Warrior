# 11-skill-governance — SKILL/MCP 治理：description 审计 + MCP 按需启用

> 状态：阶段 B 实施中
> 创建：2026-06-26
> 触发：用户提出「109 SKILL + 8 MCP 体量，能否降低 token 消耗、提高精确召回」。本会话先做了行业实践调研（progressive disclosure / semantic tool selection / SkillReducer），随后用户授权按建议落地，其中「向量召回 router」明确暂不做。

## 一、为什么做

扫描 `.claude/skills/` 109 个 SKILL 后定位 4 类问题：

| 类别 | 数量 | 典型样本 | 危害 |
|---|---|---|---|
| **极短 description**（<40 字符，缺触发词） | 8 | k6(16) / rigging(19) / combat-balancer(23) / game-art(24) / setup-fastlane(25) / redis-best-practices(27) / milestone-tracker(30) / kafka-development(31) | 模型无法判断何时该召回 → 误召回率高 |
| **极长 description**（>500 字符，关键词堆叠冗余） | 5 | texture-art(691) / game-ui-design(682) / vfx-realtime(628) / 3d-modeling(598) / animation-systems(574) | 启动 token 浪费；SkillReducer 实验显示压缩 39-48% 性能反而提升 |
| **互相重叠未划界** | 6+ 组 | redis-best-practices↔redis-specialist / game-design-core↔game-design-theory / level-design↔level-designer / multiplayer-game↔game-networking / unity-foundations↔unity-dev / texture-art↔hytale-texture-artist | 模型选错；同一意图召回多条造成 context 膨胀 |
| **项目无关 SKILL**（其他项目残留） | 3+ | character-sprite (Claude Office Visualizer) / agency-technical-artist / agency-unity-shader-graph-artist / hytale-texture-artist | 完全没用却占启动 token |

**MCP 侧**：`.claude/settings.local.json` 全量启用 8 个 MCP（skill4agent / codebase-memory / playwright / blender / godot / frame-ronin / atlassian / codex-art-gen），其中 4 个低频（blender / godot / frame-ronin / atlassian），每次启动都拉 schema 浪费 token。

## 二、目标

1. **109 SKILL description 全部满足下限**：长度 60-250 字符 + 含触发词或「适用 / 触发」前缀
2. **重叠 SKILL 显式划界**：description 加 `❌ 不适用：xxx` 一行
3. **项目无关 SKILL 在 SKILLS_INDEX 里标注「候选淘汰」**：不立刻删（避免误伤），但下次清理周期可决断
4. **MCP 启用清单按频次重排**：常驻 3（skill4agent / codebase-memory / codex-art-gen）+ 高频 1（playwright）+ 默认关闭 4（blender / godot / frame-ronin / atlassian）
5. **CLAUDE.md / SKILL_MATRIX.md 同步**：把「7 个 MCP」改成准确数字 + 加治理说明

## 三、关键决策（基于先前调研）

| 项 | 选择 | 备选理由 |
|---|---|---|
| D1 是否做向量召回 router | **不做**（用户明确） | router 是 P1 收益最大项，但工程量重。本次专注 description 与 MCP 治理这条 P0 路径 |
| D2 是否删除项目无关 SKILL | **暂不删，仅标注候选** | 怕误伤；character-sprite 等可能被某些 agent 引用，删之前需 grep verification |
| D3 description 长度区间 | **60-250 字符** | 下限保证有触发词；上限避免 SkillReducer 论文里的「噪声-相关」效应 |
| D4 划界标记格式 | **`❌ 不适用：<场景>，请用 <SKILL>`** | 与 ai-art / codex-image-gen 现有划界风格一致 |
| D5 MCP 默认关闭 4 个的恢复成本 | **改 settings.local.json 即可** | 用户单次需要时手动启 1-2 个，比常驻 8 个划算 |

## 四、不做

- ❌ 不实施向量召回 router（D1）
- ❌ 不删除任何 SKILL 目录（D2）
- ❌ 不改 109 SKILL 的 body（SKILL.md 正文不动，只动 frontmatter description）
- ❌ 不改 SKILL 的 `frontmatter.skills` 白名单（Agent ↔ SKILL 映射这次不动）
- ❌ 不重写 [SKILL_MATRIX.md](../../../.claude/SKILL_MATRIX.md) 全文（只补「治理元规则」一节）

## 五、验收标准

- [ ] 复跑扫描脚本：8 个极短全部 ≥60 字符且含触发词
- [ ] 5 个极长全部 ≤250 字符
- [ ] 6 组重叠 SKILL 全部加 `❌ 不适用` 划界
- [ ] 3 个项目无关 SKILL 在 [SKILLS_INDEX.md](../../../.claude/skills/SKILLS_INDEX.md) 标注为「候选淘汰」
- [ ] `.claude/settings.local.json` 的 `enabledMcpjsonServers` 降为 4 个
- [ ] [CLAUDE.md §八](../../../.claude/CLAUDE.md) MCP 数量 7 → 8 + 治理说明
- [ ] 平均 description 长度从 158 降到 120-130 区间（极短补强 + 极长压缩双向收敛）
