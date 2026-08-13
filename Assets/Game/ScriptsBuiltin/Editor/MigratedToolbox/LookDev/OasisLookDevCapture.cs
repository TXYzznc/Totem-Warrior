using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class OasisLookDevCapture
{
    internal static readonly string[] CameraNames =
    {
        "CAM_Overview_SouthEast",
        "CAM_District_Central",
        "CAM_Building_Tower_BF01",
        "CAM_River_Bridge03",
    };

    [Serializable]
    private sealed class CaptureManifest
    {
        public string generatedUtc;
        public string bakeTier;
        public int width;
        public int height;
        public List<CaptureEntry> captures = new();
    }

    [Serializable]
    private sealed class CaptureEntry
    {
        public string lookId;
        public string camera;
        public string file;
        public string volumeProfileGuid;
        public string lightingDataGuid;
        public bool valid;
    }

    internal static string CaptureMatrix(OasisLookDevCatalog catalog, OasisLookDevSession session, OasisBakeTier tier)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (session == null) throw new ArgumentNullException(nameof(session));

        Dictionary<string, Camera> cameras = Resources.FindObjectsOfTypeAll<Camera>()
            .Where(CameraGroupPanel.IsLoadedSceneCamera)
            .Where(camera => CameraNames.Contains(camera.name, StringComparer.Ordinal))
            .ToDictionary(camera => camera.name, StringComparer.Ordinal);
        string[] missing = CameraNames.Where(name => !cameras.ContainsKey(name)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("缺少对比相机：" + string.Join(", ", missing));

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("无法解析项目根目录。");
        string output = Path.Combine(projectRoot, "openspec", "changes", "add-oasis-city-lookdev-lighting", "art", "renders", tier.ToString().ToLowerInvariant());
        Directory.CreateDirectory(output);
        CaptureManifest manifest = new()
        {
            generatedUtc = DateTime.UtcNow.ToString("O"),
            bakeTier = tier.ToString(),
            width = 2560,
            height = 1440,
        };

        try
        {
            foreach (string id in OasisLookDevCatalog.RequiredIds)
            {
                OasisLookPreset preset = catalog.Find(id);
                if (preset == null || preset.GetLightingData(tier) == null)
                    throw new InvalidOperationException($"{id} 尚无有效 {tier} LightingDataAsset，不能生成完整对比组。");
                if (!session.Apply(preset, tier, out string error))
                    throw new InvalidOperationException(error);

                foreach (string cameraName in CameraNames)
                {
                    string fileName = $"{id}_{cameraName}_2560x1440.png";
                    Capture(cameras[cameraName], Path.Combine(output, fileName), 2560, 1440);
                    manifest.captures.Add(new CaptureEntry
                    {
                        lookId = id,
                        camera = cameraName,
                        file = fileName,
                        volumeProfileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset.VolumeProfile)),
                        lightingDataGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset.GetLightingData(tier))),
                        valid = true,
                    });
                }
            }
        }
        finally
        {
            session.Restore();
        }

        File.WriteAllText(Path.Combine(output, "comparison-manifest.json"), JsonUtility.ToJson(manifest, true));
        File.WriteAllText(Path.Combine(output, "README.md"), BuildIndex(manifest));
        return output;
    }

    private static void Capture(Camera camera, string path, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D texture = new(width, height, TextureFormat.RGB24, false, false);
        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static string BuildIndex(CaptureManifest manifest)
    {
        List<string> lines = new()
        {
            "# OasisCity LookDev 对比",
            string.Empty,
            $"- 烘焙层级：{manifest.bakeTier}",
            $"- 分辨率：{manifest.width}x{manifest.height}",
            $"- 生成时间（UTC）：{manifest.generatedUtc}",
            string.Empty,
            "| 风格 | 相机 | 文件 | 校验 |",
            "|---|---|---|---|",
        };
        lines.AddRange(manifest.captures.Select(item => $"| {item.lookId} | {item.camera} | [{item.file}]({item.file}) | {(item.valid ? "通过" : "失败")} |"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
