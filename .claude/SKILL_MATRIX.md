# Skill → Agent Ownership Matrix

97 个 skill 在 `.claude/skills/` 扁平存放，团队归属靠 agent system prompt 白名单约束（软隔离）。

## 公共/跨团队
| Skill | 主属 | 共享给 |
|---|---|---|
| grill-me | producer | all leads |
| grill-with-docs | producer | all leads |
| find-skills | tools-engineer | producer |
| skill-creator | tools-engineer | — |
| xlsx | gd-system | producer, network |

## producer (1 agent)
project-management, design-system, review-all-gdds, milestone-tracker, risk-assessment, competitive-analysis, sprint-retrospective, task-estimation, brainstorm, propagate-design-change

## design — game-designer (gd-lead + gd-system)
game-design-core, game-design-theory, progression-systems, player-onboarding, game-monetization, balance-check, casino-math-balancer, godot-combat-system, godot-economy-system, godot-dialogue-system, godot-genre-visual-novel
(平台无关：Godot/UE skill 当通用设计模式用)

## design — level-designer
level-design, agency-level-designer, combat-balancer, puzzle-design, godot-genre-puzzle, godot-genre-stealth, godot-genre-roguelike, godot-genre-platformer, godot-tilemap-mastery, godot-3d-world-building, anvil-level-design-blender, ue-procedural-generation, ue-world-level-streaming

## art — art-director (协调)
gpt-image-2-style-library, game-art, art-direction

## art — art-ui
game-ui-design, unity-ui

## art — art-font
typeset, font-pairing-suggester (生态贫瘠，多数待自建)

## art — art-vfx
vfx-realtime, unity-lighting-vfx, shader-effects (与 client-ta 共享 unity-lighting-vfx/vfx-realtime — 美术驱动)

## art — art-2d
character-sprite, hytale-texture-artist + frame-ronin MCP

## art — art-3d
3d-modeling, texture-art, blender-mcp + blender MCP

## art — art-anim
animation-systems, unity-animation, rigging

## client (lead + unity + ta)
unity-foundations, unity-ecs-patterns, unity-input-correctness, unity-async-patterns, unity-ui, unity-animation, unity-shaders-rendering, unity-lighting-vfx, vfx-realtime, shader-effects, agency-unity-shader-graph-artist, agency-technical-artist, unity-skills

## network (lead + backend + db)
multiplayer-game, unity-networking, game-networking, database-schema-design, backend-testing, arch-api, algo-rank-trueskill, atomic-matchmaking, jwt-auth, oauth-implementation, redis-best-practices, redis-specialist, kafka-development, k6, opentelemetry, prometheus

## qa-engineer
testing-strategies, backend-testing, agent-browser, uloop-run-tests, ab-testing

## devops-engineer
devops-deployment, github-actions-docs, mobile-cicd, setup-fastlane, semver, feature-flags, cdn-setup, secrets-management, asc-submission-health, deploy-checklist

## tools-engineer
skill-creator, find-skills, moai-docs-generation, unity-skills, uloop-execute-dynamic-code

## 重叠路由规则

1. **client-ta**: `unity-shaders-rendering` vs `agency-unity-shader-graph-artist`
   - 程序员驱动（性能/SRP/compute）→ unity-shaders-rendering
   - 美术驱动（视效/材质/custom pass）→ agency-unity-shader-graph-artist

2. **art-vfx ↔ client-ta**: `unity-lighting-vfx` / `vfx-realtime` 共享
   - 美术：长什么样
   - TA：怎么实现

3. **level-designer**: `level-design`（基础）+ `agency-level-designer`（专家）
   - 入门或评审 → level-design
   - 深度策划（encounter/pacing）→ agency-level-designer

4. **gd**: `progression-systems` ↔ `game-monetization`（battle pass 重叠）
   - 系统设计角度 → progression-systems
   - 商业模式角度 → game-monetization

5. **network**: `redis-best-practices`（基础）+ `redis-specialist`（用例：leaderboard/lock/pub-sub）

## 自建 backlog（tools-engineer 责任）

🔥 已确认完全空白，tools-engineer 用 skill-creator 自建：
1. unity-architecture (DI: Zenject/VContainer)
2. save-serialization (JsonUtility/Newtonsoft/版本迁移)
3. state-machine (FSM/BT/Animator)
4. localization-i18n (Unity Localization Package)
5. physics-collision (rigidbody/raycast/layer)
6. font-selection-cjk (中日韩字体)
7. pixel-font-rendering
8. quest-mission-design
9. achievement-design
10. player-guidance (sightline/affordance/lighting cues)
11. crash-analytics (Sentry/Crashlytics CLI)
12. mobile-device-testing (BrowserStack/Firebase Test Lab)
13. unity-build-pipeline (BuildPipeline + batchmode + license)
14. steam-deploy (steamcmd/steampipe)
15. addressables-hotfix (Unity OTA)
16. unity-editor-scripting (EditorWindow/PropertyDrawer/Postprocessor)
17. dev-console / cheat menu / debug overlay
