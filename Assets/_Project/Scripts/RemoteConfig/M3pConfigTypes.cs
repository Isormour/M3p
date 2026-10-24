using System;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// Stable transport identifiers. They stay valid when an asset is renamed or moved.
    /// </summary>
    public static class M3pConfigTypes
    {
        public const string Battle = "m3p.battle";
        public const string LevelProgression = "m3p.levelProgression";
        public const int CurrentTypeSchemaVersion = 1;

        public static string For(Type type)
        {
            if (type == null)
                return "m3p.unknown";

            if (type == typeof(BattleConfig))
                return Battle;

            if (type == typeof(LevelProgressionConfig))
                return LevelProgression;

            return "m3p." + ConfigContracts.ContractName(type.Name);
        }
    }
}
