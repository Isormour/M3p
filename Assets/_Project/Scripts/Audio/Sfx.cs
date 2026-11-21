namespace M3P
{
    public static class Sfx
    {
        public static void Play(SfxId id) => SfxPlayer.Play(id);

        public static void StartLoop(SfxId id) => SfxPlayer.StartLoop(id);

        public static void StopLoop(SfxId id) => SfxPlayer.StopLoop(id);

        public static void PlaySkill(SkillDefinition skill, bool enemyCaster) =>
            SfxPlayer.PlaySkill(skill, enemyCaster);
    }
}
