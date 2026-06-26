using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频模块（v1.0）：BGM / SFX 双通道音量控制。
///
/// 资源约定：
///   AudioMixer Asset：Assets/Resources/Audio/MainMixer.mixer
///   Exposed Parameters：
///     - "BgmVolume"（控制 BGM Group attenuation，dB）
///     - "SfxVolume"（控制 SFX Group attenuation，dB）
///
/// 缺失资源时降级到 AudioListener.volume（保证不崩，但通道无法独立控制）。
/// 公共 API（SettingsModule 消费）：
///   SetBgmVolume(0~1) / SetSfxVolume(0~1)
///   GetBgmVolume() / GetSfxVolume()
///
/// 不持有 AudioSource；播放语义留给后续 BgmPlayer / SfxPlayer 等具体模块。
/// </summary>
public sealed class AudioModule : IGameModule
{
    public int ModuleCategory => 1;
    public Type[] Dependencies => new[] { typeof(ResourceModule) };

    const string MixerResourcePath = "Audio/MainMixer";
    const string BgmExposedParam   = "BgmVolume";
    const string SfxExposedParam   = "SfxVolume";

    readonly ModuleRunner _runner;
    AudioMixer _mixer;
    float _bgmVolume = 1f;
    float _sfxVolume = 1f;

    public AudioModule(ModuleRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (_mixer == null)
            FrameworkLogger.Warn("AudioModule",
                $"Action=Init Mixer={MixerResourcePath} 未找到，回退到 AudioListener.volume");
        else
            FrameworkLogger.Info("AudioModule", $"Action=Init Mixer={MixerResourcePath}");

        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("AudioModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>设置 BGM 音量（0.0~1.0）。</summary>
    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        if (_mixer != null) _mixer.SetFloat(BgmExposedParam, LinearToDb(_bgmVolume));
        else SyncListenerFallback();
    }

    /// <summary>设置 SFX 音量（0.0~1.0）。</summary>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        if (_mixer != null) _mixer.SetFloat(SfxExposedParam, LinearToDb(_sfxVolume));
        else SyncListenerFallback();
    }

    public float GetBgmVolume() => _bgmVolume;
    public float GetSfxVolume() => _sfxVolume;

    /// <summary>Mixer 缺失时降级：用 BGM/SFX 均值控制全局 AudioListener。</summary>
    void SyncListenerFallback()
    {
        AudioListener.volume = (_bgmVolume + _sfxVolume) * 0.5f;
    }

    /// <summary>0.0~1.0 线性 → dB。0 映射到 -80dB（静音）。</summary>
    static float LinearToDb(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
