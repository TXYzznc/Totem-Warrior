---
name: ue-procedural-generation
description: 当你在Unreal Engine中进行程序化生成相关工作时使用本技能，涵盖内容包括：PCG框架、ProceduralMesh、实例化网格、HISM、样条曲线、运行时网格、噪声、地形生成或地牢生成。PCG节点类型请参考references/pcg-node-reference.md，网格生成模式请参考references/procedural-mesh-patterns.md若需了解程序化几何体的物理相关内容，请查看ue-physics-collision。
metadata:
  version: 1.0.0
tags: unreal-engine-procedural-generation, pcg-framework, procedural-mesh, ism-hism,
  spline-generation
tags_cn: Unreal Engine程序化生成, PCG框架, ProceduralMesh开发, ISM/HISM使用, 样条曲线生成
---

# Unreal Engine程序化生成

你是Unreal Engine程序化生成系统的专家，涵盖PCG框架、ProceduralMeshComponent、实例化静态网格、噪声函数和基于样条曲线的生成。

## 上下文检查

在提供建议前，请阅读`.agents/ue-project-context.md`以确定：
- PCG插件是否已启用（插件列表）
- 目标生成类型：世界布局、地形、地牢、植被、运行时网格
- 性能约束（移动平台、主机、是否启用Nanite）
- 多人游戏需求（服务器权威 vs 确定性种子）

## 信息收集

请询问以下信息以明确需求：
1. **生成类型**：世界布局（PCG）、运行时网格（ProceduralMeshComponent）、实例化几何体（ISM/HISM）还是样条曲线驱动？
2. **时机**：编辑器阶段烘焙结果还是运行时动态生成？
3. **实例数量**：数百个（ISM）还是数万个（HISM）？
4. **碰撞**：生成的几何体是否需要物理碰撞？
5. **确定性**：相同种子是否必须在不同会话或网络客户端中产生相同结果？

---

## 1. PCG框架（UE 5.2+）

基于节点的规则驱动型世界生成系统，针对包含变换、密度、颜色、种子和元数据属性的点云进行操作。

### 插件设置

```csharp
// Build.cs
PublicDependencyModuleNames.Add("PCG");
```
```json
// .uproject Plugins array
{ "Name": "PCG", "Enabled": true }
```

### 核心类

| 类 | 头文件 | 用途 |
|---|---|---|
| `UPCGComponent` | `PCGComponent.h` | 驱动生成的Actor组件 |
| `UPCGGraph` | `PCGGraph.h` | 资源：节点+连线 |
| `UPCGGraphInstance` | `PCGGraph.h` | 带有参数覆盖的Graph实例 |
| `UPCGPointData` | `Data/PCGPointData.h` | 节点间传递的点云数据 |
| `UPCGSettings` | `PCGSettings.h` | 节点设置基类 |
| `UPCGBlueprintBaseElement` | `Elements/Blueprint/PCGBlueprintBaseElement.h` | 自定义Blueprint节点基类 |

### UPCGComponent核心API（来自`PCGComponent.h`）

```cpp
// Assign graph (NetMulticast)
void SetGraph(UPCGGraphInterface* InGraph);

// Trigger generation (NetMulticast, Reliable) — use for multiplayer
void Generate(bool bForce);

// Local non-replicated generation
void GenerateLocal(bool bForce);

// Cleanup
void Cleanup(bool bRemoveComponents);
void CleanupLocal(bool bRemoveComponents);

// Notify to re-evaluate after Blueprint property change
void NotifyPropertiesChangedFromBlueprint();

// Read generated output
const FPCGDataCollection& GetGeneratedGraphOutput() const;
```

生成触发方式（`EPCGComponentGenerationTrigger`）：
- `GenerateOnLoad` — 在BeginPlay时一次性生成
- `GenerateOnDemand` — 仅通过显式调用`Generate()`触发
- `GenerateAtRuntime` — 由`UPCGSubsystem`根据预算调度生成

### UPCGGraph节点API（来自`PCGGraph.h`）

```cpp
// Add node by settings class
UPCGNode* AddNodeOfType(TSubclassOf<UPCGSettings> InSettingsClass, UPCGSettings*& DefaultNodeSettings);

// Connect two nodes
UPCGNode* AddEdge(UPCGNode* From, const FName& FromPinLabel, UPCGNode* To, const FName& ToPinLabel);

// Graph parameters (typed template)
template<typename T>
TValueOrError<T, EPropertyBagResult> GetGraphParameter(const FName PropertyName) const;

template<typename T>
EPropertyBagResult SetGraphParameter(const FName PropertyName, const T& Value);
```

### 自定义Blueprint PCG节点

继承自`UPCGBlueprintBaseElement`：

```cpp
UCLASS(BlueprintType, Blueprintable)
class UMyPCGNode : public UPCGBlueprintBaseElement
{
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "PCG|Execution")
    void Execute(const FPCGDataCollection& Input, FPCGDataCollection& Output);
};

// In Execute:
FRandomStream Stream = GetRandomStreamWithContext(GetContextHandle()); // deterministic seed

for (const FPCGTaggedData& In : Input.GetInputsByPin(PCGPinConstants::DefaultInputLabel))
{
    const UPCGPointData* InPts = Cast<UPCGPointData>(In.Data);
    if (!InPts) continue;

    UPCGPointData* OutPts = NewObject<UPCGPointData>();
    for (const FPCGPoint& Pt : InPts->GetPoints())
    {
        FPCGPoint NewPt = Pt;
        NewPt.Density = Stream.FRandRange(0.5f, 1.0f);
        OutPts->GetMutablePoints().Add(NewPt);
    }
    Output.TaggedData.Emplace_GetRef().Data = OutPts;
}
```

`UPCGBlueprintBaseElement`的关键属性：
- `bIsCacheable = false` — 当节点生成Actor或组件时设置
- `bRequiresGameThread = true` — 用于生成Actor、添加组件的场景
- `CustomInputPins` / `CustomOutputPins` — 额外的类型化引脚

### PCG的确定性

默认情况下，PCG图是确定性的——相同的种子会产生完全相同的输出。每个节点通过`GetRandomStreamWithContext()`获取一个带种子的随机流。若要在不同实例间生成不同输出，请设置PCG组件的`Seed`属性。对于多人游戏，确保所有客户端使用相同的种子（可通过GameState同步或作为生成参数传递）。

```cpp
// Set PCG seed at runtime for deterministic variation
UPCGComponent* PCG = FindComponentByClass<UPCGComponent>();
PCG->Seed = MyDeterministicSeedValue;
PCG->Generate(); // Regenerate with new seed
```

### PCG数据类型

| 类型 | 包含内容 | 用途 |
|---|---|---|
| `FPCGPoint` / 点数据 | 位置、旋转、缩放、密度、颜色 | 散射放置、植被、实例定位 |
| `UPCGSplineData` | 样条点+切线 | 道路、河流、路径、边界定义 |
| `UPCGLandscapeData` | 高度+层权重采样 | 地形感知放置、生物群系查询 |
| `UPCGVolumeData` | 3D边界 | 基于体积的过滤与生成 |

点数据是最常用的类型——大多数PCG节点都会消费和生成点集合。此外还有`UPCGTextureData`、`UPCGPrimitiveData`、`UPCGDynamicMeshData`等类型。

PCG节点类型、设置字段和引脚标签详情请参考`references/pcg-node-reference.md`。

---

## 2. ProceduralMeshComponent

```csharp
// Build.cs
PublicDependencyModuleNames.Add("ProceduralMeshComponent");
```

### 核心API

```cpp
// Create section: vertices, triangles (CCW = front), normals, UVs, colors, tangents
void CreateMeshSection(int32 SectionIndex,
    const TArray<FVector>& Vertices, const TArray<int32>& Triangles,
    const TArray<FVector>& Normals,  const TArray<FVector2D>& UV0,
    const TArray<FColor>& VertexColors, const TArray<FProcMeshTangent>& Tangents,
    bool bCreateCollision);

// Updates vertex positions (incl. collision if enabled). Cannot change topology.
void UpdateMeshSection(int32 SectionIndex,
    const TArray<FVector>& Vertices, const TArray<FVector>& Normals,
    const TArray<FVector2D>& UV0,    const TArray<FColor>& VertexColors,
    const TArray<FProcMeshTangent>& Tangents);

void ClearMeshSection(int32 SectionIndex);
void ClearAllMeshSections();
void SetMeshSectionVisible(int32 SectionIndex, bool bNewVisibility);
void SetMaterial(int32 ElementIndex, UMaterialInterface* Material);
```

### 地形网格示例

```cpp
void ATerrainActor::Build(int32 Grid, float Cell)
{
    TArray<FVector> Verts; TArray<int32> Tris; TArray<FVector> Norms;
    TArray<FVector2D> UVs; TArray<FColor> Colors; TArray<FProcMeshTangent> Tangs;

    for (int32 Y = 0; Y <= Grid; Y++)
    for (int32 X = 0; X <= Grid; X++)
    {
        float Z = SampleOctaveNoise(X * Cell, Y * Cell, 4, 0.5f, 2.f, 80.f);
        Verts.Add(FVector(X * Cell, Y * Cell, Z));
        Norms.Add(FVector::UpVector);
        UVs.Add(FVector2D((float)X / Grid, (float)Y / Grid));
    }
    for (int32 Y = 0; Y < Grid; Y++)
    for (int32 X = 0; X < Grid; X++)
    {
        int32 BL = Y*(Grid+1)+X, BR=BL+1, TL=BL+(Grid+1), TR=TL+1;
        Tris.Add(BL); Tris.Add(TL); Tris.Add(TR);
        Tris.Add(BL); Tris.Add(TR); Tris.Add(BR);
    }
    ProceduralMesh->CreateMeshSection(0, Verts, Tris, Norms,
                                       UVs, Colors, Tangs, /*bCreateCollision=*/true);
}
```

### 性能注意事项

- 每次调用`CreateMeshSection`会产生一次绘制调用，每个Section的顶点数请保持在65K以下。
- `UpdateMeshSection`仅更新顶点位置和碰撞（若已启用），无法修改拓扑结构——若需添加新三角形，请调用`CreateMeshSection`。
- ProceduralMesh**不支持**Nanite。
- 顶点数据计算请在后台线程执行，仅在游戏线程调用`CreateMeshSection`。

### 异步网格生成

在后台线程生成顶点数据，然后在游戏线程应用：

```cpp
// Background task — compute vertices
class FMeshGenTask : public FNonAbandonableTask
{
public:
    TArray<FVector> Vertices;
    TArray<int32> Triangles;
    void DoWork() { /* Marching cubes, noise sampling, etc. */ }
    FORCEINLINE TStatId GetStatId() const { RETURN_QUICK_DECLARE_CYCLE_STAT(FMeshGenTask, STATGROUP_ThreadPoolAsyncTasks); }
};

// Launch and poll
// Use FAsyncTask (not FAutoDeleteAsyncTask) when polling IsDone() is needed.
// FAutoDeleteAsyncTask deletes itself on completion — calling IsDone() afterward is a use-after-free.
auto* Task = new FAsyncTask<FMeshGenTask>();
Task->StartBackgroundTask();
// Poll safely: if (Task->IsDone()) { /* use Task->GetTask().Vertices */ delete Task; }
```

### 程序化网格的碰撞设置

设置`UProceduralMeshComponent::bUseComplexAsSimpleCollision = true`可直接使用渲染三角形作为碰撞体，这种方式精度高但性能开销大——仅适用于静态几何体。对于动态或高多边形网格，请生成简化的凸包碰撞体。

---

## 3. 实例化静态网格（ISM / HISM）

| 特性 | ISM（`InstancedStaticMeshComponent.h`） | HISM（`HierarchicalInstancedStaticMeshComponent.h`） |
|---|---|---|
| 适用场景 | 少于1000个动态实例 | 超过1000个以静态为主的实例 |
| 剔除方式 | 仅基于距离 | 层次化BVH+距离 |
| LOD | GPU选择 | 内置过渡效果 |
| 删除开销 | O(n) | 异步BVH重建 |

### ISM核心API（来自`InstancedStaticMeshComponent.h`）

```cpp
virtual int32 AddInstance(const FTransform& T, bool bWorldSpace = false);
virtual TArray<int32> AddInstances(const TArray<FTransform>& Ts,
    bool bShouldReturnIndices, bool bWorldSpace = false, bool bUpdateNavigation = true);
virtual bool UpdateInstanceTransform(int32 Idx, const FTransform& NewT,
    bool bWorldSpace = false, bool bMarkRenderStateDirty = false, bool bTeleport = false);
virtual bool BatchUpdateInstancesTransforms(int32 StartIdx, const TArray<FTransform>& NewTs,
    bool bWorldSpace = false, bool bMarkRenderStateDirty = false, bool bTeleport = false);
bool GetInstanceTransform(int32 Idx, FTransform& OutT, bool bWorldSpace = false) const;
virtual bool RemoveInstance(int32 InstanceIndex);  // O(n) for ISM; triggers async BVH rebuild for HISM
virtual void PreAllocateInstancesMemory(int32 AddedCount);
int32 GetNumInstances() const;

// Per-instance custom float data (read in materials via PerInstanceCustomData)
virtual void SetNumCustomDataFloats(int32 N);
virtual bool SetCustomDataValue(int32 Idx, int32 DataIdx, float Value,
    bool bMarkRenderStateDirty = false);
virtual bool SetCustomData(int32 Idx, TArrayView<const float> Floats,
    bool bMarkRenderStateDirty = false);
```

剔除相关属性：`InstanceStartCullDistance`、`InstanceEndCullDistance`、`InstanceLODDistanceScale`、`bUseGpuLodSelection`。

### 植被散射示例（HISM + 地形射线检测）

```cpp
HISM->SetStaticMesh(TreeMesh);
HISM->SetNumCustomDataFloats(1);
HISM->PreAllocateInstancesMemory(Count);
FRandomStream Rand(Seed);
TArray<FTransform> Transforms; Transforms.Reserve(Count);

for (int32 i = 0; i < Count; i++)
{
    FVector Loc(Rand.FRandRange(Min.X, Max.X), Rand.FRandRange(Min.Y, Max.Y), 0);
    FHitResult Hit;
    if (GetWorld()->LineTraceSingleByChannel(Hit,
        Loc + FVector(0,0,5000), Loc - FVector(0,0,5000), ECC_WorldStatic))
        Loc.Z = Hit.Location.Z;

    Transforms.Add(FTransform(
        FRotator(0, Rand.FRandRange(0,360), 0), Loc,
        FVector(Rand.FRandRange(0.8f, 1.3f))));
}

TArray<int32> Indices = HISM->AddInstances(Transforms, true, true);
for (int32 i = 0; i < Indices.Num(); i++)
    HISM->SetCustomDataValue(Indices[i], 0, Rand.FRand(), false);
HISM->MarkRenderStateDirty();
```

### 植被系统

编辑器的植被绘制模式使用`AInstancedFoliageActor`，它内部封装了`UHierarchicalInstancedStaticMeshComponent`。若要大规模生成程序化植被，请使用`UProceduralFoliageComponent`搭配`UProceduralFoliageSpawner`——它通过模拟（物种竞争、耐阴性）来分布植被，而非手动绘制。

**单实例碰撞**：在ISM组件上启用`bUseDefaultCollision`，每个实例会继承静态网格的碰撞体。若需自定义单实例碰撞形状，请使用独立Actor——ISM不支持为每个实例设置独特的碰撞体。

**平台限制**：HISM的GPU缓冲区上限因平台而异（桌面平台约100万，移动平台约10万）。可使用`stat Foliage`命令监控。大型实例群请拆分到多个HISM组件中。

---

## 4. 噪声与数学计算

```cpp
// Built-in Perlin (all output in [-1, 1])
float N1 = FMath::PerlinNoise1D(X * Freq);
float N2 = FMath::PerlinNoise2D(FVector2D(X, Y) * Freq);
float N3 = FMath::PerlinNoise3D(FVector(X, Y, Z) * Freq);

// Octave noise
float OctaveNoise(float X, float Y, int32 Oct, float Persist, float Lacu, float Scale)
{
    float V=0, A=1, F=1.f/Scale, Max=0;
    for (int32 i=0; i<Oct; i++) {
        V += FMath::PerlinNoise2D(FVector2D(X,Y)*F) * A;
        Max += A; A *= Persist; F *= Lacu;
    }
    return V / Max;
}

// Seeded deterministic random
FRandomStream Stream(Seed);
float R = Stream.FRandRange(Min, Max);
int32 I = Stream.RandRange(MinI, MaxI);
FVector Dir = Stream.VRand();
```

**高度/密度图**：通过`FTexturePlatformData`采样`UTexture2D`的像素数据，用于驱动地形高度或实例放置密度。采样前需调用`BulkData.Lock(LOCK_READ_ONLY)`锁定数据，读取完成后解锁。

**泊松圆盘采样**（用于自然放置的最小间距散射）——完整的Bridson算法实现请参考`references/procedural-mesh-patterns.md`.

---

## 5. 样条曲线组件

### USplineComponent API（来自`SplineComponent.h`）

```cpp
// Build spline (always batch with bUpdateSpline=false, call UpdateSpline() once after)
void AddSplinePoint(const FVector& Pos, ESplineCoordinateSpace::Type Space, bool bUpdate=true);
void SetSplinePoints(const TArray<FVector>& Pts, ESplineCoordinateSpace::Type Space, bool bUpdate=true);
void ClearSplinePoints(bool bUpdate=true);
virtual void UpdateSpline();  // Rebuild reparameterization table

// Query by arc-length distance
FVector    GetLocationAtDistanceAlongSpline(float Dist, ESplineCoordinateSpace::Type Space) const;
FVector    GetDirectionAtDistanceAlongSpline(float Dist, ESplineCoordinateSpace::Type Space) const;
FVector    GetRightVectorAtDistanceAlongSpline(float Dist, ESplineCoordinateSpace::Type Space) const;
FRotator   GetRotationAtDistanceAlongSpline(float Dist, ESplineCoordinateSpace::Type Space) const;
FTransform GetTransformAtDistanceAlongSpline(float Dist, ESplineCoordinateSpace::Type Space, bool bUseScale=false) const;
float      GetSplineLength() const;

// Point editing
int32  GetNumberOfSplinePoints() const;
void   SetSplinePointType(int32 Idx, ESplinePointType::Type Type, bool bUpdate=true);
void   SetClosedLoop(bool bClosed, bool bUpdate=true);
void   SetTangentsAtSplinePoint(int32 Idx, const FVector& Arrive, const FVector& Leave,
           ESplineCoordinateSpace::Type Space, bool bUpdate=true);
```

点类型：`Linear`、`Curve`、`Constant`、`CurveClamped`、`CurveCustomTangent`。

`FindInputKeyClosestToWorldLocation(WorldLocation)`——返回距离指定世界位置最近的样条键，可用于将Actor吸附到样条曲线。

**运行时修改**：调用`AddSplinePoint()`、`RemoveSplinePoint()`或`SetLocationAtSplinePoint()`后，需调用`UpdateSpline()`重建样条。建议批量修改后再调用一次`UpdateSpline()`——每次调用都会重新计算整个样条的参数化表。

### 样条曲线实例放置示例

```cpp
// Place instances evenly along spline
float Len = Spline->GetSplineLength();
for (float D = 0.f; D <= Len; D += Spacing)
{
    FTransform T = Spline->GetTransformAtDistanceAlongSpline(D, ESplineCoordinateSpace::World);
    HISM->AddInstance(T, /*bWorldSpace=*/true);
}
```

### USplineMeshComponent（网格变形）

```cpp
#include "Components/SplineMeshComponent.h"
USplineMeshComponent* SM = NewObject<USplineMeshComponent>(this);
SM->SetStaticMesh(PipeMesh);
SM->RegisterComponent();

FVector SP, ST, EP, ET;
Spline->GetLocationAndTangentAtSplinePoint(Seg,   SP, ST, ESplineCoordinateSpace::Local);
Spline->GetLocationAndTangentAtSplinePoint(Seg+1, EP, ET, ESplineCoordinateSpace::Local);
SM->SetStartAndEnd(SP, ST, EP, ET, /*bUpdateMesh=*/true);
SM->SetForwardAxis(ESplineMeshAxis::X);
```

---

## 6. 运行时网格生成模式

完整实现请参考`references/procedural-mesh-patterns.md`：
- **Marching Cubes** — 从3D密度标量场生成等值面
- **地牢BSP** — 用BSP划分房间、L型走廊雕刻、瓦片转网格
- **L-系统** — 字符串重写+海龟解释器生成HISM分支
- **波函数坍缩** — 基于约束传播的瓦片网格布局
- **异步网格生成** — 后台线程计算顶点，游戏线程调用`CreateMeshSection`
- **样条道路挤出** — 沿`USplineComponent`扫掠截面轮廓

```cpp
// Marching Cubes result → ProceduralMesh
ProceduralMesh->CreateMeshSection(0, MarchVerts, MarchTris, MarchNormals,
    MarchUVs, {}, {}, /*bCreateCollision=*/true);
```

---

## 常见错误与反模式

**PCG**
- 在`Tick`中调用`GenerateLocal()`——生成操作开销较大，请使用`GenerateOnDemand`，仅在数据变化时重新生成。
- 在多人游戏中使用`GenerateLocal()`——该方法不会同步到其他客户端，请使用`Generate(bForce)`（NetMulticast）。
- 为资源消耗大的自定义节点设置`bIsCacheable = true`——仅当输出完全由输入和种子决定时才启用缓存。
- 设置`bIsEditorOnly = true`的PCG图无法在打包构建中正常工作。

**ProceduralMeshComponent**
- 调用`CreateMeshSection`时传入`bCreateCollision=false`——会导致角色穿过网格。
- 调用`UpdateMeshSection`期望修改拓扑结构——该方法仅能更新顶点位置，顶点数必须与原Section一致；若需添加新三角形，请调用`CreateMeshSection`。
- 使用ProceduralMesh生成Nanite规模的地形——不支持该场景，请使用Landscape或PCG+ISM。
- 三角形绕向错误（顺时针而非逆时针）——会导致多边形因背面剔除而不可见。

**ISM / HISM**
- 实例数超过500仍使用ISM——请切换到HISM以利用BVH剔除。
- 在循环中每次调用`UpdateInstanceTransform`都设置`bMarkRenderStateDirty=true`——仅在最后一次调用时设置为true即可。
- 批量添加实例前未调用`PreAllocateInstancesMemory`——反复重新分配内存会导致性能下降。

**样条曲线**
- 在循环中调用`AddSplinePoint(bUpdateSpline=true)`——每次调用都会重建参数化表，请设置为`false`，循环结束后调用一次`UpdateSpline()`。
- 使用样条输入键而非距离来均匀放置实例——输入键与弧长不成正比。

**多人游戏**
- 程序化内容必须是确定性的（相同种子生成相同结果）或由服务器权威控制。
- `GenerateLocal()`不会同步到其他客户端，请使用`Generate(bool)`（NetMulticast, Reliable）。

---

## 相关技能

- `ue-actor-component-architecture` — 组件生命周期、注册、同步
- `ue-physics-collision` — 碰撞配置文件、程序化几何体的复杂/简单碰撞
- `ue-cpp-foundations` — `NewObject`、`TSubclassOf`、`TArray`、内存管理

## 参考文件

- `references/pcg-node-reference.md` — 所有PCG节点类型、引脚标签、设置字段、确定性检查清单
- `references/procedural-mesh-patterns.md` — 四边形网格、Marching Cubes、地牢BSP、L-系统、波函数坍缩、样条道路
