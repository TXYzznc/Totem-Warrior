---
change_id: 22-gameplay-visual-polish
status: in-progress
created: 2026-07-01
---

# Design — 22-gameplay-visual-polish

> 阶段 A 共识：proposal.md 已把 4 子项范围、DoD、性能约束写死。本文只补技术落地方案，不再讨论"要不要做"。

## 决策日志（阶段 A 固化）

| 决策 | 选择 | 备选 | 理由 |
|---|---|---|---|
| **Bot 染色实现** | `SpriteRenderer.color` 直接改（内部走 MPB） | 独立材质 / Shader Graph / URP RenderFeature | SRP Batcher 合并不破，49 Bot 无 SetPass 增加，代码量最小 |
| **Bot 描边** | 挂子对象放大 5% 纯黑 SpriteRenderer, sortingOrder -1 | shader outline / RenderFeature | 无 shader 依赖，一段代码搞定 |
| **Bot 色板数据源** | ~~新增 DataTable~~ → **改为 BotVisualBinder 内硬编码 8 色** | DataTable / SO / 硬编码 | 硬编码色板 8 个只是渲染尝试用，加 DataTable 要打断跑 DataTableGenerator；未来抽 DataTable 是 1 小时的独立 change |
| **VFX 接线** | 复用 `VFXModule`，补 `[EventHandler]` 订阅 | 新建 `VFXBridgeModule` | VFXModule 已订阅 6 个事件（Tattoo/Boss），模式一致，不新增模块开销 |
| **阴影素材** | 1 张 128×128 黑色软阴影 PNG（复用/占位）| 每 Actor 一张定制 / URP 动态阴影 | 圆形贴地即可，动态阴影超范围 |
| **阴影挂点** | 静态帮助方法 `ActorShadowHelper.Attach(go, y)` | Prefab 内预置 Shadow / SpawnerModule 唯一入口 | SpawnerModule 非统一入口（玩家/敌人分路径），helper 让所有 spawn 路径复用 |
| **音效模块** | 扩展 `AudioModule` 加 `PlayOneShot / PlayBgm` | 新建 SfxPlayer / BgmPlayer 模块 | Proposal 说"简单模块或 static helper 皆可"；扩现有模块更省编排 |
| **MainMixer.mixer 生成** | 用户手动 Unity Editor 新建（1 分钟） | Editor 脚本反射生成 | AudioMixer 是特殊 asset，反射生成脆弱，手动更稳 |
| **音效资源清单** | 本轮用**内置 UI 音**或占位 CC0，不出真音效 | 找专业音效师做 | Proposal §D "能听即可"；正稿延后到独立音频 change |
| **WeaponConfig 加字段** | JSON schema 追加 3 字段 + 用户跑 DataTableGenerator | 用 SO 或 ScriptableObject 兜底 | 遵守 CLAUDE.md §十二"配置表更新必须走 DataTableGenerator" |
| **并行策略** | 4 子项按 Fan-Out 顺序推（不并行）| 4 agent 并行 | 4 子项都碰 EventBus 订阅 / Actor spawn 路径，串行避免上下文碎片，工作量总量不大 |

## 目标

让战场从"能跑"升级到"能看/能感"：

1. 49 Bot 一眼分辨 Smart vs Light 组
2. 攻击命中 / 击杀 / 死亡有 VFX + 音效反馈
3. 所有战斗单位脚下有阴影
4. MainMenu / InGame 有 BGM，UI 按钮有 click 音
5. 消除 AudioModule MainMixer 缺失 Warning

## 技术方案

### 子项 A：Bot 染色 + 描边

**数据流**：
```
DataTable: BotColorPresetConfig (SmartHex[], LightHex[])
    ↓
BotControllerModule.BuildControllers()
    ├─ 生成 Bot GameObject（现有）
    └─ ★ 新增 BotVisualBinder.ApplyColorAndOutline(go, isSmart, index)
         ├─ SpriteRenderer.color = presets[index % presets.Length]
         └─ AttachOutline(go)（放大 5% 子对象）
```

**BotVisualBinder** 新建为轻量 static class：
```csharp
public static class BotVisualBinder
{
    public static void ApplyColorAndOutline(GameObject bot, Color color)
    {
        var sr = bot.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = color;
        AttachOutline(bot, sr);
    }

    static void AttachOutline(GameObject bot, SpriteRenderer sr)
    {
        if (sr == null) return;
        var outline = new GameObject("Outline");
        outline.transform.SetParent(bot.transform, false);
        outline.transform.localScale = Vector3.one * 1.05f;
        var osr = outline.AddComponent<SpriteRenderer>();
        osr.sprite = sr.sprite;
        osr.color  = Color.black;
        osr.sortingOrder = sr.sortingOrder - 1;
    }
}
```

**色板**：BotVisualBinder 内硬编码 Smart 4 暖色 + Light 4 冷色，`(index % 4)` 循环。未来抽 DataTable 只需把常量换成 Get 调用即可。

### 子项 B：VFX 接线

**VFXModule 追加 3 个 [EventHandler]**：
```csharp
[EventHandler]
void OnWeaponAttackHit(WeaponAttackHitEvent e)
{
    var pos = e.Target?.transform?.position ?? Vector3.zero;
    SpawnHitspark(pos, e.IsCrit);   // 复用已有 SpawnHitspark
}

[EventHandler]
void OnTargetKilled(TargetKilledEvent e)
{
    var pos = e.Target?.transform?.position ?? Vector3.zero;
    SpawnKillBurst(pos);   // 新增，复用现有 SpawnAOEBurst 骨架
}

[EventHandler]
void OnPlayerDied(PlayerDiedEvent _)
{
    var playerPos = GetPlayerPosition();
    SpawnKillBurst(playerPos, scale: 2f);  // 复用同一 helper，放大
}
```

**保持范围**：不做屏幕震动 / 击退波纹（推后续）。`SpawnHitspark`/`SpawnKillBurst` 内部走现有粒子系统 API。

### 子项 C：阴影

**素材**：`Assets/Resources/Sprite/` 无现成黑色圆形 → **代码运行时生成 Texture2D（64×64 径向渐变）+ Sprite.Create 一次缓存**，不落 PNG。避免打断用户调 codex-image-gen。

**ActorShadowHelper**：
```csharp
public static class ActorShadowHelper
{
    static Sprite _shadowSprite;
    public static void Attach(GameObject actor, float radius = 0.5f, float yOffset = -0.4f)
    {
        if (_shadowSprite == null)
            _shadowSprite = Resources.Load<Sprite>("Sprite/VFX/shadow_soft");
        if (_shadowSprite == null) return;

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(actor.transform, false);
        shadow.transform.localPosition = new Vector3(0, yOffset, 0);
        shadow.transform.localScale    = Vector3.one * radius * 2f;
        var sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite = _shadowSprite;
        sr.color  = new Color(0, 0, 0, 0.4f);
        sr.sortingOrder = -100;  // 底层
    }
}
```

**接入点**：
- `SpawnerModule.SpawnActor(...)`：末尾 `ActorShadowHelper.Attach(go)`
- 玩家 spawn 路径（`Player.prefab` 实例化处，grep 确认位置）：同上
- 覆盖玩家 + 49 Bot + 敌人 = 全体

### 子项 D：音效接入

**AudioModule 扩展**：
```csharp
public void PlayOneShot(string clipPath, Vector3 position, float volume = 1f)
{
    var clip = Resources.Load<AudioClip>(clipPath);
    if (clip == null) { FrameworkLogger.Warn("AudioModule", $"Clip={clipPath} not found"); return; }
    AudioSource.PlayClipAtPoint(clip, position, volume * _sfxVolume);
}

public void PlayBgm(string clipPath, bool loop = true, float fadeSec = 0.5f)
{
    if (_bgmSource == null) _bgmSource = CreateBgmSource();
    var clip = Resources.Load<AudioClip>(clipPath);
    if (clip == null) { FrameworkLogger.Warn("AudioModule", $"BGM={clipPath} not found"); return; }
    _bgmSource.clip = clip;
    _bgmSource.loop = loop;
    _bgmSource.volume = _bgmVolume;
    _bgmSource.Play();
}
```

`_bgmSource` 用 `new GameObject("BgmSource").DontDestroyOnLoad + AudioSource`。淡入淡出用 DOTween 一句 `DOFade`。

**MainMixer.mixer 手动创建**（必须通知用户）：
1. Unity Editor 中 `Assets/Resources/Audio/` → 右键 → Create → Audio Mixer → 命名 `MainMixer`
2. 打开 Mixer，Groups 面板：右键 Master → Add child group → `BGM`；再加 `SFX`
3. Attenuation 面板：右键 BGM 的 Volume → Expose to script → Rename → `BgmVolume`；SFX 同理 → `SfxVolume`

**新增事件订阅（EventDrivenAudio 挂 GameApp 或 AudioModule）**：
```csharp
[EventHandler] void OnAttackHit(WeaponAttackHitEvent e)
{
    var cfg = DataTables.WeaponConfig.Get(e.WeaponId);
    var path = e.IsCrit ? cfg.HitSfxPath : cfg.HitSfxPath;
    _audio.PlayOneShot(path, e.Target?.transform?.position ?? Vector3.zero);
}

[EventHandler] void OnTargetKilled(TargetKilledEvent e)
{
    var cfg = DataTables.WeaponConfig.Get(...); // 从最近武器上下文
    _audio.PlayOneShot("Audio/Sfx/kill_generic", e.Target.transform.position);
}

[EventHandler] void OnGameStateChanged(StateChangedEvent e)
{
    if (e.New == GameState.MainMenu) _audio.PlayBgm("Audio/Bgm/menu");
    else if (e.New == GameState.InGame) _audio.PlayBgm("Audio/Bgm/combat");
}
```

**WeaponConfig schema 扩展**：~~本轮不改 WeaponConfig~~。音效路径**按 WeaponConfig.Class 分类**（Melee / Ranged / Special）在 AudioBridge 内**硬编码到常量**：
```csharp
static string PickHitSfx(string weaponClass) => weaponClass switch
{
    "Melee"  => "Audio/Sfx/hit_melee",
    "Ranged" => "Audio/Sfx/hit_ranged",
    _        => "Audio/Sfx/hit_generic",
};
```
避免用户手动跑 DataTableGenerator。3 个音效资源路径未来抽出到 DataTable 是独立小 change。

**音效资源**：本轮找 3~5 个 CC0 音效放 `Assets/Resources/Audio/Sfx/`，或写空 clip 让 Warn 输出但不崩。占位可用：Unity Package Manager 里的 Standard Assets Legacy Audio。

## 关键时序

- **Bot 染色时机**：`BotControllerModule.BuildControllers()` 已经在 InGame 前一步跑完；`ApplyColorAndOutline` 在 Bot GameObject 实例化后同步调，不异步。
- **VFX vs 音效**：都订阅 `WeaponAttackHitEvent`，各自独立处理，无冲突。
- **BGM 切换**：`GameStateModule.StateChanged` 是同步事件，`PlayBgm` 内部异步淡入不阻塞主循环。
- **阴影挂点**：`SpawnActor` 是同步方法，`Attach` 在 return 前挂完子对象，不会遇到"Actor 已死但 Shadow 未挂"的时序问题。

## 强制人工节点（Auto Mode 中断）

按 CLAUDE.md §十二关键约束：

1. **MainMixer.mixer 创建**（可选）：若用户希望消除 AudioModule Warn 需要手动 3 步在 Unity Editor 创建；不做也不阻塞（AudioListener.volume fallback 已存在）。**本轮标为选做**。
2. ~~配置表更新~~：本轮通过硬编码色板 + 硬编码音效路径**规避**这个打断点。
3. ~~阴影 sprite 采集~~：通过代码运行时 Texture2D 生成**规避**。

其他所有决策（子对象命名、Warn 日志格式、event handler 排列顺序）主对话直接自决。

## 例外打断（其他情况都不 halt）

- 现有 `WeaponAttackHitEvent` / `TargetKilledEvent` / `PlayerDiedEvent` 签名需要**破坏性变更** → halt
- 需要新增/删除 `Assets/Scripts/Core/` 框架类 → halt
- Bot 数量或 spawn 路径重构 → halt

## 验收（对应 proposal DoD）

1. ✅ PlayMode 截图肉眼可见 Smart/Light 组色差 + 每 Bot 有黑色描边
2. ✅ 攻击命中屏幕出 hitspark，击杀出 burst，玩家死亡出大 burst
3. ✅ 玩家 + 49 Bot + 敌人脚下都有半透明阴影
4. ✅ MainMenu 进入有 BGM，切 InGame 换战斗 BGM
5. ✅ Console 0 Error，AudioModule Mixer=Audio/MainMixer 找到（Info，非 Warn）
6. ✅ Editor Stats 面板 SetPass calls 相比 change #21 增幅 < 10%
