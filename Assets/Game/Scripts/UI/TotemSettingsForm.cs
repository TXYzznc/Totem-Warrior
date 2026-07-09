using UnityEngine;

public sealed class TotemSettingsForm : TotemOverlayFormBase
{
    private float bgmVolume;
    private float sfxVolume;
    private int qualityLevel;
    private bool committed;

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
        var panel = RebuildPanel("Settings", new Vector2(520f, 420f));
        AddText(panel, "Summary", TotemSettingsService.FormatSnapshot(BuildDraft()), 17, TextAnchor.MiddleCenter, 44f);
        AddButton(panel, "BgmDown", "BGM -", () => AdjustBgm(-0.1f));
        AddButton(panel, "BgmUp", "BGM +", () => AdjustBgm(0.1f));
        AddButton(panel, "SfxDown", "SFX -", () => AdjustSfx(-0.1f));
        AddButton(panel, "SfxUp", "SFX +", () => AdjustSfx(0.1f));
        AddButton(panel, "QualityButton", $"Quality Next ({qualityLevel})", NextQuality);
        AddButton(panel, "SaveButton", "Save", Save);
        AddButton(panel, "CancelButton", "Cancel", Cancel);
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
