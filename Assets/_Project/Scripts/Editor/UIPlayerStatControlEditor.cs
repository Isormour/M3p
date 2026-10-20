#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(UIPlayerStatControl))]
    public sealed class UIPlayerStatControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            UIPlayerStatControl control = (UIPlayerStatControl)target;
            bool canPreview = Application.isPlaying && control.isActiveAndEnabled;

            using (new EditorGUI.DisabledScope(!canPreview))
            {
                if (GUILayout.Button("Lock"))
                    control.SetLocked(true);

                if (GUILayout.Button("Unlock"))
                    control.Unlock();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to preview the lock animation.", MessageType.None);
        }
    }
}
#endif
