using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Scene music. Battle picks the arena that matches the pending map biome.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "M3P/Music Library", order = 21)]
    public sealed class MusicLibrary : ScriptableObject
    {
        [SerializeField] AudioClip _menu;
        [SerializeField] AudioClip _map;
        [SerializeField] AudioClip _dungeon;
        [SerializeField] AudioClip _lava;
        [SerializeField] AudioClip _frost;

        public bool TryGetClip(string sceneName, MapBiome biome, out AudioClip clip)
        {
            clip = null;
            if (sceneName == SceneFlow.BootScene || sceneName == SceneFlow.MenuScene)
                clip = _menu;
            else if (sceneName == SceneFlow.MapScene)
                clip = _map;
            else if (sceneName == SceneFlow.BattleScene)
                clip = Arena(biome);
            else
                return false;

            return clip != null;
        }

        AudioClip Arena(MapBiome biome)
        {
            switch (biome)
            {
                case MapBiome.Lava:
                    return _lava != null ? _lava : _dungeon;
                case MapBiome.Frost:
                    return _frost != null ? _frost : _dungeon;
                default:
                    return _dungeon;
            }
        }
    }
}
