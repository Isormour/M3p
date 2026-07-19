#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    [CustomEditor(typeof(BattleEffect), true)]
    public class BattleEffectEditor : UnityEditor.Editor
    {
        static Type[] s_logicTypes;

        SerializedProperty _target;
        SerializedProperty _logic;

        void OnEnable()
        {
            _target = serializedObject.FindProperty("_target");
            _logic = serializedObject.FindProperty("_logic");
            CacheLogicTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_target);
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
                EditorGUILayout.HelpBox("Choose an implementation for this effect.", MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(_logic, GUIContent.none, true);
        }

        void ShowLogicMenu()
        {
            CacheLogicTypes();

            if (s_logicTypes == null || s_logicTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("No BattleEffectLogic types found.", MessageType.Warning);
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
                .Where(type => !type.IsAbstract && typeof(BattleEffectLogic).IsAssignableFrom(type))
                .OrderBy(type => type.Name)
                .ToArray();
        }
    }
}
#endif
