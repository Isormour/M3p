using Match3;
using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "M3P/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] Match3TileTypeDefinition[] _tileTypes;

        public Match3TileTypeDefinition[] TileTypes => _tileTypes;

        public int TileTypeCount => _tileTypes != null ? _tileTypes.Length : 0;

        public Match3TileTypeDefinition GetTileType(int typeId)
        {
            if (_tileTypes == null || typeId < 0 || typeId >= _tileTypes.Length)
                return null;

            return _tileTypes[typeId];
        }

        public Sprite GetTileTypeSprite(int typeId)
        {
            Match3TileTypeDefinition definition = GetTileType(typeId);
            return definition != null ? definition.Sprite : null;
        }

        public Color GetTileTypeColor(int typeId)
        {
            Match3TileTypeDefinition definition = GetTileType(typeId);
            return definition != null ? definition.Color : Color.white;
        }

        public Match3.TileTypeGraphics GetTileTypeRuneGraphics(int typeId)
        {
            Match3TileTypeDefinition definition = GetTileType(typeId);
            return definition != null ? definition.RuneGraphics : null;
        }

        public Match3.TileTypeGraphics GetTileTypeTileGraphics(int typeId)
        {
            Match3TileTypeDefinition definition = GetTileType(typeId);
            return definition != null ? definition.TileGraphics : null;
        }
    }
}
