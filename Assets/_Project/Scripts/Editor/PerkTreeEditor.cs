#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(PerkTree), true)]
    public class PerkTreeEditor : UnityEditor.Editor
    {
        static Type[] s_logicTypes;

        SerializedProperty _nazwa;
        SerializedProperty _id;
        SerializedProperty _perks;
        SerializedProperty _statBonus;
        SerializedProperty _icon;

        void OnEnable()
        {
            _nazwa = serializedObject.FindProperty("Nazwa");
            _id = serializedObject.FindProperty("id");
            _perks = serializedObject.FindProperty("perks");
            _statBonus = serializedObject.FindProperty("TreeStatBonusLogic");
            _icon = serializedObject.FindProperty("_icon");
            CacheLogicTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_nazwa, new GUIContent("Nazwa"));
            EditorGUILayout.PropertyField(_id);
            EditorGUILayout.PropertyField(_icon);
            EditorGUILayout.PropertyField(_perks, true);
            DrawLogicSection();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawLogicSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stat Bonus", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                string label = _statBonus.managedReferenceValue != null
                    ? _statBonus.managedReferenceValue.GetType().Name
                    : "None";

                EditorGUILayout.PrefixLabel("Logic");
                EditorGUILayout.LabelField(label);

                if (GUILayout.Button("Choose", GUILayout.Width(70f)))
                    ShowLogicMenu();
            }

            if (_statBonus.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Choose how this tree grants hard stats.", MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(_statBonus, GUIContent.none, true);
        }

        void ShowLogicMenu()
        {
            CacheLogicTypes();
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
            _statBonus.managedReferenceValue = Activator.CreateInstance(logicType);
            serializedObject.ApplyModifiedProperties();
        }

        static void CacheLogicTypes()
        {
            if (s_logicTypes != null)
                return;

            s_logicTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && typeof(TreeStatBonusLogic).IsAssignableFrom(type))
                .OrderBy(type => type.Name)
                .ToArray();
        }
    }
}
#endif
