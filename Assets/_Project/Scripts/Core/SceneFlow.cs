using UnityEngine.SceneManagement;

namespace M3P
{
    public static class SceneFlow
    {
        public const string BootScene = "Boot";
        public const string MenuScene = "Menu";
        public const string MapScene = "Map";
        public const string BattleScene = "Battle";

        public static void LoadMenu() => SceneManager.LoadScene(MenuScene);

        public static void LoadMap() => SceneManager.LoadScene(MapScene);

        public static void LoadBattle() => SceneManager.LoadScene(BattleScene);

        public static void LoadMain() => LoadMenu();
    }
}
