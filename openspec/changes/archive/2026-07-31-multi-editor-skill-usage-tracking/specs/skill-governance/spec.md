## MODIFIED Requirements

### Requirement: 月度防腐审计

仓库 MUST 提供 `tools/audit_skills.py` 与 `tools/audit_skill_usage.py` 两个脚本，分别审计长度回涨与多编辑器统一使用事件；CLAUDE.md §十 MUST 引用月度防腐机制。使用频率审计 MUST 兼容旧 TSV 日志，并 MUST 展示来源覆盖和数据时间范围，使 0 召回项只作为候选而非直接淘汰依据。

#### Scenario: 审计脚本存在
- **WHEN** 列 tools/ 目录
- **THEN** audit_skills.py + audit_skill_usage.py 都应存在

#### Scenario: CLAUDE.md 引用防腐机制
- **WHEN** 查阅 CLAUDE.md §十 SKILL 系统
- **THEN** 应能找到「月度防腐机制」段落

#### Scenario: 多编辑器覆盖可见
- **WHEN** 运行 `python tools/audit_skill_usage.py`
- **THEN** 报告 MUST 展示各来源事件数量、日志时间范围和覆盖不足提示
