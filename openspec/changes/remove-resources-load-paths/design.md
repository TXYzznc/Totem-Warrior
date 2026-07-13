## Context

提交 `ca3cdb422` 已将资源与 `.meta` 从 `Assets/Resources` 移入 `Assets/Game`，当前不再存在 `Assets/Resources`。但资源目录的 50 条 Sprite 活动路径、PCG 目录/贴图读取、纹身目录以及部分编辑器工具仍按旧路径或 `Resources.Load` 工作。`TotemAssetService` 目前仅在 Editor 用 `AssetDatabase`，Player 只走回退逻辑；项目已有 `GFBuiltin.Resource`/`GF.Resource.LoadAsset` 的资源管线。

## Goals / Non-Goals

**Goals:**

- 使迁移后资源的目录、运行时与 Player 加载路径一致。
- 使 PCG 和纹身配置不再假定 Resources 目录存在。
- 让内置密钥与 AOT 元数据走现有 GF 资源加载/构建管线。
- 更新活动编辑器规则和诊断，阻止旧路径重新出现。

**Non-Goals:**

- 不重新移动、复制或替换美术资源，不改变其 GUID。
- 不引入 Addressables 或新的第三方资源系统。
- 不修改既有业务玩法、数据表字段语义或输入系统。

## Decisions

- 目录索引保留 Unity 资产路径（`Assets/Game/...`），而不是把它退回 Resources 相对路径；这与现有 GF 资源构建命名及 UI 路径工具一致。
- `TotemAssetService` 以现有 `GFBuiltin.Resource.LoadAsset` 作为 Player 加载实现，同时保留 Editor 的 `AssetDatabase` 快路径。这样复用已验证的项目资源系统，避免引入新依赖。
- PCG JSON、Sprite 与 Texture 统一通过运行时资产服务/同一 GF 异步加载接口取得；同步的 `Resources.Load` 入口移除。调试场景与正式地图服务共用该入口，避免双轨行为。
- AppSettings、Obfuz 和 AOT 元数据的构建位置与加载名同步改为 `Assets/Game` 下的现有目录。若 GF 构建规则无法覆盖某种内置资源，先以可验证的现有构建 API 接入，不创建新的 Resources 兼容目录。
- `legacySourcePath` 仅保留为迁移证据，不参与运行时回退；活动目录和默认目录全部指向新位置。

## Risks / Trade-offs

- [GF 资源名与 Unity 资产路径的映射不一致] → 先复用项目既有 `UtilityBuiltin.AssetsPath` 和 ResourceComponent 调用，并通过 Player 构建验证。
- [异步加载改变原同步调用时序] → 将调用点改为显式异步完成回调/UniTask，并在服务初始化后再使用结果。
- [框架内置构建流程受保护] → 仅修改确有旧路径的最小入口；每项改动附带诊断或定向检查。
- [遗留编辑器工具被遗漏] → 全局检索活动 `Assets/Resources` 引用，历史归档/报告明确排除。

## Migration Plan

1. 更新目录与生成默认目录，验证所有活动路径对应文件存在。
2. 接入并验证运行时、PCG 与框架资源的 GF 加载路径。
3. 更新 ResourceConfig、编辑器迁移器、导入器与诊断的活动路径。
4. 编译、运行 GF_X 全量诊断、启动/UI/PCG 冒烟，并执行 Player 资源加载验证。
5. 回滚时仅还原代码与配置提交；不移动资产或改写 `.meta`。

## Open Questions

- 当前资源构建规则中 AppSettings、Obfuz 与 AOT 元数据的最终资源名需要在实现前从 GF 构建配置确认。
