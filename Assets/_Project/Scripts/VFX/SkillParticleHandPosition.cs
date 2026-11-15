using UnityEngine;
using UnityEngine.Timeline;

namespace M3P
{
    /// <summary>Samples the caster's hand when the particle Control clip starts.</summary>
    [DisallowMultipleComponent]
    public sealed class SkillParticleHandPosition : MonoBehaviour, ITimeControl
    {
        public void OnControlTimeStart()
        {
            if (!Application.isPlaying)
                return;

            WorldCharacter caster = GetComponentInParent<WorldCharacter>();
            if (caster != null && caster.Hand != null)
                transform.position = caster.Hand.position;
        }

        public void SetTime(double time)
        {
            // Keep the sampled position for the duration of the effect.
        }

        public void OnControlTimeStop()
        {
        }
    }
}
