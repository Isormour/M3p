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

        /// <summary>Raised after healing increases health.</summary>
        public event Action<int> Healed;

        public CombatModifiers Modifiers => _modifiers;

        protected void SetCharacterStats(CharacterStats stats)
        {
            UnbindSoftVitals();
            _characterStats = stats;
            BindSoftVitals();
        }

        void BindSoftVitals()
        {
            _boundSoft = _characterStats != null ? _characterStats.Soft : null;
            if (_boundSoft == null)
                return;

            _boundSoft.Damaged += HandleSoftDamaged;
            _boundSoft.Healed += HandleSoftHealed;
        }

        void UnbindSoftVitals()
        {
            if (_boundSoft != null)
            {
                _boundSoft.Damaged -= HandleSoftDamaged;
                _boundSoft.Healed -= HandleSoftHealed;
            }

            _boundSoft = null;
        }

        void HandleSoftDamaged(int amount)
        {
            Damaged?.Invoke(amount);
        }

        void HandleSoftHealed(int amount)
        {
            Healed?.Invoke(amount);
        }

        void OnDestroy()
        {
            UnbindSoftVitals();
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

        /// <summary>Flat reduction applied to every incoming direct hit by lasting statuses. Not consumed.</summary>
        public int GetPersistentIncomingReduction()
        {
            int total = 0;
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                StatusEffectDefinition definition = status?.Definition;
                if (definition == null || !definition.PersistentIncomingReduction || status.Magnitude <= 0)
                    continue;

                total += status.Magnitude * status.Stacks;
            }

            return total;
        }

        /// <summary>Flat bonus added to every outgoing direct hit by lasting statuses. Not consumed.</summary>
        public int GetPersistentOutgoingBonus()
        {
            int total = 0;
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                StatusEffectDefinition definition = status?.Definition;
                if (definition == null || !definition.PersistentOutgoingBonus || status.Magnitude <= 0)
                    continue;

                total += status.Magnitude * status.Stacks;
            }

            return total;
        }

        /// <summary>Flat penalty subtracted from every outgoing direct hit by lasting statuses. Not consumed.</summary>
        public int GetPersistentOutgoingPenalty()
        {
            int total = 0;
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                StatusEffectDefinition definition = status?.Definition;
                if (definition == null || !definition.PersistentOutgoingPenalty || status.Magnitude <= 0)
                    continue;

                total += status.Magnitude * status.Stacks;
            }

            return total;
        }

        /// <summary>Magnitude stored on the matching status, or 0 when it is absent.</summary>
        public int GetStatusMagnitude(StatusEffectDefinition definition)
        {
            if (definition == null)
                return 0;

            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                if (status != null && definition.Matches(status.Definition))
                    return Mathf.Max(0, status.Magnitude);
            }

            return 0;
        }

        /// <summary>True while a status such as Zakopanie makes direct hits miss. Status ticks still land.</summary>
        public bool EvadesDirectHits()
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusEffectDefinition definition = _statuses[i]?.Definition;
                if (definition != null && definition.EvadeDirectHits)
                    return true;
            }

            return false;
        }

        /// <summary>Lets statuses such as Toxic Skin answer a direct hit that was not evaded.</summary>
        public void NotifyDirectHitReceived(BattleCharacter attacker)
        {
            if (attacker == null || _statuses.Count == 0)
                return;

            var snapshot = new List<StatusInstance>(_statuses);
            for (int i = 0; i < snapshot.Count; i++)
            {
                StatusEffectDefinition definition = snapshot[i]?.Definition;
                if (definition == null || definition.OnDirectHitEffects.Length == 0)
                    continue;

                definition.ApplyOnDirectHit(this, attacker);
            }
        }

        /// <summary>
        /// Consumes one incoming-guard status on the next direct hit: reduction, then optional counter.
        /// Newest matching status wins. Stacked copies lose one stack per hit.
        /// </summary>
        public bool TryConsumeIncomingGuard(out int reduction, out int counterDamage, out bool counterPhysical)
        {
            reduction = 0;
            counterDamage = 0;
            counterPhysical = false;

            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                StatusInstance status = _statuses[i];
                StatusEffectDefinition definition = status?.Definition;
                if (definition == null || !definition.HasIncomingGuard)
                    continue;

                reduction = definition.IncomingDamageReduction;
                counterDamage = definition.IncomingCounterDamage;
                counterPhysical = definition.IncomingCounterPhysical;

                status.Stacks--;
                if (status.Stacks <= 0)
                    _statuses.RemoveAt(i);

                RaiseStatusesChanged();
                return true;
            }

            return false;
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
            ApplyStatusStacks(definition, source, 1, 0, 0);
        }

        /// <summary>
        /// Adds <paramref name="stacks"/> copies at once.
        /// <paramref name="durationOverride"/> and <paramref name="magnitude"/> replace the authored
        /// duration and store a scaled payload when they are greater than zero.
        /// </summary>
        public void ApplyStatusStacks(
            StatusEffectDefinition definition,
            BattleCharacter source,
            int stacks,
            int durationOverride,
            int magnitude)
        {
            if (definition == null || stacks <= 0)
                return;

            int duration = durationOverride > 0 ? durationOverride : definition.DurationTurns;

            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance existing = _statuses[i];
                if (!definition.Matches(existing.Definition))
                    continue;

                if (definition.CanStack)
                {
                    existing.Stacks += stacks;
                    Sfx.Play(SfxId.StatusStrengthen);
                }
                else
                {
                    Sfx.Play(SfxId.StatusApply);
                }

                existing.RemainingTurns = duration;
                existing.Source = source;
                if (magnitude > 0)
                    existing.Magnitude = magnitude;
                RaiseStatusesChanged();
                return;
            }

            StatusInstance created = new StatusInstance(definition, source, duration, stacks);
            if (magnitude > 0)
                created.Magnitude = magnitude;
            _statuses.Add(created);
            Sfx.Play(SfxId.StatusApply);
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
        /// Fires per-turn status logics, then decrements duration unless the status persists
        /// until consumed. Called from <see cref="OnTurnStarted"/>, so a timed status applied
        /// mid-turn first ticks on the bearer's next turn.
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

                if (definition.PersistUntilConsumed)
                    continue;

                status.RemainingTurns--;
                if (status.RemainingTurns <= 0)
                {
                    Sfx.Play(SfxId.StatusExpire);
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
            int armorBefore = soft != null ? soft.CurrentArmor : 0;

            definition.ApplyOnTurnEffects(this, status.Source, status.Stacks);

            if (soft == null)
                return;

            if (soft.CurrentHealth > healthBefore)
                Sfx.Play(SfxId.RegenerationTick);

            if (soft.CurrentHealth < healthBefore || soft.CurrentShield < shieldBefore || soft.CurrentArmor < armorBefore)
            {
                if (definition.StatusType == EStatusType.Burn)
                    Sfx.Play(SfxId.BurnTick);
                StatusDamageProcessed?.Invoke(definition.StatusType);
            }
        }

        void RaiseStatusesChanged()
        {
            StatusesChanged?.Invoke();
        }
    }
}
