using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Authoring data for a lasting combat status: optional stat modifiers, per-turn effects,
    /// or an incoming-hit guard consumed by the next direct attack.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffect", menuName = "M3P/Status Effect", order = 11)]
    public class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] EStatusType _statusType;
        [SerializeField] string _displayName;
        [TextArea, SerializeField] string _description;
        [Tooltip("Sprite shown as the status icon in battle UI.")]
        [SerializeField] Sprite _icon;
        [Min(1), SerializeField] int _durationTurns = 1;
        [Tooltip("When false, re-applying refreshes remaining turns on the existing instance.")]
        [SerializeField] bool _canStack;
        [Tooltip("Constitution modifiers are ignored at runtime so MaxHP does not change mid-battle.")]
        [SerializeField] StatusStatModifier[] _statModifiers = Array.Empty<StatusStatModifier>();
        [Tooltip("Fired on the status bearer at the start of each of their turns. Authored Effect Target is ignored; the bearer always receives the effect.")]
        [SerializeField] BattleEffect[] _onTurnEffects = Array.Empty<BattleEffect>();
        [Tooltip("When true, remaining turns do not tick down. The status stays until an incoming hit consumes it, or the battle ends.")]
        [SerializeField] bool _persistUntilConsumed;
        [Tooltip("Flat damage subtracted from the next direct hit that consumes this status.")]
        [Min(0), SerializeField] int _incomingDamageReduction;
        [Min(0), SerializeField] int _incomingCounterDamage;
        [SerializeField] bool _incomingCounterPhysical = true;
        [Tooltip("While active, subtract the instance magnitude from every incoming direct hit. The status is not consumed.")]
        [SerializeField] bool _persistentIncomingReduction;
        [Tooltip("While active, add the instance magnitude to every outgoing direct hit. The status is not consumed.")]
        [SerializeField] bool _persistentOutgoingBonus;
        [Tooltip("While active, subtract the instance magnitude from every outgoing direct hit. The status is not consumed.")]
        [SerializeField] bool _persistentOutgoingPenalty;
        [Tooltip("While active, direct hits against the bearer miss. Status ticks still land.")]
        [SerializeField] bool _evadeDirectHits;
        [Tooltip("Fired on the attacker when a direct hit connects with the bearer. Authored Effect Target is ignored; the attacker always receives the effect.")]
        [SerializeField] BattleEffect[] _onDirectHitEffects = Array.Empty<BattleEffect>();
        [Tooltip("While active, each outgoing basic attack or skill hit grants the bearer one Soul. Status ticks do not.")]
        [SerializeField] bool _grantSoulOnOutgoingHit;

        public EStatusType StatusType => _statusType;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public string Description => _description ?? string.Empty;

        public Sprite Icon => _icon;

        public int DurationTurns => Mathf.Max(1, _durationTurns);

        public bool CanStack => _canStack;

        public StatusStatModifier[] StatModifiers => _statModifiers ?? Array.Empty<StatusStatModifier>();

        public BattleEffect[] OnTurnEffects => _onTurnEffects ?? Array.Empty<BattleEffect>();

        public bool PersistUntilConsumed => _persistUntilConsumed;

        public int IncomingDamageReduction => Mathf.Max(0, _incomingDamageReduction);

        public int IncomingCounterDamage => Mathf.Max(0, _incomingCounterDamage);

        public bool IncomingCounterPhysical => _incomingCounterPhysical;

        public bool HasIncomingGuard => IncomingDamageReduction > 0 || IncomingCounterDamage > 0;

        public bool PersistentIncomingReduction => _persistentIncomingReduction;

        public bool PersistentOutgoingBonus => _persistentOutgoingBonus;

        public bool PersistentOutgoingPenalty => _persistentOutgoingPenalty;

        public bool EvadeDirectHits => _evadeDirectHits;

        public BattleEffect[] OnDirectHitEffects => _onDirectHitEffects ?? Array.Empty<BattleEffect>();

        public bool GrantSoulOnOutgoingHit => _grantSoulOnOutgoingHit;

        public void AppendIncomingGuardTooltip(List<TooltipLine> lines, int reduction = -1, int counter = -1)
        {
            if (lines == null)
                return;

            int shownReduction = reduction >= 0 ? reduction : IncomingDamageReduction;
            int shownCounter = counter >= 0 ? counter : IncomingCounterDamage;

            if (shownReduction > 0)
                lines.Add(new TooltipLine(
                    TooltipLineKind.Other,
                    $"Następny atak: -{shownReduction} obrażeń"));

            if (shownCounter > 0)
                lines.Add(new TooltipLine(
                    TooltipLineKind.Damage,
                    $"Kontratak: {shownCounter}"));
        }

        public void AppendPersistentModifierTooltip(List<TooltipLine> lines, int magnitude)
        {
            if (lines == null || magnitude <= 0)
                return;

            if (PersistentIncomingReduction)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Redukcja obrażeń: {magnitude}"));

            if (PersistentOutgoingBonus)
                lines.Add(new TooltipLine(TooltipLineKind.Damage, $"Ataki: +{magnitude}"));

            if (PersistentOutgoingPenalty)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Ataki: -{magnitude}"));
        }

        /// <summary>Runs on-hit effects against the attacker. Does not follow the authored effect target.</summary>
        public void ApplyOnDirectHit(BattleCharacter bearer, BattleCharacter attacker)
        {
            BattleEffect[] effects = OnDirectHitEffects;
            if (bearer == null || attacker == null || effects.Length == 0)
                return;

            var context = new BattleEffectContext(bearer, attacker, directHit: false);
            for (int i = 0; i < effects.Length; i++)
            {
                BattleEffect effect = effects[i];
                if (effect?.Logic == null)
                    continue;

                effect.Logic.Apply(context, EEffectTarget.Opponent);
            }
        }

        public bool Matches(StatusEffectDefinition other)
        {
            if (other == null)
                return false;

            if (this == other)
                return true;

            return _statusType != EStatusType.None && _statusType == other.StatusType;
        }

        /// <summary>Runs on-turn effects against the bearer. <paramref name="source"/> scales magic effects when present.</summary>
        public void ApplyOnTurnEffects(
            BattleCharacter bearer,
            BattleCharacter source,
            int stacks = 1,
            int actionPointDrain = 0)
        {
            BattleEffect[] effects = OnTurnEffects;
            if (bearer == null || effects.Length == 0)
                return;

            BattleCharacter caster = source != null ? source : bearer;
            var context = new BattleEffectContext(
                caster,
                bearer,
                statusStacks: stacks,
                actionPointDrain: actionPointDrain);

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
