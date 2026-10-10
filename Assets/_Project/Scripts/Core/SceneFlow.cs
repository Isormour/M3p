using UnityEngine.SceneManagement;

namespace M3P
{
    public static class SceneFlow
    {
        public const string BootScene = "Boot";
        public const string MenuScene = "Menu";
        public const string MapScene = "Map";
        public const string BattleScene = "Battle";

        public static void LoadMenu() => Load(MenuScene);

        public static void LoadMap() => Load(MapScene);

        public static void LoadBattle() => Load(BattleScene);

        static void Load(string scene)
        {
            Sfx.Play(SfxId.SceneTransition);
            SceneManager.LoadScene(scene);
        }

        public static void LoadMain() => LoadMenu();
    }
}
