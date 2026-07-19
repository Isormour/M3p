using UnityEngine;

namespace Match3
{
    [CreateAssetMenu(fileName = "TileTypeDefinition", menuName = "Match3/Tile Type Definition", order = 0)]
    public class Match3TileTypeDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = Color.white;

        public GameObject Prefab => prefab;
        public Sprite Sprite => sprite;
        public Color Color => color;
    }
}
