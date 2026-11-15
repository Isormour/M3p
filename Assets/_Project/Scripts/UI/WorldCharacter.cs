using M3P;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum ECharacterType
{
    Ground,
    Flying
}

[RequireComponent(typeof(CharacterVFX))]
public class WorldCharacter : MonoBehaviour
{
    static readonly int AttackVariantsId = Animator.StringToHash("AttackVariants");
    static readonly int BasicAttackId = Animator.StringToHash("BasicAttack");
    static readonly int GuardUpId = Animator.StringToHash("GuardUp");
    static readonly int GetHitId = Animator.StringToHash("GetHit");
    static readonly int DieId = Animator.StringToHash("Die");
    static readonly int[] ActionTriggerIds = { BasicAttackId, GuardUpId };

    [SerializeField] Animator _animator;
    [SerializeField] int _attackVariantCount = 5;
    [SerializeField] CharacterVFX _vfx;
    [SerializeField] BattleCharacterShield _shieldIndicator;

    [Tooltip("Renderers receiving rank textures (all material slots). Empty auto-selects slots sharing the primary body texture.")]
    [SerializeField] Renderer[] _rankTextureRenderers;

    public void ApplyMainTexture(Texture2D texture)
    {
        if (texture == null)
            return;

        Renderer[] targets = ResolveBodyRenderers(out Material primary, out bool explicitTargets);
        if (!explicitTargets && primary == null)
            return;

        var block = new MaterialPropertyBlock();
        foreach (Renderer target in targets)
        {
            if (target == null || (!(target is MeshRenderer) && !(target is SkinnedMeshRenderer)))
                continue;
            Material[] materials = target.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
            {
                Material material = materials[slot];
                if (material == null || (!explicitTargets && !SharesBodyTexture(material, primary)))
                    continue;
                ApplySlotTexture(target, slot, material, texture, block);
            }
        }
    }

    /// <summary>Add a rank's elite/boss layer without replacing the body's original materials.</summary>
    public void ApplyAdditionalBodyMaterial(Material material, IReadOnlyList<EnemyMaterialTexture> materialTextures = null)
    {
        if (material == null)
            return;

        Renderer[] targets = ResolveBodyRenderers(out Material primary, out bool explicitTargets);
        foreach (Renderer target in targets)
        {
            if (!(target is MeshRenderer) && !(target is SkinnedMeshRenderer))
                continue;

            Material[] materials = target.sharedMaterials;
            if (System.Array.IndexOf(materials, material) >= 0)
                continue;

            bool isBody = explicitTargets;
            foreach (Material existing in materials)
            {
                if (SharesBodyTexture(existing, primary))
                    isBody = true;
                if (existing == null || materialTextures == null)
                    continue;
                for (int i = 0; i < materialTextures.Count; i++)
                    if (existing == materialTextures[i].Material)
                        isBody = true;
            }
            if (!isBody)
                continue;

            System.Array.Resize(ref materials, materials.Length + 1);
            materials[materials.Length - 1] = material;
            target.sharedMaterials = materials;
        }
    }

    Renderer[] ResolveBodyRenderers(out Material primary, out bool explicitTargets)
    {
        explicitTargets = _rankTextureRenderers != null && _rankTextureRenderers.Length > 0;
        Renderer[] targets = explicitTargets ? _rankTextureRenderers : GetComponentsInChildren<Renderer>(true);
        primary = null;
        if (explicitTargets)
            return targets;

        foreach (Renderer target in targets)
            if (target is SkinnedMeshRenderer && target.sharedMaterial != null)
            {
                primary = target.sharedMaterial;
                return targets;
            }
        foreach (Renderer target in targets)
            if (target is MeshRenderer && target.sharedMaterial != null)
            {
                primary = target.sharedMaterial;
                break;
            }
        return targets;
    }

    static bool SharesBodyTexture(Material material, Material primary)
    {
        return material != null && primary != null &&
            (material == primary || (primary.mainTexture != null && material.mainTexture == primary.mainTexture));
    }

    /// <summary>Apply the legacy body texture, then explicit per-material rank overrides.</summary>
    public void ApplyRankTextures(Texture2D mainTexture, IReadOnlyList<EnemyMaterialTexture> materialTextures)
    {
        ApplyMainTexture(mainTexture);
        if (materialTextures == null || materialTextures.Count == 0)
            return;

        var block = new MaterialPropertyBlock();
        foreach (Renderer target in GetComponentsInChildren<Renderer>(true))
        {
            if (!(target is MeshRenderer) && !(target is SkinnedMeshRenderer))
                continue;
            Material[] materials = target.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
                for (int i = 0; i < materialTextures.Count; i++)
                {
                    EnemyMaterialTexture entry = materialTextures[i];
                    if (entry.Material == null || entry.Texture == null || materials[slot] != entry.Material)
                        continue;
                    ApplySlotTexture(target, slot, materials[slot], entry.Texture, block);
                }
        }
    }

    static void ApplySlotTexture(Renderer target, int slot, Material material, Texture2D texture, MaterialPropertyBlock block)
    {
        block.Clear();
        target.GetPropertyBlock(block, slot);
        // Preserve renderer-wide effects when introducing a per-material block.
        if (block.isEmpty)
            target.GetPropertyBlock(block);
        bool supported = false;
        for (int p = 0; p < material.shader.GetPropertyCount(); p++)
        {
            if ((material.shader.GetPropertyFlags(p) & UnityEngine.Rendering.ShaderPropertyFlags.MainTexture) == 0)
                continue;
            block.SetTexture(material.shader.GetPropertyNameId(p), texture);
            supported = true;
        }
        // CharacterMaterial Shader Graph uses this custom reference name.
        if (material.HasProperty("_MainTexture"))
            block.SetTexture("_MainTexture", texture);
        if (!supported && material.HasProperty("_BaseMap"))
            block.SetTexture("_BaseMap", texture);
        else if (!supported && material.HasProperty("_MainTex"))
            block.SetTexture("_MainTex", texture);
        target.SetPropertyBlock(block, slot);
    }

    [Tooltip("Hands (or weapon tips) that gather energy and throw attacks. Leave empty to use the hand bones of a humanoid rig.")]
    [SerializeField] Transform[] _attackOrigins;

    [Tooltip("Hand used as the starting position of skill timeline particles. Assign the casting hand bone or an attachment point.")]
    [SerializeField] Transform hand;

    [field: SerializeField] public ECharacterType CharacterType { private set; get; } = ECharacterType.Ground;

    Transform[] _resolvedAttackOrigins;
    PlayableDirector _activeTimeline;

    public Animator Anim => _animator;

    public Transform Hand => hand;

    public CharacterVFX VFX => _vfx;

    public bool HasShield => _shieldIndicator != null && _shieldIndicator.IsVisible;

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        EnsureVfx();
        EnsureShield();
        _vfx?.CollectRenderers();
    }

    void Reset()
    {
        EnsureVfx();
        EnsureShield();
        _vfx?.CollectRenderers();
    }

    void OnValidate()
    {
        EnsureVfx();
        EnsureShield();
    }

    void EnsureVfx()
    {
        if (_vfx == null)
            _vfx = GetComponent<CharacterVFX>();
        if (_vfx == null)
            _vfx = GetComponentInChildren<CharacterVFX>(true);
    }

    void EnsureShield()
    {
        if (_shieldIndicator == null)
            _shieldIndicator = GetComponentInChildren<BattleCharacterShield>(true);
    }

    public void BindShield(SoftStats stats)
    {
        EnsureShield();
        _shieldIndicator?.Bind(stats);
    }

    /// <summary>
    /// Where the <paramref name="index"/>-th attack of a flurry comes from. Successive indices cycle
    /// through the available hands, so a series of swings alternates instead of repeating one arm.
    /// </summary>
    public Transform GetAttackOrigin(int index)
    {
        Transform[] origins = ResolveAttackOrigins();
        if (origins.Length == 0)
            return transform;

        int wrapped = index % origins.Length;
        if (wrapped < 0)
            wrapped += origins.Length;

        return origins[wrapped];
    }

    Transform[] ResolveAttackOrigins()
    {
        if (_resolvedAttackOrigins != null)
            return _resolvedAttackOrigins;

        List<Transform> origins = new List<Transform>(2);

        if (_attackOrigins != null)
        {
            for (int i = 0; i < _attackOrigins.Length; i++)
            {
                if (_attackOrigins[i] != null)
                    origins.Add(_attackOrigins[i]);
            }
        }

        if (origins.Count == 0 && _animator != null && _animator.isHuman)
        {
            AddBoneOrigin(origins, HumanBodyBones.RightHand);
            AddBoneOrigin(origins, HumanBodyBones.LeftHand);
        }

        if (origins.Count == 0)
            origins.Add(transform);

        _resolvedAttackOrigins = origins.ToArray();
        return _resolvedAttackOrigins;
    }

    void AddBoneOrigin(List<Transform> origins, HumanBodyBones bone)
    {
        Transform resolved = _animator.GetBoneTransform(bone);
        if (resolved != null)
            origins.Add(resolved);
    }

    /// <summary>
    /// Plays the skill timeline on this character. Player and enemies share this path.
    /// An activation track enables its particle object for the clip, then disables it.
    /// A track named like a particle already on this character toggles that particle.
    /// </summary>
    public void PlaySkill(SkillDefinition skill)
    {
        PlayableDirector source = skill != null ? skill.PlayableDirector : null;
        bool playedTimeline = PlayTimeline(source);
        if (playedTimeline && TimelineDrivesAnimator(source))
            return;

        string triggerName = skill != null && !string.IsNullOrEmpty(skill._animationName)
            ? skill._animationName
            : "BasicAttack";
        PlayAttack(triggerName);
    }

    /// <summary>
    /// Stores a projectile on the playing timeline. The Projectile clip launches it once the clip
    /// has started. Returns false when this character has no timeline with a Projectile clip.
    /// </summary>
    public bool ArmProjectile(SkillProjectileRequest request)
    {
        if (request == null || _activeTimeline == null)
            return false;

        if (!ProjectileTrack.ContainsClip(_activeTimeline.playableAsset))
            return false;

        SkillProjectileBinding binding = _activeTimeline.GetComponent<SkillProjectileBinding>();
        if (binding == null)
            binding = _activeTimeline.gameObject.AddComponent<SkillProjectileBinding>();

        binding.Request = request;
        return true;
    }

    public void StopSkillTimeline()
    {
        PlayableDirector director = _activeTimeline;
        if (director == null)
            return;

        _activeTimeline = null;
        director.stopped -= HandleTimelineStopped;
        if (director.state == PlayState.Playing)
            director.Stop();
        Destroy(director.gameObject);
    }

    public void PlayAttack(string triggerName = "BasicAttack", int variantCount = -1)
    {
        if (_animator == null)
            return;

        if (string.IsNullOrEmpty(triggerName))
            triggerName = "BasicAttack";

        bool isBasicAttack = triggerName == "BasicAttack";
        if (isBasicAttack)
        {
            int count = variantCount >= 0 ? variantCount : _attackVariantCount;
            if (count > 0)
                _animator.SetFloat(AttackVariantsId, Random.Range(0, count) / (float)count);
        }

        for (int i = 0; i < ActionTriggerIds.Length; i++)
            _animator.ResetTrigger(ActionTriggerIds[i]);

        _animator.SetTrigger(triggerName);
    }

    public void PlayGetHit()
    {
        if (HasShield)
        {
            _shieldIndicator.Pulse();
            return;
        }

        if (_animator == null)
            return;

        _animator.SetTrigger(GetHitId);
    }

    public void PlayDie()
    {
        StopSkillTimeline();
        _shieldIndicator?.Unbind();

        if (_animator != null)
        {
            _animator.ResetTrigger(GetHitId);
            _animator.SetTrigger(DieId);
        }

        var flying = GetComponent<FlyingCharacterAnimation>();
        if (flying == null)
            flying = GetComponentInChildren<FlyingCharacterAnimation>(true);
        flying?.Die();
    }

    public void PlayHitReaction(bool died, bool shielded = false)
    {
        if (died)
        {
            PlayDie();
            return;
        }

        if (HasShield)
        {
            _shieldIndicator.Pulse();
            return;
        }

        PlayGetHit();
    }

    void OnDestroy()
    {
        if (_activeTimeline == null)
            return;

        _activeTimeline.stopped -= HandleTimelineStopped;
        _activeTimeline = null;
    }

    bool PlayTimeline(PlayableDirector source)
    {
        if (source == null || source.playableAsset == null)
            return false;

        if (BelongsToCharacter(source.transform))
        {
            Debug.LogError(
                $"Skill timeline '{source.name}' is part of a character. Assign a separate timeline prefab.",
                source);
            return false;
        }

        StopSkillTimeline();

        GameObject instance = Instantiate(source.gameObject, transform);
        instance.transform.localPosition = source.transform.localPosition;
        instance.transform.localRotation = source.transform.localRotation;
        instance.transform.localScale = source.transform.localScale;

        PlayableDirector director = instance.GetComponent<PlayableDirector>();
        if (director == null)
        {
            Destroy(instance);
            return false;
        }

        director.playOnAwake = false;
        if (director.state == PlayState.Playing)
            director.Stop();

        if (instance.GetComponent<SkillProjectileBinding>() == null)
            instance.AddComponent<SkillProjectileBinding>();

        BindSkillTimeline(director, instance.transform);
        director.RebuildGraph();
        director.time = 0;
        _activeTimeline = director;
        director.stopped += HandleTimelineStopped;
        director.Play();
        return true;
    }

    void HandleTimelineStopped(PlayableDirector director)
    {
        if (director != _activeTimeline)
            return;

        _activeTimeline = null;
        director.stopped -= HandleTimelineStopped;
        Destroy(director.gameObject);
    }

    void BindSkillTimeline(PlayableDirector director, Transform timelineRoot)
    {
        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        if (timeline == null)
            return;

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            AnimationTrack animation = track as AnimationTrack;
            if (animation != null)
            {
                if (_animator != null
                    && !animation.mutedInHierarchy
                    && AnimationTrackHasClip(animation)
                    && !IsInside(director.GetGenericBinding(animation), timelineRoot))
                    director.SetGenericBinding(animation, _animator);
                continue;
            }

            ActivationTrack activation = track as ActivationTrack;
            if (activation == null)
                continue;

            GameObject target = ResolveActivationTarget(director, activation, timelineRoot);
            if (target != null)
                director.SetGenericBinding(activation, target);
        }
    }

    GameObject ResolveActivationTarget(PlayableDirector director, ActivationTrack track, Transform timelineRoot)
    {
        GameObject bound = director.GetGenericBinding(track) as GameObject;
        if (IsInside(bound, timelineRoot))
            return bound;

        Transform onTimeline = FindNamed(timelineRoot, track.name, null);
        if (onTimeline != null)
            return onTimeline.gameObject;

        Transform onCharacter = FindNamed(transform, track.name, timelineRoot);
        if (onCharacter != null)
            return onCharacter.gameObject;

        return bound;
    }

    static bool TimelineDrivesAnimator(PlayableDirector source)
    {
        TimelineAsset timeline = source != null ? source.playableAsset as TimelineAsset : null;
        if (timeline == null)
            return false;

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            AnimationTrack animation = track as AnimationTrack;
            if (animation != null && !animation.mutedInHierarchy && AnimationTrackHasClip(animation))
                return true;
        }

        return false;
    }

    static bool AnimationTrackHasClip(AnimationTrack animation)
    {
        if (animation.infiniteClip != null)
            return true;

        foreach (TimelineClip clip in animation.GetClips())
        {
            AnimationPlayableAsset playable = clip.asset as AnimationPlayableAsset;
            if (playable != null && playable.clip != null)
                return true;
        }

        return false;
    }

    static bool BelongsToCharacter(Transform root)
    {
        Transform cursor = root;
        while (cursor != null)
        {
            if (cursor.GetComponent<WorldCharacter>() != null)
                return true;
            cursor = cursor.parent;
        }

        return false;
    }

    static Transform FindNamed(Transform root, string objectName, Transform skipSubtree)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == root)
                continue;
            if (skipSubtree != null && candidate.IsChildOf(skipSubtree))
                continue;
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    static bool IsInside(Object bound, Transform timelineRoot)
    {
        if (bound == null || timelineRoot == null)
            return false;

        Transform boundTransform = null;
        Component component = bound as Component;
        if (component != null)
            boundTransform = component.transform;
        else
        {
            GameObject gameObject = bound as GameObject;
            if (gameObject != null)
                boundTransform = gameObject.transform;
        }

        return boundTransform != null && boundTransform.IsChildOf(timelineRoot);
    }
}
