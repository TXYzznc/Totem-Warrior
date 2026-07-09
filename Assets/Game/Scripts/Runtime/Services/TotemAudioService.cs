using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public sealed class TotemAudioService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private const string MainMenuBgmCueId = "bgm_main_menu";
    private const string InGameBgmCueId = "bgm_in_game";
    private const string HitMeleeCueId = "sfx_hit_melee";
    private const string HitRangedCueId = "sfx_hit_ranged";
    private const string HitSpecialCueId = "sfx_hit_special";
    private const string HitDefaultCueId = "sfx_hit_default";
    private const string SkillCastCueId = "sfx_skill_cast";
    private const string KillCueId = "sfx_kill";
    private const string PlayerDiedCueId = "sfx_player_died";

    private readonly Dictionary<string, TotemAudioCueDefinition> cues = new Dictionary<string, TotemAudioCueDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, float> lastSfxTimes = new Dictionary<string, float>(StringComparer.Ordinal);

    private TotemGameFlowService flowService;
    private TotemSettingsService settingsService;
    private TotemActorService actorService;
    private TotemWeaponService weaponService;
    private TotemBossService bossService;

    private bool active;
    private bool backendAvailable;
    private float bgmVolume = 0.8f;
    private float sfxVolume = 0.8f;
    private int bgmRequestCount;
    private int sfxRequestCount;
    private int backendPlayAttemptCount;
    private int backendPlaySuccessCount;
    private int backendUnavailableCount;
    private int missingCueCount;
    private int intervalSkipCount;
    private int observedBossPhase;
    private string observedBossBgmCueId = string.Empty;
    private string currentBgmCueId = string.Empty;
    private string currentBgmAssetName = string.Empty;
    private string lastSfxCueId = string.Empty;
    private string lastSfxAssetName = string.Empty;
    private string lastMissingCueId = string.Empty;
    private string lastSkippedCueId = string.Empty;
    private string lastReason = string.Empty;

    public override string ServiceName => "Audio";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        ReloadCueCatalog(runtime.GetService<TotemDataService>()?.GameplayCatalog);

        flowService = runtime.GetService<TotemGameFlowService>();
        settingsService = runtime.GetService<TotemSettingsService>();
        actorService = runtime.GetService<TotemActorService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        bossService = runtime.GetService<TotemBossService>();

        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

        if (settingsService != null)
        {
            settingsService.SettingsChanged += OnSettingsChanged;
            OnSettingsChanged(settingsService.CaptureSnapshot());
        }

        if (actorService != null)
        {
            actorService.DamageResolved += OnDamageResolved;
        }

        active = true;
        backendAvailable = TryGetSoundComponent(out _);
        if (flowService != null && flowService.CurrentState != TotemGameFlowState.None)
        {
            OnFlowStateChanged(TotemGameFlowState.None, flowService.CurrentState);
        }

        GFTrace.Success("TotemAudio", "Initialized", null, GFTrace.Data(
            "cueCount", cues.Count.ToString(),
            "backendAvailable", backendAvailable.ToString()));
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        if (settingsService != null)
        {
            settingsService.SettingsChanged -= OnSettingsChanged;
            settingsService = null;
        }

        if (actorService != null)
        {
            actorService.DamageResolved -= OnDamageResolved;
            actorService = null;
        }

        weaponService = null;
        bossService = null;
        cues.Clear();
        lastSfxTimes.Clear();
        active = false;
    }

    public void Tick(float deltaTime)
    {
        if (!active)
        {
            return;
        }

        backendAvailable = TryGetSoundComponent(out _);
        ObserveBossPhaseBgm();
    }

    public bool TryGetCue(string cueId, out TotemAudioCueDefinition cue)
    {
        return cues.TryGetValue(cueId ?? string.Empty, out cue);
    }

    public bool PlayBgmCue(string cueId, string reason)
    {
        return PlayCue(cueId, TotemAudioCueKind.Bgm, Vector3.zero, reason);
    }

    public bool PlaySfxCue(string cueId, Vector3 worldPosition, string reason)
    {
        return PlayCue(cueId, TotemAudioCueKind.Sfx, worldPosition, reason);
    }

    public TotemAudioSnapshot CaptureSnapshot()
    {
        return new TotemAudioSnapshot
        {
            active = active,
            backendAvailable = backendAvailable,
            cueCount = cues.Count,
            bgmRequestCount = bgmRequestCount,
            sfxRequestCount = sfxRequestCount,
            backendPlayAttemptCount = backendPlayAttemptCount,
            backendPlaySuccessCount = backendPlaySuccessCount,
            backendUnavailableCount = backendUnavailableCount,
            missingCueCount = missingCueCount,
            intervalSkipCount = intervalSkipCount,
            currentBgmCueId = currentBgmCueId,
            currentBgmAssetName = currentBgmAssetName,
            lastSfxCueId = lastSfxCueId,
            lastSfxAssetName = lastSfxAssetName,
            lastMissingCueId = lastMissingCueId,
            lastSkippedCueId = lastSkippedCueId,
            lastReason = lastReason,
            bgmVolume = bgmVolume,
            sfxVolume = sfxVolume,
            observedBossPhase = observedBossPhase,
            observedBossBgmCueId = observedBossBgmCueId,
        };
    }

    private void ReloadCueCatalog(TotemGameplayCatalog catalog)
    {
        cues.Clear();
        var definitions = NonEmpty(catalog?.CreateAudioCueDefinitions(), TotemGameplayCatalog.BuildDefault().CreateAudioCueDefinitions());
        for (int i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.CueId) || definition.Kind == TotemAudioCueKind.Unknown)
            {
                continue;
            }

            if (cues.ContainsKey(definition.CueId))
            {
                GFTrace.Warning("TotemAudio", "Cue.Duplicate", null, GFTrace.Data("cueId", definition.CueId));
                continue;
            }

            cues.Add(definition.CueId, definition);
        }
    }

    private bool PlayCue(string cueId, TotemAudioCueKind expectedKind, Vector3 worldPosition, string reason)
    {
        if (!cues.TryGetValue(cueId ?? string.Empty, out var cue))
        {
            missingCueCount++;
            lastMissingCueId = cueId ?? string.Empty;
            lastReason = string.IsNullOrWhiteSpace(reason) ? "MissingCue" : reason;
            GFTrace.Warning("TotemAudio", "Cue.Missing", null, GFTrace.Data("cueId", cueId ?? string.Empty, "reason", lastReason));
            return false;
        }

        if (cue.Kind != expectedKind)
        {
            missingCueCount++;
            lastMissingCueId = cue.CueId ?? string.Empty;
            lastReason = string.IsNullOrWhiteSpace(reason) ? "CueKindMismatch" : reason;
            GFTrace.Warning("TotemAudio", "Cue.KindMismatch", null, GFTrace.Data(
                "cueId", cue.CueId,
                "expected", expectedKind.ToString(),
                "actual", cue.Kind.ToString()));
            return false;
        }

        if (cue.Kind == TotemAudioCueKind.Bgm && string.Equals(currentBgmCueId, cue.CueId, StringComparison.Ordinal))
        {
            lastReason = string.IsNullOrWhiteSpace(reason) ? "Bgm.AlreadyCurrent" : reason;
            return true;
        }

        if (cue.Kind == TotemAudioCueKind.Sfx && ShouldSkipByInterval(cue))
        {
            intervalSkipCount++;
            lastSkippedCueId = cue.CueId;
            lastReason = string.IsNullOrWhiteSpace(reason) ? "Sfx.IntervalSkip" : reason;
            return false;
        }

        if (cue.Kind == TotemAudioCueKind.Bgm)
        {
            bgmRequestCount++;
            currentBgmCueId = cue.CueId;
            currentBgmAssetName = cue.AssetName;
        }
        else
        {
            sfxRequestCount++;
            lastSfxCueId = cue.CueId;
            lastSfxAssetName = cue.AssetName;
            lastSfxTimes[cue.CueId] = Time.unscaledTime;
        }

        lastReason = reason ?? string.Empty;
        TryPlayThroughBackend(cue, worldPosition);
        GFTrace.Info("TotemAudio", "Cue.Requested", null, GFTrace.Data(
            "cueId", cue.CueId,
            "kind", cue.Kind.ToString(),
            "asset", cue.AssetName,
            "reason", lastReason));
        return true;
    }

    private bool ShouldSkipByInterval(TotemAudioCueDefinition cue)
    {
        if (cue.MinIntervalSec <= 0f)
        {
            return false;
        }

        return lastSfxTimes.TryGetValue(cue.CueId, out float lastTime)
            && Time.unscaledTime - lastTime < cue.MinIntervalSec;
    }

    private void TryPlayThroughBackend(TotemAudioCueDefinition cue, Vector3 worldPosition)
    {
        backendPlayAttemptCount++;
        if (!TryGetSoundComponent(out var soundComponent))
        {
            backendUnavailableCount++;
            return;
        }

        int serialId = 0;
        if (cue.Kind == TotemAudioCueKind.Bgm)
        {
            serialId = soundComponent.PlayBGM(cue.AssetName);
        }
        else
        {
            serialId = soundComponent.PlaySound(cue.AssetName, Const.SoundGroup.Sound.ToString(), worldPosition, cue.Loop);
        }

        if (serialId != 0)
        {
            backendPlaySuccessCount++;
        }
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.MainMenu)
        {
            observedBossPhase = 0;
            observedBossBgmCueId = string.Empty;
            PlayBgmCue(MainMenuBgmCueId, "GameFlow.MainMenu");
        }
        else if (nextState == TotemGameFlowState.CombatHud)
        {
            observedBossPhase = 0;
            observedBossBgmCueId = string.Empty;
            PlayBgmCue(InGameBgmCueId, "GameFlow.CombatHud");
        }
    }

    private void OnSettingsChanged(TotemSettingsSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        bgmVolume = Mathf.Clamp01(snapshot.bgmVolume);
        sfxVolume = Mathf.Clamp01(snapshot.sfxVolume);
        if (TryGetSoundComponent(out var soundComponent))
        {
            var musicGroup = soundComponent.GetSoundGroup(Const.SoundGroup.Music.ToString());
            if (musicGroup != null)
            {
                musicGroup.Volume = bgmVolume;
            }

            var sfxGroup = soundComponent.GetSoundGroup(Const.SoundGroup.Sound.ToString());
            if (sfxGroup != null)
            {
                sfxGroup.Volume = sfxVolume;
            }
        }

        GFTrace.Info("TotemAudio", "Settings.Applied", null, GFTrace.Data(
            "bgm", bgmVolume.ToString("F2"),
            "sfx", sfxVolume.ToString("F2")));
    }

    private void OnDamageResolved(TotemDamageRecord record)
    {
        if (record.Target == null)
        {
            return;
        }

        if (TotemActorService.IsEnemy(record.Target))
        {
            PlaySfxCue(ResolveHitCue(record), record.Target.Position, $"Damage.{record.Reason}");
            if (record.Killed)
            {
                PlaySfxCue(KillCueId, record.Target.Position, $"Killed.{record.Reason}");
            }

            return;
        }

        if (record.Target.Kind == TotemActorKind.Player && record.Killed)
        {
            PlaySfxCue(PlayerDiedCueId, record.Target.Position, $"PlayerDied.{record.Reason}");
        }
    }

    private string ResolveHitCue(TotemDamageRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Reason) &&
            record.Reason.IndexOf("Skill", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SkillCastCueId;
        }

        if (weaponService != null && record.Source != null && record.Source.Kind == TotemActorKind.Player)
        {
            var state = weaponService.GetOrCreateState(record.Source);
            switch (state?.Weapon?.Class ?? TotemWeaponClass.Melee)
            {
                case TotemWeaponClass.Melee:
                    return HitMeleeCueId;
                case TotemWeaponClass.Ranged:
                    return HitRangedCueId;
                case TotemWeaponClass.Special:
                    return HitSpecialCueId;
            }
        }

        return HitDefaultCueId;
    }

    private void ObserveBossPhaseBgm()
    {
        if (flowService == null || flowService.CurrentState != TotemGameFlowState.CombatHud || bossService == null)
        {
            return;
        }

        int phaseIndex = bossService.CurrentPhase;
        string cueId = ResolveBossPhaseCue(phaseIndex);
        if (phaseIndex <= 0 || string.IsNullOrWhiteSpace(cueId))
        {
            return;
        }

        if (phaseIndex == observedBossPhase && string.Equals(cueId, observedBossBgmCueId, StringComparison.Ordinal))
        {
            return;
        }

        observedBossPhase = phaseIndex;
        observedBossBgmCueId = cueId;
        PlayBgmCue(cueId, $"Boss.Phase{phaseIndex}");
    }

    private string ResolveBossPhaseCue(int phaseIndex)
    {
        var phases = bossService?.GetRuntimePhases();
        if (phases == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            if (phase != null && phase.PhaseIndex == phaseIndex)
            {
                return phase.PhaseBGMCueId ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool TryGetSoundComponent(out SoundComponent soundComponent)
    {
        try
        {
            soundComponent = GF.Sound;
            return soundComponent != null;
        }
        catch (Exception)
        {
            soundComponent = null;
            return false;
        }
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }
}
