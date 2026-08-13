## ADDED Requirements

### Requirement: 迁移后运行时资源可在所有目标环境加载
系统 MUST 使用 `Assets/Game` 下的活动资产路径加载运行时目录中登记的资源；Editor 快路径与 Player 的 GF 资源加载均不得依赖 `Assets/Resources`。

#### Scenario: 运行时目录加载 Sprite
- **WHEN** 运行时目录请求一条已登记的 Sprite
- **THEN** 系统从其 `Assets/Game/Sprites` 活动路径返回该 Sprite，且不尝试 `Assets/Resources` 回退

#### Scenario: Player 加载资源
- **WHEN** Player 构建请求已登记的 Prefab、Sprite 或 Texture
- **THEN** 系统通过 GF ResourceComponent 完成加载，而不是直接返回运行时回退对象

### Requirement: PCG 业务资源不依赖 Resources 目录
PCG 目录配置 MAY 放在 `Assets/Resources/PCG` 作为启动配置白名单；PCG Sprite 和 Texture MUST 从迁移后的 `Assets/Game` 路径读取。

#### Scenario: 初始化 PCG 目录
- **WHEN** 地图服务或 PCG 调试场景初始化目录
- **THEN** 系统读取 `Resources/PCG` 下的目录 JSON
- **AND** 目录内登记的 Sprite / Texture 资源路径指向 `Assets/Game/Sprites/PCG`

### Requirement: Resources 仅承载启动配置白名单
`Assets/Resources` MUST NOT 重新承载业务美术、Prefab 或 DataTable 资源；只允许 AppSettings、Obfuz、PCG 配置与 AOT 元数据等启动早期配置。

#### Scenario: 启动热更新流程
- **WHEN** 启动流程配置 Obfuz 或加载 AOT 元数据
- **THEN** 系统 MAY 请求 `Resources/Obfuz` 或 `Resources/AotDlls`
- **AND** Runtime Asset Catalog 中登记的业务资源仍指向 `Assets/Game`
