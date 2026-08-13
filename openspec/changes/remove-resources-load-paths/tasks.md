## 1. 资源目录与配置

- [x] 1.1 将运行时资源目录和默认目录中的 50 条 Sprite 活动路径更新到 `Assets/Game/Sprites`，并移除活动旧路径回退。
- [x] 1.2 更新 ResourceConfig、游戏目录与活动编辑器规则中的资源路径约定。

## 2. 运行时加载链路

- [ ] 2.1 让 TotemAssetService 在 Player 中经现有 GF ResourceComponent 加载目录资源。
- [x] 2.2 将 PCG Sprite 和 Texture 加载迁到 `Assets/Game` 路径；PCG 目录 JSON 作为启动配置白名单保留在 `Resources/PCG`。
- [x] 2.3 将 AppSettings、Obfuz 密钥和 AOT 元数据保留为 `Resources` 启动配置白名单，并防止业务资源回流。

## 3. 验证与诊断

- [x] 3.1 更新迁移器、导入器和诊断中活动的旧资源目录引用。
- [ ] 3.2 执行编译、路径检索、GF_X 全量诊断及启动、UI、PCG 和 Player 资源加载验证。
