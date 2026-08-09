using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public sealed class PlayerBattleCharacter : BattleCharacter
    {
        readonly List<SkillDefinition> _skills = new List<SkillDefinition>();

        PlayerProfile _detachedProfile;

        public override bool IsPlayerControlled => true;

        public override EEffectSource EffectSource => EEffectSource.Player;

        /// <summary>Skills the profile brought into this battle, resolved from their saved ids.</summary>
        public IReadOnlyList<SkillDefinition> Skills => _skills;

        /// <summary>The persistent profile this character fights with.</summary>
        public PlayerProfile Profile => ResolveProfile();

        public void PrepareForBattle()
        {
            PlayerProfile profile = ResolveProfile();
            ResolveSkills(profile);
            SetCharacterStats(profile.CreateBattleStats());
        }

        public override void OnTurnStarted()
        {
            CharacterStats stats = Stats;
            stats?.Soft?.ResetActionPoints(stats.Hard);
        }

        PlayerProfile ResolveProfile()
        {
            ProfileManager profiles = GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;
            if (profiles != null)
                return profiles.CurrentProfile;

            if (_detachedProfile == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerBattleCharacter)}: no {nameof(GameManager)} in the scene, so nothing is saved and the character fights with a blank profile.",
                    this);
                _detachedProfile = new PlayerProfile();
            }

            return _detachedProfile;
        }

        /// <summary>Turns the ids stored in the profile back into the skill assets a battle casts.</summary>
        void ResolveSkills(PlayerProfile profile)
        {
            _skills.Clear();

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            SkillConfig skillConfig = config != null ? config.Skills : null;

            if (skillConfig == null || profile.Skills == null)
                return;

            for (int i = 0; i < profile.Skills.Count; i++)
            {
                int skillId = profile.Skills[i].SkillId;

                if (skillConfig.TryGetSkill(skillId, out SkillDefinition skill))
                    _skills.Add(skill);
                else
                    Debug.LogWarning(
                        $"{nameof(PlayerBattleCharacter)}: profile references skill id {skillId}, which is missing from {nameof(SkillConfig)}.",
                        this);
            }
        }
    }
}
