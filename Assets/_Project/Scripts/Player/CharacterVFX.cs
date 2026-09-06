using System.Collections.Generic;
using M3P;
using UnityEngine;

/// <summary>
/// Pushes status outline parameters onto the character's skinned meshes.
/// </summary>
[DisallowMultipleComponent]
public class CharacterVFX : MonoBehaviour
{
    static readonly int OutlineSizeId = Shader.PropertyToID("_OutlineSize");
    static readonly int OutlineMultNPowerId = Shader.PropertyToID("_OutlineMultNPower");
    static readonly int OutlineMidColorId = Shader.PropertyToID("_OutlineMidColor");
    static readonly int OutlineHighColorId = Shader.PropertyToID("_OutlineHighColor");

    [SerializeField] SkinnedMeshRenderer[] renderers;

    MaterialPropertyBlock _block;
    EStatusType _activeStatus;
    bool _hasApplied;

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
    }

    [ContextMenu("Collect Renderers")]
    public void CollectRenderers()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    /// <summary>
    /// Applies the palette for the last active status. No status uses the authored None look.
    /// </summary>
    public void Refresh(IReadOnlyList<StatusInstance> statuses)
    {
        ApplyStatus(ResolveVisibleStatus(statuses));
    }

    public void Clear()
    {
        ApplyStatus(EStatusType.None);
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
