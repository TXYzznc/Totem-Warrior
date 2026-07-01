---
created: 2026-06-30
scope: 16-min-game-loop-closure Round 1 Bug#2 修复说明
---

# Bug#2 修复说明 — event_invoke 找不到 StartButton

## 根因分析

Prefab 文件 `Assets/Resources/Prefab/UI/MainMenu.prefab` 中 **StartButton 节点完整存在**：

- GameObject 名称：`StartButton`（fileID 5309563200705839398）
- 挂载 Button 组件（fileID 6032490761473454099）
- `MainMenuForm._startBtn` 字段已序列化引用该 Button（Prefab 第 452 行）

Bug 根因是 **unity-skills REST 的 `event_invoke name=StartButton` 使用 `GameObject.Find("StartButton")` 查找**，而 `GameObject.Find` 在不带路径时只匹配根层级对象。`StartButton` 是 `MainMenu` 的直接子节点，不是场景根，因此返回 `GameObject not found: name 'StartButton'`。

## 修复方案（无需改 Prefab 或脚本）

调用 `event_invoke` 时改用层级路径：

```
event_invoke name=MainMenu/StartButton componentName=Button eventName=onClick
```

`GameObject.Find` 支持斜杠路径（`父节点名/子节点名`），改用路径即可命中正确节点。

## 需要用户确认的步骤

无需在 Unity Editor 手动操作，Prefab 和脚本均正确。

**请在下次 Playtest 调用时，将**：
```
event_invoke name=StartButton componentName=Button eventName=onClick
```
**替换为**：
```
event_invoke name=MainMenu/StartButton componentName=Button eventName=onClick
```

如果 unity-skills REST 服务仍然返回 not found（路径格式不支持），备选方案是：
- 在 PlaytestDriverEditor 菜单中直接调用 `editor_execute_menu` 触发 `Tools/Playtest/Debug/StartGame -> InGame`（该菜单已存在于 PlaytestDriverEditor.cs），绕过 event_invoke。

## 同时注意 Bug#1

bugs.md Bug#1 指出 SKILL.md 文档里的参数名是 `objectName`，但 REST 实际参数名是 `name`。请确保调用时用 `name=` 而非 `objectName=`。
