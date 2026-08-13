using System.Collections.Generic;

public sealed class TotemUIService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private const int OverlayBaseSortOrder = 200;

    private readonly List<int> overlayFormIds = new List<int>(4);
    private int currentFormId = -1;
    private int exclusiveOpenRequestCount;
    private int overlayOpenRequestCount;
    private int overlayCloseRequestCount;
    private UIViews lastExclusiveView;
    private UIViews lastOverlayView;
    private bool lastExclusiveSucceeded;
    private bool lastOverlaySucceeded;
    private bool lastOverlayAllowEscape;
    private int lastOverlaySortOrder;
    private TotemInputService inputService;
    private TotemGameFlowService flowService;

    public override string ServiceName => "UI";

    public TotemRunResultSnapshot ActiveRunResult { get; private set; }

    public int LastLocalMatchSeed { get; private set; } = 1;

    public bool LastLocalMatchFastMode { get; private set; }

    public string LastResultEvidenceFile { get; private set; } = string.Empty;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        inputService = runtime.GetService<TotemInputService>();
        flowService = runtime.GetService<TotemGameFlowService>();
        OpenMainMenu();
    }

    protected override void OnShutdown()
    {
        CloseOverlays(clearData: true);
        CloseCurrent();
        inputService = null;
        flowService = null;
    }

    public void Tick(float deltaTime)
    {
        var input = inputService?.Current ?? TotemInputSnapshot.Empty;
        if (input.escapePressed)
        {
            if (CanUseGFUI() && GF.UI.TryCloseTopEscapeUIForm())
            {
                overlayCloseRequestCount++;
                return;
            }

            if (flowService?.CurrentState == TotemGameFlowState.CombatHud)
            {
                OpenPauseMenu();
                return;
            }
        }

        // The legacy self-tattoo overlay allowed mutation during combat. The
        // first playable owns tattoo mutation through the build-phase service;
        // this input is intentionally ignored until the new construction UI is bound.
    }

    public int OpenMainMenu()
    {
        Runtime.GetService<TotemGameFlowService>()?.EnterMainMenu();
        return OpenExclusive(UIViews.MainMenu);
    }

    public int OpenCombatHud()
    {
        Runtime.GetService<TotemActorService>()?.BeginPlayerStartupProtection("UI.OpenCombatHud");
        Runtime.GetService<TotemGameFlowService>()?.EnterCombatHud();
        return OpenExclusive(UIViews.CombatHUD);
    }

    public bool StartLocalFirstPlayable()
    {
        return StartLocalFirstPlayable(1, false);
    }

    public bool RestartLocalFirstPlayable()
    {
        return StartLocalFirstPlayable(LastLocalMatchSeed, LastLocalMatchFastMode);
    }

    public bool StartLocalFirstPlayable(int seed, bool useFastMode)
    {
        TotemGameFlowService gameFlow = Runtime.GetService<TotemGameFlowService>();
        if (gameFlow == null)
        {
            GFTrace.Failure("TotemUI", "LocalMatch.StartRejected", "Game flow service is unavailable.");
            return false;
        }

        LastLocalMatchSeed = seed;
        LastLocalMatchFastMode = useFastMode;
        Runtime.GetService<TotemMatchFlowService>()?.Configure(new TotemMatchTimingConfig(), useFastMode);
        Runtime.GetService<TotemMapService>()?.RequestNextCombatMap(seed, 1);
        Runtime.GetService<TotemActorService>()?.BeginPlayerStartupProtection("UI.LocalMatchConfirmed");
        gameFlow.ConfirmLocalFirstPlayable();
        GFTrace.Success("TotemUI", "LocalMatch.Confirmed", null, GFTrace.Data(
            "formId", "UI-FP-MATCH-001",
            "participants", TotemFirstPlayableRules.ParticipantCount.ToString(),
            "teams", TotemFirstPlayableRules.TeamCount.ToString(),
            "bots", TotemFirstPlayableRules.BotCount.ToString(),
            "seed", seed.ToString(),
            "fastMode", useFastMode.ToString()));
        return true;
    }

    public void CloseCurrent()
    {
        CloseOverlays(clearData: true);
        if (currentFormId <= 0)
        {
            return;
        }

        if (CanUseGFUI() && GF.UI.HasUIForm(currentFormId))
        {
            GF.UI.CloseUIForm(currentFormId);
        }

        currentFormId = -1;
    }

    public int OpenPauseMenu()
    {
        CloseOverlays(clearData: true);
        return OpenOverlay(UIViews.PauseMenu, closeExisting: false);
    }

    public int OpenSettings()
    {
        return OpenOverlay(UIViews.Settings, closeExisting: false);
    }

    public int OpenRunResult(TotemRunResultSnapshot result)
    {
        CloseOverlays(clearData: true);
        ActiveRunResult = result;
        TotemFirstPlayableResultEvidence evidence = TotemFirstPlayableResultEvidenceWriter.Build(Runtime, this, result);
        if (!TotemFirstPlayableResultEvidenceWriter.TryWrite(null, evidence, out string evidenceFile, out string evidenceError))
        {
            LastResultEvidenceFile = string.Empty;
            GFTrace.Failure("TotemUI", "ResultEvidence.WriteFailed", evidenceError);
        }
        else
        {
            LastResultEvidenceFile = evidenceFile;
            GFTrace.Success("TotemUI", "ResultEvidence.Written", null, GFTrace.Data(
                "file", evidenceFile,
                "seed", evidence.seed.ToString(),
                "participants", evidence.participants.Length.ToString()));
        }

        return OpenOverlay(UIViews.RunResult, closeExisting: false, allowEscape: false);
    }

    public TotemUISnapshot CaptureSnapshot()
    {
        return new TotemUISnapshot
        {
            canUseGFUI = CanUseGFUI(),
            currentFormId = currentFormId,
            overlayFormCount = overlayFormIds.Count,
            exclusiveOpenRequestCount = exclusiveOpenRequestCount,
            overlayOpenRequestCount = overlayOpenRequestCount,
            overlayCloseRequestCount = overlayCloseRequestCount,
            lastExclusiveView = FormatView(lastExclusiveView),
            lastOverlayView = FormatView(lastOverlayView),
            lastExclusiveSucceeded = lastExclusiveSucceeded,
            lastOverlaySucceeded = lastOverlaySucceeded,
            lastOverlayAllowEscape = lastOverlayAllowEscape,
            lastOverlaySortOrder = lastOverlaySortOrder,
            hasActiveRunResult = ActiveRunResult != null,
        };
    }

    public void ForgetOverlay(int serialId)
    {
        overlayFormIds.Remove(serialId);
        if (overlayFormIds.Count == 0)
        {
            ClearOverlayData();
        }
    }

    private int OpenExclusive(UIViews view)
    {
        CloseCurrent();
        exclusiveOpenRequestCount++;
        lastExclusiveView = view;
        lastExclusiveSucceeded = false;
        if (!CanUseGFUI())
        {
            currentFormId = -1;
            GFTrace.Warning("TotemUI", "OpenExclusive.Headless", null, GFTrace.Data("view", view.ToString()));
            return -1;
        }

        int serialId = GF.UI.OpenUIForm(view);
        currentFormId = serialId;
        lastExclusiveSucceeded = serialId > 0;
        GFTrace.Success("TotemUI", "OpenExclusive", null, GFTrace.Data(
            "view", view.ToString(),
            "serialId", serialId.ToString()));
        return serialId;
    }

    private int OpenOverlay(UIViews view, bool closeExisting, bool allowEscape = true)
    {
        if (closeExisting)
        {
            CloseOverlays(clearData: true);
        }

        overlayOpenRequestCount++;
        lastOverlayView = view;
        lastOverlayAllowEscape = allowEscape;
        lastOverlaySortOrder = OverlayBaseSortOrder + overlayFormIds.Count * 10;
        lastOverlaySucceeded = false;
        if (!CanUseGFUI())
        {
            GFTrace.Warning("TotemUI", "OpenOverlay.Headless", null, GFTrace.Data("view", view.ToString()));
            return -1;
        }

        int sortOrder = lastOverlaySortOrder;
        int serialId = GF.UI.OpenUIForm(view, UIParams.Create(allowEscape, sortOrder));
        if (serialId > 0)
        {
            overlayFormIds.Add(serialId);
        }

        lastOverlaySucceeded = serialId > 0;
        GFTrace.Success("TotemUI", "OpenOverlay", null, GFTrace.Data(
            "view", view.ToString(),
            "serialId", serialId.ToString(),
            "sortOrder", sortOrder.ToString()));
        return serialId;
    }

    private void CloseOverlays(bool clearData)
    {
        if (overlayFormIds.Count > 0)
        {
            overlayCloseRequestCount++;
        }

        for (int i = overlayFormIds.Count - 1; i >= 0; i--)
        {
            int serialId = overlayFormIds[i];
            if (serialId > 0 && CanUseGFUI() && GF.UI.HasUIForm(serialId))
            {
                GF.UI.CloseUIForm(serialId);
            }
        }

        overlayFormIds.Clear();
        if (clearData)
        {
            ClearOverlayData();
        }
    }

    private void ClearOverlayData()
    {
        ActiveRunResult = null;
    }

    private static bool CanUseGFUI()
    {
        return GF.UI != null && GF.DataTable != null;
    }

    private static string FormatView(UIViews view)
    {
        return view == 0 ? "None" : view.ToString();
    }
}
