---
name: grill-with-docs
description: 一场针对现有领域模型挑战你的计划、打磨术语，并在决策明确时同步更新文档（CONTEXT.md、ADRs）的审查会议。当用户想要针对其项目的语言规范和已记录的决策对计划进行压力测试时使用。
disable-model-invocation: true
tags: domain-model, documentation-update, adr-management, plan-review, terminology-sharpening
tags_cn: 领域模型, 文档更新, ADR管理, 计划审查, 术语打磨
---

请针对该计划的各个方面对我进行全面提问，直到我们达成共识。逐一梳理设计树的每个分支，逐个解决决策之间的依赖关系。对于每个问题，请给出你的推荐答案。

每次只提一个问题，等待反馈后再继续提问。

如果某个问题可以通过探索代码库找到答案，请直接探索代码库。

## 领域感知

在探索代码库时，同时查找现有文档：

### 文件结构

大多数代码库只有一个上下文：

```
/
├── CONTEXT.md
├── docs/
│   └── adr/
│       ├── 0001-event-sourced-orders.md
│       └── 0002-postgres-for-write-model.md
└── src/
```

如果根目录下存在`CONTEXT-MAP.md`，则该代码库包含多个上下文。该文件会指向每个上下文的位置：

```
/
├── CONTEXT-MAP.md
├── docs/
│   └── adr/                          ← 系统级决策
├── src/
│   ├── ordering/
│   │   ├── CONTEXT.md
│   │   └── docs/adr/                 ← 上下文专属决策
│   └── billing/
│       ├── CONTEXT.md
│       └── docs/adr/
```

延迟创建文件——仅当有内容可写时再创建。如果不存在`CONTEXT.md`，则在第一个术语明确后创建它。如果不存在`docs/adr/`目录，则在需要创建第一个ADR时创建该目录。

## 会议期间

### 对照术语表提出质疑

当用户使用的术语与`CONTEXT.md`中的现有术语冲突时，立即指出：“你的术语表将‘cancellation’定义为X，但你似乎指的是Y——到底是哪一个？”

### 明确模糊表述

当用户使用模糊或多义术语时，提出精准的标准术语建议：“你提到的‘account’——是指Customer还是User？这两者是不同的概念。”

### 讨论具体场景

在讨论领域关系时，用具体场景进行压力测试。设计能探查边缘情况的场景，迫使用户明确概念之间的边界。

### 与代码交叉验证

当用户说明某事物的工作方式时，检查代码是否一致。如果发现矛盾，及时指出：“你的代码会取消整个Order，但你刚才说可以部分取消——哪个是正确的？”

### 同步更新CONTEXT.md

当术语明确后，立即更新`CONTEXT.md`。不要批量处理——在决策确定时就记录下来。使用[CONTEXT-FORMAT.md](./CONTEXT-FORMAT.md)中的格式。

不要将`CONTEXT.md`与实现细节绑定。仅包含对领域专家有意义的术语。

### 谨慎创建ADR

仅当同时满足以下三个条件时，才创建ADR：

1. **难以逆转**——后续更改决策的成本很高
2. **缺乏上下文会令人困惑**——未来的读者会疑惑“他们为什么要这么做？”
3. **是实际权衡的结果**——存在真正的替代方案，且你因特定原因选择了其中一个

如果缺少任意一个条件，就不要创建ADR。使用[ADR-FORMAT.md](./ADR-FORMAT.md)中的格式。