using System.Collections.Generic;

public sealed class TotemUIService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private const int OverlayBaseSortOrder = 200;

    private readonly List<int> overlayFormIds = new List<int>(4);
    private int currentFormId = -1;
    private int selfTattooFormId = -1;
    private int exclusiveOpenRequestCount;
    private int overlayOpenRequestCount;
    private int overlayCloseRequestCount;
    private int selfTattooToggleRequestCount;
    private UIViews lastExclusiveView;
    private UIViews lastOverlayView;
    private bool lastExclusiveSucceeded;
    private bool lastOverlaySucceeded;
    private bool lastOverlayAllowEscape;
    private int lastOverlaySortOrder;
    private TotemInputService inputService;
    private TotemGameFlowService flowService;
    private TotemChoiceService choiceService;

    public override string ServiceName => "UI";

    public TotemNpcModel ActiveShopNpc { get; private set; }

    public TotemNpcModel ActiveTattooNpc { get; private set; }

    public TotemChoiceSnapshot ActiveChoice { get; private set; }

    public TotemRunResultSnapshot ActiveRunResult { get; private set; }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        inputService = runtime.GetService<TotemInputService>();
        flowService = runtime.GetService<TotemGameFlowService>();
        choiceService = runtime.GetService<TotemChoiceService>();
        OpenMainMenu();
    }

    protected override void OnShutdown()
    {
        CloseOverlays(clearData: true);
        CloseCurrent();
        inputService = null;
        flowService = null;
        choiceService = null;
    }

    public void Tick(float deltaTime)
    {
        var input = inputService?.Current ?? TotemInputSnapshot.Empty;
        if (input.escapePressed)
        {
            if (CanUseGFUI() && GF.UI.TryCloseTopEscapeUIForm())
            {
                return;
            }

            if (flowService?.CurrentState == TotemGameFlowState.CombatHud)
            {
                OpenPauseMenu();
                return;
            }
        }

        if (input.selfTattooTogglePressed && flowService?.CurrentState == TotemGameFlowState.CombatHud)
        {
            ToggleSelfTattoo();
        }
    }

    public int OpenMainMenu()
    {
        Runtime.GetService<TotemGameFlowService>()?.EnterMainMenu();
        return OpenExclusive(UIViews.MainMenu);
    }

    public int OpenCharacterSelect()
    {
        Runtime.GetService<TotemGameFlowService>()?.EnterCharacterSelect();
        return OpenExclusive(UIViews.CharacterSelect);
    }

    public int OpenStartupSelect()
    {
        Runtime.GetService<TotemGameFlowService>()?.EnterStartupSelect();
        return OpenExclusive(UIViews.StartupSelect);
    }

    public int OpenCombatHud()
    {
        Runtime.GetService<TotemActorService>()?.BeginPlayerStartupProtection("UI.OpenCombatHud");
        Runtime.GetService<TotemGameFlowService>()?.EnterCombatHud();
        return OpenExclusive(UIViews.CombatHUD);
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

    public int OpenShop(TotemNpcModel npc)
    {
        CloseOverlays(clearData: true);
        ActiveShopNpc = npc;
        ActiveTattooNpc = null;
        ActiveChoice = null;
        return OpenOverlay(UIViews.Shop, closeExisting: false);
    }

    public int OpenTattooStudio(TotemNpcModel npc, TotemChoiceSnapshot choice)
    {
        CloseOverlays(clearData: true, closeChoice: false);
        ActiveShopNpc = null;
        ActiveTattooNpc = npc;
        ActiveChoice = choice;
        return OpenOverlay(UIViews.TattooStudio, closeExisting: false);
    }

    public int OpenThreeChoice(TotemChoiceSnapshot choice)
    {
        ActiveChoice = choice;
        return OpenOverlay(UIViews.ThreeChoice, closeExisting: false);
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
        return OpenOverlay(UIViews.RunResult, closeExisting: false, allowEscape: false);
    }

    public int OpenTattooEnchant()
    {
        return OpenOverlay(UIViews.TattooEnchant, closeExisting: false);
    }

    public int ToggleSelfTattoo()
    {
        selfTattooToggleRequestCount++;
        if (selfTattooFormId > 0 && CanUseGFUI() && GF.UI.HasUIForm(selfTattooFormId))
        {
            GF.UI.CloseUIForm(selfTattooFormId);
            selfTattooFormId = -1;
            return -1;
        }

        CloseOverlays(clearData: true);
        selfTattooFormId = OpenOverlay(UIViews.SelfTattoo, closeExisting: false);
        return selfTattooFormId;
    }

    public TotemUISnapshot CaptureSnapshot()
    {
        return new TotemUISnapshot
        {
            canUseGFUI = CanUseGFUI(),
            currentFormId = currentFormId,
            overlayFormCount = overlayFormIds.Count,
            selfTattooFormId = selfTattooFormId,
            selfTattooOverlayTracked = selfTattooFormId > 0 && overlayFormIds.Contains(selfTattooFormId),
            exclusiveOpenRequestCount = exclusiveOpenRequestCount,
            overlayOpenRequestCount = overlayOpenRequestCount,
            overlayCloseRequestCount = overlayCloseRequestCount,
            selfTattooToggleRequestCount = selfTattooToggleRequestCount,
            lastExclusiveView = FormatView(lastExclusiveView),
            lastOverlayView = FormatView(lastOverlayView),
            lastExclusiveSucceeded = lastExclusiveSucceeded,
            lastOverlaySucceeded = lastOverlaySucceeded,
            lastOverlayAllowEscape = lastOverlayAllowEscape,
            lastOverlaySortOrder = lastOverlaySortOrder,
            hasActiveShopNpc = ActiveShopNpc != null,
            hasActiveTattooNpc = ActiveTattooNpc != null,
            hasActiveChoice = ActiveChoice != null,
            hasActiveRunResult = ActiveRunResult != null,
        };
    }

    public void ForgetOverlay(int serialId)
    {
        overlayFormIds.Remove(serialId);
        if (serialId == selfTattooFormId)
        {
            selfTattooFormId = -1;
        }

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

    private void CloseOverlays(bool clearData, bool closeChoice = true)
    {
        if (overlayFormIds.Count > 0 || selfTattooFormId > 0)
        {
            overlayCloseRequestCount++;
        }

        if (closeChoice)
        {
            choiceService?.CloseCurrentChoice("UI.CloseOverlays");
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
        selfTattooFormId = -1;
        if (clearData)
        {
            ClearOverlayData();
        }
    }

    private void ClearOverlayData()
    {
        ActiveShopNpc = null;
        ActiveTattooNpc = null;
        ActiveChoice = null;
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
