# 11-skill-governance — 任务

## T1. 修复 8 个极短 description

- [x] k6 / SKILL.md frontmatter description
- [x] rigging / SKILL.md
- [x] combat-balancer / SKILL.md
- [x] game-art / SKILL.md
- [x] setup-fastlane / SKILL.md
- [x] redis-best-practices / SKILL.md
- [x] milestone-tracker / SKILL.md
- [x] kafka-development / SKILL.md

## T2. 压缩 5 个极长 description

- [x] texture-art / SKILL.md (691→≤250)
- [x] game-ui-design / SKILL.md (682→≤250)
- [x] vfx-realtime / SKILL.md (628→≤250)
- [x] 3d-modeling / SKILL.md (598→≤250)
- [x] animation-systems / SKILL.md (574→≤250)

## T3. 6 组重叠 SKILL 加 ❌ 不适用 划界

- [x] redis-best-practices ↔ redis-specialist
- [x] game-design-core ↔ game-design-theory
- [x] level-design SKILL 加注（指向 level-designer agent）
- [x] multiplayer-game ↔ game-networking
- [x] unity-foundations ↔ unity-dev
- [x] texture-art ↔ hytale-texture-artist

## T4. SKILLS_INDEX.md 加「候选淘汰区」

- [x] character-sprite / agency-technical-artist / agency-unity-shader-graph-artist / hytale-texture-artist 入册

## T5. MCP 启用清单重排

- [x] `.claude/settings.local.json` enabledMcpjsonServers 从 8 个降为 4 个
- [x] CLAUDE.md §八 MCP 表格补 codex-art-gen 行 + 改 7→8 + 加「默认启 4」说明

## T6. SKILL_MATRIX.md 沉淀治理元规则

- [x] 末尾加「description 写作规范」一节（template + 反模式 + 长度区间）

## T7. 复跑扫描脚本验收

- [x] 8 个极短全部 ≥60 字符
- [x] 5 个极长全部 ≤250 字符
- [x] 平均长度从 158 收敛到 145
- [x] 更新 `.claude/skills/_audit.json` 留底
