using UnityEngine;

namespace Match3
{
    [System.Serializable]
    public class TileTypeGraphics
    {
        [SerializeField] private Sprite mainSprite;
        [SerializeField] private Texture2D normalMap;
        [SerializeField] private Material spriteMaterial;

        public Sprite MainSprite => mainSprite;
        public Texture2D NormalMap => normalMap;
        public Material SpriteMaterial => spriteMaterial;
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

        public GameObject Prefab => prefab;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public TileTypeGraphics RuneGraphics => runeGraphics;
        public TileTypeGraphics TileGraphics => tileGraphics;
    }
}
