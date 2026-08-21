using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TotemFirstPlayableHudPresenter : MonoBehaviour
{
    private const string RootName = "UI-FP-HUD-001";

    private TotemGameRuntime runtime;
    private TotemMatchFlowService matchFlow;
    private TotemActorService actors;
    private TotemFirstPlayableLifecycleService lifecycle;
    private TotemFirstPlayableTattooBuildService builds;
    private TotemFirstPlayableSocialService social;
    private TMP_FontAsset font;
    private RectTransform root;
    private bool ownsRuntimeRoot;
    private bool hasAuthoredHud;
    private GameObject authoredModal;
    private RectTransform modal;
    private TextMeshProUGUI phaseText;
    private TextMeshProUGUI rosterText;
    private TextMeshProUGUI lifeText;
    private GameObject spectatorPanel;
    private TextMeshProUGUI spectatorText;
    private Button buildButton;
    private TotemMatchPhase lastPhase = TotemMatchPhase.FrontEnd;
    private TotemFirstPlayablePatternId selectedPattern = TotemFirstPlayablePatternId.P01;
    private TotemFirstPlayableElement selectedElement = TotemFirstPlayableElement.Fire;
    private TotemPigmentKind requestedPigment = TotemPigmentKind.Fire;
    private int requestedAmount = 1;
    private int intelligenceIndex;
    private bool intelligenceDetailExpanded;
    private bool locallyReady;
    private string feedback = string.Empty;

    public void Initialize(TotemGameRuntime gameRuntime, TMP_FontAsset sharedFont)
    {
        runtime = gameRuntime;
        matchFlow = runtime?.GetService<TotemMatchFlowService>();
        actors = runtime?.GetService<TotemActorService>();
        lifecycle = runtime?.GetService<TotemFirstPlayableLifecycleService>();
        builds = runtime?.GetService<TotemFirstPlayableTattooBuildService>();
        social = runtime?.GetService<TotemFirstPlayableSocialService>();
        font = sharedFont;
        if (font == null)
        {
            throw new MissingReferenceException("TotemFirstPlayableHudPresenter requires the shared SIMHEI SDF font reference.");
        }
        BuildPersistentHud();
        Refresh(force: true);
    }

    public void Refresh(bool force = false)
    {
        if (root == null || matchFlow == null)
        {
            return;
        }

        TotemMatchPhase phase = matchFlow.CurrentPhase;
        if (force || phase != lastPhase)
        {
            locallyReady = false;
            feedback = string.Empty;
            lastPhase = phase;
            if (TotemMatchPhaseContract.IsBuild(phase))
            {
                ShowBuildPanel();
            }
            else
            {
                CloseModal();
            }
        }

        SetText(phaseText, TotemCombatHUDForm.FormatMatchFlowStatus(matchFlow));
        SetText(rosterText, FormatLocalTeamRoster());
        SetText(lifeText, FormatLifeState(actors?.Player, lifecycle));
        RefreshSpectatorState();
        if (buildButton != null)
        {
            bool isBuildPhase = TotemMatchPhaseContract.IsBuild(phase);
            buildButton.interactable = true;
            SetButtonLabel(buildButton, isBuildPhase ? "构筑" : "构筑（只读）");
        }
    }

    public void Shutdown()
    {
        if (root != null && ownsRuntimeRoot)
        {
            Destroy(root.gameObject);
        }

        root = null;
        ownsRuntimeRoot = false;
        hasAuthoredHud = false;
        authoredModal = null;
        modal = null;
        runtime = null;
        matchFlow = null;
        actors = null;
        lifecycle = null;
        builds = null;
        social = null;
        font = null;
    }

    private void BuildPersistentHud()
    {
        if (transform.Find("Panel_Phase") != null)
        {
            root = transform as RectTransform;
            ownsRuntimeRoot = false;
            hasAuthoredHud = true;
            phaseText = FindAuthored<TextMeshProUGUI>("Txt_Phase");
            rosterText = FindAuthored<TextMeshProUGUI>("Txt_Roster");
            lifeText = FindAuthored<TextMeshProUGUI>("Txt_Life");
            spectatorPanel = FindAuthored<RectTransform>("Panel_Spectator").gameObject;
            spectatorText = FindAuthored<TextMeshProUGUI>("Txt_Spectator");
            buildButton = FindAuthored<Button>("Btn_Build");
            Button intelButton = FindAuthored<Button>("Btn_Intel");
            Button requestButton = FindAuthored<Button>("Btn_Request");
            BindAuthoredButton(buildButton, ShowBuildPanel);
            BindAuthoredButton(intelButton, ShowIntelligencePanel);
            BindAuthoredButton(requestButton, ShowPigmentRequestPanel);
            return;
        }

        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        root = CreateRect(RootName, transform);
        ownsRuntimeRoot = true;
        hasAuthoredHud = false;
        Stretch(root);

        var top = CreatePanel("UI-FP-HUD-001/PhaseBar", root, new Color(0.03f, 0.04f, 0.06f, 0.82f));
        Anchor(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(620f, 44f));
        phaseText = CreateText("UI-FP-HUD-001/PhaseText", top, string.Empty, 18, TextAnchor.MiddleCenter);
        Stretch(phaseText.rectTransform);

        var left = CreatePanel("UI-FP-HUD-001/Roster", root, new Color(0.03f, 0.04f, 0.06f, 0.76f));
        Anchor(left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -72f), new Vector2(270f, 210f), new Vector2(0f, 1f));
        rosterText = CreateText("UI-FP-HUD-001/RosterText", left, string.Empty, 14, TextAnchor.UpperLeft);
        Stretch(rosterText.rectTransform, 10f);

        var life = CreatePanel("UI-FP-HUD-001/LifeFeedback", root, new Color(0.28f, 0.05f, 0.05f, 0.82f));
        Anchor(life, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(620f, 54f), new Vector2(0.5f, 0f));
        lifeText = CreateText("UI-FP-HUD-001/LifeText", life, string.Empty, 17, TextAnchor.MiddleCenter);
        Stretch(lifeText.rectTransform, 6f);

        var actions = CreatePanel("UI-FP-HUD-001/Actions", root, new Color(0.03f, 0.04f, 0.06f, 0.76f));
        Anchor(actions, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -72f), new Vector2(200f, 170f), new Vector2(1f, 1f));
        var layout = actions.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        buildButton = CreateButton("UI-FP-BUILD-001/Open", actions, "构筑", ShowBuildPanel);
        CreateButton("UI-FP-INTEL-001/Open", actions, "六人情报", ShowIntelligencePanel);
        CreateButton("UI-FP-REQ-001/Open", actions, "请求颜料", ShowPigmentRequestPanel);
    }

    private T FindAuthored<T>(string nodeName) where T : Component
    {
        Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].name == nodeName && nodes[i].TryGetComponent<T>(out T component))
            {
                return component;
            }
        }

        throw new MissingReferenceException($"CombatHUD root is missing required {typeof(T).Name} node '{nodeName}'.");
    }

    private static void BindAuthoredButton(Button button, UnityAction action)
    {
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RefreshSpectatorState()
    {
        if (!hasAuthoredHud || spectatorPanel == null)
        {
            return;
        }

        TotemActorModel player = actors?.Player;
        TotemFirstPlayableParticipantLifeState lifeState = lifecycle?.GetOrCreateState(player);
        bool eliminated = lifeState != null && lifeState.LifeState == TotemFirstPlayableLifeState.Eliminated;
        spectatorPanel.SetActive(eliminated);
        if (!eliminated)
        {
            return;
        }

        TotemSpectatorState spectatorState = lifecycle.ResolveSpectatorState(player);
        TotemActorModel target = lifecycle.ResolveSpectatorTarget(player);
        spectatorText.SetText(spectatorState == TotemSpectatorState.SpectatingTeammate
            ? $"已淘汰 · 正在观战 {target?.Name ?? "队友"}"
            : "已淘汰 · 队伍无人存活，等待本局结果");
    }

    private void SetAuthoredText(string nodeName, string value)
    {
        FindAuthored<TextMeshProUGUI>(nodeName).SetText(value ?? string.Empty);
    }

    private static void SetButtonLabel(Button button, string value)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.SetText(value ?? string.Empty);
        }
    }

    private static void SetButtonSelected(Button button, bool selected)
    {
        if (button?.targetGraphic is Image image)
        {
            image.color = selected ? new Color(0.56f, 1f, 0.92f, 1f) : Color.white;
        }
    }

    private void ShowBuildPanel()
    {
        if (ShowAuthoredModal("Overlay_Build"))
        {
            bool canEdit = TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd) && !locallyReady;
            TotemFirstPlayableTattooBuildState authoredState = builds?.GetOrCreateState(actors?.Player);
            SetAuthoredText("Txt_BuildInventory", FormatInventory(authoredState));
            SetAuthoredText("Txt_BuildFeedback", canEdit ? feedback : "战斗阶段：构筑信息仅供查看，下一次构筑开始后可修改。");
            SetAuthoredText("Txt_BuildSelection", $"待装备：{selectedPattern} · {selectedElement}");
            SetAuthoredText("Txt_BuildSkillPreview", FormatLeftArmSkillPreview(authoredState));
            for (int i = 0; i < TotemFirstPlayableTattooBuildState.SlotCount; i++)
            {
                TotemTattooSlotId authoredSlot = (TotemTattooSlotId)i;
                TotemTattooLoadoutEntry authoredEntry = authoredState?.GetSlot(authoredSlot) ?? default;
                Button slotButton = FindAuthored<Button>("Btn_Slot" + i);
                SetButtonLabel(slotButton, FormatSlot(authoredEntry, authoredSlot));
                slotButton.interactable = canEdit;
                int captured = i;
                BindAuthoredButton(slotButton, () => ToggleSlot((TotemTattooSlotId)captured));
            }
            Button p01Button = FindAuthored<Button>("Btn_P01");
            Button p02Button = FindAuthored<Button>("Btn_P02");
            Button fireButton = FindAuthored<Button>("Btn_Fire");
            Button iceButton = FindAuthored<Button>("Btn_Ice");
            Button lightningButton = FindAuthored<Button>("Btn_Lightning");
            p01Button.interactable = canEdit;
            p02Button.interactable = canEdit;
            fireButton.interactable = canEdit;
            iceButton.interactable = canEdit;
            lightningButton.interactable = canEdit;
            BindAuthoredButton(p01Button, () => SelectPattern(TotemFirstPlayablePatternId.P01));
            BindAuthoredButton(p02Button, () => SelectPattern(TotemFirstPlayablePatternId.P02));
            BindAuthoredButton(fireButton, () => SelectElement(TotemFirstPlayableElement.Fire));
            BindAuthoredButton(iceButton, () => SelectElement(TotemFirstPlayableElement.Ice));
            BindAuthoredButton(lightningButton, () => SelectElement(TotemFirstPlayableElement.Lightning));
            SetButtonSelected(FindAuthored<Button>("Btn_P01"), selectedPattern == TotemFirstPlayablePatternId.P01);
            SetButtonSelected(FindAuthored<Button>("Btn_P02"), selectedPattern == TotemFirstPlayablePatternId.P02);
            SetButtonSelected(FindAuthored<Button>("Btn_Fire"), selectedElement == TotemFirstPlayableElement.Fire);
            SetButtonSelected(FindAuthored<Button>("Btn_Ice"), selectedElement == TotemFirstPlayableElement.Ice);
            SetButtonSelected(FindAuthored<Button>("Btn_Lightning"), selectedElement == TotemFirstPlayableElement.Lightning);
            Button readyButton = FindAuthored<Button>("Btn_BuildReady");
            Button requestButton = FindAuthored<Button>("Btn_BuildRequest");
            readyButton.interactable = canEdit;
            requestButton.interactable = canEdit;
            BindAuthoredButton(readyButton, ToggleReady);
            BindAuthoredButton(requestButton, ShowPigmentRequestPanel);
            BindAuthoredButton(FindAuthored<Button>("Btn_BuildIntel"), ShowIntelligencePanel);
            BindAuthoredButton(FindAuthored<Button>("Btn_BuildClose"), CloseModal);
            return;
        }

        CloseModal();
        modal = CreateModal("UI-FP-BUILD-001", new Vector2(780f, 690f));
        AddLabel(modal, "UI-FP-BUILD-001/Title", "纹身构筑", 24, 42f, TextAnchor.MiddleCenter);

        TotemFirstPlayableTattooBuildState state = builds?.GetOrCreateState(actors?.Player);
        AddLabel(modal, "UI-FP-BUILD-001/Inventory", FormatInventory(state), 16, 48f, TextAnchor.MiddleCenter);
        AddLabel(modal, "UI-FP-BUILD-001/Cost", "装备消耗 10 份颜料 · 移除返还 6 份", 14, 30f, TextAnchor.MiddleCenter);

        var patternRow = AddHorizontal(modal, "UI-FP-BUILD-001/Patterns", 48f);
        CreateButton("UI-FP-BUILD-001/P01", patternRow, selectedPattern == TotemFirstPlayablePatternId.P01 ? "[P01]" : "P01", () => SelectPattern(TotemFirstPlayablePatternId.P01));
        CreateButton("UI-FP-BUILD-001/P02", patternRow, selectedPattern == TotemFirstPlayablePatternId.P02 ? "[P02]" : "P02", () => SelectPattern(TotemFirstPlayablePatternId.P02));

        var elementRow = AddHorizontal(modal, "UI-FP-BUILD-001/Elements", 48f);
        CreateButton("UI-FP-BUILD-001/Fire", elementRow, selectedElement == TotemFirstPlayableElement.Fire ? "[火]" : "火", () => SelectElement(TotemFirstPlayableElement.Fire));
        CreateButton("UI-FP-BUILD-001/Ice", elementRow, selectedElement == TotemFirstPlayableElement.Ice ? "[冰]" : "冰", () => SelectElement(TotemFirstPlayableElement.Ice));
        CreateButton("UI-FP-BUILD-001/Lightning", elementRow, selectedElement == TotemFirstPlayableElement.Lightning ? "[雷]" : "雷", () => SelectElement(TotemFirstPlayableElement.Lightning));

        for (int i = 0; i < TotemFirstPlayableTattooBuildState.SlotCount; i++)
        {
            TotemTattooSlotId slot = (TotemTattooSlotId)i;
            TotemTattooLoadoutEntry entry = state?.GetSlot(slot) ?? default;
            int captured = i;
            CreateButton("UI-FP-BUILD-001/Slot-" + i, modal, FormatSlot(entry, slot), () => ToggleSlot((TotemTattooSlotId)captured), !locallyReady);
        }

        AddLabel(modal, "UI-FP-BUILD-001/Feedback", feedback, 14, 32f, TextAnchor.MiddleCenter);
        CreateButton("UI-FP-BUILD-001/Ready", modal, locallyReady ? "继续编辑" : "构筑完成（等待计时）", ToggleReady);
        CreateButton("UI-FP-BUILD-001/Intel", modal, "查看六人情报", ShowIntelligencePanel);
        CreateButton("UI-FP-BUILD-001/Close", modal, "收起", CloseModal);
    }

    private void SelectPattern(TotemFirstPlayablePatternId pattern)
    {
        if (!TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd)) return;
        selectedPattern = pattern;
        locallyReady = false;
        ShowBuildPanel();
    }

    private void SelectElement(TotemFirstPlayableElement element)
    {
        if (!TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd)) return;
        selectedElement = element;
        locallyReady = false;
        ShowBuildPanel();
    }

    private void ToggleSlot(TotemTattooSlotId slot)
    {
        if (!TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd)) return;
        TotemActorModel player = actors?.Player;
        TotemFirstPlayableTattooBuildState state = builds?.GetOrCreateState(player);
        TotemTattooLoadoutEntry current = state?.GetSlot(slot) ?? default;
        bool applied = current.IsEquipped
            ? builds != null && builds.TryRemove(player, slot, out TotemTattooMutationResult removeResult) && SetFeedback(removeResult)
            : builds != null && builds.TryEquip(player, slot, selectedPattern, selectedElement, out TotemTattooMutationResult equipResult) && SetFeedback(equipResult);
        if (!applied && string.IsNullOrEmpty(feedback))
        {
            feedback = "操作未生效：仅可在构筑阶段修改，并需要足够颜料。";
        }
        locallyReady = false;
        ShowBuildPanel();
    }

    private bool SetFeedback(TotemTattooMutationResult result)
    {
        feedback = result.Applied ? "构筑已更新。" : "操作失败：" + result.Code;
        return result.Applied;
    }

    private void ToggleReady()
    {
        if (!TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd)) return;
        locallyReady = !locallyReady;
        feedback = locallyReady ? "已准备；本轮计时结束前可继续编辑。" : "已恢复编辑。";
        ShowBuildPanel();
    }

    private void ShowIntelligencePanel()
    {
        intelligenceDetailExpanded = false;
        RefreshIntelligencePanel();
    }

    private void RefreshIntelligencePanel()
    {
        if (ShowAuthoredModal("Overlay_Intel"))
        {
            TotemConstructionIntelligenceSnapshot[] authoredSnapshots = social?.CaptureFrozenSnapshots() ?? Array.Empty<TotemConstructionIntelligenceSnapshot>();
            SetAuthoredText("Txt_IntelTitle", "六人情报 · 构筑开始前快照");
            Transform overview = FindAuthored<Transform>("Grp_IntelOverview");
            Transform detail = FindAuthored<Transform>("Grp_IntelDetail");
            overview.gameObject.SetActive(!intelligenceDetailExpanded);
            detail.gameObject.SetActive(intelligenceDetailExpanded);
            if (authoredSnapshots.Length == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Button card = FindAuthored<Button>("Btn_IntelCard" + i);
                    SetButtonLabel(card, "等待构筑阶段快照…");
                    card.interactable = false;
                }
                SetAuthoredText("Txt_IntelDetailTitle", "暂无情报");
                SetAuthoredText("Txt_IntelBuildContent", "当前还没有可公开的构筑阶段快照。");
                SetAuthoredText("Txt_IntelAchievementContent", string.Empty);
            }
            else
            {
                intelligenceIndex = Mathf.Clamp(intelligenceIndex, 0, authoredSnapshots.Length - 1);
                TotemConstructionIntelligenceSnapshot authoredSnapshot = authoredSnapshots[intelligenceIndex];
                SetAuthoredText("Txt_IntelDetailTitle", $"参与者 {authoredSnapshot.participantId} · 队伍 {authoredSnapshot.teamId + 1} · 构筑开始前快照");
                SetAuthoredText("Txt_IntelBuildContent", FormatIntelligenceBuild(authoredSnapshot));
                SetAuthoredText("Txt_IntelAchievementContent", FormatIntelligenceAchievements(authoredSnapshot.achievements));
                for (int i = 0; i < 6; i++)
                {
                    Button card = FindAuthored<Button>("Btn_IntelCard" + i);
                    bool hasSnapshot = i < authoredSnapshots.Length;
                    SetButtonLabel(card, hasSnapshot ? FormatIntelligenceSummary(authoredSnapshots[i]) : "未获得快照");
                    card.interactable = hasSnapshot;
                    int capturedIndex = i;
                    BindAuthoredButton(card, () => ShowIntelligenceDetail(capturedIndex));
                }
            }
            BindAuthoredButton(FindAuthored<Button>("Btn_IntelPrevious"), PreviousIntelligence);
            BindAuthoredButton(FindAuthored<Button>("Btn_IntelNext"), NextIntelligence);
            BindAuthoredButton(FindAuthored<Button>("Btn_IntelOverview"), ShowIntelligencePanel);
            Button buildButton = FindAuthored<Button>("Btn_IntelBuild");
            buildButton.interactable = TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd);
            BindAuthoredButton(buildButton, ShowBuildPanel);
            BindAuthoredButton(FindAuthored<Button>("Btn_IntelClose"), CloseModal);
            return;
        }

        CloseModal();
        modal = CreateModal("UI-FP-INTEL-001", new Vector2(820f, 690f));
        TotemConstructionIntelligenceSnapshot[] snapshots = social?.CaptureFrozenSnapshots() ?? Array.Empty<TotemConstructionIntelligenceSnapshot>();
        if (snapshots.Length == 0)
        {
            AddLabel(modal, "UI-FP-INTEL-001/Empty", "当前还没有可公开的构筑阶段快照。", 18, 180f, TextAnchor.MiddleCenter);
        }
        else
        {
            intelligenceIndex = Mathf.Clamp(intelligenceIndex, 0, snapshots.Length - 1);
            TotemConstructionIntelligenceSnapshot snapshot = snapshots[intelligenceIndex];
            AddLabel(modal, "UI-FP-INTEL-001/Title", $"参与者 {snapshot.participantId} · 队伍 {snapshot.teamId}", 23, 42f, TextAnchor.MiddleCenter);
            AddLabel(modal, "UI-FP-INTEL-001/Content", FormatIntelligence(snapshot), 14, 480f, TextAnchor.UpperLeft);
            var navigation = AddHorizontal(modal, "UI-FP-INTEL-001/Navigation", 48f);
            CreateButton("UI-FP-INTEL-001/Previous", navigation, "上一位", PreviousIntelligence);
            CreateButton("UI-FP-INTEL-001/Next", navigation, "下一位", NextIntelligence);
        }
        CreateButton("UI-FP-INTEL-001/Build", modal, "返回构筑", ShowBuildPanel, TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd));
        CreateButton("UI-FP-INTEL-001/Close", modal, "关闭", CloseModal);
    }

    private void ShowIntelligenceDetail(int index)
    {
        intelligenceIndex = index;
        intelligenceDetailExpanded = true;
        RefreshIntelligencePanel();
    }

    private void PreviousIntelligence()
    {
        int count = social?.CaptureFrozenSnapshots().Length ?? 0;
        if (count > 0) intelligenceIndex = (intelligenceIndex - 1 + count) % count;
        intelligenceDetailExpanded = true;
        RefreshIntelligencePanel();
    }

    private void NextIntelligence()
    {
        int count = social?.CaptureFrozenSnapshots().Length ?? 0;
        if (count > 0) intelligenceIndex = (intelligenceIndex + 1) % count;
        intelligenceDetailExpanded = true;
        RefreshIntelligencePanel();
    }

    private void ShowPigmentRequestPanel()
    {
        if (ShowAuthoredModal("Overlay_Request"))
        {
            TotemActorModel authoredTeammate = FindTeammate();
            TotemFirstPlayableTattooBuildState authoredTeammateState = builds?.GetOrCreateState(authoredTeammate);
            int authoredMax = authoredTeammateState?.GetPigment(requestedPigment) ?? 0;
            TotemFirstPlayableTattooBuildState authoredPlayerState = builds?.GetOrCreateState(actors?.Player);
            requestedAmount = Mathf.Clamp(requestedAmount, 1, Mathf.Max(1, authoredMax));
            SetAuthoredText("Txt_RequestAvailable", $"队友拥有：{requestedPigment} {authoredMax} 份");
            SetAuthoredText("Txt_RequestSelf", $"自己拥有：{requestedPigment} {authoredPlayerState?.GetPigment(requestedPigment) ?? 0} 份");
            SetAuthoredText("Txt_RequestAmount", requestedAmount.ToString());
            SetAuthoredText("Txt_RequestFeedback", feedback);
            BindAuthoredButton(FindAuthored<Button>("Btn_RequestFire"), () => SelectRequestedPigment(TotemPigmentKind.Fire));
            BindAuthoredButton(FindAuthored<Button>("Btn_RequestIce"), () => SelectRequestedPigment(TotemPigmentKind.Ice));
            BindAuthoredButton(FindAuthored<Button>("Btn_RequestLightning"), () => SelectRequestedPigment(TotemPigmentKind.Lightning));
            Button decreaseButton = FindAuthored<Button>("Btn_RequestDecrease");
            Button increaseButton = FindAuthored<Button>("Btn_RequestIncrease");
            bool canRequest = TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd);
            decreaseButton.interactable = canRequest && requestedAmount > 1;
            increaseButton.interactable = canRequest && requestedAmount < authoredMax;
            BindAuthoredButton(decreaseButton, DecreaseRequestAmount);
            BindAuthoredButton(increaseButton, IncreaseRequestAmount);
            Button submitButton = FindAuthored<Button>("Btn_RequestSubmit");
            submitButton.interactable = authoredMax > 0 && canRequest;
            BindAuthoredButton(submitButton, SubmitPigmentRequest);
            BindAuthoredButton(FindAuthored<Button>("Btn_RequestClose"), CloseModal);
            return;
        }

        CloseModal();
        modal = CreateModal("UI-FP-REQ-001", new Vector2(560f, 500f));
        TotemActorModel teammate = FindTeammate();
        TotemFirstPlayableTattooBuildState teammateState = builds?.GetOrCreateState(teammate);
        int max = teammateState?.GetPigment(requestedPigment) ?? 0;
        requestedAmount = Mathf.Clamp(requestedAmount, 1, Mathf.Max(1, max));
        AddLabel(modal, "UI-FP-REQ-001/Title", "向队友请求颜料", 24, 42f, TextAnchor.MiddleCenter);
        AddLabel(modal, "UI-FP-REQ-001/Available", $"队友拥有：{requestedPigment} {max} 份", 17, 44f, TextAnchor.MiddleCenter);
        var pigmentRow = AddHorizontal(modal, "UI-FP-REQ-001/Pigments", 48f);
        CreateButton("UI-FP-REQ-001/Fire", pigmentRow, "火", () => SelectRequestedPigment(TotemPigmentKind.Fire));
        CreateButton("UI-FP-REQ-001/Ice", pigmentRow, "冰", () => SelectRequestedPigment(TotemPigmentKind.Ice));
        CreateButton("UI-FP-REQ-001/Lightning", pigmentRow, "雷", () => SelectRequestedPigment(TotemPigmentKind.Lightning));
        var amountRow = AddHorizontal(modal, "UI-FP-REQ-001/Amount", 48f);
        CreateButton("UI-FP-REQ-001/Decrease", amountRow, "-", DecreaseRequestAmount);
        AddLabel(amountRow, "UI-FP-REQ-001/AmountValue", requestedAmount.ToString(), 20, 44f, TextAnchor.MiddleCenter);
        CreateButton("UI-FP-REQ-001/Increase", amountRow, "+", IncreaseRequestAmount);
        AddLabel(modal, "UI-FP-REQ-001/Feedback", feedback, 14, 36f, TextAnchor.MiddleCenter);
        CreateButton("UI-FP-REQ-001/Submit", modal, "发送请求", SubmitPigmentRequest, max > 0 && TotemMatchPhaseContract.IsBuild(matchFlow?.CurrentPhase ?? TotemMatchPhase.FrontEnd));
        CreateButton("UI-FP-REQ-001/Close", modal, "关闭", CloseModal);
    }

    private void SelectRequestedPigment(TotemPigmentKind pigment) { requestedPigment = pigment; requestedAmount = 1; ShowPigmentRequestPanel(); }
    private void DecreaseRequestAmount() { requestedAmount = Mathf.Max(1, requestedAmount - 1); ShowPigmentRequestPanel(); }
    private void IncreaseRequestAmount()
    {
        int max = builds?.GetOrCreateState(FindTeammate())?.GetPigment(requestedPigment) ?? 0;
        requestedAmount = Mathf.Min(Mathf.Max(1, max), requestedAmount + 1);
        ShowPigmentRequestPanel();
    }

    private void SubmitPigmentRequest()
    {
        TotemActorModel player = actors?.Player;
        TotemPigmentRequest request = default;
        TotemPigmentTransfer transfer = default;
        bool accepted = player != null && social != null && social.TryRequestPigment(
            new TotemParticipantId(player.ParticipantId), requestedPigment, requestedAmount,
            out request, out transfer);
        feedback = accepted
            ? transfer.RequiresAtomicCommit ? $"队友已同意，获得 {transfer.Amount} 份 {transfer.Pigment}。" : $"请求 {request.RequestId} 已发送。"
            : "请求未发送：队友库存已变化、请求已失效，或当前不在构筑阶段。";
        ShowPigmentRequestPanel();
    }

    private TotemActorModel FindTeammate()
    {
        TotemActorModel player = actors?.Player;
        var roster = actors?.Actors;
        if (player == null || roster == null) return null;
        for (int i = 0; i < roster.Count; i++)
        {
            TotemActorModel candidate = roster[i];
            if (candidate != null && candidate != player && candidate.TeamId == player.TeamId) return candidate;
        }
        return null;
    }

    private RectTransform CreateModal(string name, Vector2 size)
    {
        var blocker = CreatePanel(name + "/Blocker", root, new Color(0f, 0f, 0f, 0.72f));
        Stretch(blocker);
        RectTransform panel = CreatePanel(name, blocker, new Color(0.07f, 0.08f, 0.11f, 0.98f));
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.spacing = 7f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        return panel;
    }

    private void CloseModal()
    {
        if (authoredModal != null)
        {
            authoredModal.SetActive(false);
            authoredModal = null;
            return;
        }

        if (modal != null)
        {
            Destroy(modal.parent.gameObject);
            modal = null;
        }
    }

    private bool ShowAuthoredModal(string overlayName)
    {
        if (!hasAuthoredHud)
        {
            return false;
        }

        CloseModal();
        Transform overlay = root.Find(overlayName);
        if (overlay == null)
        {
            throw new MissingReferenceException($"CombatHUD root is missing authored overlay '{overlayName}'.");
        }

        authoredModal = overlay.gameObject;
        authoredModal.SetActive(true);
        return true;
    }

    public static string FormatInventory(TotemFirstPlayableTattooBuildState state) => state == null
        ? "颜料：火 0 · 冰 0 · 雷 0"
        : $"颜料：火 {state.GetPigment(TotemPigmentKind.Fire)} · 冰 {state.GetPigment(TotemPigmentKind.Ice)} · 雷 {state.GetPigment(TotemPigmentKind.Lightning)}";

    public static string FormatSlot(TotemTattooLoadoutEntry entry, TotemTattooSlotId slot)
    {
        string slotName = slot switch
        {
            TotemTattooSlotId.Head => "头部",
            TotemTattooSlotId.Torso => "躯干",
            TotemTattooSlotId.LeftArm => "左臂·主动",
            TotemTattooSlotId.RightArm => "右臂·普攻",
            TotemTattooSlotId.LeftLeg => "左腿",
            TotemTattooSlotId.RightLeg => "右腿",
            _ => slot.ToString(),
        };

        return entry.IsEquipped
            ? $"{slotName}\n{entry.Pattern} · {entry.Element}\n点击移除"
            : $"{slotName}\n空槽\n点击装备";
    }

    private string FormatLeftArmSkillPreview(TotemFirstPlayableTattooBuildState state)
    {
        TotemTattooLoadoutEntry leftArm = state?.GetSlot(TotemTattooSlotId.LeftArm) ?? default;
        string current = leftArm.IsEquipped
            ? $"当前左臂：{leftArm.Pattern} · {leftArm.Element}"
            : "当前左臂：未装备主动技能";
        string targetRule = selectedPattern == TotemFirstPlayablePatternId.P01
            ? "单目标元素主动技能"
            : "范围元素主动技能";
        return $"{current}\n预览：{selectedPattern} · {selectedElement} · {targetRule}\n战斗阶段可用 · 构筑结束刷新冷却";
    }

    public static string FormatIntelligence(TotemConstructionIntelligenceSnapshot snapshot)
    {
        if (snapshot == null) return "暂无情报。";
        var builder = new StringBuilder(512);
        builder.AppendLine("当前构筑快照");
        if (snapshot.tattoos == null || snapshot.tattoos.Length == 0) builder.AppendLine("- 未装备纹身");
        else for (int i = 0; i < snapshot.tattoos.Length; i++)
        {
            TotemPublicTattooSnapshotEntry tattoo = snapshot.tattoos[i];
            builder.Append("- ").Append(tattoo.slot).Append(' ').Append(tattoo.pattern).Append('/').Append(tattoo.element).Append("：").AppendLine(tattoo.publicEffectText);
        }
        if (snapshot.attributes != null) for (int i = 0; i < snapshot.attributes.Length; i++)
        {
            TotemAttributeSnapshotEntry attribute = snapshot.attributes[i];
            builder.Append("- ").Append(attribute.attributeId).Append(" 基础 ").Append(attribute.baseValue.ToString("0.##")).Append(" + 局内 ").AppendLine(attribute.inMatchBonus.ToString("0.##"));
        }
        TotemMatchAchievementSnapshot a = snapshot.achievements;
        builder.AppendLine("\n本局个人成果")
            .Append("玩家伤害 ").Append(a.playerDamage.ToString("0")).Append(" / 击倒 ").Append(a.playerDowns).Append(" / 淘汰 ").AppendLine(a.playerEliminations.ToString())
            .Append("治疗队友 ").Append(a.allyHealing.ToString("0")).Append(" / 护盾或减伤 ").AppendLine(a.allyShieldOrMitigation.ToString("0"))
            .Append("成功救援 ").Append(a.successfulRevives).Append(" / 净化解除 ").AppendLine(a.cleansesOrControlRemovals.ToString())
            .Append("有效控制 ").Append(a.effectiveControlSeconds.ToString("0.0")).Append("秒 / ").Append(a.effectiveControlCount).AppendLine("次")
            .Append("队友增伤收益 ").Append(a.allyDamageGainCreated.ToString("0")).Append(" / 间接元素伤害 ").AppendLine(a.indirectElementDamage.ToString("0"))
            .Append("资源获取 ").Append(a.resourcesAcquired).Append(" / 分享 ").Append(a.resourcesShared).Append(" / 自身倒地 ").Append(a.selfDowns);
        return builder.ToString();
    }

    private static string FormatIntelligenceBuild(TotemConstructionIntelligenceSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "暂无情报。";
        }

        var builder = new StringBuilder(384);
        builder.AppendLine("当前构筑快照");
        if (snapshot.tattoos == null || snapshot.tattoos.Length == 0)
        {
            builder.AppendLine("- 未装备纹身");
        }
        else
        {
            for (int i = 0; i < snapshot.tattoos.Length; i++)
            {
                TotemPublicTattooSnapshotEntry tattoo = snapshot.tattoos[i];
                builder.Append("- ").Append(tattoo.slot).Append(' ').Append(tattoo.pattern).Append('/').Append(tattoo.element).Append("：").AppendLine(tattoo.publicEffectText);
            }
        }

        builder.AppendLine("\n当前属性（基础 / 局内强化）");
        if (snapshot.attributes == null || snapshot.attributes.Length == 0)
        {
            builder.Append("- 暂无强化");
        }
        else
        {
            for (int i = 0; i < snapshot.attributes.Length; i++)
            {
                TotemAttributeSnapshotEntry attribute = snapshot.attributes[i];
                builder.Append("- ").Append(attribute.attributeId).Append(' ').Append(attribute.baseValue.ToString("0.##")).Append(" / +").AppendLine(attribute.inMatchBonus.ToString("0.##"));
            }
        }

        return builder.ToString();
    }

    private static string FormatIntelligenceAchievements(TotemMatchAchievementSnapshot achievement)
    {
        var builder = new StringBuilder(256);
        builder.AppendLine("本局个人成果")
            .Append("玩家伤害 ").Append(achievement.playerDamage.ToString("0")).Append(" / 击倒 ").Append(achievement.playerDowns).Append(" / 淘汰 ").AppendLine(achievement.playerEliminations.ToString())
            .Append("治疗队友 ").Append(achievement.allyHealing.ToString("0")).Append(" / 护盾或减伤 ").AppendLine(achievement.allyShieldOrMitigation.ToString("0"))
            .Append("成功救援 ").Append(achievement.successfulRevives).Append(" / 净化解除 ").AppendLine(achievement.cleansesOrControlRemovals.ToString())
            .Append("有效控制 ").Append(achievement.effectiveControlSeconds.ToString("0.0")).Append("秒 / ").Append(achievement.effectiveControlCount).AppendLine("次")
            .Append("队友增伤收益 ").Append(achievement.allyDamageGainCreated.ToString("0")).Append(" / 间接元素伤害 ").AppendLine(achievement.indirectElementDamage.ToString("0"))
            .Append("资源获取 ").Append(achievement.resourcesAcquired).Append(" / 分享 ").Append(achievement.resourcesShared).Append(" / 自身倒地 ").Append(achievement.selfDowns);
        return builder.ToString();
    }

    private static string FormatIntelligenceSummary(TotemConstructionIntelligenceSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "未获得快照";
        }

        var builder = new StringBuilder(128);
        builder.Append("P").Append(snapshot.participantId).Append("  ·  队伍 ").Append(snapshot.teamId + 1).AppendLine();
        builder.Append("构筑：");
        if (snapshot.tattoos == null || snapshot.tattoos.Length == 0)
        {
            builder.Append("未装备纹身");
        }
        else
        {
            int count = Mathf.Min(snapshot.tattoos.Length, 3);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) builder.Append(" · ");
                TotemPublicTattooSnapshotEntry tattoo = snapshot.tattoos[i];
                builder.Append(tattoo.pattern).Append('/').Append(tattoo.element);
            }
        }

        TotemMatchAchievementSnapshot achievement = snapshot.achievements;
        builder.AppendLine();
        builder.Append("成果：伤害 ").Append(achievement.playerDamage.ToString("0"))
            .Append(" · 淘汰 ").Append(achievement.playerEliminations)
            .Append(" · 救援 ").Append(achievement.successfulRevives);
        return builder.ToString();
    }

    public static string FormatRoster(TotemActorService actorService, TotemFirstPlayableLifecycleService lifeService)
    {
        var roster = actorService?.Actors;
        if (roster == null || roster.Count == 0) return "等待六人阵容生成…";
        var builder = new StringBuilder(192);
        for (int i = 0; i < roster.Count; i++)
        {
            TotemActorModel actor = roster[i];
            if (actor == null) continue;
            TotemFirstPlayableParticipantLifeState state = lifeService?.GetOrCreateState(actor);
            builder.Append('P').Append(actor.ParticipantId).Append("  ").Append(actor.TeamId.ToString()).Append("  ").Append(FormatLifeStateLabel(state));
            if (actor.ControllerKind != TotemParticipantControllerKind.Human) builder.Append("  BOT");
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private string FormatLocalTeamRoster()
    {
        TotemActorModel player = actors?.Player;
        var roster = actors?.Actors;
        if (player == null || roster == null)
        {
            return "等待小队生成…";
        }

        var builder = new StringBuilder(128);
        TotemFirstPlayableParticipantLifeState playerState = lifecycle?.GetOrCreateState(player);
        TotemActorModel downedTeammate = null;
        for (int i = 0; i < roster.Count; i++)
        {
            TotemActorModel actor = roster[i];
            if (actor == null || actor.TeamId.Value != player.TeamId.Value)
            {
                continue;
            }

            TotemFirstPlayableParticipantLifeState state = lifecycle?.GetOrCreateState(actor);
            if (actor != player && state?.IsDowned == true)
            {
                downedTeammate = actor;
            }
            builder.Append(actor.ParticipantId == player.ParticipantId ? "你" : "队友")
                .Append(" P").Append(actor.ParticipantId)
                .Append(" · HP ").Append(actor.Health.ToString("0"))
                .Append(" · ").Append(FormatLifeStateLabel(state))
                .AppendLine();
        }

        if (playerState?.IsDowned == true)
        {
            builder.Append("救援：等待队友 · 范围 ")
                .Append(TotemFirstPlayableLifecycleService.ReviveInteractRadius.ToString("0"))
                .Append("m");
        }
        else if (downedTeammate != null)
        {
            float distance = Vector3.Distance(player.Position, downedTeammate.Position);
            builder.Append("救援：")
                .Append(distance <= TotemFirstPlayableLifecycleService.ReviveInteractRadius ? "可按交互键" : "接近至 3m")
                .Append(" · ")
                .Append(distance.ToString("0.0"))
                .Append("m");
        }
        else
        {
            builder.Append("救援：队友状态正常 · 范围 3m");
        }

        return builder.Length == 0 ? "等待小队生成…" : builder.ToString().TrimEnd();
    }

    public static string FormatLifeState(TotemActorModel player, TotemFirstPlayableLifecycleService lifeService)
    {
        if (player == null || lifeService == null) return string.Empty;
        TotemFirstPlayableParticipantLifeState state = lifeService.GetOrCreateState(player);
        if (state == null || state.LifeState == TotemFirstPlayableLifeState.Alive) return string.Empty;
        if (state.IsDowned) return $"已倒地 · 流血倒计时 {state.BleedoutRemaining:0.0}s · 救援 {state.ReviveProgress / TotemDownedStateContract.ReviveSeconds:P0}";
        TotemSpectatorState spectator = lifeService.ResolveSpectatorState(player);
        TotemActorModel target = lifeService.ResolveSpectatorTarget(player);
        return spectator == TotemSpectatorState.SpectatingTeammate ? $"已淘汰 · 正在观战 {target?.Name ?? "队友"}" : "已淘汰 · 等待本局结果";
    }

    private static string FormatLifeStateLabel(TotemFirstPlayableParticipantLifeState state)
    {
        return state?.LifeState switch
        {
            TotemFirstPlayableLifeState.Downed => "倒地",
            TotemFirstPlayableLifeState.Eliminated => "淘汰",
            _ => "存活",
        };
    }

    private static RectTransform AddHorizontal(Transform parent, string name, float height)
    {
        RectTransform row = CreateRect(name, parent);
        var element = row.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        return row;
    }

    private TextMeshProUGUI AddLabel(Transform parent, string name, string value, int fontSize, float height, TextAnchor alignment)
    {
        TextMeshProUGUI text = CreateText(name, parent, value, fontSize, alignment);
        var element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, UnityAction action, bool interactable = true)
    {
        RectTransform rect = CreateRect(name, parent);
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 42f;
        element.minHeight = 42f;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = interactable ? new Color(0.20f, 0.43f, 0.68f, 1f) : new Color(0.22f, 0.22f, 0.22f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        if (action != null) button.onClick.AddListener(action);
        TextMeshProUGUI text = CreateText(name + "/Label", rect, label, 16, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = ToTmpAlignment(alignment);
        text.text = value ?? string.Empty;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null && !string.Equals(text.text, value, StringComparison.Ordinal)) text.text = value ?? string.Empty;
    }

    private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center,
        };
    }
}
