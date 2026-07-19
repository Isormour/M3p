using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "PlayerSkillDefinition", menuName = "M3P/Player Skill Definition", order = 2)]
    public class PlayerSkillDefinition : SkillDefinition
    {
        [SerializeField] int _gridWidth = 1;
        [SerializeField] int _gridHeight = 1;
        [SerializeField] bool[] _affectedTiles = { true };

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        public bool IsTileAffected(int x, int y)
        {
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return false;

            int index = y * _gridWidth + x;
            return index >= 0 && index < _affectedTiles.Length && _affectedTiles[index];
        }

        public bool TryGetAffectedTile(int x, int y, out bool affected)
        {
            affected = IsTileAffected(x, y);
            return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
        }

        void OnValidate()
        {
            _gridWidth = Mathf.Max(1, _gridWidth);
            _gridHeight = Mathf.Max(1, _gridHeight);

            int requiredLength = _gridWidth * _gridHeight;
            if (_affectedTiles == null || _affectedTiles.Length != requiredLength)
            {
                bool[] resized = new bool[requiredLength];
                if (_affectedTiles != null)
                {
                    int copyCount = Mathf.Min(_affectedTiles.Length, requiredLength);
                    for (int i = 0; i < copyCount; i++)
                        resized[i] = _affectedTiles[i];
                }

                if (requiredLength == 1 && (_affectedTiles == null || _affectedTiles.Length == 0))
                    resized[0] = true;

                _affectedTiles = resized;
            }
        }
    }
}
