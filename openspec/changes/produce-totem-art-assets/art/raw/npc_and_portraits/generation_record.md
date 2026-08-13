# NPC 与占位角色立绘生成记录（2026-07-13）

## 产物

| 资产 ID | 原始 chroma-key / 原图 | 处理结果 | 用途 |
|---|---|---|---|
| `npc_tattooist` | `npc_tattooist_idle_chromakey_source.png` | `npc_tattooist_idle.png` | 场景摆放用全身 Idle，三分之四正面 |
| `npc_merchant` | `npc_merchant_idle_chromakey_source.png` | `npc_merchant_idle.png` | 场景摆放用全身 Idle，三分之四正面 |
| `player_2` | `player_2_signal_hunter_source.png` | `player_2_signal_hunter_portrait.png` | 角色选择占位半身立绘 |
| `player_3` | `player_3_augmented_subject_source.png` | `player_3_augmented_subject_portrait.png` | 角色选择占位半身立绘 |

## 生成规范与结果

- 工具：内置 image generation；NPC 身份与视觉语言参考了 `tmp/imagegen/npc_tattoo_artist_chromakey.png` 与 `tmp/imagegen/npc_merchant_chromakey.png`。
- 风格：半写实厚涂。NPC 延续既有身份；`player_2` 为荒原讯号猎手，`player_3` 为失控改造者。
- NPC：生成阶段使用纯 `#00ff00` chroma-key 背景，随后用 `remove_chroma_key.py --auto-key border --soft-matte --despill` 处理为透明 PNG。
- `npc_tattooist_idle.png`：RGBA，1024×1536；四角 alpha 均为 0；不透明主体包围盒 `(254, 45, 739, 1478)`。
- `npc_merchant_idle.png`：RGBA，1024×1536；四角 alpha 均为 0；不透明主体包围盒 `(264, 42, 765, 1495)`。
- 两张占位肖像保留非透明的角色选择背景：`player_2` 为 RGB 1023×1537；`player_3` 为 RGB 1024×1536。

## 设计约束核对

- 商人与纹身师均为完整全身、双脚可见、Idle 站姿，可用于 2.5D 场景摆放。
- 纹身师保留现有身份要求的紫色发光纹身；商人无固定纹身。
- `player_2` 与 `player_3` 均无固定纹身，头颈、手臂与上躯干的未来贴花空间保持可读。
- 原始与处理素材保留在本目录；2026-07-13 已非破坏性导入正式 Unity 路径：`Assets/Game/Sprites/NPC/NpcTattooist/npc_tattooist_idle.png`、`Assets/Game/Sprites/NPC/NpcMerchant/npc_merchant_idle.png`、`Assets/Game/Sprites/UI/CharacterSelectForm/Portraits/player_2_signal_hunter_portrait.png` 与 `Assets/Game/Sprites/UI/CharacterSelectForm/Portraits/player_3_augmented_subject_portrait.png`。

## Unity integration (2026-07-13)

- NPC world sprites: `Sprite`, `Single`, 256 PPU, Bilinear, Clamp, mipmaps disabled, high-quality compression; the two NPC sprites use a bottom-center pivot `(0.5, 0)`.
- Portrait sprites: `Sprite`, `Single`, 100 PPU, Bilinear, Clamp, mipmaps disabled, high-quality compression; the portraits retain their authored card backgrounds.
- `NpcTattooist.prefab` and `NpcMerchant.prefab` each reference the corresponding imported Sprite; neither gains an AnimatorController because this batch is static Idle only.
- Runtime catalog: `npc.tattooist` and `npc.merchant` use neutral `#FFFFFF` tint; `ui.character.portrait.2` and `ui.character.portrait.3` resolve the two new CharacterSelect portraits. `TotemCharacterSelectForm` binds the new keys for options 2 and 3.
