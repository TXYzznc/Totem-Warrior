using System;
using System.Collections.Generic;
using GameFramework;
using GameFramework.DataTable;
using GameFramework.Entity;
using GameFramework.Procedure;
using GameFramework.Sound;
using GameFramework.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class GFDiagnosticSnapshot
{
    public int schemaVersion = 1;
    public string createdAtUtc;
    public string reason;
    public string unityVersion;
    public string platform;
    public bool isPlaying;
    public int frame;
    public float realtimeSinceStartup;
    public string currentProcedure;
    public float currentProcedureTime;
    public string resourceMode;
    public bool editorResourceMode;
    public List<string> loadedScenes = new List<string>();
    public List<GFDataTableSnapshot> dataTables = new List<GFDataTableSnapshot>();
    public List<GFUIGroupSnapshot> uiGroups = new List<GFUIGroupSnapshot>();
    public List<GFEntityGroupSnapshot> entityGroups = new List<GFEntityGroupSnapshot>();
    public List<GFSoundGroupSnapshot> soundGroups = new List<GFSoundGroupSnapshot>();
    public List<string> warnings = new List<string>();
    public List<GFTraceEvent> recentEvents = new List<GFTraceEvent>();

    public static GFDiagnosticSnapshot Capture(string reason = null, int maxTraceEvents = 120)
    {
        var snapshot = new GFDiagnosticSnapshot
        {
            createdAtUtc = DateTime.UtcNow.ToString("O"),
            reason = reason,
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            isPlaying = Application.isPlaying,
            frame = Application.isPlaying ? Time.frameCount : 0,
            realtimeSinceStartup = Application.isPlaying ? Time.realtimeSinceStartup : 0f,
            recentEvents = GFTrace.GetRecentEvents(maxTraceEvents),
        };

        snapshot.CaptureScenes();
        snapshot.CaptureProcedure();
        snapshot.CaptureResource();
        snapshot.CaptureDataTables();
        snapshot.CaptureUI();
        snapshot.CaptureEntities();
        snapshot.CaptureSounds();
        return snapshot;
    }

    private void CaptureScenes()
    {
        try
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                loadedScenes.Add($"{scene.name}|loaded={scene.isLoaded}|path={scene.path}");
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"CaptureScenes failed: {exception.Message}");
        }
    }

    private void CaptureProcedure()
    {
        try
        {
            if (!Application.isPlaying)
            {
                currentProcedure = "EditorNotPlaying";
                return;
            }

            var procedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
            if (procedureManager == null)
            {
                currentProcedure = "NoProcedureManager";
                return;
            }

            var procedure = procedureManager.CurrentProcedure;
            currentProcedure = procedure == null ? "None" : procedure.GetType().Name;
            currentProcedureTime = procedureManager.CurrentProcedureTime;
        }
        catch (Exception exception)
        {
            currentProcedure = "Unavailable";
            warnings.Add($"CaptureProcedure failed: {exception.Message}");
        }
    }

    private void CaptureResource()
    {
        try
        {
            if (GFBuiltin.Resource != null)
            {
                resourceMode = GFBuiltin.Resource.ResourceMode.ToString();
            }

            if (GFBuiltin.Base != null)
            {
                editorResourceMode = GFBuiltin.Base.EditorResourceMode;
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"CaptureResource failed: {exception.Message}");
        }
    }

    private void CaptureDataTables()
    {
        try
        {
            if (GFBuiltin.DataTable == null)
            {
                return;
            }

            DataTableBase[] tables = GFBuiltin.DataTable.GetAllDataTables();
            foreach (var table in tables)
            {
                dataTables.Add(new GFDataTableSnapshot
                {
                    fullName = table.FullName,
                    name = table.Name,
                    rowType = table.Type == null ? string.Empty : table.Type.Name,
                    rowCount = table.Count,
                });
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"CaptureDataTables failed: {exception.Message}");
        }
    }

    private void CaptureUI()
    {
        try
        {
            if (GFBuiltin.UI == null)
            {
                return;
            }

            IUIGroup[] groups = GFBuiltin.UI.GetAllUIGroups();
            foreach (var group in groups)
            {
                var groupSnapshot = new GFUIGroupSnapshot
                {
                    name = group.Name,
                    depth = group.Depth,
                    pause = group.Pause,
                    formCount = group.UIFormCount,
                    currentForm = group.CurrentUIForm == null ? string.Empty : group.CurrentUIForm.UIFormAssetName,
                };

                foreach (var form in group.GetAllUIForms())
                {
                    groupSnapshot.forms.Add($"{form.SerialId}:{form.UIFormAssetName}:depth={form.DepthInUIGroup}");
                }

                uiGroups.Add(groupSnapshot);
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"CaptureUI failed: {exception.Message}");
        }
    }

    private void CaptureEntities()
    {
        try
        {
            if (GFBuiltin.Entity == null)
            {
                return;
            }

            IEntityGroup[] groups = GFBuiltin.Entity.GetAllEntityGroups();
            foreach (var group in groups)
            {
                var groupSnapshot = new GFEntityGroupSnapshot
                {
                    name = group.Name,
                    entityCount = group.EntityCount,
                    capacity = group.InstanceCapacity,
                    expireTime = group.InstanceExpireTime,
                    autoReleaseInterval = group.InstanceAutoReleaseInterval,
                    priority = group.InstancePriority,
                };

                foreach (var entity in group.GetAllEntities())
                {
                    groupSnapshot.entities.Add($"{entity.Id}:{entity.EntityAssetName}");
                }

                entityGroups.Add(groupSnapshot);
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"CaptureEntities failed: {exception.Message}");
        }
    }

    private void CaptureSounds()
    {
        try
        {
            if (GFBuiltin.Sound == null)
            {
                return;
            }

            ISoundGroup[] groups = GFBuiltin.Sound.GetAllSoundGroups();
            foreach (var group in groups)
            {
                soundGroups.Add(new GFSoundGroupSnapshot
                {
                    name = group.Name,
                    agentCount = group.SoundAgentCount,
                    mute = group.Mute,
                    volume = group.Volume,
                    avoidSamePriorityReplace = group.AvoidBeingReplacedBySamePriority,
                });
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"CaptureSounds failed: {exception.Message}");
        }
    }
}

[Serializable]
public sealed class GFDataTableSnapshot
{
    public string fullName;
    public string name;
    public string rowType;
    public int rowCount;
}

[Serializable]
public sealed class GFUIGroupSnapshot
{
    public string name;
    public int depth;
    public bool pause;
    public int formCount;
    public string currentForm;
    public List<string> forms = new List<string>();
}

[Serializable]
public sealed class GFEntityGroupSnapshot
{
    public string name;
    public int entityCount;
    public int capacity;
    public float expireTime;
    public float autoReleaseInterval;
    public int priority;
    public List<string> entities = new List<string>();
}

[Serializable]
public sealed class GFSoundGroupSnapshot
{
    public string name;
    public int agentCount;
    public bool mute;
    public float volume;
    public bool avoidSamePriorityReplace;
}
