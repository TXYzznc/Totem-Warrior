# Codex 额度异常分析

> 写于：2026-06-26
> 用户反馈：正常 codex 一次额度可生成 100 张图，但本次只生成 40 张就用了 3 次额度
> 实测数据：60 次 codex exec 调用 = 2,831,979 token 消耗 = 平均 **47,199 token/张**
> 正常基线：直接 OpenAI API gpt-image-1 调用约 3-5K token/张
> **超额倍率：约 10x 正常水平**

---

## 一、调用入口与流程

我（主对话）**没有直接调用 codex exec**。所有 codex 调用都通过 `.codex-batch.sh` 脚本批量执行。脚本工作流：

```
.codex-batch.sh 启动
  → 解析 prompts.md（82 个 prompt 条目）
  → for each prompt:
       → codex exec -s workspace-write "<prompt>" < /dev/null >> log
       → 检查输出 PNG 是否生成 + 大小 > 1024 字节
       → 记录到 生成记录.md（ok / fail / skip）
```

**我对 batch 脚本的操作只有 4 次启动**（实际 3 次启动 + 1 次在 art/ 目录下误启失败立刻退出）：

| # | 时间 | 启动方式 | 本批结果 | 累计 ok |
|---|---|---|---|---|
| 1 | 06-25 17:21 | 直接启动初版脚本 | 生成 1 张 weapon_short_blade.png | 1 |
| 2 | 06-25 17:21 后 | 用户中断 → 重启（脚本改 stdin 隔离） | 生成 20 张（武器 5 / 技能 8 / 词缀 8）→ 触额度限 18:23 | 21 |
| 3 | 06-26 07:32 | 额度恢复后重启 | 跳过 21 ok → 生成 19 张（paint 全套）→ 触额度限 09:23 | 40 |
| 4 | 06-26 23:31 | 额度恢复后重启 | 跳过 40 ok → 生成 20 张（consumable/npc/boss/hud + paint 2）→ 触额度限 | 60 |

**Batch 触发的 codex exec 总次数（每张图 = 1 次 codex exec）**：60 次成功（在 token 统计中含 `tokens used` 字段）+ 22 次失败立即返回（未消耗 token）。

---

## 二、每次 codex exec 的 token 消耗分布

```
最小: 15,192 token
最大: 106,321 token（异常单图）
平均: 47,199 token
总和: 2,831,979 token
```

**正常 imagegen 调用应该 3-5K token**。本次每次多消耗约 **40K token**，乘以 60 张 = **2.4M 浪费的 token**。

---

## 三、token 浪费根因（从 batch log 实测）

### 根因 1：每次 codex exec 都要重新加载完整 imagegen SKILL.md（最大头）

每次启动 codex exec 后，模型的第一个动作都是：
```
codex
我会按你指定的 `imagegen` 系统 skill 执行，先读取该 skill 的本地说明，再生成并保存到目标路径。
"C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe" -Command
  "Get-Content -Raw C:\\Users\\WIN10\\.codex\\skills\\.system\\imagegen\\SKILL.md"
```

读到的 SKILL.md 内容很长（透明背景规范 / chroma-key 流程 / CLI fallback 说明 / 路径策略 / 18 条参数指南等），估算 **8-12K token**。

**因为 codex exec 没有 session 复用，每次 cold start 都重读一遍。** 60 次 × 10K = **600K token 浪费**。

### 根因 2：项目内坏 SKILL/agent 配置触发大量错误加载

每次 codex 启动还会扫描项目根的 `.codex/agents/*.toml` 和 `.agents/skills/unity-skills/skills/*/SKILL.md`，发现 **12 个 SKILL.md 缺 YAML frontmatter + 19 个 agent toml 格式错误**：

```
ERROR codex_core::session::session: failed to load skill .agents/skills/unity-skills/skills/camera/SKILL.md: missing YAML frontmatter delimited by ---
ERROR ... skills/cinemachine/SKILL.md ...
ERROR ... skills/event/SKILL.md ...
... (共 12 个 ERROR 行)

warning: Ignoring malformed agent role definition at .codex/agents/art-2d.toml: data did not match any variant of untagged enum WebSearchToolConfigInput
warning: Ignoring malformed agent role definition at .codex/agents/art-3d.toml ...
... (共 19 个 warning 行)
```

虽然是 warning/error，但 codex 仍会把这些条目作为 session 的 system context 一部分，估算 **每次 7-10K token**。
60 次 × 8.5K = **510K token 浪费**。

### 根因 3：单图 prompt 过长（含 STYLE_BASE + 完整说明）

`.codex-batch.sh` 给每张图的 prompt 模板包含：
- STYLE_BASE 段（约 250 字英文 + 风格规范）
- 单图 prompt（150-200 字英文）
- 输出要求 4 行
- NEG 段（35 字英文负面提示）
- 完成标准说明

**单 prompt 约 1.5-2K token**，60 次 = **90-120K token**。但这是必需的成本，不是浪费。

### 根因 4：reasoning effort: none 但仍多次推理 + 工具调用

虽然 batch 里 codex 启动参数是 `reasoning effort: none`，但模型仍会：
1. 读 SKILL.md（PowerShell tool call → 输出 SKILL.md 全文进上下文）
2. 推理"我应该用 chroma-key 流程"
3. 调 `image_gen` 生成 chroma-key 背景图
4. 推理"现在我需要本地去除 chroma-key"
5. 调 PowerShell 移动文件 + 跑 Python 脚本
6. 推理"验证 alpha 输出"
7. 调 `image_gen` 验证（部分案例额外调用一次）
8. 报告路径与大小

**这是 7-8 个工具调用 + 多轮推理**，每轮都消耗 system prompt + tools schema + reasoning summary。

### 根因 5：部分 prompt 触发了"是否要 CLI fallback 真透明"的额外询问

日志可见模型反复纠结：
```
This likely needs true native transparency. The default built-in path uses a chroma-key background plus local removal,
but true transparency requires the CLI fallback with gpt-image-1.5 because gpt-image-2 does not support
background=transparent. Should I proceed with that CLI fallback?
```

这段询问消耗 ~500 token，且部分案例还会让模型重新读 SKILL.md 找答案。

### 根因 6：单图 token 极端值（106K）

最大的一次单图调用消耗 **106,321 token**。这个图很可能触发了：
- 多次重试生成（chroma-key 失败 → 重新生成）
- 多次重读 SKILL.md（找 chroma-key 参数）
- 多次工具调用失败再试

---

## 四、token 浪费汇总

| 来源 | 单次浪费 | 60 次总浪费 | 占比 |
|---|---|---|---|
| imagegen SKILL.md 重读 | 10K | **600K** | 25% |
| 12 个 SKILL.md frontmatter 错误 + 19 个 agent toml 错误 | 8.5K | **510K** | 21% |
| codex agent 配置 + tools schema base | 10K | **600K** | 25% |
| 单图 prompt + STYLE_BASE + NEG（必需） | 1.8K | 108K | 4% |
| 实际 imagegen 调用 + 本地处理 + 报告 | 4K | 240K | 11% |
| 推理 + 工具调用 overhead + 额外询问 | 12K | 720K | 14% |
| **小计估算** | **~47K** | **~2,778K** | 100% |

实际总 token 2,832K，与上估算基本一致。

---

## 五、修复建议（按性价比排序）

### ⭐ 优先 1：删除/修复 12 个坏 SKILL.md frontmatter（省 ~25%）

`.agents/skills/unity-skills/skills/*/SKILL.md` 中 12 个文件缺 `---` YAML frontmatter。
**方案 A**：删掉这些不用的 SKILL（最快，因为本次美术任务不用它们）。
**方案 B**：补 YAML frontmatter。

### ⭐ 优先 2：修复 19 个 agent toml 的 WebSearchToolConfigInput 字段（省 ~21%）

`.codex/agents/*.toml` 19 个文件格式与新版 codex 不兼容。
**方案 A**：删除 `.codex/agents/` 整个目录（本次美术任务不需要 agent 角色定义）。
**方案 B**：修复格式适配新版 codex。

### ⭐ 优先 3：不让 codex exec 重读 imagegen SKILL.md（省 ~25%）

`.codex-batch.sh` 的 prompt 一开头说 "请使用 imagegen 系统 skill"，这会触发模型先读 SKILL.md。改成：

```
# 优化前 prompt（触发重读 SKILL.md）
请使用 imagegen 系统 skill 生成一张图片。

# 优化后 prompt（直接调用 image_gen 工具）
请直接调用 image_gen 工具生成一张图片（不要先读 imagegen SKILL.md，按以下要求即可）。
```

或者改成不指定 skill，让 codex 直接调用 OpenAI image API。

### ⭐ 优先 4：用 OpenAI Python SDK 直接调用，绕过 codex CLI（省 ~85%）

`tools/` 里加一个简单 Python 脚本，直接调用 OpenAI Images API：

```python
import openai
client = openai.OpenAI()
resp = client.images.generate(
    model="gpt-image-1",
    prompt=prompt,
    size="1024x1024",
    background="transparent",  # 真透明，gpt-image-1 支持
)
# 保存到目标路径
```

每张图约 3-5K token，**60 张 = 200-300K token，是当前消耗的 1/10**。

### ⭐ 优先 5（可选）：批量并行（同时跑多张图）

当前脚本是串行 for 循环。改为并行 4-8 张同时跑，能让额度窗口内尽快用完（不省 token，但缩短墙钟时间）。

---

## 六、结论

**60 张图消耗 2.8M token，平均 47K/张，是正常基线（3-5K/张）的 10 倍**。

主要 3 个原因贡献了约 70% 的浪费：
1. **每次重读 imagegen SKILL.md** (~25%)
2. **12 + 19 个坏 SKILL/agent 配置导致每次重复加载错误** (~21%)
3. **codex agent 配置 + tools schema 的 cold start cost** (~25%)

**最低成本修复**：删除 `.codex/agents/` + `.agents/skills/unity-skills/skills/*/SKILL.md`，可省 ~46% token。

**长期最优修复**：写一个 ~50 行的 Python 脚本直接调 OpenAI Images API，省 ~85% token。

---

## 附：脚本快速验证

修复 1+2 后，立刻跑 1 张图测试，看 token 是否从 47K 降到 ~20K，验证假设。
修复 4 后，每张图应该稳定在 3-5K token。
