using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A lightweight, asset-independent visual breakup for opaque static props.
/// Gameplay owns the decision to call PlayDestruction; this component never reads input.
/// </summary>
[DisallowMultipleComponent]
public sealed class TotemVisualDestruction : MonoBehaviour
{
    private static readonly int ProgressId = Shader.PropertyToID("_DestructionProgress");
    private static readonly int HitPointId = Shader.PropertyToID("_HitPointOS");
    private static readonly int StrengthId = Shader.PropertyToID("_DestructionStrength");
    private static readonly int SeedId = Shader.PropertyToID("_DestructionSeed");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public enum MaterialKind
    {
        Ceramic,
        Wood,
        Stone,
        Metal,
    }

    [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();
    [SerializeField] private MaterialKind materialKind = MaterialKind.Ceramic;
    [SerializeField, Min(0.05f)] private float duration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float shardAmount = 0.35f;
    [SerializeField] private bool disableGameObjectOnComplete;
    [Tooltip("Restores the source materials and renderers after the effect so this prop can be previewed again.")]
    [SerializeField] private bool autoResetAfterDestruction;

    [Header("Editor Preview")]
    [Tooltip("Only used in the Unity Editor to preview this component when Play Mode begins.")]
    [SerializeField] private bool previewOnPlayModeStart;
    [Tooltip("Only used in the Unity Editor. Press Space in Play Mode to trigger another preview after the current effect finishes.")]
    [SerializeField] private bool enableSpacebarTest;

    private RendererState[] rendererStates = Array.Empty<RendererState>();
    private MaterialPropertyBlock propertyBlock;
    private ParticleSystem debrisParticles;
    private Material debrisMaterial;
    private Coroutine playback;
    private bool initialized;

    public bool IsPlaying => playback != null;

    public event Action<TotemVisualDestruction> Completed;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (previewOnPlayModeStart)
        {
            PreviewVisualDestruction();
        }
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (enableSpacebarTest && !IsPlaying && TotemUnityInputProvider.Instance.GetKeyDown(KeyCode.Space))
        {
            PreviewVisualDestruction();
        }
#endif
    }

    private void OnDisable()
    {
        if (playback != null)
        {
            StopCoroutine(playback);
            playback = null;
        }

        RestoreSourceMaterials();
    }

    private void OnDestroy()
    {
        if (debrisMaterial != null)
        {
            Destroy(debrisMaterial);
            debrisMaterial = null;
        }
    }

    /// <summary>Starts a one-shot visual breakup at the supplied world-space hit point.</summary>
    public bool PlayDestruction(Vector3 worldHitPoint, float strength = 1f)
    {
        if (IsPlaying || !Initialize())
        {
            return false;
        }

        if (!TryApplyDestructionMaterials())
        {
            CompletePlayback();
            return false;
        }

        SpawnDebris(worldHitPoint, Mathf.Clamp01(strength));
        playback = StartCoroutine(PlayRoutine(worldHitPoint, Mathf.Clamp01(strength)));
        return true;
    }

    [ContextMenu("Preview Visual Destruction")]
    private void PreviewVisualDestruction()
    {
        PlayDestruction(transform.position + transform.up * 0.5f, 1f);
    }

    private bool Initialize()
    {
        if (initialized)
        {
            return rendererStates.Length > 0;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        int validCount = 0;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (IsSupported(targetRenderers[i]))
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            Debug.LogWarning($"[{nameof(TotemVisualDestruction)}] No supported opaque Renderer on '{name}'.", this);
            return false;
        }

        rendererStates = new RendererState[validCount];
        int index = 0;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (!IsSupported(renderer))
            {
                continue;
            }

            rendererStates[index++] = new RendererState(renderer, renderer.sharedMaterials);
        }

        propertyBlock = new MaterialPropertyBlock();
        initialized = true;
        return true;
    }

    private static bool IsSupported(Renderer renderer)
    {
        if (renderer == null || renderer is SkinnedMeshRenderer)
        {
            return false;
        }

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null || materials[i].renderQueue > 2500)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryApplyDestructionMaterials()
    {
        Shader destructionShader = Shader.Find("Totem/Visual Destruction");
        if (destructionShader == null)
        {
            Debug.LogError("[TotemVisualDestruction] Shader 'Totem/Visual Destruction' was not found.", this);
            return false;
        }

        for (int stateIndex = 0; stateIndex < rendererStates.Length; stateIndex++)
        {
            RendererState state = rendererStates[stateIndex];
            Material[] sourceMaterials = state.SourceMaterials;
            Material[] effectMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                effectMaterials[materialIndex] = CreateEffectMaterial(sourceMaterials[materialIndex], destructionShader);
            }

            state.EffectMaterials = effectMaterials;
            state.Renderer.sharedMaterials = effectMaterials;
            rendererStates[stateIndex] = state;
        }

        return true;
    }

    private static Material CreateEffectMaterial(Material source, Shader destructionShader)
    {
        Material material = new Material(destructionShader)
        {
            name = $"{source.name} (VisualDestruction)",
        };

        Texture baseMap = source.HasProperty(BaseMapId) ? source.GetTexture(BaseMapId) : source.GetTexture(MainTexId);
        Color baseColor = source.HasProperty(BaseColorId) ? source.GetColor(BaseColorId) : source.GetColor(ColorId);
        material.SetTexture(BaseMapId, baseMap);
        material.SetColor(BaseColorId, baseColor);
        return material;
    }

    private IEnumerator PlayRoutine(Vector3 worldHitPoint, float strength)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, duration);
        float seed = UnityEngine.Random.value;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / safeDuration);
            ApplyShaderParameters(worldHitPoint, progress, strength, seed);
            yield return null;
        }

        CompletePlayback();
    }

    private void ApplyShaderParameters(Vector3 worldHitPoint, float progress, float strength, float seed)
    {
        for (int i = 0; i < rendererStates.Length; i++)
        {
            Renderer renderer = rendererStates[i].Renderer;
            if (renderer == null)
            {
                continue;
            }

            propertyBlock.Clear();
            propertyBlock.SetFloat(ProgressId, progress);
            propertyBlock.SetVector(HitPointId, renderer.transform.InverseTransformPoint(worldHitPoint));
            propertyBlock.SetFloat(StrengthId, strength);
            propertyBlock.SetFloat(SeedId, seed);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void SpawnDebris(Vector3 position, float strength)
    {
        EnsureDebrisParticles();
        int count = Mathf.RoundToInt(Mathf.Lerp(8f, 24f, shardAmount * strength));
        var emit = new ParticleSystem.EmitParams
        {
            position = position,
            startColor = ResolveDebrisColor(),
            startSize = Mathf.Lerp(0.05f, 0.16f, strength),
            startLifetime = 0.55f,
            velocity = Vector3.zero,
        };
        debrisParticles.Emit(emit, count);
    }

    private void EnsureDebrisParticles()
    {
        if (debrisParticles != null)
        {
            return;
        }

        var root = new GameObject("[VisualDestruction Debris]");
        root.transform.SetParent(transform, false);
        debrisParticles = root.AddComponent<ParticleSystem>();
        var main = debrisParticles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;
        main.startSpeed = 3.2f;
        main.gravityModifier = 0.9f;
        var emission = debrisParticles.emission;
        emission.enabled = false;
        var shape = debrisParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.04f;

        var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Totem/Visual Destruction Particles");
        if (particleShader == null)
        {
            Debug.LogError("[TotemVisualDestruction] Particle shader was not found; debris particles are disabled.", this);
            particleRenderer.enabled = false;
            return;
        }

        debrisMaterial = new Material(particleShader)
        {
            name = "[Runtime] Visual Destruction Particles",
        };
        particleRenderer.sharedMaterial = debrisMaterial;
    }

    private Color ResolveDebrisColor()
    {
        switch (materialKind)
        {
            case MaterialKind.Wood: return new Color(0.48f, 0.25f, 0.08f, 1f);
            case MaterialKind.Stone: return new Color(0.46f, 0.46f, 0.43f, 1f);
            case MaterialKind.Metal: return new Color(0.57f, 0.62f, 0.66f, 1f);
            default: return new Color(0.84f, 0.80f, 0.68f, 1f);
        }
    }

    private void CompletePlayback()
    {
        RestoreSourceMaterials();
        for (int i = 0; i < rendererStates.Length; i++)
        {
            if (rendererStates[i].Renderer != null)
            {
                rendererStates[i].Renderer.enabled = autoResetAfterDestruction;
            }
        }

        playback = null;
        Completed?.Invoke(this);
        if (!autoResetAfterDestruction && disableGameObjectOnComplete)
        {
            gameObject.SetActive(false);
        }
    }

    private void RestoreSourceMaterials()
    {
        for (int i = 0; i < rendererStates.Length; i++)
        {
            RendererState state = rendererStates[i];
            if (state.Renderer != null && state.SourceMaterials != null)
            {
                state.Renderer.sharedMaterials = state.SourceMaterials;
                state.Renderer.SetPropertyBlock(null);
            }

            if (state.EffectMaterials == null)
            {
                continue;
            }

            for (int materialIndex = 0; materialIndex < state.EffectMaterials.Length; materialIndex++)
            {
                if (state.EffectMaterials[materialIndex] != null)
                {
                    Destroy(state.EffectMaterials[materialIndex]);
                }
            }

            state.EffectMaterials = null;
            rendererStates[i] = state;
        }
    }

    private struct RendererState
    {
        public RendererState(Renderer renderer, Material[] sourceMaterials)
        {
            Renderer = renderer;
            SourceMaterials = sourceMaterials;
            EffectMaterials = null;
        }

        public Renderer Renderer;
        public Material[] SourceMaterials;
        public Material[] EffectMaterials;
    }
}
