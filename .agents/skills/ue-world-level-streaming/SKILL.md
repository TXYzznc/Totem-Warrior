---
name: ue-world-level-streaming
description: 当你使用World Partition、关卡流送、关卡切换、OpenLevel、ServerTravel、数据层、世界子系统、关卡实例、子关卡、无缝切换、开放世界或HLOD时，可以使用本技能。请查看references/streaming-patterns.md获取按游戏类型划分的配置模式。
metadata:
  version: 1.0.0
tags: ue-world-partition, level-streaming, level-travel, unreal-engine, hlod
tags_cn: UE World Partition, 关卡流送, 关卡切换, Unreal Engine开发, HLOD优化
---

# UE 世界与关卡流送

你是Unreal Engine世界管理与关卡流送系统方面的专家。

---

## 上下文

在提供建议前，请阅读`.agents/ue-project-context.md`。请注意：
- **引擎版本** — World Partition仅支持UE5；子关卡流送在UE4和UE5中均可用。
- **构建目标** — 专用服务器不支持基于渲染的流送；流送必须保证服务器安全。
- **世界大小** — 决定应使用World Partition还是手动子关卡流送。
- **多人游戏** — 无缝切换的要求及每个玩家的流送半径。

---

## 需要收集的信息

在推荐流送方案前，请确认：

1. **世界大小与类型**：这是开放世界（World Partition）、一组独立关卡，还是中心辐射式地图？
2. **多人游戏**：是否运行专用服务器？是否需要每个玩家的流送半径？
3. **流送控制**：游戏玩法代码是否需要显式控制加载/卸载，还是应基于距离驱动？
4. **关卡切换**：非无缝切换（大厅流程）、无缝切换（多人游戏回合过渡），还是无需切换？
5. **持久化数据**：关卡切换时必须保留哪些内容——玩家状态、物品栏、会话状态？

---

## World Partition（UE5）

### 启用World Partition

通过关卡菜单启用：**World -> World Partition -> Convert Level**。启用后，关卡中的所有Actor将由World Partition的网格系统管理。该关卡将无法再拥有传统子关卡。对于协作编辑，请使用**One File Per Actor (OFPA)**：每个Actor将作为独立的`.uasset`文件保存到`__ExternalActors__`目录下。

### 运行时数据层

数据层替代了旧的子关卡切换模式。运行时数据层可在运行时加载/卸载，无需切换到新地图。

```cpp
// MyGameMode.cpp
#include "WorldPartition/DataLayer/DataLayerManager.h"

void AMyGameMode::ActivateDungeonDataLayer()
{
    UDataLayerManager* DLMgr = UDataLayerManager::GetDataLayerManager(GetWorld());
    if (!DLMgr) return;

    // Get by asset reference (set up in editor as a UDataLayerAsset)
    UDataLayerAsset* DungeonLayer = DungeonDataLayerAsset.LoadSynchronous();
    DLMgr->SetDataLayerRuntimeState(DungeonLayer, EDataLayerRuntimeState::Activated);
}

void AMyGameMode::DeactivateDungeonDataLayer()
{
    UDataLayerManager* DLMgr = UDataLayerManager::GetDataLayerManager(GetWorld());
    if (!DLMgr) return;

    UDataLayerAsset* DungeonLayer = DungeonDataLayerAsset.LoadSynchronous();
    DLMgr->SetDataLayerRuntimeState(DungeonLayer, EDataLayerRuntimeState::Unloaded);
}
```

**数据层状态：**
- `Unloaded` — 未加载，不可见。
- `Loaded` — 已加载到内存，不可见（预加载）。
- `Activated` — 已加载且可见（完全激活）。

### 流送源

默认情况下，每个玩家控制器都是一个流送源。对于自定义源（如电影摄像机、AI导演），请实现`IWorldPartitionStreamingSourceProvider`。

### HLOD

HLOD为World Partition单元格提供远距离合并网格表示。在World Partition编辑器中配置HLOD层；发布前通过**Build -> Build World Partition HLODs**构建。如果没有HLOD，流送半径之外的内容将直接缺失。

### 将子关卡转换为World Partition

使用**Tools -> World Partition -> Convert Level**。Actor将迁移到持久关卡并由WP管理。转换前请检查跨关卡引用——对转换后Actor的硬引用将失效。

### World Partition与多人游戏

在多人游戏会话中，每个玩家控制器作为流送源，拥有可配置的半径。服务器基于服务器端源进行流送；客户端通过`AServerStreamingLevelsVisibility`接收可见性更新。在专用服务器上，基于渲染的流送不适用——流送仅由服务器端源驱动。

流送半径在World Partition编辑器UI中按分区配置（`URuntimePartition`上的`LoadingRange`），而非通过ini文件。

---

## 关卡流送（手动子关卡）

### ULevelStreaming状态机

来自`LevelStreaming.h`的完整状态序列：

```
Removed -> Unloaded -> Loading -> LoadedNotVisible -> MakingVisible -> LoadedVisible -> MakingInvisible -> LoadedNotVisible
                                      |
                                 FailedToLoad   (check logs; level asset missing or corrupt)
```

使用以下代码查询状态：

```cpp
ULevelStreaming* StreamingLevel = /* ... */;
ELevelStreamingState State = StreamingLevel->GetLevelStreamingState();

switch (State)
{
    case ELevelStreamingState::Unloaded:         /* not in memory */ break;
    case ELevelStreamingState::Loading:          /* async load in progress */ break;
    case ELevelStreamingState::LoadedNotVisible: /* in memory, not rendered */ break;
    case ELevelStreamingState::MakingVisible:    /* adding to world */ break;
    case ELevelStreamingState::LoadedVisible:    /* fully active */ break;
    case ELevelStreamingState::MakingInvisible:  /* removing from rendering */ break;
    case ELevelStreamingState::FailedToLoad:     /* check logs */ break;
}
```

### UGameplayStatics: LoadStreamLevel / UnloadStreamLevel

对于支持蓝图的异步流送（使用 latent actions，来自`GameplayStatics.h`）：

```cpp
// MyActor.cpp — async load using FLatentActionInfo
#include "Kismet/GameplayStatics.h"

void AMyActor::StreamInRoom(FName LevelName)
{
    FLatentActionInfo LatentInfo;
    LatentInfo.CallbackTarget = this;
    LatentInfo.ExecutionFunction = FName("OnRoomLoaded");
    LatentInfo.Linkage = 0;
    LatentInfo.UUID = GetUniqueID();

    UGameplayStatics::LoadStreamLevel(
        this,           // WorldContextObject
        LevelName,      // e.g., FName("Room_01")
        true,           // bMakeVisibleAfterLoad
        false,          // bShouldBlockOnLoad — keep false for async
        LatentInfo
    );
}

UFUNCTION()
void AMyActor::OnRoomLoaded()
{
    // Room is now loaded and visible
}

void AMyActor::StreamOutRoom(FName LevelName)
{
    FLatentActionInfo LatentInfo;
    LatentInfo.CallbackTarget = this;
    LatentInfo.ExecutionFunction = FName("OnRoomUnloaded");
    LatentInfo.Linkage = 0;
    LatentInfo.UUID = GetUniqueID() + 1;

    UGameplayStatics::UnloadStreamLevel(
        this,
        LevelName,
        LatentInfo,
        false // bShouldBlockOnUnload
    );
}
```

对于软对象指针（打包安全的首选方式），使用`LoadStreamLevelBySoftObjectPtr`并传入相同参数。

### ULevelStreamingDynamic: 运行时关卡实例

使用`ULevelStreamingDynamic::LoadLevelInstance`在不同变换位置多次加载同一个关卡包——适用于程序化地牢、模块化建筑或实例化房间（来自`LevelStreamingDynamic.h`）：

```cpp
#include "Engine/LevelStreamingDynamic.h"

void AMyDungeonGenerator::SpawnRoom(FVector Location, FRotator Rotation)
{
    bool bSuccess = false;
    ULevelStreamingDynamic* StreamingLevel = ULevelStreamingDynamic::LoadLevelInstance(
        this,                              // WorldContextObject
        TEXT("/Game/Levels/Room_Corridor"), // LongPackageName — full path
        Location,
        Rotation,
        bSuccess
    );

    if (bSuccess && StreamingLevel)
    {
        // Bind to delegate to know when visible
        StreamingLevel->OnLevelShown.AddDynamic(this, &AMyDungeonGenerator::OnRoomShown);
        StreamingLevel->OnLevelHidden.AddDynamic(this, &AMyDungeonGenerator::OnRoomHidden);

        LoadedRooms.Add(StreamingLevel);
    }
}

void AMyDungeonGenerator::UnloadRoom(ULevelStreamingDynamic* StreamingLevel)
{
    if (StreamingLevel)
    {
        StreamingLevel->SetShouldBeLoaded(false);
        StreamingLevel->SetShouldBeVisible(false);
        StreamingLevel->SetIsRequestingUnloadAndRemoval(true);
    }
}
```

对于网络场景：使用`OptionalLevelNameOverride`为所有客户端和服务器指定同一个实例包名。如果不设置，每个进程会自动生成唯一名称，导致跨连接不匹配。

```cpp
ULevelStreamingDynamic::FLoadLevelInstanceParams Params(
    GetWorld(),
    TEXT("/Game/Levels/Room_Corridor"),
    FTransform(Rotation, Location)
);
Params.OptionalLevelNameOverride = &InstanceName; // FString, same on server and clients
Params.bInitiallyVisible = true;

bool bSuccess = false;
ULevelStreamingDynamic* Level = ULevelStreamingDynamic::LoadLevelInstance(Params, bSuccess);
```

### OnLevelShown / OnLevelHidden委托

来自`LevelStreaming.h`的四个`BlueprintAssignable`委托：`OnLevelLoaded`、`OnLevelUnloaded`、`OnLevelShown`、`OnLevelHidden`。使用`AddDynamic`绑定：

```cpp
StreamingLevel->OnLevelShown.AddDynamic(this, &UMyManager::HandleLevelShown);
StreamingLevel->OnLevelLoaded.AddDynamic(this, &UMyManager::HandleLevelLoaded);
```

### 流送体积

`ALevelStreamingVolume`会在玩家摄像机进入或离开体积时自动控制子关卡的加载。来自`LevelStreamingVolume.h`：

```cpp
// EStreamingVolumeUsage — set on the volume in editor
SVB_Loading                 // load but do not make visible
SVB_LoadingAndVisibility    // load and make visible (most common)
SVB_VisibilityBlockingOnLoad // force blocking load when entering
SVB_BlockingOnLoad          // block load of associated levels
SVB_LoadingNotVisible       // load, keep invisible (pre-warm)
```

体积通过子关卡的`EditorStreamingVolumes`数组分配给该子关卡。当你希望仅通过代码控制时，可设置`ULevelStreaming::bDisableDistanceStreaming = true`来禁用体积驱动的流送。

### 手动可见性控制

```cpp
// Get streaming level reference from world
const TArray<ULevelStreaming*>& Levels = GetWorld()->GetStreamingLevels();
for (ULevelStreaming* Level : Levels)
{
    if (Level->GetWorldAssetPackageFName() == FName("/Game/Levels/MySubLevel"))
    {
        Level->SetShouldBeLoaded(true);
        Level->SetShouldBeVisible(true);
        break;
    }
}
```

强制刷新所有流送（会阻塞直到完成——谨慎使用）：
```cpp
UGameplayStatics::FlushLevelStreaming(this);
```

---

## 关卡实例

`ALevelInstance`在编辑器中作为可重用块放置关卡。内部的Actor可作为一个单元进行编辑。如需运行时实例化，请查看上文的`ULevelStreamingDynamic`。

**打包关卡Actor**会将实例网格合并为单个静态网格以提升性能。通过右键点击Level Instance → **Pack Level Actor**启用。

**每个实例的属性覆盖（UE5.1+）：** 每个放置的`ALevelInstance`可覆盖单个Actor的属性（材质、游戏玩法数值），无需修改源关卡。在细节面板中配置覆盖；覆盖的值会在打包时烘焙到关卡数据中。

---

## 关卡切换

### 非无缝切换：UGameplayStatics::OpenLevel

销毁当前世界并加载新世界；所有客户端断开连接。来自`GameplayStatics.h`：

```cpp
UGameplayStatics::OpenLevel(this, FName("/Game/Maps/MainMenu"), true);
UGameplayStatics::OpenLevel(this, FName("/Game/Maps/GameLevel"), true, TEXT("?Difficulty=Hard"));
UGameplayStatics::OpenLevelBySoftObjectPtr(this, GameLevelAsset, true); // packaging-safe
```

### Server Travel（多人游戏，非无缝）

由服务器发起；所有连接的客户端跟随（来自`World.h`）：

```cpp
GetWorld()->ServerTravel(TEXT("/Game/Maps/Level02?listen"), /*bAbsolute=*/false);
```

### 无缝切换

无缝切换通过过渡（中点）地图在后台加载目标地图。客户端保持连接。来自`World.h`：

```cpp
void UWorld::SeamlessTravel(const FString& InURL, bool bAbsolute);
bool UWorld::IsInSeamlessTravel() const;
void UWorld::SetSeamlessTravelMidpointPause(bool bNowPaused);
```

**设置要求：**

1. 在`AGameModeBase`上设置`bUseSeamlessTravel = true`：

```cpp
// bUseSeamlessTravel is already declared in AGameModeBase — do NOT redeclare it.
// Just set it in the constructor:

// MyGameMode.cpp constructor
bUseSeamlessTravel = true;
```

2. 在`DefaultEngine.ini`中设置过渡地图：

```ini
[/Script/Engine.GameMapsSettings]
TransitionMap=/Game/Maps/Transition
```

3. 重写`GetSeamlessTravelActorList`以控制哪些Actor可以保留：

```cpp
// GameMode — called on server side during transition
void AMyGameMode::GetSeamlessTravelActorList(bool bToTransition, TArray<AActor*>& ActorList)
{
    Super::GetSeamlessTravelActorList(bToTransition, ActorList);

    if (!bToTransition)
    {
        // bToTransition=false means we're moving TO the destination
        // Add actors that should survive (e.g., GameState, custom managers)
        ActorList.Add(MyPersistentManager);
    }
}

// GameMode — called after destination map is loaded
void AMyGameMode::PostSeamlessTravel()
{
    Super::PostSeamlessTravel();
    // Re-initialize any post-travel systems
}

// GameMode — handle re-possessing players after travel
void AMyGameMode::HandleSeamlessTravelPlayer(AController*& C)
{
    Super::HandleSeamlessTravelPlayer(C);
    // Restore player-specific state here
}
```

4. 在服务器上触发：

```cpp
// From GameMode, server-only
GetWorld()->ServerTravel(TEXT("/Game/Maps/Level02?listen"));
// Seamless travel is automatic because bUseSeamlessTravel is true
```

**切换流程：** 当前世界 -> 过渡地图 -> 目标世界。使用`SetSeamlessTravelMidpointPause(true)`可在中点暂停以进行预加载。

### 客户端切换

对于客户端发起的切换（加入服务器、更改选项），从玩家控制器调用`APlayerController::ClientTravel(URL, ETravelType::TRAVEL_Absolute)`。

---

## 世界子系统

`UWorldSubsystem`（来自`Subsystems/WorldSubsystem.h`）会为每个`UWorld`自动实例化一次。当世界被销毁时（包括关卡切换），它也会被销毁。这是实现每个世界单例逻辑的正确位置：流送管理器、区域追踪器、世界状态缓存。

```cpp
// MyStreamingManager.h
UCLASS()
class MYGAME_API UMyStreamingManager : public UWorldSubsystem
{
    GENERATED_BODY()
public:
    virtual void PostInitialize() override;                          // after all subsystems init
    virtual void OnWorldBeginPlay(UWorld& InWorld) override;         // after all BeginPlay
    virtual void PreDeinitialize() override;                         // cleanup hook
    virtual bool ShouldCreateSubsystem(UObject* Outer) const override; // filter world type

    void RequestLoadZone(FName ZoneName);
    void RequestUnloadZone(FName ZoneName);
private:
    TMap<FName, TWeakObjectPtr<ULevelStreaming>> ActiveZones;
};
```

通过世界上下文从任何位置访问：

```cpp
UMyStreamingManager* Manager = GetWorld()->GetSubsystem<UMyStreamingManager>();
if (Manager)
{
    Manager->RequestLoadZone(FName("Zone_A"));
}
```

### UTickableWorldSubsystem

用于每帧更新（距离检查、区域检测）。继承自`UTickableWorldSubsystem`。必须调用`Super::Initialize`和`Super::Deinitialize`来启用/禁用tick。实现`GetStatId`并返回`RETURN_QUICK_DECLARE_CYCLE_STAT`。

---

## 关卡切换间的持久化数据

| 机制 | 生命周期 | 使用场景 |
|---|---|---|
| `UGameInstance` | 整个应用会话 | 跨关卡玩家状态、会话配置 |
| `UGameInstanceSubsystem` | 整个应用会话 | 比任何世界生命周期都长的服务 |
| 无缝切换Actor列表 | 仅切换过程中 | 物理跨关卡的Actor（GameState、管理器） |
| `USaveGame` + `SaveGameToSlot` | 磁盘持久化 | 长期保存、进度记录 |
| `UWorldSubsystem` | 每个世界 | 世界范围的缓存；在切换清除前，在`Deinitialize()`中将数据推送到`UGameInstance` |

### GameInstance模式

将跨关卡数据存储在`UGameInstance`属性中（可在所有关卡切换中保留）。通过世界上下文从任何位置访问：

```cpp
UMyGameInstance* GI = GetGameInstance<UMyGameInstance>();
if (GI) GI->PlayerScore += 100;
```

---

## 常见错误与反模式

**一次性加载所有内容**。在多个子关卡上设置`bShouldBlockOnLoad = true`会导致卡顿。使用异步加载和latent action模式。仅在加载屏幕后方块加载。

**流送体积间隙**。重叠体积会导致不必要的卸载/重新加载循环。在流送关卡上使用`MinTimeBetweenVolumeUnloadRequests`添加冷却时间，防止闪烁。

**多人游戏中无缝切换失效**。如果`bUseSeamlessTravel`为true但未设置过渡地图，无缝切换会自动回退到非无缝切换。务必在`DefaultEngine.ini`中设置`TransitionMap`。

**跨关卡硬引用**。不同流送关卡中Actor之间的硬对象引用（`UPROPERTY() UObject*`）会导致整个被引用关卡保持加载。跨关卡边界时请始终使用`TSoftObjectPtr`或`TSoftClassPtr`。

**动态流送关卡名称在服务器与客户端不匹配**。使用`ULevelStreamingDynamic::LoadLevelInstance`时，每个进程会生成唯一名称。在多人游戏中，请为服务器和所有客户端提供相同的`OptionalLevelNameOverride`。

**专用服务器上的World Partition**。服务器不使用基于渲染的流送。必须在服务器端显式添加流送源（如玩家位置），否则World Partition无法在服务器上正确流送Actor。

**直接修改`StreamingLevels`**。不要直接向`UWorld::StreamingLevels`添加内容。请使用`AddStreamingLevels`、`AddUniqueStreamingLevels`和`RemoveStreamingLevels`（来自`World.h`），它们会处理内部记录和`StreamingLevelsToConsider`。

**在`UTickableWorldSubsystem`中忘记调用`Super::Initialize` / `Super::Deinitialize`**。这些调用分别用于启用和禁用tick。跳过它们会导致子系统永远不会tick或永远不会停止tick。

---

## 快速参考：关键API

| API | 头文件 | 说明 |
|---|---|---|
| `UGameplayStatics::LoadStreamLevel` | `Kismet/GameplayStatics.h` | 命名子关卡的异步latent加载 |
| `UGameplayStatics::UnloadStreamLevel` | `Kismet/GameplayStatics.h` | 异步latent卸载 |
| `UGameplayStatics::FlushLevelStreaming` | `Kismet/GameplayStatics.h` | 阻塞式刷新——仅在加载屏幕后使用 |
| `UGameplayStatics::OpenLevel` | `Kismet/GameplayStatics.h` | 非无缝关卡切换 |
| `ULevelStreamingDynamic::LoadLevelInstance` | `Engine/LevelStreamingDynamic.h` | 运行时关卡实例化 |
| `ULevelStreaming::GetLevelStreamingState` | `Engine/LevelStreaming.h` | 查询当前流送状态 |
| `ULevelStreaming::SetShouldBeLoaded` | `Engine/LevelStreaming.h` | 驱动加载状态 |
| `ULevelStreaming::SetShouldBeVisible` | `Engine/LevelStreaming.h` | 驱动可见性 |
| `ULevelStreaming::SetIsRequestingUnloadAndRemoval` | `Engine/LevelStreaming.h` | 从世界中移除关卡 |
| `UWorld::ServerTravel` | `Engine/World.h` | 多人游戏关卡切换 |
| `UWorld::SeamlessTravel` | `Engine/World.h` | 后台无缝切换 |
| `UWorld::GetStreamingLevels` | `Engine/World.h` | 遍历所有流送关卡 |
| `UDataLayerManager::SetDataLayerRuntimeState` | `WorldPartition/DataLayer/DataLayerManager.h` | World Partition数据层控制（使用`UDataLayerManager::GetDataLayerManager(World)`） |
| `UWorldSubsystem::OnWorldBeginPlay` | `Subsystems/WorldSubsystem.h` | BeginPlay后的初始化钩子 |
| `AGameModeBase::GetSeamlessTravelActorList` | `GameFramework/GameModeBase.h` | 控制Actor持久化 |

---

## 相关技能

- `ue-gameplay-framework` — GameMode切换回调、`PostSeamlessTravel`、Actor持久化规则。
- `ue-data-assets-tables` — 与关卡流送互补的异步资产加载模式。
- `ue-networking-replication` — 网络可见性事务、服务器流送权限。
- `ue-cpp-foundations` — 子系统模式、`UGameInstance`生命周期。