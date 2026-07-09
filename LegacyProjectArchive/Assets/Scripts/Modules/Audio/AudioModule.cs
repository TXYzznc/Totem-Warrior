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

    // change #22 子项 D：BGM 播放通道 + Clip 缓存
    GameObject _bgmHost;
    AudioSource _bgmSource;
    string _currentBgm;
    readonly System.Collections.Generic.Dictionary<string, AudioClip> _clipCache
        = new System.Collections.Generic.Dictionary<string, AudioClip>();

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

        // change #22 子项 D：BGM 载体（跨场景常驻）
        _bgmHost = new GameObject("[AudioModule.BgmHost]");
        UnityEngine.Object.DontDestroyOnLoad(_bgmHost);
        _bgmSource = _bgmHost.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.spatialBlend = 0f; // 2D
        _bgmSource.volume = _bgmVolume;

        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        if (_bgmHost != null)
        {
            UnityEngine.Object.Destroy(_bgmHost);
            _bgmHost = null;
            _bgmSource = null;
        }
        _clipCache.Clear();
        FrameworkLogger.Info("AudioModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 一次性 SFX：从 Resources 加载 clip 后在 position 播一次。
    /// 找不到 clip 时 Warn 兜底，不阻塞流程。
    /// </summary>
    public void PlayOneShot(string clipPath, Vector3 position, float volume = 1f)
    {
        if (string.IsNullOrEmpty(clipPath)) return;
        var clip = LoadClip(clipPath);
        if (clip == null)
        {
            FrameworkLogger.Warn("AudioModule", $"Action=PlayOneShot Clip={clipPath} 未找到");
            return;
        }
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume) * _sfxVolume);
    }

    /// <summary>
    /// BGM 切换。相同 clipPath 幂等（不重播）。fadeSec 目前只用作占位，直接切歌。
    /// </summary>
    public void PlayBgm(string clipPath, bool loop = true, float fadeSec = 0.5f)
    {
        if (_bgmSource == null) return;
        if (string.IsNullOrEmpty(clipPath))
        {
            _bgmSource.Stop();
            _currentBgm = null;
            return;
        }
        if (_currentBgm == clipPath && _bgmSource.isPlaying) return;

        var clip = LoadClip(clipPath);
        if (clip == null)
        {
            FrameworkLogger.Warn("AudioModule", $"Action=PlayBgm Clip={clipPath} 未找到");
            return;
        }
        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.volume = _bgmVolume;
        _bgmSource.Play();
        _currentBgm = clipPath;
    }

    AudioClip LoadClip(string path)
    {
        if (_clipCache.TryGetValue(path, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>(path);
        _clipCache[path] = clip; // 记录 null 也算，避免反复 Load
        return clip;
    }

    /// <summary>设置 BGM 音量（0.0~1.0）。</summary>
    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
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
