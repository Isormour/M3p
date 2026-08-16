#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(TileUpgradeDefinition), true)]
    public class TileUpgradeEditor : UnityEditor.Editor
    {
        static Type[] s_logicTypes;

        SerializedProperty _displayName;
        SerializedProperty _description;
        SerializedProperty _icon;
        SerializedProperty _craftCost;
        SerializedProperty _logic;

        void OnEnable()
        {
            _displayName = serializedObject.FindProperty("_displayName");
            _description = serializedObject.FindProperty("_description");
            _icon = serializedObject.FindProperty("_icon");
            _craftCost = serializedObject.FindProperty("_craftCost");
            _logic = serializedObject.FindProperty("_logic");
            CacheLogicTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.PropertyField(_icon);
            EditorGUILayout.PropertyField(_craftCost, true);
            DrawLogicSection();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawLogicSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Implementation", EditorStyles.boldLabel);

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
                EditorGUILayout.HelpBox("Choose an implementation for this upgrade.", MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(_logic, GUIContent.none, true);
        }

        void ShowLogicMenu()
        {
            CacheLogicTypes();

            if (s_logicTypes == null || s_logicTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("No TileUpgradeLogic types found.", MessageType.Warning);
                return;
            }

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
                .Where(type => !type.IsAbstract && typeof(TileUpgradeLogic).IsAssignableFrom(type))
                .OrderBy(type => type.Name)
                .ToArray();
        }
    }
}
#endif
