using UnityEngine;

namespace M3P
{
    public sealed class BattleWorld : MonoBehaviour
    {
        [SerializeField] GameObject _playerObject;
        [Tooltip("Parent for spawned enemy models; defaults to this transform.")]
        [SerializeField] Transform _enemyModelParent;

        WorldCharacter _playerCharacter;
        WorldCharacter _enemyCharacter;
        GameObject _spawnedEnemyModel;

        void Awake()
        {
            _playerCharacter = ResolveWorldCharacter(_playerObject);
        }

        public void SpawnEnemyModel(GameObject prefab)
        {
            ClearEnemyModel();

            if (prefab == null)
                return;

            Transform parent = _enemyModelParent != null ? _enemyModelParent : transform;
            _spawnedEnemyModel = Instantiate(prefab, parent);
            _spawnedEnemyModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _enemyCharacter = ResolveWorldCharacter(_spawnedEnemyModel);
        }

        public void ClearEnemyModel()
        {
            if (_spawnedEnemyModel != null)
            {
                Destroy(_spawnedEnemyModel);
                _spawnedEnemyModel = null;
            }

            _enemyCharacter = null;
        }

        public void NotifyPlayerSkillUsed(SkillDefinition skill)
        {
            PlayCharacterAttack(_playerCharacter, skill);
        }

        public void NotifyEnemySkillUsed(SkillDefinition skill)
        {
            PlayCharacterAttack(_enemyCharacter, skill);
        }

        public void NotifyMatchWave(int tilesDestroyed)
        {
            if (tilesDestroyed > 0)
                _playerCharacter?.PlayAttack("BasicAttack");
        }

        static void PlayCharacterAttack(WorldCharacter character, SkillDefinition skill)
        {
            if (character == null)
                return;

            string triggerName = skill != null && !string.IsNullOrEmpty(skill._animationName)
                ? skill._animationName
                : "BasicAttack";

            character.PlayAttack(triggerName);
        }

        static WorldCharacter ResolveWorldCharacter(GameObject root)
        {
            if (root == null)
                return null;

            WorldCharacter character = root.GetComponent<WorldCharacter>();
            return character != null ? character : root.GetComponentInChildren<WorldCharacter>();
        }
    }
}
