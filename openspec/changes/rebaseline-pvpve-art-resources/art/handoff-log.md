# 美术 → 主玩法交接日志

> 目标 change：`rebuild-six-player-pvpve-first-playable`  
> 规则：此文件收集接口需求；美术 change 不直接修改程序、DataTable 或框架。

## H-001：稳定 UI 节点与占位资源替换

- 状态：layout 已产出，待用户审核确认后交由主玩法装配占位 Prefab。
- 请求：主玩法可在 layout 被确认后，按稳定 Canvas 节点与 slot ID 拼装可交互占位 Prefab。
- 约束：后续替换正式素材时不得改业务节点 ID；动态文字、数值、倒计时、玩家名与版本号均由程序渲染。
- 当前证据：`art/prefab-layout.md` 与 `first-playable-ui-art`、`ui-planning` capability 的结构先行要求。

## H-002：敌人美术接口暂缓

- 状态：已移出本 change；等待用户后续单独发起敌人设计任务。
- 请求：当前主玩法仅使用不带造型语义的目标 bounds、hit point/normal 与中性占位锚点，不要求本 change 提供敌人专属视觉接口。
- 约束：本日志不预设敌人类别、身体结构、弱点位置、材质、动画或 Prefab；后续任务自行重新澄清并定义。
- 当前证据：用户明确要求敌人后续单独设计；本 change `first-playable-combat-art` 排除敌人资产。

## H-003：事件队列 VFX 消费

- 状态：A8 详细设计已产出，待用户审核与主玩法确认事件 payload。
- 请求：按程序已排序队列提供独立的表现绑定点：P01/P02、头部/弱点、枪械臂、躯干及三元素状态。
- 约束：VFX 只按指令顺序播放，不重新排序、不判断伤害、不创建玩法事件；关闭演出延迟不改变绑定。
- 当前证据：`art/vfx/first-playable-vfx-design.md` 与主玩法 `elemental-reaction-and-effect-queue` capability。

## H-004：基础枪械的替换门禁

- 状态：用户已确认“民用模块化巡防步枪”类型与方向；详细设计待整包审核。
- 请求：主玩法继续消费中性占位 key；详细设计审核通过后由单一稳定 key `weapon.rifle.patrol.v1` 替换，不增加第二类武器。
- 约束：正式模型、UV、PBR、LOD 与挂点生产须在用户审核完整设计包并明确授权后开始。
- 当前证据：`art/weapon/WPN-FP-001_rifle-design-brief.md`。

## H-005：设置项与结果页开发字段

- 状态：待主玩法确认已有 runtime 能力。
- 请求：核对 `prefab-layout.md` 中音频、显示、质量、垂直同步、帧率、鼠标灵敏度、glyph 模式及无障碍设置的实际数据绑定；仅开发构建显示 seed、快速模式与重开。
- 约束：未实现的设置项不得做成假功能；release 构建必须折叠开发节点而非仅隐藏文字。
- 当前证据：`art/prefab-layout.md` 第 7、16 节。

## H-006：通用 VFX 锚点与目标 payload

- 状态：待主玩法确认命名映射。
- 请求：提供已结算 hit point/normal、目标 bounds、团队关系、反应影响位置，以及可选 `VFX_TargetCore`、`VFX_StatusTop` 和武器 `Socket_Muzzle`、`Socket_ElementRail`。
- 约束：缺少锚点时回退到 renderer bounds；美术不查询敌人种类、不定义敌人弱点位置、不选择反应目标。
- 当前证据：`art/vfx/first-playable-vfx-design.md` 与 `art/weapon/WPN-FP-001_rifle-design-brief.md`。

## H-007：独立美术资源测试场景

- 状态：设计合同已补充，场景在正式 VFX 生产阶段创建。
- 请求：由主玩法 / 客户端提供 `Assets/Game/Scene/ArtResourceTest.unity` 所需的 InputModule 动作绑定、相机基准、正式后处理基准和可替换资源 key 预览接口。
- 约束：测试场景不包含敌人造型或玩法规则；全部交互必须走 `TotemInputService` / `ITotemInputProvider`，不得直接轮询按键。未通过测试场景和目标平台构建复测的资源不得接入正式流程。
- 当前证据：`art/vfx/first-playable-vfx-design.md` 第 7 节与 `first-playable-combat-art` capability。
