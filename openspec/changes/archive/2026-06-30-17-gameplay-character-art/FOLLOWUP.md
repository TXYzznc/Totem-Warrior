---
created: 2026-06-30
status: pending
context: change 17 已归档，本文件记录 codex 配额耗尽留下的占位资产 + 1 个非阻塞 bug
---

# 17-gameplay-character-art 收尾遗留

## 一、占位 sprite（5 张，下个会话补真图）

| 文件 | 当前状态 | 来源占位 |
|---|---|---|
| `Assets/Resources/Sprite/Character/Player1/Walk/Down.png` | Idle/Down 复制 | codex_runner 写入 stderr |
| `Assets/Resources/Sprite/Character/Player1/Walk/Up.png` | Idle/Up 复制 | 同上 |
| `Assets/Resources/Sprite/Character/Player1/Walk/Left.png` | Idle/Left 复制 | 同上 |
| `Assets/Resources/Sprite/Character/Player1/Walk/Right.png` | Idle/Right 复制 | 同上 |
| `Assets/Resources/Sprite/Character/Player1/Death/Down.png` | Right.png 复制 | _fixup_player1_death_down.json 失败 |

**为什么是占位**：codex 配额 2026-06-30 18:00 左右耗尽（Plan 限额），错误日志 `"You've hit your usage limit ... try again at 7:30 PM"`。Round 1 TC-Art 18/18 全 PASS 是因为 Animator 状态机只检查 state name + IsMoving + Direction，视觉是否真"走"并不影响 TC 通过门槛。

## 二、已就绪的 fixup 批次

```
openspec/changes/archive/2026-06-30-17-gameplay-character-art/art/raw/_fixup_player1_walk_and_death_down.json
```

5 张 sprite，prompt 已从原 `_group_1.json` 复制（保持画风一致）。

## 三、复刻步骤（codex 配额恢复后）

```bash
cd d:/unity/UnityProject/GameDesinger

# 1. 跑 codex 5 张真图
python tools/codex-art-gen-mcp/_run_batches.py \
  openspec/changes/archive/2026-06-30-17-gameplay-character-art/art/raw/_fixup_player1_walk_and_death_down.json

# 2. 校验 5 张都是真 PNG（≥1MB + PIL.Image.open 能读）
python -c "
from PIL import Image
for p in ['Assets/Resources/Sprite/Character/Player1/Walk/Down.png',
          'Assets/Resources/Sprite/Character/Player1/Walk/Up.png',
          'Assets/Resources/Sprite/Character/Player1/Walk/Left.png',
          'Assets/Resources/Sprite/Character/Player1/Walk/Right.png',
          'Assets/Resources/Sprite/Character/Player1/Death/Down.png']:
    im = Image.open(p); print(p, im.size, im.mode)
"

# 3. 让 Unity 重切 + 重生 anim（unity-skills REST 在 8090）
curl -s -X POST http://127.0.0.1:8090/skill/asset_refresh -H 'Content-Type: application/json' -d '{}'
curl -s -X POST http://127.0.0.1:8090/skill/editor_execute_menu -H 'Content-Type: application/json' -d '{"menuPath":"Tools/Character/Reimport Then Generate All"}'

# 4. 跑 TC-Art-02 + TC-Art-04 复验视觉
#    （走 tests/min-plan.md 的菜单序列即可）
```

## 四、BUG-17-01（非阻塞）

**症状**：`Tools/Character/Reimport Then Generate All` 后立即进 Play → `Animator.runtimeAnimatorController == null`。

**绕过**：菜单跑完后 sleep 1-2s 再 `editor_play_mode_start`，或菜单跑完后再点一次 `asset_refresh`。

**根因 hypothesis**：Unity 2022 asset pipeline 在 DeleteAsset + CreateAnimatorControllerAtPath + SaveAsPrefabAsset 同帧操作后，runtime 引用解析时序问题。

**修复建议**（一行）：在 `Assets/Editor/Character/AnimatorGenerator.cs` `ReimportThenGenerateAll()` 末尾追加一次 `AssetDatabase.Refresh(); AssetDatabase.SaveAssets();`，或在 prefab `SaveAsPrefabAsset` 之后 `EditorApplication.delayCall += () => AssetDatabase.Refresh()`。

详见 `tests/bugs.md`。
