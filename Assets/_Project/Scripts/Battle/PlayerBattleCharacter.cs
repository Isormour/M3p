using UnityEngine;

namespace M3P
{
    public sealed class PlayerBattleCharacter : BattleCharacter
    {
        [SerializeField] SkillDefinition[] _skills;
        [SerializeField] PlayerProfile _profile = new PlayerProfile();

        public override bool IsPlayerControlled => true;

        public override EEffectSource EffectSource => EEffectSource.Player;

        public SkillDefinition[] Skills => _skills;

        public PlayerProfile Profile => _profile ??= new PlayerProfile();

        public void PrepareForBattle()
        {
            SetCharacterStats(Profile.CreateBattleStats());
        }
    }
}
