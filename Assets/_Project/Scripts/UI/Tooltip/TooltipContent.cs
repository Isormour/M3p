using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public sealed class TooltipContent
    {
        public string Title;
        public string Description;
        public string Meta;
        public Sprite Icon;
        public readonly List<TooltipLine> Lines = new List<TooltipLine>();

        public bool IsEmpty =>
            string.IsNullOrEmpty(Title)
            && string.IsNullOrEmpty(Description)
            && string.IsNullOrEmpty(Meta)
            && Icon == null
            && Lines.Count == 0;
    }
}
