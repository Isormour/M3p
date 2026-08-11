using UnityEngine.SceneManagement;

namespace M3P
{
    public static class SceneFlow
    {
        public const string MainScene = "Main";
        public const string MapScene = "Map";
        public const string BattleScene = "Battle";

        public static void LoadMap() => SceneManager.LoadScene(MapScene);

        public static void LoadBattle() => SceneManager.LoadScene(BattleScene);

        public static void LoadMain() => SceneManager.LoadScene(MainScene);
    }
}
