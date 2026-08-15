#if UNITY_EDITOR
using System;
using System.Linq;
using Match3;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(BoardActionCardDefinition), true)]
    public sealed class BoardActionCardEditor : UnityEditor.Editor
    {
        static Type[] s_logicTypes;

        SerializedProperty _logic;

        void OnEnable()
        {
            _logic = serializedObject.FindProperty("_logic");
            CacheLogicTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_logic");
            DrawLogicSection();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawLogicSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Board Action", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                string label = _logic.managedReferenceValue != null
                    ? _logic.managedReferenceValue.GetType().Name
                    : "None";

                EditorGUILayout.PrefixLabel("Logic");
                EditorGUILayout.LabelField(label);

                if (GUILayout.Button("Choose", GUILayout.Width(70f)))
                    ShowLogicMenu();
            }

            if (_logic.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Choose the board action this card performs.", MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(_logic, GUIContent.none, true);
        }

        void ShowLogicMenu()
        {
            CacheLogicTypes();
            if (s_logicTypes == null || s_logicTypes.Length == 0)
                return;

            var menu = new GenericMenu();
            for (int i = 0; i < s_logicTypes.Length; i++)
            {
                Type logicType = s_logicTypes[i];
                menu.AddItem(new GUIContent(logicType.Name), false, () => SetLogic(logicType));
            }

            menu.ShowAsContext();
        }

        void SetLogic(Type logicType)
        {
            serializedObject.Update();
            _logic.managedReferenceValue = Activator.CreateInstance(logicType);
            serializedObject.ApplyModifiedProperties();
        }

        static void CacheLogicTypes()
        {
            if (s_logicTypes != null && s_logicTypes.Length > 0)
                return;

            s_logicTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && typeof(BoardActionLogic).IsAssignableFrom(type))
                .OrderBy(type => type.Name)
                .ToArray();
        }
    }
}
#endif
