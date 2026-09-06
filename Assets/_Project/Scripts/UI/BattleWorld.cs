using UnityEngine;

namespace M3P
{
    public sealed class BattleWorld : MonoBehaviour
    {
        [SerializeField] GameObject _playerObject;
        [Tooltip("Parent for spawned enemy models; defaults to this transform.")]
        [SerializeField] Transform _enemyModelParent;
        [Tooltip("Chest-height point on the player. Defaults to the player object.")]
        [SerializeField] Transform _playerVfxPoint;
        [Tooltip("Chest-height point on the enemy. Defaults to the enemy model parent.")]
        [SerializeField] Transform _enemyVfxPoint;

        WorldCharacter _playerCharacter;
        WorldCharacter _enemyCharacter;
        GameObject _spawnedEnemyModel;

        public Transform PlayerVfxPoint =>
            _playerVfxPoint != null
                ? _playerVfxPoint
                : (_playerObject != null ? _playerObject.transform : null);

        public Transform EnemyVfxPoint =>
            _enemyVfxPoint != null
                ? _enemyVfxPoint
                : (_enemyCharacter != null ? _enemyCharacter.transform : _enemyModelParent);

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

        public void NotifyPlayerHit(bool died, int damage)
        {
            _playerCharacter?.PlayHitReaction(died);
            ShakeFromHit(PlayerVfxPoint, damage);
        }

        public void NotifyEnemyHit(bool died, int damage)
        {
            _enemyCharacter?.PlayHitReaction(died);
            ShakeFromHit(EnemyVfxPoint, damage);
        }

        static void ShakeFromHit(Transform source, int damage)
        {
            if (source == null || damage <= 0 || BattleManager.Instance == null)
                return;

            BattleManager.Instance.ShakeCameraFromHit(source.position, damage);
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
