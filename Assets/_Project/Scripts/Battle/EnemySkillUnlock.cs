using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// A skill that joins the enemy's active pool once the effective floor reaches
    /// <see cref="MinFloor"/>. <see cref="Replaces"/> drops an earlier skill so the
    /// pool stays at a few readable intentions.
    /// </summary>
    [Serializable]
    public struct EnemySkillUnlock
    {
        public SkillDefinition Skill;

        [Min(1)] public int MinFloor;

        [Tooltip("Optional weaker skill removed when this one unlocks.")]
        public SkillDefinition Replaces;

        public bool IsValid => Skill != null;
    }
}
