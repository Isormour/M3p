using Match3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public sealed class EnemyBattleCharacter : BattleCharacter
    {
        public override bool IsPlayerControlled => false;

        public override EEffectSource EffectSource => EEffectSource.Enemy;

        [Tooltip("Delay before the telegraphed action plays.")]
        [SerializeField] float _thinkSeconds = 0.35f;

        EnemyRuntimeSpec _spec;
        SkillDefinition _lastUsedSkill;

        public SkillDefinition TelegraphedSkill { get; private set; }

        public bool IsTelegraphingOffensive =>
            TelegraphedSkill != null && TelegraphedSkill.AffectsOpponent();

        public void RefreshTelegraph()
        {
            TelegraphedSkill = null;
            IReadOnlyList<SkillDefinition> skills = Skills;
            if (skills == null || skills.Count == 0)
                return;

            TryGetReadySkill(skills, out SkillDefinition skill);
            TelegraphedSkill = skill;
        }

        public EnemyRuntimeSpec RuntimeSpec => _spec;

        public EnemyDefinition Definition => _spec != null ? _spec.Definition : null;

        public override IReadOnlyList<SkillDefinition> Skills =>
            _spec != null && _spec.ActiveSkills != null
                ? _spec.ActiveSkills
                : System.Array.Empty<SkillDefinition>();

        public void Configure(EnemyDefinition definition)
        {
            Configure(EnemyProgressionResolver.Resolve(definition, 1, MapNodeType.Battle));
        }

        public void Configure(EnemyRuntimeSpec spec)
        {
            _spec = spec;
            _lastUsedSkill = null;
            TelegraphedSkill = null;
            ClearStatuses();
            ClearSkillCooldowns();
            if (spec == null || spec.Definition == null)
                return;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            CharacterStats stats = new CharacterStats(
                spec.HardStats,
                config != null ? config.StatProgression : null);
            stats.RecalculateSoftStatsForBattle();
            stats.Soft?.ScaleMaxHealth(spec.HealthMultiplier);
            SetCharacterStats(stats);

            string displayName = spec.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
                gameObject.name = $"{nameof(EnemyBattleCharacter)}_{displayName}";
        }

        public IEnumerator PlayTurn(Match3Board board)
        {
            yield return new WaitForSeconds(_thinkSeconds);

            IReadOnlyList<SkillDefinition> skills = Skills;
            if (skills == null || skills.Count == 0)
                yield break;

            BattleManager manager = BattleManager.Instance;
            if (manager == null)
                yield break;

            BattleCharacter target = manager.Player;
            if (target == null || !IsAlive || !target.IsAlive)
                yield break;

            SkillDefinition skill = TelegraphedSkill;
            if (skill == null || !IsSkillReady(skill))
            {
                if (!TryGetReadySkill(skills, out skill))
                    yield break;
            }

            manager.ExecuteSkill(skill, this, target);
            StartSkillCooldown(skill);
            _lastUsedSkill = skill;
        }

        bool TryGetReadySkill(IReadOnlyList<SkillDefinition> skills, out SkillDefinition skill)
        {
            skill = null;
            if (skills == null)
                return false;

            int readyCount = 0;
            int readyExceptLast = 0;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition candidate = skills[i];
                if (candidate == null || !IsSkillReady(candidate))
                    continue;

                readyCount++;
                if (candidate != _lastUsedSkill)
                    readyExceptLast++;
            }

            if (readyCount == 0)
                return false;

            bool skipLastUsed = readyExceptLast > 0;
            int poolSize = skipLastUsed ? readyExceptLast : readyCount;
            int pick = Random.Range(0, poolSize);
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition candidate = skills[i];
                if (candidate == null || !IsSkillReady(candidate))
                    continue;

                if (skipLastUsed && candidate == _lastUsedSkill)
                    continue;

                if (pick == 0)
                {
                    skill = candidate;
                    return true;
                }

                pick--;
            }

            return false;
        }
    }
}
