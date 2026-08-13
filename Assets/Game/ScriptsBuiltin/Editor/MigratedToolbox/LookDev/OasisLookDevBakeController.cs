using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[FilePath("Library/OasisLookDev/BakeState.asset", FilePathAttribute.Location.ProjectFolder)]
internal sealed class OasisLookDevBakeState : ScriptableSingleton<OasisLookDevBakeState>
{
    [SerializeField] internal bool running;
    [SerializeField] internal bool cancelled;
    [SerializeField] internal OasisBakeTier tier;
    [SerializeField] internal int currentIndex;
    [SerializeField] internal List<string> completedIds = new();
    [SerializeField] internal string status = "空闲";

    internal void Persist() => Save(true);
}

internal sealed class OasisLookDevBakeController
{
    private readonly OasisLookDevSession session;
    private OasisLookDevCatalog catalog;
    private bool subscribed;

    internal OasisLookDevBakeController(OasisLookDevSession session) => this.session = session;

    internal bool IsRunning => OasisLookDevBakeState.instance.running;
    internal string Status => OasisLookDevBakeState.instance.status;
    internal float Progress
    {
        get
        {
            OasisLookDevBakeState state = OasisLookDevBakeState.instance;
            int total = OasisLookDevCatalog.RequiredIds.Length;
            return total == 0 ? 0f : Mathf.Clamp01((state.completedIds.Count + (Lightmapping.isRunning ? 0.5f : 0f)) / total);
        }
    }

    internal void Start(OasisLookDevCatalog targetCatalog, OasisBakeTier tier)
    {
        if (targetCatalog == null)
            throw new ArgumentNullException(nameof(targetCatalog));
        if (tier == OasisBakeTier.Final && !targetCatalog.PreviewApproved)
            throw new InvalidOperationException("最终烘焙被预览审批闸门阻止。请先完成并批准三套预览对比。");
        if (Lightmapping.isRunning)
            throw new InvalidOperationException("Unity 当前已有光照烘焙任务。" );

        catalog = targetCatalog;
        OasisLookDevBakeState state = OasisLookDevBakeState.instance;
        state.running = true;
        state.cancelled = false;
        state.tier = tier;
        state.currentIndex = 0;
        state.completedIds.Clear();
        state.status = $"准备 {tier} 烘焙";
        state.Persist();
        Subscribe();
        StartCurrent();
    }

    internal void Resume(OasisLookDevCatalog targetCatalog)
    {
        OasisLookDevBakeState state = OasisLookDevBakeState.instance;
        if (state.running || Lightmapping.isRunning)
            return;
        catalog = targetCatalog;
        state.running = true;
        state.cancelled = false;
        state.status = "恢复烘焙";
        state.Persist();
        Subscribe();
        StartCurrent();
    }

    internal void Cancel()
    {
        OasisLookDevBakeState state = OasisLookDevBakeState.instance;
        state.cancelled = true;
        state.running = false;
        state.status = "已取消，可从首个未完成风格恢复";
        state.Persist();
        if (Lightmapping.isRunning)
            Lightmapping.Cancel();
        Unsubscribe();
    }

    internal void Shutdown()
    {
        if (Lightmapping.isRunning && IsRunning)
            Cancel();
        else
            Unsubscribe();
    }

    private void StartCurrent()
    {
        OasisLookDevBakeState state = OasisLookDevBakeState.instance;
        while (state.currentIndex < OasisLookDevCatalog.RequiredIds.Length &&
               state.completedIds.Contains(OasisLookDevCatalog.RequiredIds[state.currentIndex]))
            state.currentIndex++;

        if (state.currentIndex >= OasisLookDevCatalog.RequiredIds.Length)
        {
            state.running = false;
            state.status = $"{state.tier} 三套烘焙完成";
            state.Persist();
            Unsubscribe();
            session.Restore();
            return;
        }

        string id = OasisLookDevCatalog.RequiredIds[state.currentIndex];
        OasisLookPreset preset = catalog.Find(id);
        if (!session.Apply(preset, state.tier, out string error))
        {
            Fail(error);
            return;
        }

        LightingSettings settings = preset.GetLightingSettings(state.tier);
        if (settings == null)
        {
            Fail($"{id} 缺少 {state.tier} LightingSettings。");
            return;
        }

        Lightmapping.lightingSettings = settings;
        Lightmapping.lightingDataAsset = null;
        state.status = $"正在烘焙 {id}（{state.tier}）";
        state.Persist();
        if (!TryStartBakeWithFallback(settings, out string backend))
        {
            Fail($"Unity 未能用 Progressive GPU 或 CPU 启动 {id} 的异步烘焙。");
            return;
        }
        state.status = $"正在烘焙 {id}（{state.tier} / {backend}）";
        state.Persist();
    }

    private static bool TryStartBakeWithFallback(LightingSettings settings, out string backend)
    {
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        EditorUtility.SetDirty(settings);
        if (Lightmapping.BakeAsync())
        {
            backend = "Progressive GPU";
            return true;
        }

        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
        EditorUtility.SetDirty(settings);
        if (Lightmapping.BakeAsync())
        {
            backend = "Progressive CPU（GPU 回退）";
            return true;
        }
        backend = "不可用";
        return false;
    }

    private void OnBakeCompleted()
    {
        OasisLookDevBakeState state = OasisLookDevBakeState.instance;
        if (!state.running || state.cancelled)
            return;
        string id = OasisLookDevCatalog.RequiredIds[state.currentIndex];
        OasisLookPreset preset = catalog.Find(id);
        LightingDataAsset data = Lightmapping.lightingDataAsset;
        if (data == null)
        {
            Fail($"{id} 烘焙完成但没有生成 LightingDataAsset。");
            return;
        }

        string destination = GetBakeDirectory(id, state.tier);
        try
        {
            MoveGeneratedBakeAssets(data, destination);
        }
        catch (Exception exception)
        {
            Fail($"{id} 烘焙资产归档失败：{exception.Message}");
            return;
        }
        data = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(destination + "/LightingData.asset");
        if (data == null)
        {
            Fail($"无法在确定目录中重新加载 {id} LightingDataAsset。");
            return;
        }
        preset.SetLightingData(state.tier, data);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        state.completedIds.Add(id);
        state.currentIndex++;
        state.Persist();
        StartCurrent();
    }

    private static void MoveGeneratedBakeAssets(LightingDataAsset data, string destination)
    {
        EnsureFolder(destination);
        string dataPath = AssetDatabase.GetAssetPath(data);
        string sourceDirectory = Path.GetDirectoryName(dataPath)?.Replace('\\', '/');
        HashSet<string> dependencies = AssetDatabase.GetDependencies(dataPath, true).ToHashSet(StringComparer.Ordinal);
        dependencies.Add(dataPath);
        foreach (string source in dependencies.OrderBy(path => path == dataPath ? 1 : 0))
        {
            if (!source.StartsWith(sourceDirectory + "/", StringComparison.Ordinal) || source.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                continue;
            string fileName = source == dataPath ? "LightingData.asset" : Path.GetFileName(source);
            string target = destination + "/" + fileName;
            if (string.Equals(source, target, StringComparison.Ordinal))
                continue;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target) != null)
                throw new IOException($"保留资产已存在，拒绝覆盖：{target}");
            string error = AssetDatabase.MoveAsset(source, target);
            if (!string.IsNullOrEmpty(error))
                throw new IOException($"移动烘焙资产失败：{source} -> {target}: {error}");
        }
        AssetDatabase.SaveAssets();
    }

    private static string GetBakeDirectory(string id, OasisBakeTier tier) =>
        $"{OasisLookDevCatalog.AssetRoot}/Bakes/{id}/{tier}";

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    private void Fail(string message)
    {
        OasisLookDevBakeState state = OasisLookDevBakeState.instance;
        state.running = false;
        state.status = "失败：" + message;
        state.Persist();
        Unsubscribe();
        session.Restore();
        Debug.LogError("[OasisLookDev] " + message);
    }

    private void Subscribe()
    {
        if (subscribed) return;
        Lightmapping.bakeCompleted += OnBakeCompleted;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        Lightmapping.bakeCompleted -= OnBakeCompleted;
        subscribed = false;
    }
}
