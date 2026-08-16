using M3P;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Match3
{
    [System.Serializable]
    public class TileTypeGraphics
    {
        [SerializeField] private Sprite mainSprite;
        [SerializeField] private Texture2D normalMap;
        [SerializeField] private Material spriteMaterial;

        [FormerlySerializedAs("ShardIcon")]
        [SerializeField] private Sprite shardIcon;

        public Sprite MainSprite => mainSprite;
        public Texture2D NormalMap => normalMap;
        public Material SpriteMaterial => spriteMaterial;
        public Sprite ShardIcon => shardIcon;
    }

    [CreateAssetMenu(fileName = "TileTypeDefinition", menuName = "Match3/Tile Type Definition", order = 0)]
    public class Match3TileTypeDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = Color.white;

        [Header("Graphics")]
        [SerializeField] private TileTypeGraphics runeGraphics;
        [SerializeField] private TileTypeGraphics tileGraphics;

        [Tooltip("Shards spent to craft a copy of this tile. Each entry is one colour.")]
        [SerializeField] TileTypeShardCost[] _craftCost = Array.Empty<TileTypeShardCost>();

        public GameObject Prefab => prefab;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public TileTypeGraphics RuneGraphics => runeGraphics;
        public TileTypeGraphics TileGraphics => tileGraphics;
        public TileTypeShardCost[] CraftCost => _craftCost ?? Array.Empty<TileTypeShardCost>();
        public Material UIMaterial;

        /// <summary>
        /// Icon used for this colour's shards. Falls back to the tile sprite when none is authored.
        /// </summary>
        public Sprite ResolveShardIcon()
        {
            Sprite icon = tileGraphics != null ? tileGraphics.ShardIcon : null;
            return icon != null ? icon : sprite;
        }
    }
}
