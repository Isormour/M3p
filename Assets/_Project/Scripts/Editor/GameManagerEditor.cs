#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(GameManager))]
    public sealed class GameManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profile Debug", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Save: {ProfileManager.SavePath}", MessageType.None);

            using (new EditorGUI.DisabledScope(target is GameManager gm && gm.Config == null))
            {
                if (GUILayout.Button("Reset Profile Save"))
                {
                    if (EditorUtility.DisplayDialog(
                            "Reset Profile Save",
                            "Delete the saved player profile and recreate it from PlayerStartConfig?",
                            "Reset",
                            "Cancel"))
                    {
                        ((GameManager)target).ResetProfileSave();
                    }
                }
            }
        }
    }
}
#endif
