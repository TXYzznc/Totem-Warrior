# unity-skills MCP 中文编码 bug — spike 报告

> Spike 时间盒：30 分钟 | 仅静态代码分析，未启动 server / 未发 HTTP

## 1. MCP 入口与组件定位

**重要事实修正**：`unity-skills` **不是 MCP**，而是一个 Claude **SKILL**（通过 Python 脚本 + HTTP 调 Unity Editor）。

| 组件 | 位置 |
|---|---|
| Skill 定义 | `D:\unity\UnityProject\GameDesinger\.claude\skills\unity-skills\SKILL.md` |
| Python 客户端（实际"调用方"） | `D:\unity\UnityProject\GameDesinger\.claude\skills\unity-skills\scripts\unity_skills.py` |
| Unity Editor 端 HTTP listener | `D:\unity\UnityProject\GameDesinger\OutPackages\Unity-Skills-main\SkillsForUnity\Editor\Skills\SkillsHttpServer.cs` |
| Skill 路由层 | `OutPackages\Unity-Skills-main\SkillsForUnity\Editor\Skills\SkillRouter.cs` |
| UI / GameObject / Prefab 业务 | `OutPackages\Unity-Skills-main\SkillsForUnity\Editor\Skills\{UISkills,GameObjectSkills,PrefabSkills}.cs` |
| 默认端口 | **8090**（`unity_skills.py:22-23`），用户说的 8091 应是多实例自动分配端口（见同文件 `_find_port_by_target`） |
| 项目 MCP 启动注册 | **无**。`.mcp.json` / `.codex/config.toml` / `~/.codex/config.toml` 都没有 `unity-skills` 条目；`.claude/settings.local.json` 的 `enabledMcpjsonServers` 也不包含 |

`~/.claude.json` line 622-626 显示 `skillUsage.unity-skills.usageCount=11` —— 进一步证实它是按 SKILL 走的（SKILL 才有 usage 计数）。

## 2. 链路图（ASCII）

```
[Claude 主对话生成中文 GameObject 名 / text 字段]
            │  Python list/dict（内部 str = Python 3 UTF-16-ish）
            ▼
[unity_skills.py:84]  json.dumps(kwargs, ensure_ascii=False)
            │  Python str → UTF-8 bytes（保留 0xE4-0xE9 高位字节）
            ▼
[unity_skills.py:87]  data=json_data.encode('utf-8')
            │  HTTP POST body bytes
            ▼
[HTTP over localhost, Content-Type: application/json; charset=utf-8]
            │
            ▼
[SkillsHttpServer.cs:865]  new StreamReader(InputStream, Encoding.UTF8).ReadToEnd()
            │  正确以 UTF-8 解码为 C# string（UTF-16 内部表示）
            ▼
[SkillsHttpServer.cs:1974]  JObject.Parse(job.Body)  (Newtonsoft.Json)
            │  JToken 内 string 字段保留 Unicode
            ▼
[SkillRouter.cs:346 反射]  method.Invoke(args)
            │  参数装箱传给 UISkills.UISetText(text=...) 等
            ▼
[UISkills.cs:92-103 SetTextOnComponent]  textProp.SetValue(comp, text)
            │  反射设 TMP/Text.text 属性
            ▼
[TMP_Text.text → m_text 字段 → SerializedProperty]
            │
            ▼
[PrefabUtility.SaveAsPrefabAsset → Unity YAML 序列化器]
            │  ★ 实际看到的 `����`（U+FFFD = Replacement Char）
            ▼
[Settings.prefab 落盘 = ASCII-only 文件，零高位字节]
```

> **直接取证**（`Settings.prefab`）：
> - `file` 命令报告："ASCII text, with CRLF line terminators"（即文件里**没有任何 ≥0x80 字节**，正常 UTF-8 中文 prefab 此处会被识别为 UTF-8 Unicode text）
> - 13 处 `m_text:` 命中如：
>   - `m_text: "����"` (line 68/695/1029/1743/3110/5205)
>   - `m_text: "ȡ��"` (line 203) ← **关键线索**
>   - `m_text: "��ͣ"` (line 1242)
>   - `m_text: "�����Ƴ�"` (line 1878)
>   - `m_text: "�ƶ�"` (line 3245)
> - U+0221、U+0363、U+01B3、U+01B6 这些**散落的非中文 Unicode 字符**是判案关键 —— 它们正好落在 "把 UTF-8 多字节序列按单字节字符流读入后形成的 Latin-1 残片" 的特征区间。

## 3. 根因猜测（按可能性排序）

### 1. 最可能：损坏发生在 Python 客户端**之前**，即 Claude/Codex Agent 喂给 `unity_skills.py` 的参数本身就已经被 mojibake

**证据**：
- `unity_skills.py` 全链路写得**非常对**：`ensure_ascii=False`（line 84）、`.encode('utf-8')`（line 87）、`Content-Type: application/json; charset=utf-8`（line 88）、`response.encoding = 'utf-8'`（line 91）、Windows console 输出也强制 UTF-8（line 9-14）。任何 review 都挑不出毛病。
- `SkillsHttpServer.cs:865` 严格用 `Encoding.UTF8` 读 body；`JObject.Parse` 是 Unicode-safe；`JsonConvert.SerializeObject` 默认不破坏 Unicode；响应 `ContentType = "application/json; charset=utf-8"`（line 326/1031）+ `Encoding.UTF8.GetBytes`（line 325/1032）。**server 端在业务路径上找不到任何 ASCII fallback / Default encoding 调用**。
- 唯一 `Encoding.ASCII` 出现在 `SkillsHttpServer.cs:2195`，是 SelfTest 健康检查路径，**与 ui_set_text 等业务无关**，排除。
- prefab 中 `ȡ` / `ͣ` / `Ƴ` 这些非中文小写 Unicode 字符的出现，是**UTF-8 字节按 Latin-1 / Windows-1252 当成单字节 char 强塞进字符串**的特征 —— 这种事情如果发生在 Python 内部，要么是 Agent 通过 CLI 传参时 shell 的 `chcp` / `PYTHONIOENCODING` 没配好，要么是某个上游工具用 `data.encode('latin-1')` 之类把 UTF-8 bytes 反编了一遍。
- 用 `python unity_skills.py ui_set_text name=X text=测试` 这种 CLI 方式调用时，Windows cmd/PowerShell 默认 GBK code page（cp936），如果 Codex agent 通过子进程传中文参数到 Python，**`sys.argv` 在 Windows 上经历 ANSI/Unicode argv 转换可能丢字符**。

**未亲眼看到的关键环节**：Codex / Claude Code agent 到 `unity_skills.py` 之间的胶水层。

### 2. 次可能：Newtonsoft.Json 反序列化时遇到非法 surrogate 触发 `JsonReader` 替换

如果 Python 端 JSON body 中已经混了非法字符（半个 surrogate / 0x80-0xFF 单字节），`JObject.Parse` 在 strict 模式下不会 throw 但可能把无效 escape 转成 U+FFFD。但 server 端代码本身没有强制 strict，这一步**是放大器而不是元凶**。

### 3. 不太可能：Unity TMP `m_text` SerializedProperty 自身有 ASCII fallback

TMP_Text 内部存的 string 是标准 UTF-16，YAML 序列化用 `\uXXXX` escape 是 Unity 的正常行为（YAML 1.1 在 ASCII 外的字符要 escape）。但 escape 出 `�` 说明 **string 内部已经是 U+FFFD**，不是序列化器搞坏的。

## 4. 本期决策

**是否能在本期 13-fix-broken-prefabs 顺手修：❌ 不修**

**理由**：
1. 4 个 Prefab 已经污染落盘，**修代码不会自动复原**，必须重建 Prefab。本期目标是修 prefab，**重建 4 个 prefab 是当下唯一可行路径**。
2. 根因猜测 top 1 指向 **Claude/Codex agent → Python CLI 的胶水层**，**不在本仓库代码内**，无法在 13-fix-broken-prefabs 改一行业务代码解决。
3. 真正的修复需要：
   - 复现实验：手写一个测试调用 `python unity_skills.py ui_set_text name=Test text=测试` 看 `sys.argv[2]` 拿到的是不是已经 mojibake
   - 如确认是 argv 损坏 → 改 SKILL.md 强制要求通过 stdin JSON 传参，绕过 argv
   - 如确认是 Codex shell 子进程 spawn 时编码错误 → 在 Codex 配置加 `PYTHONUTF8=1` 或 `PYTHONIOENCODING=utf-8`
   - 全部需要可重现的 e2e 验证，不是 30 分钟能拍板的事
4. 本期 13 应该 **彻底放弃用 unity-skills 自动建 Prefab 写中文文本**，回到 §十二 "Prefab 必须手动建" 的兜底原则：让用户在 Unity Editor 里手动改正乱码 + 后续保留 git commit 作为参考；client-unity 只生成代码 + 提供文字常量字典供用户对照填入。

**建议下一期治理 change 立项**：`14-mcp-encoding-fix`（题目：unity-skills CJK 编码端到端复现与根因定位），范围：
- 端到端复现实验（Python 直调 vs Codex 调）
- 必要时改 `unity_skills.py` CLI argv 解析方式（增加 `--stdin-json` 模式）
- 文档化：unity-skills SKILL.md 加 "已知问题：当前不支持中文文本字段，需先用 ASCII 占位符建 Prefab，后续用户/工具手工填中文"

## 附录：取证 grep 命中点

| 文件 | 行号 | 内容 |
|---|---|---|
| unity_skills.py | 84 | `json_data = json.dumps(kwargs, ensure_ascii=False)` ← 客户端编码正确 |
| unity_skills.py | 87 | `data=json_data.encode('utf-8')` ← 客户端编码正确 |
| unity_skills.py | 88 | `headers={'Content-Type': 'application/json; charset=utf-8'}` ← 客户端编码正确 |
| SkillsHttpServer.cs | 325-326 | `Encoding.UTF8.GetBytes(responseJson)` + `ContentType = "application/json; charset=utf-8"` ← server 端编码正确 |
| SkillsHttpServer.cs | 865 | `new StreamReader(request.InputStream, Encoding.UTF8)` ← server 端读 body 编码正确 |
| SkillsHttpServer.cs | 1974 | `JObject.Parse(job.Body)` ← Newtonsoft Unicode-safe |
| SkillsHttpServer.cs | 2195 | `Encoding.ASCII.GetBytes(httpReq)` ← **仅 SelfTest，与业务无关** |
| Settings.prefab | 68/203/695/1029/1242/1743/1878/2765/3110/3245/5205/5340/5821/6794 | 14 处 `m_text:` 含 `�` / `ȡ` / `ͣ` / `Ƴ` / `ƶ` |
| Settings.prefab | (whole) | `file` 命令识别为 ASCII（即无任何 ≥0x80 字节） |
