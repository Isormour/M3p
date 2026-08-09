using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "M3P/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] Match3TileTypeDefinition[] _tileTypes;

        [Header("Progression")]
        [Tooltip("Experience curve and level-up rewards. Built-in defaults are used when left empty.")]
        [SerializeField] LevelProgressionConfig _levelProgression;

        [Tooltip("Every skill in the game, and the ids profiles use to reference them.")]
        [SerializeField] SkillConfig _skills;

        [Tooltip("Stats and skills a profile starts with, before anything has been saved.")]
        [SerializeField] PlayerStartConfig _playerStart;

        [Tooltip("How cleared matches convert into shards. Built-in defaults are used when left empty.")]
        [SerializeField] MatchRewardRules _matchRewards;

        [Header("Basic Attack")]
        [Tooltip("Damage added per point of Strength.")]
        [SerializeField] int _damagePerStrength = 1;

        [Tooltip("Damage added per matched tile above the minimum-2 threshold, so a match of 3 scores one step.")]
        [SerializeField] int _damagePerMatchedTile = 1;

        Dictionary<Match3TileTypeDefinition, int> _tileTypeIds;

        public Match3TileTypeDefinition[] TileTypes => _tileTypes;

        public LevelProgressionConfig LevelProgression => _levelProgression;

        public SkillConfig Skills => _skills;

        public PlayerStartConfig PlayerStart => _playerStart;

        public MatchRewardRules MatchRewards => _matchRewards;

        public int TileTypeCount => _tileTypes != null ? _tileTypes.Length : 0;

        public Match3TileTypeDefinition GetTileType(int typeId)
        {
            if (_tileTypes == null || typeId < 0 || typeId >= _tileTypes.Length)
                return null;

            return _tileTypes[typeId];
        }

        /// <summary>
        /// Runtime id used by the board and mana pools, or -1 when the tile type is not part of this config.
        /// </summary>
        public int GetTileTypeId(Match3TileTypeDefinition tileType)
        {
            if (tileType == null)
                return -1;

            EnsureTileTypeIds();
            return _tileTypeIds.TryGetValue(tileType, out int typeId) ? typeId : -1;
        }

        /// <summary>
        /// Stable name a tile type is stored under in saves. Unlike the runtime id this survives
        /// reordering <see cref="TileTypes"/>, so it is what the shard wallet keys on.
        /// </summary>
        public string GetTileTypeKey(int typeId)
        {
            Match3TileTypeDefinition definition = GetTileType(typeId);
            return definition != null ? definition.name : null;
        }

        /// <summary>Runtime id for a saved tile type name, or -1 when no such tile type exists any more.</summary>
        public int GetTileTypeIdByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return -1;

            for (int i = 0; i < TileTypeCount; i++)
            {
                Match3TileTypeDefinition definition = _tileTypes[i];
                if (definition != null && definition.name == key)
                    return i;
            }

            return -1;
        }

        void EnsureTileTypeIds()
        {
            if (_tileTypeIds != null)
                return;

            _tileTypeIds = new Dictionary<Match3TileTypeDefinition, int>(TileTypeCount);

            for (int i = 0; i < TileTypeCount; i++)
            {
                Match3TileTypeDefinition definition = _tileTypes[i];
                if (definition != null)
                    _tileTypeIds[definition] = i;
            }
        }

        /// <summary>
        /// Damage of a single basic attack. One attack fires per match group, so a three-wave cascade
        /// resolves as three separate attacks rather than one larger hit.
        /// </summary>
        public int CalculateBasicAttackDamage(HardStats attacker, int matchSize)
        {
            int lengthBonus = _damagePerMatchedTile * (matchSize - (Match3Board.MinimumMatchSize - 1));
            return Mathf.Max(1, attacker.Strength * _damagePerStrength + lengthBonus);
        }

        void OnEnable()
        {
            _tileTypeIds = null;
        }

        void OnValidate()
        {
            _tileTypeIds = null;
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
