using System.Collections.Generic;

namespace M3P
{
    public enum TooltipLineKind
    {
        Damage = 0,
        Heal,
        Shield,
        Status,
        Draw,
        Other
    }

    public readonly struct TooltipLine
    {
        public TooltipLineKind Kind { get; }
        public string Text { get; }

        public TooltipLine(TooltipLineKind kind, string text)
        {
            Kind = kind;
            Text = text ?? string.Empty;
        }
    }

    public interface ITooltipPreview
    {
        void AppendTooltipLines(BattleEffectContext preview, EEffectTarget target, List<TooltipLine> lines);
    }
}
