
using UnityEngine;
using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System.Collections.Generic;
using GameFramework;
using System;
using GameFramework.Resource;
using Cysharp.Threading.Tasks;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class PreloadProcedure : ProcedureBase
{
    private int totalProgress;
    private int loadedProgress;
    private float smoothProgress;
    private bool preloadAllCompleted;
    private float progressSmoothSpeed = 10f;
    private int m_DataTablesCount;
    private bool preloadFailed;
    private string preloadFailureMessage;
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GFTrace.Info("Procedure", "Preload.Enter");
        GF.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
        GF.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
        GF.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
        GF.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDicSuccess);
        GF.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDicFailure);
        GF.BuiltinView.ShowLoadingProgress();
        GF.Log("进入HybridCLR热更流程! 预加载游戏数据...");

        InitAppSettings();
        PreloadAndInitData();
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GF.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
        GF.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
        GF.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
        GF.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDicSuccess);
        GF.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDicFailure);
        base.OnLeave(procedureOwner, isShutdown);
    }


    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (preloadFailed || totalProgress <= 0 || preloadAllCompleted) return;

        smoothProgress = Mathf.Lerp(smoothProgress, loadedProgress / (float)totalProgress, elapseSeconds * progressSmoothSpeed);

        GF.BuiltinView.SetLoadingProgress(smoothProgress);
        //预加载完成 切换场景
        if (loadedProgress >= totalProgress && smoothProgress >= 0.99f)
        {
            preloadAllCompleted = true;
            try
            {
                InitGameFrameworkSettings();
            }
            catch (Exception exception)
            {
                FailPreload("GameFramework.Settings.Init", exception.ToString());
                return;
            }

            GF.Log("预加载完成, 进入干净工作区流程.");
            GFTrace.Success("Procedure", "Preload.Completed", null, GFTrace.Data("loadedProgress", loadedProgress.ToString(), "totalProgress", totalProgress.ToString()));
            ChangeState<WorkspaceProcedure>(procedureOwner);
        }
    }
    private void InitAppSettings()
    {
        //if (string.IsNullOrWhiteSpace(GF.Setting.GetABTestGroup()))
        //{
        //    GF.Setting.SetABTestGroup("B");//设置A/B测试组; 应由服务器分配该新用户所属测试组
        //}
    }
    /// <summary>
    /// 预加载完成之后需要处理的事情
    /// </summary>
    private void InitGameFrameworkSettings()
    {
        //初始化EntityGroup
        var entityGroupTb = GF.DataTable.GetDataTable<EntityGroupTable>();
        if (entityGroupTb == null)
        {
            throw new GameFrameworkException("EntityGroupTable is not loaded.");
        }

        foreach (var tb in entityGroupTb.GetAllDataRows())
        {
            if (GF.Entity.HasEntityGroup(tb.Name))
            {
                var group = GF.Entity.GetEntityGroup(tb.Name);
                group.InstanceAutoReleaseInterval = tb.ReleaseInterval;
                group.InstanceCapacity = tb.Capacity;
                group.InstanceExpireTime = tb.ExpireTime;
                group.InstancePriority = tb.Priority;
                continue;
            }
            GF.Entity.AddEntityGroup(tb.Name, tb.ReleaseInterval, tb.Capacity, tb.ExpireTime, tb.Priority);
        }
        Dictionary<string, SoundGroupTable> defaultSoundGroupData = new Dictionary<string, SoundGroupTable>();
        //初始化SoundGroup
        var soundGroupTb = GF.DataTable.GetDataTable<SoundGroupTable>();
        if (soundGroupTb == null)
        {
            throw new GameFrameworkException("SoundGroupTable is not loaded.");
        }

        foreach (var tb in soundGroupTb.GetAllDataRows())
        {
            if (!defaultSoundGroupData.ContainsKey(tb.Name))
            {
                defaultSoundGroupData.Add(tb.Name, tb);
            }
            if (GF.Sound.HasSoundGroup(tb.Name))
            {
                var group = GF.Sound.GetSoundGroup(tb.Name);
                group.AvoidBeingReplacedBySamePriority = tb.AvoidBeingReplacedBySamePriority;
                group.Mute = tb.Mute;
                group.Volume = tb.Volume;
                continue;
            }
            GF.Sound.AddSoundGroup(tb.Name, tb.AvoidBeingReplacedBySamePriority, tb.Mute, tb.Volume, tb.SoundAgentCount);
        }
        //初始化UIGroup
        var uiGroupTb = GF.DataTable.GetDataTable<UIGroupTable>();
        if (uiGroupTb == null)
        {
            throw new GameFrameworkException("UIGroupTable is not loaded.");
        }

        foreach (var tb in uiGroupTb.GetAllDataRows())
        {
            if (GF.UI.HasUIGroup(tb.Name))
            {
                var group = GF.UI.GetUIGroup(tb.Name);
                group.Depth = tb.Depth;
                continue;
            }
            GF.UI.AddUIGroup(tb.Name, tb.Depth);
        }


        //初始化音效
        var musicGroup = GetRequiredSoundGroup(defaultSoundGroupData, Const.SoundGroup.Music);
        var soundGroup = GetRequiredSoundGroup(defaultSoundGroupData, Const.SoundGroup.Sound);
        var vibrateGroup = GetRequiredSoundGroup(defaultSoundGroupData, Const.SoundGroup.Vibrate);
        var joystickGroup = GetRequiredSoundGroup(defaultSoundGroupData, Const.SoundGroup.Joystick);

        GF.Setting.SetMediaMute(Const.SoundGroup.Music, GF.Setting.GetMediaMute(Const.SoundGroup.Music, musicGroup.Mute));
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, GF.Setting.GetMediaMute(Const.SoundGroup.Sound, soundGroup.Mute));
        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate, vibrateGroup.Mute));
        GF.Setting.SetMediaMute(Const.SoundGroup.Joystick, GF.Setting.GetMediaMute(Const.SoundGroup.Joystick, joystickGroup.Mute));

        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, GF.Setting.GetMediaVolume(Const.SoundGroup.Music, musicGroup.Volume));
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, GF.Setting.GetMediaVolume(Const.SoundGroup.Sound, soundGroup.Volume));
    }

    private SoundGroupTable GetRequiredSoundGroup(Dictionary<string, SoundGroupTable> soundGroups, Const.SoundGroup group)
    {
        string name = group.ToString();
        if (soundGroups == null || !soundGroups.TryGetValue(name, out var row))
        {
            throw new GameFrameworkException($"SoundGroupTable must contain '{name}'.");
        }

        return row;
    }
    /// <summary>
    /// 预加载数据表、游戏配置,以及初始化游戏数据
    /// </summary>
    private async void PreloadAndInitData()
    {
        try
        {
            GFTrace.Info("Preload", "InitData.Begin");
            preloadAllCompleted = false;
            preloadFailed = false;
            preloadFailureMessage = null;
            smoothProgress = 0;
            totalProgress = 0;
            loadedProgress = 0;
            m_DataTablesCount = -1;
            var appConfig = await AppConfigs.GetInstanceSync();
            if (appConfig == null)
            {
                throw new GameFrameworkException("AppConfigs.GetInstanceSync returned null.");
            }

            int dataTableCount = appConfig.DataTables == null ? 0 : appConfig.DataTables.Length;
            int configCount = appConfig.Configs == null ? 0 : appConfig.Configs.Length;
            int languageCount = appConfig.Languages == null ? 0 : appConfig.Languages.Length;
            totalProgress = dataTableCount + configCount + 2;//2是加载多语言和创建框架扩展
            GFTrace.Info("Preload", "AppConfigs.Loaded", null, GFTrace.Data("dataTables", dataTableCount.ToString(), "configs", configCount.ToString(), "languages", languageCount.ToString(), "totalProgress", totalProgress.ToString()));
            CreateGFExtension();
        }
        catch (Exception exception)
        {
            FailPreload("AppConfigs.Load", exception.ToString());
        }
    }
    private async void LoadConfigsAndDataTables()
    {
        if (preloadFailed)
        {
            return;
        }

        try
        {
            var appConfig = await AppConfigs.GetInstanceSync();
            if (appConfig == null)
            {
                throw new GameFrameworkException("AppConfigs.GetInstanceSync returned null.");
            }

            var configs = appConfig.Configs ?? Array.Empty<string>();
            var dataTables = appConfig.DataTables ?? Array.Empty<string>();
            m_DataTablesCount = dataTables.Length;
            GFTrace.Info("Preload", "LoadConfigsAndDataTables.Begin", null, GFTrace.Data("dataTables", dataTables.Length.ToString(), "configs", configs.Length.ToString()));
            foreach (var item in configs)
            {
                GFTrace.Info("Config", "Load.Begin", null, GFTrace.Data("name", item));
                GF.Config.LoadConfig(item, appConfig.LoadFromBytes, this);
            }
            foreach (var item in dataTables)
            {
                GFTrace.Info("DataTable", "Load.Begin", null, GFTrace.Data("name", item));
                GF.DataTable.LoadDataTable(item, appConfig.LoadFromBytes, this);
            }
            if (m_DataTablesCount == 0)
            {
                InitAndLoadLanguage();
            }
        }
        catch (Exception exception)
        {
            FailPreload("ConfigDataTable.Load", exception.ToString());
        }
    }
    private void CreateGFExtension()
    {
        GFTrace.Info("Resource", "GFExtension.Load.Begin", null, GFTrace.Data("asset", UtilityBuiltin.AssetsPath.GetPrefab("Core/GFExtension")));
        GF.Resource.LoadAsset(UtilityBuiltin.AssetsPath.GetPrefab("Core/GFExtension"), typeof(GameObject), new GameFramework.Resource.LoadAssetCallbacks(OnLoadGFExtensionSuccess, OnLoadGFExtensionFailed));
    }

    private async void InitAndLoadLanguage()
    {
        if (preloadFailed)
        {
            return;
        }

        try
        {
            //初始化语言
            GameFramework.Localization.Language language = GF.Setting.GetLanguage();
            if (language == GameFramework.Localization.Language.Unspecified)
            {
#if UNITY_EDITOR
                language = GF.Base.EditorLanguage;
#else
                language = GFBuiltin.Localization.SystemLanguage;//默认语言跟随用户操作系统语言
#endif
            }
            var languageName = language.ToString();
            var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
            if (langTb == null)
            {
                throw new GameFrameworkException("LanguagesTable is not loaded.");
            }

            var langRow = langTb.GetDataRow(row => row.LanguageKey == languageName);
            if (langRow == null)
            {
                langRow = langTb.MinIdDataRow;
                if (langRow == null || string.IsNullOrWhiteSpace(langRow.LanguageKey))
                {
                    throw new GameFrameworkException($"LanguagesTable has no fallback row for '{languageName}'.");
                }

                if (!Enum.TryParse(langRow.LanguageKey, out language))
                {
                    throw new GameFrameworkException($"LanguagesTable fallback key '{langRow.LanguageKey}' is not a valid language.");
                }

                GFTrace.Warning("Localization", "Language.Fallback", null, GFTrace.Data("requested", languageName, "fallback", langRow.LanguageKey));
            }
            GF.Setting.SetLanguage(language, false);
            GF.Log(Utility.Text.Format("初始化游戏设置. 游戏语言:{0},系统语言:{1}", language, GFBuiltin.Localization.SystemLanguage));
            var appConfig = await AppConfigs.GetInstanceSync();
            if (appConfig == null)
            {
                throw new GameFrameworkException("AppConfigs.GetInstanceSync returned null.");
            }

            GFTrace.Info("Localization", "Language.Load.Begin", null, GFTrace.Data("asset", langRow.AssetName, "language", language.ToString()));
            GF.Localization.LoadLanguage(langRow.AssetName, appConfig.LoadFromBytes, this);
        }
        catch (Exception exception)
        {
            FailPreload("Localization.Init", exception.ToString());
        }
    }

    private void OnLoadGFExtensionSuccess(string assetName, object asset, float duration, object userData)
    {
        if (preloadFailed)
        {
            return;
        }

        var gfExtPfb = asset as GameObject;
        if (gfExtPfb == null)
        {
            FailPreload("GFExtension.Load", $"Loaded asset is not a GameObject: {assetName}.", GFTrace.Data("asset", assetName));
            return;
        }

        if (null != GameObject.Instantiate(gfExtPfb, Vector3.zero, Quaternion.identity, GF.Base.transform))
        {
            GF.Log("GF框架扩展成功!");
            GFTrace.Success("Resource", "GFExtension.Load.Success", null, GFTrace.Data("asset", assetName, "duration", duration.ToString("F3")));
            loadedProgress++;
            LoadConfigsAndDataTables();
        }
        else
        {
            FailPreload("GFExtension.Instantiate", $"Failed to instantiate {assetName}.", GFTrace.Data("asset", assetName));
        }
    }
    private void OnLoadGFExtensionFailed(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        GF.LogError(Utility.Text.Format("GF框架扩展加载失败:{0}, Error:{1}", assetName, errorMessage));
        GFTrace.Failure("Resource", "GFExtension.Load.Failure", errorMessage, GFTrace.Data("asset", assetName, "status", status.ToString()));
        FailPreload("GFExtension.Load", errorMessage, GFTrace.Data("asset", assetName, "status", status.ToString()));
    }
    private void OnLoadDicSuccess(object sender, GameEventArgs e)
    {
        LoadDictionarySuccessEventArgs args = e as LoadDictionarySuccessEventArgs;
        if (args.UserData != this) return;
        if (preloadFailed) return;
        loadedProgress++;
        Log.Info("Load Language Success:{0}", args.DictionaryAssetName);
        GFTrace.Success("Localization", "Language.Load.Success", null, GFTrace.Data("asset", args.DictionaryAssetName));

    }
    /// <summary>
    /// 加载配置成功回调
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadConfigSuccess(object sender, GameEventArgs e)
    {
        var args = e as LoadConfigSuccessEventArgs;
        if (args.UserData != this) return;
        if (preloadFailed) return;
        loadedProgress++;
        Log.Info("Load Config Success:{0}", args.ConfigAssetName);
        GFTrace.Success("Config", "Load.Success", null, GFTrace.Data("asset", args.ConfigAssetName));
    }

    private void OnLoadDataTableSuccess(object sender, GameEventArgs e)
    {
        var args = e as LoadDataTableSuccessEventArgs;
        if (args.UserData != this) return;
        if (preloadFailed) return;
        loadedProgress++;
        m_DataTablesCount--;
        Log.Info("Load DataTable Success:{0}", args.DataTableAssetName);
        GFTrace.Success("DataTable", "Load.Success", null, GFTrace.Data("asset", args.DataTableAssetName, "remaining", m_DataTablesCount.ToString()));
        if (m_DataTablesCount == 0)
        {
            InitAndLoadLanguage();
        }
    }

    private void OnLoadDicFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadDictionaryFailureEventArgs;
        if (args.UserData != this) return;

        GF.LogError($"Load Dictionary Failed:{args.ErrorMessage}");
        GFTrace.Failure("Localization", "Language.Load.Failure", args.ErrorMessage);
        FailPreload("Localization.Load", args.ErrorMessage, GFTrace.Data("asset", args.DictionaryAssetName));
    }

    private void OnLoadDataTableFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadDataTableFailureEventArgs;
        if (args.UserData != this) return;

        GF.LogError($"Load DataTable Failed:{args.ErrorMessage}");
        GFTrace.Failure("DataTable", "Load.Failure", args.ErrorMessage, GFTrace.Data("asset", args.DataTableAssetName));
        FailPreload("DataTable.Load", args.ErrorMessage, GFTrace.Data("asset", args.DataTableAssetName));
    }

    private void OnLoadConfigFailure(object sender, GameEventArgs e)
    {
        var args = e as LoadConfigFailureEventArgs;
        if (args.UserData != this) return;

        GF.LogError($"Load Config Failed:{args.ErrorMessage}");
        GFTrace.Failure("Config", "Load.Failure", args.ErrorMessage, GFTrace.Data("asset", args.ConfigAssetName));
        FailPreload("Config.Load", args.ErrorMessage, GFTrace.Data("asset", args.ConfigAssetName));
    }

    private void FailPreload(string stage, string message, Dictionary<string, string> data = null)
    {
        if (preloadFailed)
        {
            return;
        }

        preloadFailed = true;
        preloadAllCompleted = false;
        preloadFailureMessage = Utility.Text.Format("{0}: {1}", stage, string.IsNullOrWhiteSpace(message) ? "Unknown startup failure." : message);
        GF.LogError(preloadFailureMessage);

        data ??= new Dictionary<string, string>(StringComparer.Ordinal);
        data["stage"] = stage;
        data["loadedProgress"] = loadedProgress.ToString();
        data["totalProgress"] = totalProgress.ToString();
        GFTrace.Failure("Preload", "Startup.Failure", preloadFailureMessage, data);

        if (GF.BuiltinView != null)
        {
            GF.BuiltinView.SetLoadingProgress(smoothProgress);
            GF.BuiltinView.ShowDialog("Startup Failed", preloadFailureMessage, "OK");
        }
    }
}
