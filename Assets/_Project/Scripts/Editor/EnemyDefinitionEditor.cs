#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(EnemyDefinition))]
    public sealed class EnemyDefinitionEditor : UnityEditor.Editor
    {
        StatProgressionConfig _statProgression;

        void OnEnable()
        {
            _statProgression = FindStatProgression();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DrawSoftStats();
        }

        void DrawSoftStats()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Soft Stats (Floor 1)", EditorStyles.boldLabel);

            if (_statProgression == null)
                _statProgression = FindStatProgression();

            if (_statProgression == null)
            {
                EditorGUILayout.HelpBox(
                    "No Stat Progression Config was found, so soft stats cannot be calculated.",
                    MessageType.Warning);
                return;
            }

            var definition = (EnemyDefinition)target;
            SoftStatValues stats = _statProgression.CalculateSoftStats(definition.HardStats);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Basic Attack Damage", stats.BasicAttackDamage);
                EditorGUILayout.IntField("Max HP", stats.MaxHP);
                EditorGUILayout.IntField("Max Action Points", stats.MaxActionPoints);
                EditorGUILayout.IntField("Max Hand Size", stats.MaxHandSize);
            }
        }

        static StatProgressionConfig FindStatProgression()
        {
            if (GameManager.Instance != null && GameManager.Instance.Config != null)
                return GameManager.Instance.Config.StatProgression;

            string[] gameConfigGuids = AssetDatabase.FindAssets("t:GameConfig");
            for (int i = 0; i < gameConfigGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(gameConfigGuids[i]);
                GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                if (config != null)
                    return config.StatProgression;
            }

            string[] progressionGuids = AssetDatabase.FindAssets("t:StatProgressionConfig");
            if (progressionGuids.Length == 0)
                return null;

            string progressionPath = AssetDatabase.GUIDToAssetPath(progressionGuids[0]);
            return AssetDatabase.LoadAssetAtPath<StatProgressionConfig>(progressionPath);
        }
    }
}
#endif
