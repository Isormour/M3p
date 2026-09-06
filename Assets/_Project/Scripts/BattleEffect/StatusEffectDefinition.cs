using System;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Authoring data for a lasting combat status: optional stat modifiers and optional per-turn effects.
    /// Duration ticks on the bearer's turn starts, beginning the turn after application.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffect", menuName = "M3P/Status Effect", order = 11)]
    public class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] EStatusType _statusType;
        [Min(1), SerializeField] int _durationTurns = 1;
        [Tooltip("When false, re-applying refreshes remaining turns on the existing instance.")]
        [SerializeField] bool _canStack;
        [Tooltip("Constitution modifiers are ignored at runtime so MaxHP does not change mid-battle.")]
        [SerializeField] StatusStatModifier[] _statModifiers = Array.Empty<StatusStatModifier>();
        [Tooltip("Fired on the status bearer at the start of each of their turns. Authored Effect Target is ignored; the bearer always receives the effect.")]
        [SerializeField] BattleEffect[] _onTurnEffects = Array.Empty<BattleEffect>();

        public EStatusType StatusType => _statusType;

        public int DurationTurns => Mathf.Max(1, _durationTurns);

        public bool CanStack => _canStack;

        public StatusStatModifier[] StatModifiers => _statModifiers ?? Array.Empty<StatusStatModifier>();

        public BattleEffect[] OnTurnEffects => _onTurnEffects ?? Array.Empty<BattleEffect>();

        /// <summary>Runs on-turn effects against the bearer. <paramref name="source"/> scales magic effects when present.</summary>
        public void ApplyOnTurnEffects(BattleCharacter bearer, BattleCharacter source)
        {
            BattleEffect[] effects = OnTurnEffects;
            if (bearer == null || effects.Length == 0)
                return;

            BattleCharacter caster = source != null ? source : bearer;
            var context = new BattleEffectContext(caster, bearer);

            for (int i = 0; i < effects.Length; i++)
            {
                BattleEffect effect = effects[i];
                if (effect?.Logic == null)
                    continue;

                // Always hit the bearer, regardless of the BattleEffect asset's authored target.
                effect.Logic.Apply(context, EEffectTarget.Opponent);
            }
        }
    }
}
