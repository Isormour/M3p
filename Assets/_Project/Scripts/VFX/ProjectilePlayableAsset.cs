using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace M3P
{
    public class ProjectilePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("Optional prefab. Empty uses the projectile armed by the battle.")]
        public GameObject ProjectilePrefab;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<ProjectilePlayableBehaviour> playable =
                ScriptPlayable<ProjectilePlayableBehaviour>.Create(graph);
            ProjectilePlayableBehaviour behaviour = playable.GetBehaviour();
            behaviour.Director = owner;
            behaviour.PrefabOverride = ProjectilePrefab;
            return playable;
        }
    }
}
