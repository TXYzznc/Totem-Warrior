# Codex 输入模板：L1_BATCH 多张独立图片

> 用途：让 Claude 工作流把多个“独立图片”需求合并到同一次 `codex exec` 中执行，避免逐图启动 Codex。
> 适用：角色、Boss、NPC、大型道具、场景图、需要单独构图的高质量单体资源。

## CLI 调用建议

```bash
codex exec \
  -C openspec/changes/06-v21-implementation/art/.codex-runner \
  --add-dir D:/unity/UnityProject/GameDesinger \
  -s workspace-write \
  --ephemeral \
  -o openspec/changes/06-v21-implementation/art/.codex-result-l1-001.json \
  - < openspec/changes/06-v21-implementation/art/.prompt-l1-001.txt
```

## 输入模板

```text
你是图片生成执行器。

任务：
读取下面的 batch JSON，在同一个 Codex session 内批量生成所有 items。

硬性规则：
- 不要读取项目文档。
- 不要解释、不要计划、不要询问。
- 对 items 中的每个条目生成一张独立图片。
- 每个 item 最多调用 1 次 image_gen。
- 可以并行处理多个 item；即使内部不能并行，也必须在同一个 Codex session 内完成整批。
- 单个 item 失败时记录 failed，并继续处理后续 item。
- 不要自行重试。
- 每张图片必须保存到 file 指定路径。
- 最终只输出合法 JSON 数组，不要 markdown。

返回格式：
[
  {"index":1,"file":"...","size_bytes":123456,"status":"ok"},
  {"index":2,"file":"...","size_bytes":0,"status":"failed","error":"..."}
]

batch JSON:
{
  "mode": "L1_BATCH",
  "batch_id": "{{BATCH_ID}}",
  "items": [
    {
      "index": 1,
      "name": "{{ASSET_NAME_1}}",
      "file": "openspec/changes/{{CHANGE_NAME}}/art/raw/{{CATEGORY}}/{{ASSET_NAME_1}}.png",
      "size": "1024x1024",
      "transparent": true,
      "prompt": "{{ENGLISH_PROMPT_1}}",
      "negative": "{{NEGATIVE_PROMPT_1}}"
    },
    {
      "index": 2,
      "name": "{{ASSET_NAME_2}}",
      "file": "openspec/changes/{{CHANGE_NAME}}/art/raw/{{CATEGORY}}/{{ASSET_NAME_2}}.png",
      "size": "1024x1024",
      "transparent": true,
      "prompt": "{{ENGLISH_PROMPT_2}}",
      "negative": "{{NEGATIVE_PROMPT_2}}"
    }
  ]
}
```

## Claude 填充规则

- 每批建议 6-8 个 item，最多 12 个。
- `file` 必须使用项目相对路径，不要使用绝对路径。
- `prompt` 优先英文。
- `transparent` 只在确实需要透明背景时设为 `true`。
- `env_floor_*` / `env_wall_*` 这类贴图默认应为 `transparent: false`。
- 不要把 L2 小图标类素材塞进 L1，除非该素材需要单独高质量构图。

## 适用示例

```json
{
  "mode": "L1_BATCH",
  "batch_id": "boss-npc-001",
  "items": [
    {
      "index": 1,
      "name": "boss_blood_titan",
      "file": "openspec/changes/06-v21-implementation/art/raw/boss/boss_blood_titan.png",
      "size": "1024x1024",
      "transparent": true,
      "prompt": "A massive blood titan boss character, painterly 2.5D game art, strong silhouette, dramatic upper-left lighting, transparent background",
      "negative": "text, watermark, signature, blurry, extra characters"
    },
    {
      "index": 2,
      "name": "npc_blacksmith",
      "file": "openspec/changes/06-v21-implementation/art/raw/npc/npc_blacksmith.png",
      "size": "1024x1024",
      "transparent": true,
      "prompt": "A rugged blacksmith NPC portrait, painterly 2.5D game art, warm forge lighting, centered character, transparent background",
      "negative": "text, watermark, signature, blurry, extra characters"
    }
  ]
}
```

## 验收

Claude 工作流在 Codex 返回后必须本地验证：

- 返回内容是合法 JSON。
- 每个 `status=ok` 的文件存在。
- 文件大小大于 1KB。
- failed 项写入生成记录。
- 不在 Codex 内重试；如需重试，由 Claude 工作流单独生成失败项重试批次。
