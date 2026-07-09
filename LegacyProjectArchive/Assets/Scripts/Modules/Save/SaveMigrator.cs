using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 存档版本迁移器。
/// 规则：
///   每次 CURRENT_VERSION 升级必须在 _migrations 添加对应迁移函数；
///   旧迁移函数永远不删；
///   迁移函数只增不改（已上线玩家存档必须能正确迁移）。
/// </summary>
public static class SaveMigrator
{
    // ── 迁移函数表（version -> 从该版本迁移到 version+1 的函数）────
    static readonly Dictionary<int, Func<JObject, JObject>> _migrations = new()
    {
        // v1 → v2：补全 v2.1 新增的字段（PatternUnlocks / UnlockedDecorations 等）
        [1] = v1 =>
        {
            if (v1["PatternUnlocks"] == null)
                v1["PatternUnlocks"] = new JObject();
            if (v1["UnlockedDecorations"] == null)
                v1["UnlockedDecorations"] = new JArray();
            if (v1["UnlockedTitles"] == null)
                v1["UnlockedTitles"] = new JArray();
            if (v1["UnlockedGallery"] == null)
                v1["UnlockedGallery"] = new JArray();
            if (v1["CompletedAchievements"] == null)
                v1["CompletedAchievements"] = new JArray();
            if (v1["TattooRecipeProgress"] == null)
                v1["TattooRecipeProgress"] = new JObject();
            if (v1["Settings"] == null)
                v1["Settings"] = JObject.FromObject(new SettingsData());
            if (v1["LastModifiedUtc"] == null)
                v1["LastModifiedUtc"] = string.Empty;
            if (v1["DeviceId"] == null)
                v1["DeviceId"] = string.Empty;
            if (v1["TotalPlayTime"] == null)
                v1["TotalPlayTime"] = 0f;
            v1["Version"] = 2;
            return v1;
        },
        // 未来：[2] = v2 => { ... }
    };

    static readonly JsonSerializerSettings _jsonSettings = new()
    {
        TypeNameHandling           = TypeNameHandling.None,
        NullValueHandling          = NullValueHandling.Ignore,
        MissingMemberHandling      = MissingMemberHandling.Ignore,
    };

    /// <summary>
    /// 从 JSON 字符串反序列化并自动迁移到当前版本。
    /// 若 JSON 为空或解析失败，返回默认 SaveData（新档）。
    /// </summary>
    public static SaveData Load(string json)
    {
        if (string.IsNullOrEmpty(json))
            return CreateDefault();

        JObject jo;
        try
        {
            jo = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            FrameworkLogger.Warn("SaveMigrator",
                $"Action=ParseFailed Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            return CreateDefault();
        }

        int version = jo["Version"]?.Value<int>() ?? 0;

        // 逐版本迁移
        while (version < SaveData.CURRENT_VERSION)
        {
            if (_migrations.TryGetValue(version, out var migrate))
            {
                jo = migrate(jo);
                FrameworkLogger.Info("SaveMigrator",
                    $"Action=Migrated FromVersion={version} ToVersion={version + 1}");
            }
            else
            {
                FrameworkLogger.Warn("SaveMigrator",
                    $"Action=MigrateSkipped MissingMigration FromVersion={version}");
            }
            version++;
        }

        try
        {
            return jo.ToObject<SaveData>(JsonSerializer.CreateDefault(_jsonSettings))
                   ?? CreateDefault();
        }
        catch (Exception ex)
        {
            FrameworkLogger.Warn("SaveMigrator",
                $"Action=DeserializeFailed Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            return CreateDefault();
        }
    }

    /// <summary>将 SaveData 序列化为 JSON 字符串。</summary>
    public static string Serialize(SaveData data)
    {
        return JsonConvert.SerializeObject(data, _jsonSettings);
    }

    /// <summary>创建默认存档（新玩家 / 存档损坏兜底）。</summary>
    public static SaveData CreateDefault()
    {
        var data = new SaveData();
        // MVP：第一个角色槽默认解锁
        data.CharacterSlots[0] = true;
        data.DeviceId          = UnityEngine.SystemInfo.deviceUniqueIdentifier;
        return data;
    }
}
