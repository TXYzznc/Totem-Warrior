# GameDesigner 项目环境搭建

新机器拉到项目后，按下面顺序跑一遍即可。

## 前置依赖（机器全局）

| 依赖 | 检查命令 | 用途 |
|---|---|---|
| Python 3.10+ | `python --version` | frame-ronin-mcp 运行环境 |
| Node.js 16+ | `node --version` | game-asset-mcp、skill4agent、playwright 等 |
| git | `git --version` | clone 项目 + pip 装 git 源依赖 |
| uv | `uv --version` | blender / docker / atlassian MCP（可选） |

## 一次性安装命令（在项目根目录执行）

### Windows

```bash
python -m venv .venv
.venv\Scripts\pip install -r requirements.txt
git clone https://github.com/MubarakHAlketbi/game-asset-mcp tools/game-asset-mcp
cd tools/game-asset-mcp && npm install --ignore-scripts && cd ../..
```

### macOS / Linux

```bash
python -m venv .venv
.venv/bin/pip install -r requirements.txt
git clone https://github.com/MubarakHAlketbi/game-asset-mcp tools/game-asset-mcp
cd tools/game-asset-mcp && npm install --ignore-scripts && cd ../..
```

⚠️ 非 Windows 用户还需要把 `.mcp.json` 里 `frame-ronin` 的命令改为：
```
".venv/bin/frame-ronin-mcp"
```

## 凭据填写

编辑 `.mcp.json`，把以下占位符替换为真实值（按需启用对应 MCP）：

- `YOUR_HUGGINGFACE_TOKEN` — Hugging Face API Token（game-asset-mcp）
- `YOUR_MONGODB_CONNECTION_STRING` — MongoDB 连接串
- `JIRA_URL` / `JIRA_USERNAME` / `JIRA_API_TOKEN` — Atlassian/JIRA 凭据
- `GODOT_PATH` — Godot 可执行文件路径（如装 Godot）

## 自动加载的东西

打开 Claude Code 后会自动生效：

- `.mcp.json` 中所有 MCP 服务
- `.claude/settings.json` 启用的 ponytail 插件（源在 `.claude/plugins/ponytail/`）
- `.agents/skills/` 下的 11 个 skill4agent 技能
- `.claude/skills/` 下的所有 Claude Code 原生技能

## 不应被同步的目录

如果用 git，加到 `.gitignore`；用其他同步工具，手动排除：

```
.venv/
tools/game-asset-mcp/node_modules/
.claude/plugins/ponytail/.git/
```

这些都是平台/版本绑定的产物，**新机器跑安装命令即可重建**。
