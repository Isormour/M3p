using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class ApplyStatusLogic : BattleEffectLogic, ITooltipPreview
    {
        [SerializeField] StatusEffectDefinition _status;
        [Min(1), SerializeField] int _stacks = 1;
        [SerializeField] SkillPropertyTerm[] _stackTerms = Array.Empty<SkillPropertyTerm>();
        [Tooltip("When greater than zero, replaces the status duration. Odnowienie uses 4 + 2 × Recovery.")]
        [SerializeField] int _durationBase;
        [SerializeField] SkillPropertyTerm[] _durationTerms = Array.Empty<SkillPropertyTerm>();
        [Tooltip("When greater than zero, stores a scaled incoming reduction on the status.")]
        [SerializeField] int _reductionBase;
        [SerializeField] SkillPropertyTerm[] _reductionTerms = Array.Empty<SkillPropertyTerm>();
        [SerializeField] int _counterBase;
        [SerializeField] SkillPropertyTerm[] _counterTerms = Array.Empty<SkillPropertyTerm>();
        [Tooltip("When greater than zero, the status removes this many action points on tick instead of its authored formula.")]
        [SerializeField] int _actionPointDrain;

        public StatusEffectDefinition Status => _status;
        public int Stacks => Mathf.Max(1, _stacks);

        public override void Apply(BattleEffectContext context, EEffectTarget target)
        {
            if (_status == null)
                return;

            BattleCharacter character = context.Resolve(target);
            if (character == null)
                return;

            PlayerProperties properties = SkillPropertyScaling.Read(context);
            int stacks = Mathf.Max(1, SkillPropertyScaling.Apply(_stacks, _stackTerms, properties));
            int duration = ScaledDuration(properties);
            int reduction = ScaledReduction(properties);
            int counter = ScaledCounter(properties);
            character.ApplyStatusStacks(
                _status,
                context.Caster,
                stacks,
                duration,
                reduction,
                counter,
                Mathf.Max(0, _actionPointDrain));
        }

        public void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines)
        {
            if (_status == null)
                return;

            PlayerProperties properties = SkillPropertyScaling.Read(preview);
            int stacks = Mathf.Max(1, SkillPropertyScaling.Apply(_stacks, _stackTerms, properties));
            int durationTurns = ScaledDuration(properties);
            if (durationTurns <= 0)
                durationTurns = _status.DurationTurns;

            string name = _status.DisplayName;
            string duration = _status.PersistUntilConsumed
                ? "do następnego ataku"
                : $"{durationTurns} tury";
            string text = stacks > 1
                ? $"Nakłada {stacks} stacki: {name} ({duration})"
                : $"Nakłada {name} ({duration})";
            lines.Add(new TooltipLine(TooltipLineKind.Status, text));
            if (_actionPointDrain > 0)
                lines.Add(new TooltipLine(TooltipLineKind.Other, $"Odbiera {_actionPointDrain} AP"));

            int reduction = ScaledReduction(properties);
            int counter = ScaledCounter(properties);
            _status.AppendIncomingGuardTooltip(
                lines,
                reduction > 0 ? reduction : -1,
                counter > 0 ? counter : -1);
        }

        int ScaledDuration(PlayerProperties properties)
        {
            if (_durationBase <= 0 && (_durationTerms == null || _durationTerms.Length == 0))
                return 0;

            return Mathf.Max(1, SkillPropertyScaling.Apply(_durationBase, _durationTerms, properties));
        }

        int ScaledReduction(PlayerProperties properties)
        {
            if (_reductionBase <= 0 && (_reductionTerms == null || _reductionTerms.Length == 0))
                return 0;

            return Mathf.Max(0, SkillPropertyScaling.Apply(_reductionBase, _reductionTerms, properties));
        }

        int ScaledCounter(PlayerProperties properties)
        {
            if (_counterBase <= 0 && (_counterTerms == null || _counterTerms.Length == 0))
                return 0;

            return Mathf.Max(0, SkillPropertyScaling.Apply(_counterBase, _counterTerms, properties));
        }
    }
}
