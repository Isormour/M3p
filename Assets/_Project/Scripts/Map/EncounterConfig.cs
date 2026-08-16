using UnityEngine;

namespace M3P
{
    /// <summary>
    /// One map-room definition: type, (for battles) the enemy sent to the Battle scene,
    /// and (for chests) the loot table. Map markers are chosen by <see cref="MapNodeType"/>
    /// in <see cref="MapManager"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EncounterConfig", menuName = "M3P/Encounter Config", order = 21)]
    public class EncounterConfig : ScriptableObject
    {
        [SerializeField] MapNodeType _type = MapNodeType.Battle;

        [Tooltip("Enemy fought when Type is Battle. Ignored for other types.")]
        [SerializeField] EnemyDefinition _enemy;

        [Tooltip("Loot granted when Type is Chest. Ignored for other types.")]
        [SerializeField] ChestConfig _chest;

        public MapNodeType Type => _type;

        public EnemyDefinition Enemy => _enemy;

        public ChestConfig Chest => _chest;

        public bool IsBattle => _type == MapNodeType.Battle;

        public bool IsChest => _type == MapNodeType.Chest;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_type == MapNodeType.Battle && _enemy == null)
            {
                Debug.LogWarning(
                    $"{nameof(EncounterConfig)} '{name}': Battle encounters should assign an {nameof(EnemyDefinition)}.",
                    this);
            }

            if (_type == MapNodeType.Chest && _chest == null)
            {
                Debug.LogWarning(
                    $"{nameof(EncounterConfig)} '{name}': Chest encounters should assign a {nameof(ChestConfig)}.",
                    this);
            }
        }
#endif
    }
}
