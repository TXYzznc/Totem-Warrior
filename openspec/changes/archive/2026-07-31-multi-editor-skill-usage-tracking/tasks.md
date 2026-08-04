## 1. 统一协议与记录 CLI

- [x] 1.1 实现 JSONL 事件模型、字段清理、事件 ID 和跨平台安全追加
- [x] 1.2 实现 `hook`、`record`、`migrate` 子命令并保持旧无参数调用兼容
- [x] 1.3 实现 Claude Code、Codex 和通用 stdin 载荷归一化

## 2. 编辑器接入

- [x] 2.1 更新 Claude Code `PreToolUse` Hook 使用显式来源适配器
- [x] 2.2 更新 Codex SessionStart/PreToolUse Hook 接入统一记录器
- [x] 2.3 编写任意 AI 编辑器 CLI/stdin 接入模板与隐私说明

## 3. 审计与兼容

- [x] 3.1 升级审计器读取 JSONL 与旧 TSV、去重并按来源聚合
- [x] 3.2 更新零召回可信度提示和月度防腐文档
- [x] 3.3 更新 `.gitignore` 忽略规范日志及锁文件

## 4. 自动化验证

- [x] 4.1 添加协议解析、隐私过滤、Claude/Codex 适配器测试
- [x] 4.2 添加旧日志幂等迁移、双格式去重和报表测试
- [x] 4.3 运行测试、真实 Hook fixture、审计脚本和 OpenSpec 验证
