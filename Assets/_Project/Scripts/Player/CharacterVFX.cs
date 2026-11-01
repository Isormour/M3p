using M3P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pushes status outline parameters onto the character's skinned meshes
/// and keeps the burn and heal particles emitting while those statuses are active.
/// </summary>
[DisallowMultipleComponent]
public class CharacterVFX : MonoBehaviour
{
    const int StatusEmissionMinStacks = 1;
    const int StatusEmissionMaxStacks = 10;
    const float StatusEmissionMinRate = 20f;
    const float StatusEmissionMaxRate = 200f;

    static readonly int OutlineSizeId = Shader.PropertyToID("_OutlineSize");
    static readonly int OutlineMultNPowerId = Shader.PropertyToID("_OutlineMultNPower");
    static readonly int OutlineMidColorId = Shader.PropertyToID("_OutlineMidColor");
    static readonly int OutlineHighColorId = Shader.PropertyToID("_OutlineHighColor");

    [SerializeField] SkinnedMeshRenderer[] renderers;
    [SerializeField] ParticleSystem flameParticle;
    [SerializeField] ParticleSystem healParticle;
    [SerializeField] ParticleSystem burnStatusParticle;
    [SerializeField] ParticleSystem healStatusParticle;
    MaterialPropertyBlock _block;
    EStatusType _activeStatus;
    bool _hasApplied;
    bool _statusParticlesDriven;

    public EStatusType ActiveStatus => _activeStatus;

    void Reset()
    {
        CollectRenderers();
    }

    void OnValidate()
    {
        if (renderers == null || renderers.Length == 0)
            CollectRenderers();
    }

    void Awake()
    {
        _block = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            CollectRenderers();

        ApplyStatus(EStatusType.None, force: true);
    }

    void Start()
    {
        if (_activeStatus == EStatusType.None)
            ApplyStatus(EStatusType.None, force: true);

        if (!_statusParticlesDriven)
            ClearStatusParticles();
    }

    [ContextMenu("Collect Renderers")]
    public void CollectRenderers()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    /// <summary>
    /// Applies the palette for the last active status. No status uses the authored None look.
    /// Burn and heal emission follow their stacks even when another status owns the outline.
    /// </summary>
    public void Refresh(IReadOnlyList<StatusInstance> statuses)
    {
        ApplyStatus(ResolveVisibleStatus(statuses));
        _statusParticlesDriven = true;
        SetStatusParticle(burnStatusParticle, CountStacks(statuses, EStatusType.Burn));
        SetStatusParticle(healStatusParticle, CountHealStacks(statuses));
    }

    public void Clear()
    {
        ApplyStatus(EStatusType.None);
        ClearStatusParticles();
    }

    /// <summary>Restarts the authored flame burst. No-ops when the particle is unassigned.</summary>
    public void PlayFlame()
    {
        PlayBurst(flameParticle);
    }

    /// <summary>Restarts the authored heal burst. No-ops when the particle is unassigned.</summary>
    public void PlayHeal()
    {
        PlayBurst(healParticle);
    }

    void ClearStatusParticles()
    {
        _statusParticlesDriven = true;
        SetStatusParticle(burnStatusParticle, 0);
        SetStatusParticle(healStatusParticle, 0);
    }

    /// <summary>
    /// 1 stack emits at 20 particles per second, 10 stacks at 200. Values outside that range clamp.
    /// </summary>
    void SetStatusParticle(ParticleSystem particle, int stacks)
    {
        if (particle == null)
            return;

        stacks = Mathf.Max(0, stacks);
        if (stacks <= 0)
        {
            if (particle.isPlaying)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return;
        }

        ParticleSystem.MainModule main = particle.main;
        main.loop = true;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.enabled = true;
        emission.rateOverTime = StatusEmissionRate(stacks);

        if (particle.isPlaying)
            return;

        BindBodyShape(particle);
        particle.Play();
    }

    static int CountStacks(IReadOnlyList<StatusInstance> statuses, EStatusType statusType)
    {
        if (statuses == null || statusType == EStatusType.None)
            return 0;

        int stacks = 0;
        for (int i = 0; i < statuses.Count; i++)
        {
            StatusInstance status = statuses[i];
            StatusEffectDefinition definition = status?.Definition;
            if (definition == null || definition.StatusType != statusType)
                continue;

            stacks += Mathf.Max(0, status.Stacks);
        }

        return stacks;
    }

    static int CountHealStacks(IReadOnlyList<StatusInstance> statuses)
    {
        if (statuses == null)
            return 0;

        int stacks = 0;
        for (int i = 0; i < statuses.Count; i++)
        {
            StatusInstance status = statuses[i];
            StatusEffectDefinition definition = status?.Definition;
            if (definition == null || !HealsOverTime(definition))
                continue;

            stacks += Mathf.Max(0, status.Stacks);
        }

        return stacks;
    }

    static bool HealsOverTime(StatusEffectDefinition definition)
    {
        BattleEffect[] effects = definition.OnTurnEffects;
        for (int i = 0; i < effects.Length; i++)
        {
            BattleEffectLogic logic = effects[i] != null ? effects[i].Logic : null;
            if (logic is HealLogic || logic is FloorScaledHealLogic)
                return true;
        }

        return false;
    }

    static float StatusEmissionRate(int stacks)
    {
        float t = Mathf.InverseLerp(StatusEmissionMinStacks, StatusEmissionMaxStacks, stacks);
        return Mathf.Lerp(StatusEmissionMinRate, StatusEmissionMaxRate, t);
    }

    void PlayBurst(ParticleSystem particle)
    {
        if (particle == null)
            return;

        BindBodyShape(particle);
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }

    void BindBodyShape(ParticleSystem particle)
    {
        SkinnedMeshRenderer mesh = ResolveBodyMesh();
        if (mesh == null)
            return;

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
        shape.skinnedMeshRenderer = mesh;
    }

    SkinnedMeshRenderer ResolveBodyMesh()
    {
        if (renderers == null || renderers.Length == 0)
            CollectRenderers();

        if (renderers == null)
            return null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                return renderers[i];
        }

        return null;
    }

    void ApplyStatus(EStatusType statusType, bool force = false)
    {
        if (!force && _hasApplied && statusType == _activeStatus)
            return;

        VFXConfig config = ResolveConfig();
        StatusVFXParams parameters = config != null
            ? config.GetParams(statusType)
            : StatusVFXParams.Off;

        Apply(statusType, parameters);
    }

    void Apply(EStatusType statusType, StatusVFXParams parameters)
    {
        _activeStatus = statusType;
        _hasApplied = true;

        if (renderers == null || renderers.Length == 0)
            CollectRenderers();

        if (_block == null)
            _block = new MaterialPropertyBlock();

        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_block);
            _block.SetFloat(OutlineSizeId, parameters.OutlineSize);
            _block.SetVector(OutlineMultNPowerId, parameters.OutlineMultNPower);
            _block.SetColor(OutlineMidColorId, parameters.OutlineMidColor);
            _block.SetColor(OutlineHighColorId, parameters.OutlineHighColor);
            renderer.SetPropertyBlock(_block);
        }
    }

    static EStatusType ResolveVisibleStatus(IReadOnlyList<StatusInstance> statuses)
    {
        if (statuses == null)
            return EStatusType.None;

        VFXConfig config = ResolveConfig();
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectDefinition definition = statuses[i] != null ? statuses[i].Definition : null;
            if (definition == null || definition.StatusType == EStatusType.None)
                continue;

            if (config == null || config.TryGetParams(definition.StatusType, out _))
                return definition.StatusType;
        }

        return EStatusType.None;
    }

    static VFXConfig ResolveConfig()
    {
        GameConfig gameConfig = GameManager.Instance != null ? GameManager.Instance.Config : null;
        return gameConfig != null ? gameConfig.VFX : null;
    }
}
