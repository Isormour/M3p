using System;

namespace M3P
{
    /// <summary>Flat hard-stat delta while a status is active. Constitution is ignored at runtime.</summary>
    [Serializable]
    public struct StatusStatModifier
    {
        public EStatType Stat;
        public int Amount;
    }
}
