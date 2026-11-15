using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace M3P
{
    /// <summary>
    /// Runtime data for a projectile released by a <see cref="ProjectileTrack"/> clip.
    /// The clip waits until this is armed, so Basic Attack can release it after the windup
    /// and Enemy Strike can release it on the authored frame.
    /// </summary>
    public sealed class SkillProjectileRequest
    {
        public GameObject Prefab;
        public Transform Origin;
        public Transform Target;
        public Color Color = Color.white;
        public int Damage;
        public Action OnLaunch;
        public Action OnArrived;

        public bool Launched { get; private set; }

        public void Launch(GameObject prefabOverride)
        {
            if (Launched)
                return;

            Launched = true;
            OnLaunch?.Invoke();

            GameObject prefab = prefabOverride != null ? prefabOverride : Prefab;
            if (prefab == null || Origin == null || Target == null)
            {
                OnArrived?.Invoke();
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, Origin.position, Quaternion.identity);
            ProjectileVFX projectile = instance.GetComponent<ProjectileVFX>();
            if (projectile != null)
            {
                projectile.Launch(Target, Color, Damage, OnArrived);
                return;
            }

            UnityEngine.Object.Destroy(instance);
            OnArrived?.Invoke();
        }
    }

    /// <summary>Lives on the spawned skill timeline. The projectile clip reads the request from here.</summary>
    public sealed class SkillProjectileBinding : MonoBehaviour
    {
        public SkillProjectileRequest Request;

        GameObject _pendingOverride;
        bool _pending;

        public void ReleaseWhenArmed(GameObject prefabOverride)
        {
            if (Request != null)
            {
                if (!Request.Launched)
                    Request.Launch(prefabOverride);
                _pending = false;
                return;
            }

            _pendingOverride = prefabOverride;
            _pending = true;
        }

        void LateUpdate()
        {
            if (!_pending || Request == null || Request.Launched)
                return;

            _pending = false;
            Request.Launch(_pendingOverride);
        }
    }

    [TrackClipType(typeof(ProjectilePlayableAsset))]
    [TrackColor(0.35f, 0.7f, 1f)]
    public class ProjectileTrack : TrackAsset
    {
        public static bool ContainsClip(PlayableAsset asset)
        {
            TimelineAsset timeline = asset as TimelineAsset;
            if (timeline == null)
                return false;

            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                ProjectileTrack projectile = track as ProjectileTrack;
                if (projectile == null || projectile.mutedInHierarchy)
                    continue;

                foreach (TimelineClip clip in projectile.GetClips())
                {
                    if (clip != null)
                        return true;
                }
            }

            return false;
        }
    }

    public class ProjectilePlayableBehaviour : PlayableBehaviour
    {
        public GameObject Director;
        public GameObject PrefabOverride;

        bool _clipStarted;
        bool _launched;
        bool _waitingForRequest;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying)
                return;

            _clipStarted = true;
            TryLaunch();
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (!_clipStarted || _launched || !Application.isPlaying)
                return;

            TryLaunch();
        }

        void TryLaunch()
        {
            if (_launched || Director == null)
                return;

            SkillProjectileBinding binding = Director.GetComponent<SkillProjectileBinding>();
            if (binding == null)
                return;

            SkillProjectileRequest request = binding.Request;
            if (request == null)
            {
                if (_waitingForRequest)
                    return;

                _waitingForRequest = true;
                binding.ReleaseWhenArmed(PrefabOverride);
                return;
            }

            if (request.Launched)
            {
                _launched = true;
                return;
            }

            _launched = true;
            request.Launch(PrefabOverride);
        }
    }
}
