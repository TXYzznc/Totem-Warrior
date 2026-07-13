using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using GameFramework.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class LoadHotfixDllProcedure : ProcedureBase
{
    private List<string> hotfixDlls;
    private bool hotfixListIsLoaded;
    private int totalProgress;
    private int loadedProgress;

#if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void SetUpStaticSecretKey()
    {
        Obfuz.EncryptionService<Obfuz.DefaultStaticEncryptionScope>.Encryptor = new Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey").bytes);
    }
#endif

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GFTrace.Info("Procedure", "LoadHotfixDll.Enter");
#if ENABLE_HYBRIDCLR
        GFBuiltin.Event.Subscribe(LoadHotfixDllEventArgs.EventId, OnLoadHotfixDllCallback);
#endif
        PreloadAndInitData();
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
#if ENABLE_HYBRIDCLR
        GFBuiltin.Event.Unsubscribe(LoadHotfixDllEventArgs.EventId, OnLoadHotfixDllCallback);
#endif
        base.OnLeave(procedureOwner, isShutdown);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (!hotfixListIsLoaded)
        {
            return;
        }

        if (loadedProgress >= totalProgress)
        {
            loadedProgress = -1;
            var entryFunc = Utility.Assembly.GetType("HotfixEntry")?.GetMethod("StartHotfixLogic", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (entryFunc == null)
            {
                Log.Fatal("Game startup failed, HotfixEntry.StartHotfixLogic was not found.");
                GFTrace.Failure("Procedure", "LoadHotfixDll.EntryMissing", "HotfixEntry.StartHotfixLogic not found.");
                return;
            }

#if ENABLE_HYBRIDCLR
            GFTrace.Success("Procedure", "LoadHotfixDll.EntryInvoke", null, GFTrace.Data("enableHybridCLR", "True"));
            entryFunc.Invoke(null, new object[] { true });
#else
            GFTrace.Success("Procedure", "LoadHotfixDll.EntryInvoke", null, GFTrace.Data("enableHybridCLR", "False"));
            entryFunc.Invoke(null, new object[] { false });
#endif
        }
    }

    private void PreloadAndInitData()
    {
        GFTrace.Info("Procedure", "LoadHotfixDll.Preload.Begin");
        GFBuiltin.BuiltinView.ShowLoadingProgress();
        totalProgress = 0;
        loadedProgress = 0;
        hotfixListIsLoaded = true;

#if ENABLE_HYBRIDCLR
        if (!GFBuiltin.Base.EditorResourceMode)
        {
            hotfixListIsLoaded = false;
            LoadAotDlls();
            LoadHotfixDlls();
        }
#endif
    }

#if ENABLE_HYBRIDCLR
    private void LoadAotDlls()
    {
        var aotMetaDlls = Resources.LoadAll<TextAsset>(ConstBuiltin.AOT_DLL_DIR);
        totalProgress += aotMetaDlls.Length;
        LoadMetadata(aotMetaDlls);
    }

    private void LoadMetadata(TextAsset[] aotMetaDlls)
    {
        var encryptDllList = AppSettings.Instance.EncryptAOTDlls;
        var encryptCode = Encoding.UTF8.GetBytes(ConstBuiltin.AOT_DLLS_KEY);
        foreach (var dll in aotMetaDlls)
        {
            var dllBytes = dll.bytes;
            if (encryptDllList != null && encryptDllList.Contains(dll.name))
            {
                dllBytes = Utility.Encryption.GetQuickXorBytes(dllBytes, encryptCode);
            }

            var resultCode = LoadMetadataForAOT(dllBytes);
            GFBuiltin.Log(Utility.Text.Format("Load AOT metadata:{0}. ret:{1}", dll.name, resultCode));
            GFTrace.Record("Hotfix", "AOT.LoadMetadata", resultCode == HybridCLR.LoadImageErrorCode.OK ? GFTrace.ResultSuccess : GFTrace.ResultFailure, null, GFTrace.Data("dll", dll.name, "result", resultCode.ToString()));
            if (resultCode == HybridCLR.LoadImageErrorCode.OK)
            {
                loadedProgress++;
            }
        }
    }

    private void LoadHotfixDlls()
    {
        GFBuiltin.Log("Start load hotfix dlls...");
        GFTrace.Info("Hotfix", "DllList.Load.Begin");
        var hotfixListFile = UtilityBuiltin.AssetsPath.GetCombinePath("Assets", ConstBuiltin.HOT_FIX_DLL_DIR, "HotfixFileList.txt");
        if (GFBuiltin.Resource.HasAsset(hotfixListFile) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Fatal("HotfixFileList Not Exist :{0}", hotfixListFile);
            GFTrace.Failure("Hotfix", "DllList.Missing", null, GFTrace.Data("asset", hotfixListFile));
            return;
        }

        GFBuiltin.Resource.LoadAsset(hotfixListFile, new LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            var textAsset = asset as TextAsset;
            if (textAsset == null)
            {
                GFTrace.Failure("Hotfix", "DllList.InvalidAsset", null, GFTrace.Data("asset", assetName));
                return;
            }

            hotfixListIsLoaded = true;
            hotfixDlls = UtilityBuiltin.Json.ToObject<List<string>>(textAsset.text);
            totalProgress += hotfixDlls.Count;
            GFTrace.Success("Hotfix", "DllList.Load.Success", null, GFTrace.Data("asset", assetName, "count", hotfixDlls.Count.ToString()));
            if (hotfixDlls.Count == 1)
            {
                var mainDll = UtilityBuiltin.AssetsPath.GetHotfixDll(hotfixDlls.Last());
                LoadHotfixDll(mainDll, this);
            }
            else
            {
                for (int i = 0; i < hotfixDlls.Count - 1; i++)
                {
                    var dllAsset = UtilityBuiltin.AssetsPath.GetHotfixDll(hotfixDlls[i]);
                    LoadHotfixDll(dllAsset, this);
                }
            }
        }));
    }

    private void OnLoadHotfixDllCallback(object sender, GameEventArgs e)
    {
        var args = e as LoadHotfixDllEventArgs;
        if (args.UserData != this)
        {
            return;
        }

        if (args.Assembly == null)
        {
            GFBuiltin.LogError($"Load dll failed:{args.DllName}");
            GFTrace.Failure("Hotfix", "Dll.AssemblyMissing", null, GFTrace.Data("dllName", args.DllName));
            return;
        }

        loadedProgress++;
        if (totalProgress > 0)
        {
            GFBuiltin.BuiltinView.SetLoadingProgress(loadedProgress / (float)totalProgress);
        }

        if (hotfixDlls.Contains(args.DllName))
        {
            hotfixDlls.Remove(args.DllName);
            if (hotfixDlls.Count == 1)
            {
                var mainDll = UtilityBuiltin.AssetsPath.GetHotfixDll(hotfixDlls.Last());
                LoadHotfixDll(mainDll, this);
            }
        }
    }

    public void LoadHotfixDll(string dllAssetName, object userData)
    {
        GFTrace.Info("Hotfix", "Dll.Load.Begin", null, GFTrace.Data("asset", dllAssetName));
        GFBuiltin.Resource.LoadAsset(dllAssetName, typeof(TextAsset), new LoadAssetCallbacks(OnLoadDllSuccess, OnLoadDllFail), userData);
    }

    private void OnLoadDllFail(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        Log.Error("Load {0} failed! Error:{1}", assetName, errorMessage);
        GFTrace.Failure("Hotfix", "Dll.Load.Failure", errorMessage, GFTrace.Data("asset", assetName, "status", status.ToString()));
        GFBuiltin.Event.Fire(this, ReferencePool.Acquire<LoadHotfixDllEventArgs>().Fill(Path.GetFileNameWithoutExtension(assetName), null, userData));
    }

    private void OnLoadDllSuccess(string assetName, object asset, float duration, object userData)
    {
        var dllTextAsset = asset as TextAsset;
        System.Reflection.Assembly dllAssembly = null;
        if (dllTextAsset != null)
        {
            try
            {
                dllAssembly = System.Reflection.Assembly.Load(dllTextAsset.bytes);
            }
            catch (Exception e)
            {
                Log.Error("Assembly.Load hotfix dll failed:{0}, Error:{1}", assetName, e.Message);
                GFTrace.Exception("Hotfix", "Dll.AssemblyLoad.Exception", e, GFTrace.Data("asset", assetName));
                throw;
            }
        }

        var dllName = Path.GetFileNameWithoutExtension(assetName);
        GFTrace.Success("Hotfix", "Dll.Load.Success", null, GFTrace.Data("asset", assetName, "dllName", dllName));
        GFBuiltin.Event.Fire(this, ReferencePool.Acquire<LoadHotfixDllEventArgs>().Fill(dllName, dllAssembly, userData));
    }

    private HybridCLR.LoadImageErrorCode LoadMetadataForAOT(byte[] dllBytes)
    {
        return HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);
    }
#endif
}
