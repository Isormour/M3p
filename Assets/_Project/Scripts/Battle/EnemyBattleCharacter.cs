using Match3;
using System.Collections;
using UnityEngine;

namespace M3P
{
    public sealed class EnemyBattleCharacter : BattleCharacter
    {
        public override bool IsPlayerControlled => false;

        public override EEffectSource EffectSource => EEffectSource.Enemy;

        [Tooltip("Delay before the first attack, and between extra attacks from Agility.")]
        [SerializeField] float _thinkSeconds = 0.35f;

        EnemyDefinition _definition;

        public EnemyDefinition Definition => _definition;

        public void Configure(EnemyDefinition definition)
        {
            _definition = definition;
            ClearStatuses();
            ClearSkillCooldowns();
            if (definition == null)
                return;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            CharacterStats stats = new CharacterStats(
                definition.HardStats,
                config != null ? config.StatProgression : null);
            stats.RecalculateSoftStatsForBattle();
            SetCharacterStats(stats);

            string displayName = definition.Name;
            if (!string.IsNullOrEmpty(displayName))
                gameObject.name = $"{nameof(EnemyBattleCharacter)}_{displayName}";
        }

        public IEnumerator PlayTurn(Match3Board board)
        {
            yield return new WaitForSeconds(_thinkSeconds);

            SkillDefinition[] skills = _definition?.Skills;
            if (skills == null || skills.Length == 0)
                yield break;

            BattleManager manager = BattleManager.Instance;
            if (manager == null)
                yield break;

            BattleCharacter target = manager.Player;
            if (target == null)
                yield break;

            int attackCount = Mathf.Max(0, GetEffectiveHard().Agility);
            int searchStart = 0;
            for (int attack = 0; attack < attackCount; attack++)
            {
                if (!IsAlive || target == null || !target.IsAlive)
                    yield break;

                if (!TryGetReadySkill(skills, searchStart, out SkillDefinition skill, out int usedIndex))
                    yield break;

                manager.ExecuteSkill(skill, this, target);
                StartSkillCooldown(skill);
                searchStart = usedIndex + 1;

                if (attack < attackCount - 1)
                    yield return new WaitForSeconds(_thinkSeconds);
            }
        }

        bool TryGetReadySkill(SkillDefinition[] skills, int searchStart, out SkillDefinition skill, out int index)
        {
            skill = null;
            index = -1;
            if (skills == null)
                return false;

            int count = skills.Length;
            for (int offset = 0; offset < count; offset++)
            {
                int candidateIndex = (searchStart + offset) % count;
                SkillDefinition candidate = skills[candidateIndex];
                if (candidate == null || !IsSkillReady(candidate))
                    continue;

                skill = candidate;
                index = candidateIndex;
                return true;
            }

            return false;
        }
    }
}
