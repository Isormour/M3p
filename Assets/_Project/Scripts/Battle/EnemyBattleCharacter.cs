using System.Collections;
using Match3;
using UnityEngine;

namespace M3P
{
    public sealed class EnemyBattleCharacter : BattleCharacter
    {
        public override bool IsPlayerControlled => false;

        public override EEffectSource EffectSource => EEffectSource.Enemy;

        [SerializeField] float _thinkSeconds = 0.35f;

        EnemyDefinition _definition;

        public EnemyDefinition Definition => _definition;

        public void Configure(EnemyDefinition definition)
        {
            _definition = definition;
            if (definition == null)
                return;

            int constitution = Mathf.Max(1, definition.maxHP / 10);
            HardStats hardStats = new HardStats(1, 1, constitution);
            CharacterStats stats = new CharacterStats(hardStats);
            stats.RecalculateSoftStatsForBattle();
            SetCharacterStats(stats);

            string displayName = definition.Name;
            if (!string.IsNullOrEmpty(displayName))
                gameObject.name = $"{nameof(EnemyBattleCharacter)}_{displayName}";
        }

        public IEnumerator PlayTurn(Match3Board board)
        {
            yield return new WaitForSeconds(_thinkSeconds);

            if (_definition?.Skills == null || _definition.Skills.Length == 0)
                yield break;

            BattleManager manager = BattleManager.Instance;
            if (manager == null)
                yield break;

            BattleCharacter target = manager.Player;
            if (target == null)
                yield break;

            foreach (SkillDefinition skill in _definition.Skills)
            {
                if (skill == null)
                    continue;

                manager.ExecuteSkill(skill, this, target);
            }
        }
    }
}
