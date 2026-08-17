# Fast Script Reload（FSR）开发指南

FSR 用于缩短 **Unity Editor 的 Play Mode** 中修改 C# 代码后的等待时间：保持当前运行会话，动态替换可支持的方法实现。本项目使用 FSR 1.8.0 的本地 UPM 包；它不是面向已发布 Player 的热更新方案，也不替代 HybridCLR 或资源更新流程。

## 包与适用范围

- UPM 依赖固定为 `file:../LocalPackages/FastScriptReload/Assets`，不要再从 Git URL 或 Asset Store 额外安装第二份 FSR。
- FSR 源码以 `LocalPackages/FastScriptReload` Git submodule 固定在项目维护的 fork `TXYzznc/FastScriptReload` 提交 `5ef1a8b8566cc8c18326ef80300fc55bad81062c`。该提交使用官方 `Lib.Harmony.Thin 2.4.2` 及固定依赖，并使 2021+ Roslyn 文件名与程序集名一致，以兼容 Unity 2022.3/Burst。版本、来源、许可证和 SHA-256 见 `LocalPackages/FastScriptReload/Assets/Plugins/Harmony/HarmonyThinDependencies.md`。克隆项目时使用 `git clone --recurse-submodules`；已克隆的项目执行 `git submodule sync --recursive` 后再执行 `git submodule update --init --recursive`。
- `FastScriptReload.Editor` 仅在 Editor 中编译；运行时程序集还受 `UNITY_EDITOR || LiveScriptReload_IncludeInBuild_Enabled` 约束。不要定义后一个符号，因此 FSR 不会进入发布构建。
- 每位开发者都要在本机的 Unity Preferences 中完成一次下述配置；这些 Preferences 属于本机设置，不会随 Git 同步。

## 首次配置

1. 用 Unity 2022.3.62f3c1 打开项目，等待 Package Manager 和脚本编译完成。
2. 在 `Edit > Preferences > Asset Pipeline` 中，将 **Auto Refresh** 设为 **Enabled Outside Playmode**。
3. 打开 `Window > Fast Script Reload > Start Screen`，在 **Reload** 页配置：
   - 勾选 **Enable auto Hot-Reload for changed files (in play mode)**；
   - 勾选 **Enable on demand hot reload**；
   - 勾选 **Force prevent assembly reload during playmode**；
   - 将 **Batch script changes and reload every N seconds** 设为 `1`；
   - 若项目代码使用 `this` 且 FSR 提示相关编译问题，勾选 **(Experimental) Enable method calls with 'this' as argument fix**。
4. **Specify watched files/folders manually** 保持未勾选。默认监听项目 `Assets` 下的 `.cs` 文件；仅在需要监听包外代码时才改用手动监听。

配置完成后，先退出并重新进入一次 Play Mode，让 FSR 缓存当前脚本版本。

## 日常开发流程

1. 从 `Assets/Game/Scene/Launch.unity` 进入 Play Mode，推进到需要调试的状态。
2. 优先修改已有类型中已有的、非泛型方法的方法体，例如分支、计算、日志、UI 行为或调参逻辑。
3. 保存 `.cs` 文件，等待约 1 秒。Console 出现 `FSR: Hot-reload completed` 即表示补丁已应用，Play Mode 不应退出。
4. 自动重载未触发时，使用 `Window > Fast Script Reload > Force Reload` 手动执行；该菜单依赖前述 **Enable on demand hot reload**。
5. 每个小批次结束后停止 Play Mode。Unity 会进行一次正常编译；确认 Console 没有新增编译错误后再继续。

## 可选的重载回调

回调必须在**进入 Play Mode 前**就已存在于脚本中。仅在确实需要在补丁后刷新临时状态时添加，不要把它作为正常初始化入口。

```csharp
// 仅适合存活的 MonoBehaviour 实例。
private void OnScriptHotReload()
{
    RefreshPreview();
}

// 适合非 MonoBehaviour 类型，或不依赖实例的刷新。
private static void OnScriptHotReloadNoInstance()
{
    RefreshStaticPreview();
}
```

`OnScriptHotReload` 不会对 `ProcedureBase`、普通 C# 服务等非 `MonoBehaviour` 类型的实例执行；这些场景使用静态回调，或在正常流程中显式刷新。

## 何时不要使用 FSR

以下改动需要停止 Play Mode，让 Unity 进行完整 Domain Reload / 正常编译后再验证：

- 新增或删除类型、程序集定义、包依赖、泛型类型或泛型方法；
- 新增或删除字段、修改序列化字段、Prefab/Inspector 绑定，或修改 ScriptableObject 数据结构；
- 修改初始化顺序、静态状态、事件注册、场景结构或资源引用；
- FSR Console 报编译失败、类型找不到、方法签名不兼容，或行为与预期不一致。

FSR 对新增字段提供实验性支持，但本项目默认不依赖它；结构变更一律按正常 Unity 工作流验证。

## 故障排查

| 现象 | 处理 |
| --- | --- |
| 保存后没有 FSR 日志 | 检查 Auto Refresh 是否为 `Enabled Outside Playmode`，以及自动/按需重载是否已勾选；停止并重新进入 Play Mode。 |
| `Force Reload` 菜单不可用 | 在 FSR 的 Reload 页勾选 **Enable on demand hot reload**，并确认当前处于 Play Mode。 |
| FSR 编译报错或某类代码不生效 | 停止 Play Mode，按普通 Unity 编译处理；必要时查 FSR 的 **User Script Overrides**，不要强行绕过结构变更。 |
| 出现 DOTween / `InternalsVisibleTo` 解析警告 | 这类警告不一定阻断热重载。只有未出现 `Hot-reload completed` 或修改后行为未生效时，才停止运行并做完整编译/进一步排查。 |

## 验收标准

一次 FSR 开发验证应同时满足：

- Console 有 `FSR: Hot-reload completed`；
- Play Mode 保持运行；
- 修改后的行为在当前会话中生效；
- 停止 Play Mode 后，Unity 正常编译无新增错误。
