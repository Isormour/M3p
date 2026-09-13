using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public abstract class BattleCharacter : MonoBehaviour
    {
        readonly List<StatusInstance> _statuses = new List<StatusInstance>();
        readonly Dictionary<SkillDefinition, int> _skillCooldowns = new Dictionary<SkillDefinition, int>();
        readonly HashSet<SkillDefinition> _skillsUsedThisTurn = new HashSet<SkillDefinition>();
        readonly CombatModifiers _modifiers = new CombatModifiers();

        CharacterStats _characterStats;
        SoftStats _boundSoft;

        public abstract bool IsPlayerControlled { get; }

        public abstract EEffectSource EffectSource { get; }

        public abstract IReadOnlyList<SkillDefinition> Skills { get; }

        public CharacterStats Stats => _characterStats;

        public bool IsAlive => _characterStats != null && _characterStats.IsAlive;

        public IReadOnlyList<StatusInstance> Statuses => _statuses;

        /// <summary>Raised after the status list is added to, removed from, refreshed or cleared.</summary>
        public event Action StatusesChanged;

        /// <summary>Raised when a status's on-turn effects reduce health or shield.</summary>
        public event Action<EStatusType> StatusDamageProcessed;

        /// <summary>Raised after incoming damage reduces health or shield.</summary>
        public event Action<int> Damaged;

        public CombatModifiers Modifiers => _modifiers;

        protected void SetCharacterStats(CharacterStats stats)
        {
            UnbindSoftDamage();
            _characterStats = stats;
            BindSoftDamage();
        }

        void BindSoftDamage()
        {
            _boundSoft = _characterStats != null ? _characterStats.Soft : null;
            if (_boundSoft != null)
                _boundSoft.Damaged += HandleSoftDamaged;
        }

        void UnbindSoftDamage()
        {
            if (_boundSoft != null)
                _boundSoft.Damaged -= HandleSoftDamaged;

            _boundSoft = null;
        }

        void HandleSoftDamaged(int amount)
        {
            Damaged?.Invoke(amount);
        }

        void OnDestroy()
        {
            UnbindSoftDamage();
        }

        public virtual void OnTurnStarted()
        {
            _skillsUsedThisTurn.Clear();
            TickStatuses();
        }

        public void ClearStatuses()
        {
            _statuses.Clear();
            _skillsUsedThisTurn.Clear();
            _modifiers.Clear();
            RaiseStatusesChanged();
        }

        /// <summary>
        /// Fires on-turn effects for the matching status once, using its stacked amount, without
        /// shortening remaining duration or removing the status.
        /// </summary>
        public void TriggerStatusTick(StatusEffectDefinition definition)
        {
            if (definition == null)
                return;

            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                if (!definition.Matches(status.Definition))
                    continue;

                ApplyOnTurnEffectsAndNotify(status);
            }
        }

        /// <summary>True when the skill has no remaining lockout (or authored cooldown is zero).</summary>
        public bool IsSkillReady(SkillDefinition skill)
        {
            if (skill == null)
                return false;

            if (skill.OncePerTurn && _skillsUsedThisTurn.Contains(skill))
                return false;

            if (skill.Cooldown <= 0)
                return true;

            return !_skillCooldowns.TryGetValue(skill, out int remaining) || remaining <= 0;
        }

        public bool HasUsedSkillThisTurn(SkillDefinition skill)
        {
            return skill != null && _skillsUsedThisTurn.Contains(skill);
        }

        public void MarkSkillUsedThisTurn(SkillDefinition skill)
        {
            if (skill != null)
                _skillsUsedThisTurn.Add(skill);
        }

        public int GetRemainingCooldown(SkillDefinition skill)
        {
            if (skill == null || skill.Cooldown <= 0)
                return 0;

            return _skillCooldowns.TryGetValue(skill, out int remaining) ? Mathf.Max(0, remaining) : 0;
        }

        public void StartSkillCooldown(SkillDefinition skill)
        {
            if (skill == null || skill.Cooldown <= 0)
                return;

            _skillCooldowns[skill] = skill.Cooldown;
        }

        /// <summary>Reduces this character's active skill cooldowns by one. Call at the end of their own turn.</summary>
        public void TickSkillCooldowns()
        {
            if (_skillCooldowns.Count == 0)
                return;

            var skills = new List<SkillDefinition>(_skillCooldowns.Keys);
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                int remaining = _skillCooldowns[skill] - 1;
                if (remaining <= 0)
                    _skillCooldowns.Remove(skill);
                else
                    _skillCooldowns[skill] = remaining;
            }
        }

        protected void SetRemainingCooldown(SkillDefinition skill, int remaining)
        {
            if (skill == null)
                return;

            if (remaining <= 0)
                _skillCooldowns.Remove(skill);
            else
                _skillCooldowns[skill] = remaining;
        }

        protected void ClearSkillCooldowns()
        {
            _skillCooldowns.Clear();
        }

        public int CountStatus(StatusEffectDefinition definition)
        {
            if (definition == null)
                return 0;

            int count = 0;
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                if (definition.Matches(status.Definition))
                    count += status.Stacks;
            }

            return count;
        }

        /// <summary>Removes every matching status and returns how many stacks were consumed.</summary>
        public int ConsumeStatus(StatusEffectDefinition definition)
        {
            if (definition == null)
                return 0;

            int consumed = 0;
            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                StatusInstance status = _statuses[i];
                if (!definition.Matches(status.Definition))
                    continue;

                consumed += status.Stacks;
                _statuses.RemoveAt(i);
            }

            if (consumed > 0)
                RaiseStatusesChanged();

            return consumed;
        }

        /// <summary>
        /// Adds a status. Stacking definitions pile onto the existing instance; non-stacking ones
        /// refresh remaining turns (and source) on that instance.
        /// </summary>
        public void ApplyStatus(StatusEffectDefinition definition, BattleCharacter source)
        {
            if (definition == null)
                return;

            int duration = definition.DurationTurns;

            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance existing = _statuses[i];
                if (!definition.Matches(existing.Definition))
                    continue;

                if (definition.CanStack)
                    existing.Stacks++;

                existing.RemainingTurns = duration;
                existing.Source = source;
                RaiseStatusesChanged();
                return;
            }

            _statuses.Add(new StatusInstance(definition, source, duration));
            RaiseStatusesChanged();
        }

        /// <summary>
        /// Hard stats plus active status modifiers. Constitution modifiers are ignored so MaxHP stays fixed.
        /// </summary>
        public HardStats GetEffectiveHard()
        {
            HardStats hard = _characterStats != null ? _characterStats.Hard : default;

            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusStatModifier[] modifiers = _statuses[i].Definition != null
                    ? _statuses[i].Definition.StatModifiers
                    : null;
                if (modifiers == null)
                    continue;

                for (int m = 0; m < modifiers.Length; m++)
                {
                    StatusStatModifier modifier = modifiers[m];
                    if (modifier.Amount == 0 || modifier.Stat == EStatType.Constitution)
                        continue;

                    hard = hard.WithPointsAdded(modifier.Stat, modifier.Amount * _statuses[i].Stacks);
                }
            }

            return hard;
        }

        /// <summary>
        /// Fires per-turn status logics, then decrements duration. Called from <see cref="OnTurnStarted"/>,
        /// so a status applied mid-turn first ticks on the bearer's next turn.
        /// </summary>
        void TickStatuses()
        {
            if (_statuses.Count == 0)
                return;

            bool changed = false;
            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                StatusInstance status = _statuses[i];
                StatusEffectDefinition definition = status.Definition;
                if (definition == null)
                {
                    _statuses.RemoveAt(i);
                    changed = true;
                    continue;
                }

                ApplyOnTurnEffectsAndNotify(status);

                status.RemainingTurns--;
                if (status.RemainingTurns <= 0)
                {
                    _statuses.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
                RaiseStatusesChanged();
        }

        void ApplyOnTurnEffectsAndNotify(StatusInstance status)
        {
            StatusEffectDefinition definition = status.Definition;
            SoftStats soft = _characterStats != null ? _characterStats.Soft : null;
            int healthBefore = soft != null ? soft.CurrentHealth : 0;
            int shieldBefore = soft != null ? soft.CurrentShield : 0;

            definition.ApplyOnTurnEffects(this, status.Source, status.Stacks);

            if (soft == null)
                return;

            if (soft.CurrentHealth < healthBefore || soft.CurrentShield < shieldBefore)
                StatusDamageProcessed?.Invoke(definition.StatusType);
        }

        void RaiseStatusesChanged()
        {
            StatusesChanged?.Invoke();
        }
    }
}
