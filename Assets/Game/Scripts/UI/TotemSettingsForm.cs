using UnityEngine;

public sealed class TotemSettingsForm : TotemOverlayFormBase
{
    [SerializeField] private TMPro.TMP_Text summaryText;
    [SerializeField] private UnityEngine.UI.Button bgmDownButton;
    [SerializeField] private UnityEngine.UI.Button bgmUpButton;
    [SerializeField] private UnityEngine.UI.Button sfxDownButton;
    [SerializeField] private UnityEngine.UI.Button sfxUpButton;
    [SerializeField] private UnityEngine.UI.Button qualityButton;
    [SerializeField] private UnityEngine.UI.Button saveButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    private float bgmVolume;
    private float sfxVolume;
    private int qualityLevel;
    private bool committed;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        summaryText ??= FindChildComponent<TMPro.TMP_Text>("Txt_SettingsSummary");
        bgmDownButton = BindButton(bgmDownButton, "Btn_BgmDown", () => AdjustBgm(-0.1f));
        bgmUpButton = BindButton(bgmUpButton, "Btn_BgmUp", () => AdjustBgm(0.1f));
        sfxDownButton = BindButton(sfxDownButton, "Btn_SfxDown", () => AdjustSfx(-0.1f));
        sfxUpButton = BindButton(sfxUpButton, "Btn_SfxUp", () => AdjustSfx(0.1f));
        qualityButton = BindButton(qualityButton, "Btn_Quality", NextQuality);
        saveButton = BindButton(saveButton, "Btn_SettingsSave", Save);
        cancelButton = BindButton(cancelButton, "Btn_SettingsCancel", Cancel);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        var service = Runtime?.GetService<TotemSettingsService>();
        service?.BeginEdit();
        committed = false;
        var snapshot = service?.CaptureSnapshot() ?? new TotemSettingsSnapshot();
        bgmVolume = snapshot.bgmVolume;
        sfxVolume = snapshot.sfxVolume;
        qualityLevel = snapshot.qualityLevel;
        BuildView();
        GFTrace.Success("TotemUI", "Settings.Open");
    }

    private void BuildView()
    {
        summaryText?.SetText(TotemSettingsService.FormatSnapshot(BuildDraft()));
    }

    private void AdjustBgm(float delta)
    {
        bgmVolume = Mathf.Clamp01(bgmVolume + delta);
        PreviewAndRebuild();
    }

    private void AdjustSfx(float delta)
    {
        sfxVolume = Mathf.Clamp01(sfxVolume + delta);
        PreviewAndRebuild();
    }

    private void NextQuality()
    {
        int count = QualitySettings.names == null || QualitySettings.names.Length == 0 ? 3 : QualitySettings.names.Length;
        qualityLevel = (qualityLevel + 1) % Mathf.Max(1, count);
        PreviewAndRebuild();
    }

    private void PreviewAndRebuild()
    {
        Runtime?.GetService<TotemSettingsService>()?.Preview(bgmVolume, sfxVolume, qualityLevel);
        BuildView();
    }

    private void Save()
    {
        Runtime?.GetService<TotemSettingsService>()?.Commit();
        committed = true;
        OnClickClose();
    }

    private void Cancel()
    {
        Runtime?.GetService<TotemSettingsService>()?.Rollback();
        committed = true;
        OnClickClose();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (!committed)
        {
            Runtime?.GetService<TotemSettingsService>()?.Rollback();
        }

        base.OnClose(isShutdown, userData);
    }

    private TotemSettingsSnapshot BuildDraft()
    {
        return new TotemSettingsSnapshot
        {
            bgmVolume = bgmVolume,
            sfxVolume = sfxVolume,
            qualityLevel = qualityLevel,
            editing = true,
        };
    }
}
