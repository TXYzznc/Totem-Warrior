using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
public sealed class TotemGameRuntime : MonoBehaviour
{
    public const string RuntimeObjectName = "[TotemGameRuntime]";

    private readonly List<ITotemRuntimeService> services = new List<ITotemRuntimeService>(20);

    public static TotemGameRuntime Instance { get; private set; }

    public bool Started { get; private set; }
    public bool ServicesReady { get; private set; }
    public string CurrentProcedure { get; private set; } = string.Empty;
    public string LastTransitionUtc { get; private set; } = string.Empty;

    public static TotemGameRuntime EnsureCreated()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindObjectOfType<TotemGameRuntime>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var go = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(go);
        return go.AddComponent<TotemGameRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        GFTrace.Success("TotemRuntime", "Awake", null, GFTrace.Data("object", name));
    }

    void OnDestroy()
    {
        ShutdownRuntime();
    }

    public void ShutdownRuntime()
    {
        for (int i = services.Count - 1; i >= 0; i--)
        {
            services[i].Shutdown();
        }

        services.Clear();
        ServicesReady = false;
        Started = false;
        CurrentProcedure = string.Empty;
        LastTransitionUtc = string.Empty;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!ServicesReady)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i] is ITotemRuntimeTickService tickService)
            {
                tickService.Tick(deltaTime);
            }
        }
    }

    void LateUpdate()
    {
        if (!ServicesReady)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i] is ITotemRuntimeLateTickService lateTickService)
            {
                lateTickService.LateTick(deltaTime);
            }
        }
    }

    public void MarkProcedureEntered(string procedureName)
    {
        Started = true;
        CurrentProcedure = string.IsNullOrWhiteSpace(procedureName) ? string.Empty : procedureName;
        LastTransitionUtc = DateTime.UtcNow.ToString("O");

        GFTrace.Success("TotemRuntime", "ProcedureEntered", null, GFTrace.Data(
            "procedure", CurrentProcedure,
            "transitionUtc", LastTransitionUtc));
    }

    public void StartRuntime()
    {
        if (ServicesReady)
        {
            GF.Log($"[TotemStartup] Runtime already ready. services={services.Count}, procedure={CurrentProcedure}.");
            return;
        }

        RegisterDefaultServices();
        GF.Log($"[TotemStartup] Initializing {services.Count} runtime services. procedure={CurrentProcedure}.");
        int readyCount = 0;
        for (int i = 0; i < services.Count; i++)
        {
            services[i].Initialize(this);
            GF.Log($"[TotemStartup] Service initialized. name={services[i].ServiceName}, state={services[i].State}.");
            if (services[i].State == TotemRuntimeServiceState.Ready)
            {
                readyCount++;
            }
        }

        ServicesReady = readyCount == services.Count;
        GFTrace.Success("TotemRuntime", "Services.Ready", null, GFTrace.Data(
            "readyCount", readyCount.ToString(),
            "serviceCount", services.Count.ToString()));
        GF.Log($"[TotemStartup] Runtime initialization complete. ready={readyCount}/{services.Count}, allReady={ServicesReady}.");
    }

    public void RegisterService(ITotemRuntimeService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        for (int i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceName == service.ServiceName)
            {
                throw new InvalidOperationException($"Totem runtime service already registered: {service.ServiceName}");
            }
        }

        services.Add(service);
        GFTrace.Info("TotemRuntime", "Service.Register", null, GFTrace.Data("service", service.ServiceName));
    }

    public bool TryGetService<TService>(out TService service) where TService : class, ITotemRuntimeService
    {
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i] is TService typedService)
            {
                service = typedService;
                return true;
            }
        }

        service = null;
        return false;
    }

    public TService GetService<TService>() where TService : class, ITotemRuntimeService
    {
        return TryGetService<TService>(out var service) ? service : null;
    }

    public void MarkProcedureLeaving(string procedureName, bool isShutdown)
    {
        GFTrace.Info("TotemRuntime", "ProcedureLeaving", null, GFTrace.Data(
            "procedure", procedureName,
            "isShutdown", isShutdown.ToString()));
    }

    public TotemGameRuntimeSnapshot CaptureSnapshot()
    {
        var serviceStatuses = new TotemRuntimeServiceStatus[services.Count];
        int readyCount = 0;
        int failedCount = 0;
        for (int i = 0; i < services.Count; i++)
        {
            serviceStatuses[i] = services[i].CaptureStatus();
            if (services[i].State == TotemRuntimeServiceState.Ready)
            {
                readyCount++;
            }
            else if (services[i].State == TotemRuntimeServiceState.Failed)
            {
                failedCount++;
            }
        }

        return new TotemGameRuntimeSnapshot
        {
            started = Started,
            servicesReady = ServicesReady,
            currentProcedure = CurrentProcedure,
            lastTransitionUtc = LastTransitionUtc,
            runtimeObjectName = name,
            serviceCount = services.Count,
            readyServiceCount = readyCount,
            failedServiceCount = failedCount,
            services = serviceStatuses,
        };
    }

    private void RegisterDefaultServices()
    {
        if (services.Count > 0)
        {
            return;
        }

        RegisterService(new TotemGameFlowService());
        RegisterService(new TotemMatchClockService());
        RegisterService(new TotemInputService());
        RegisterService(new TotemDataService());
        RegisterService(new TotemAssetService());
        RegisterService(new TotemSettingsService());
        RegisterService(new TotemAudioService());
        RegisterService(new TotemRunStatsService());
        RegisterService(new TotemMetaProgressService());
        RegisterService(new TotemMapService());
        RegisterService(new TotemCombatRelationshipService());
        RegisterService(new TotemActorService());
        RegisterService(new TotemParticipantReadinessService());
        RegisterService(new TotemEconomyService());
        RegisterService(new TotemStatusService());
        RegisterService(new TotemTattooService());
        RegisterService(new TotemWeaponService());
        RegisterService(new TotemChestService());
        RegisterService(new TotemSkillService());
        RegisterService(new TotemZoneService());
        RegisterService(new TotemAIService());
        RegisterService(new TotemNpcService());
        RegisterService(new TotemChoiceService());
        RegisterService(new TotemInteractionService());
        RegisterService(new TotemCameraService());
        RegisterService(new TotemVfxService());
        RegisterService(new TotemEnemyWorldService());
        RegisterService(new TotemEnemyService());
        RegisterService(new TotemEnemyLootService());
        RegisterService(new TotemCombatService());
        RegisterService(new TotemUIService());
    }
}

[Serializable]
public sealed class TotemGameRuntimeSnapshot
{
    public bool started;
    public bool servicesReady;
    public string currentProcedure;
    public string lastTransitionUtc;
    public string runtimeObjectName;
    public int serviceCount;
    public int readyServiceCount;
    public int failedServiceCount;
    public TotemRuntimeServiceStatus[] services;
}
