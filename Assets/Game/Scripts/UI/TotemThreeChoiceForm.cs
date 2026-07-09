using UnityEngine;

public sealed class TotemThreeChoiceForm : TotemOverlayFormBase
{
    public const float ChoiceInputGraceSeconds = 3f;

    private float openedUnscaledTime;
    private Coroutine unlockCoroutine;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        openedUnscaledTime = Time.unscaledTime;
        BuildView();
        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
        }

        unlockCoroutine = StartCoroutine(RefreshAfterInputGrace());
        GFTrace.Success("TotemUI", "ThreeChoice.Open", null, GFTrace.Data("eventId", UIService?.ActiveChoice?.EventId ?? string.Empty));
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }

        base.OnClose(isShutdown, userData);
    }

    private void BuildView()
    {
        var panel = RebuildPanel("Three Choices", new Vector2(620f, 420f));
        var choice = UIService?.ActiveChoice ?? Runtime?.GetService<TotemChoiceService>()?.Current;
        AddText(panel, "EventInfo", FormatChoiceHeader(choice), 16, TextAnchor.MiddleLeft, 34f);

        var options = choice?.Options;
        bool choiceButtonsInteractable = AreChoiceButtonsInteractable(Time.unscaledTime, openedUnscaledTime, ChoiceInputGraceSeconds);
        if (options == null || options.Length == 0)
        {
            AddText(panel, "Empty", "No options available.", 16, TextAnchor.MiddleCenter, 42f);
        }
        else
        {
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                AddButton(panel, $"Choice_{option.OptionId}", FormatChoiceText(option), () => OnChoiceClicked(option), choiceButtonsInteractable);
            }
        }

        AddButton(panel, "CloseButton", "Close", OnClickClose);
    }

    private void OnChoiceClicked(TotemChoiceOption option)
    {
        if (!AreChoiceButtonsInteractable(Time.unscaledTime, openedUnscaledTime, ChoiceInputGraceSeconds))
        {
            GFTrace.Info("TotemUI", "ThreeChoice.ClickBlocked", null, GFTrace.Data("optionId", option?.OptionId ?? string.Empty));
            return;
        }

        bool applied = Runtime?.GetService<TotemChoiceService>()?.ApplyChoice(option) ?? false;
        if (applied)
        {
            GFTrace.Success("TotemUI", "ThreeChoice.Apply", null, GFTrace.Data("optionId", option?.OptionId ?? string.Empty));
            OnClickClose();
        }
        else
        {
            GFTrace.Warning("TotemUI", "ThreeChoice.ApplyRejected", null, GFTrace.Data("optionId", option?.OptionId ?? string.Empty));
            BuildView();
        }
    }

    public override void OnClickClose()
    {
        Runtime?.GetService<TotemChoiceService>()?.CloseCurrentChoice("UI.Close");
        base.OnClickClose();
    }

    public static bool AreChoiceButtonsInteractable(float nowUnscaledTime, float openedUnscaledTime, float graceSeconds = ChoiceInputGraceSeconds)
    {
        return nowUnscaledTime - openedUnscaledTime >= Mathf.Max(0f, graceSeconds);
    }

    public static string FormatChoiceHeader(TotemChoiceSnapshot choice)
    {
        if (choice == null)
        {
            return "Event: none";
        }

        if (choice.State == TotemChoiceRuntimeState.Showing)
        {
            return $"Event: {choice.EventId}  {choice.RemainingSec:0.0}s";
        }

        return choice.State == TotemChoiceRuntimeState.Idle
            ? $"Event: {choice.EventId}"
            : $"Event: {choice.EventId}  {choice.State}";
    }

    public static string FormatChoiceText(TotemChoiceOption option)
    {
        if (option == null)
        {
            return "Empty choice";
        }

        return $"{option.DisplayName}  {option.EffectType}  {option.Magnitude:0.##}";
    }

    private System.Collections.IEnumerator RefreshAfterInputGrace()
    {
        while (!AreChoiceButtonsInteractable(Time.unscaledTime, openedUnscaledTime, ChoiceInputGraceSeconds))
        {
            yield return null;
        }

        unlockCoroutine = null;
        BuildView();
    }
}
