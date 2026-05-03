using System.Collections;
using Match3;
using UnityEngine;

namespace M3P
{
    public sealed class EnemyBattleCharacter : BattleCharacter
    {
        public override bool IsPlayerControlled => false;

        [SerializeField] float _thinkSeconds = 0.35f;

        EnemyDefinition _definition;

        /// <summary>Definition used to configure this instance, when spawned from battle.</summary>
        public EnemyDefinition Definition => _definition;

        public void Configure(EnemyDefinition definition)
        {
            _definition = definition;
            if (definition == null)
                return;

            ConfigureHealth(definition.maxHP);

            string displayName = definition.Name;
            if (!string.IsNullOrEmpty(displayName))
                gameObject.name = $"{nameof(EnemyBattleCharacter)}_{displayName}";
        }

        public IEnumerator PlayTurn(Match3Board board)
        {
            yield return new WaitForSeconds(_thinkSeconds);

            if (board == null)
                yield break;

            board.TryRandomLegalSwap();
        }
    }
}
