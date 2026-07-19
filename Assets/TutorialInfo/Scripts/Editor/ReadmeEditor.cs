using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

/// <summary>
/// Separate from CustomEditor type so inspector lifecycle does not double-subscribe delayCall on domain reload quirks.
/// </summary>
[InitializeOnLoad]
static class ReadmeEditorBootstrap
{
    static ReadmeEditorBootstrap()
    {
        EditorApplication.delayCall += ReadmeEditor.SelectReadmeAutomatically;
    }
}

[CustomEditor(typeof(Readme))]
public class ReadmeEditor : Editor
{
    internal static string s_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
    
    internal static string s_ReadmeSourceDirectory = "Assets/TutorialInfo";

    internal const float k_Space = 16f;

    /// <summary>Called once after domain reload via <see cref="ReadmeEditorBootstrap"/>.</summary>
    internal static void SelectReadmeAutomatically()
    {
        if (SessionState.GetBool(s_ShowedReadmeSessionStateName, false))
            return;

        SessionState.SetBool(s_ShowedReadmeSessionStateName, true);

        Readme readme = SelectReadme();
        if (readme != null && !readme.loadedLayout)
        {
            LoadLayout();
            readme.loadedLayout = true;
            EditorUtility.SetDirty(readme);
        }
    }

    static void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",
            
            $"All contents under {s_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
            "Proceed",
            "Cancel"))
        {
            if (Directory.Exists(s_ReadmeSourceDirectory))
            {
                FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory);
                FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory + ".meta");
            }
            else
            {
                Debug.Log($"Could not find the Readme folder at {s_ReadmeSourceDirectory}");
            }

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                FileUtil.DeleteFileOrDirectory(path);
            }

            AssetDatabase.Refresh();
        }
    }

    static void LoadLayout()
    {
        Assembly assembly = typeof(EditorApplication).Assembly;
        Type windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", throwOnError: false);
        if (windowLayoutType == null)
            return;

        MethodInfo method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            return;

        string layoutPath = Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt");
        if (!File.Exists(layoutPath))
            return;

        method.Invoke(null, new object[] { layoutPath, false });
    }

    static Readme SelectReadme()
    {
        string[] ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids == null || ids.Length == 0)
        {
            Debug.Log("ReadmeEditor: no Readme assets found (search: 'Readme t:Readme').");
            return null;
        }

        Readme preferred = null;
        string preferredPath = null;

        foreach (string guid in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            UnityEngine.Object readmeObject = AssetDatabase.LoadMainAssetAtPath(path);
            if (readmeObject is not Readme readme)
            {
                Debug.LogWarning($"ReadmeEditor: skip '{path}' — not a Readme ScriptableObject (script missing?).");
                continue;
            }

            bool isDefaultPath = path == "Assets/Readme.asset";
            if (preferred == null || isDefaultPath)
            {
                preferred = readme;
                preferredPath = path;
                if (isDefaultPath)
                    break;
            }
        }

        if (preferred == null)
        {
            Debug.LogWarning("ReadmeEditor: no valid Readme assets after load; not changing selection.");
            return null;
        }

        if (ids.Length > 1)
            Debug.Log($"ReadmeEditor: multiple Readme assets found; using '{preferredPath}'.");

        Selection.objects = new UnityEngine.Object[] { preferred };
        return preferred;
    }

    protected override void OnHeaderGUI()
    {
        if (target is not Readme readme)
            return;

        Init();

        var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

        GUILayout.BeginHorizontal("In BigTitle");
        {
            if (readme.icon != null)
            {
                GUILayout.Space(k_Space);
                GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
            }
            GUILayout.Space(k_Space);
            GUILayout.BeginVertical();
            {

                GUILayout.FlexibleSpace();
                GUILayout.Label(readme.title, TitleStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
    }

    public override void OnInspectorGUI()
    {
        if (target is not Readme readme)
            return;

        Init();

        if (readme.sections == null)
            return;

        foreach (var section in readme.sections)
        {
            if (!string.IsNullOrEmpty(section.heading))
            {
                GUILayout.Label(section.heading, HeadingStyle);
            }

            if (!string.IsNullOrEmpty(section.text))
            {
                GUILayout.Label(section.text, BodyStyle);
            }

            if (!string.IsNullOrEmpty(section.linkText))
            {
                if (LinkLabel(new GUIContent(section.linkText)))
                {
                    Application.OpenURL(section.url);
                }
            }

            GUILayout.Space(k_Space);
        }

        if (GUILayout.Button("Remove Readme Assets", ButtonStyle))
        {
            RemoveTutorial();
        }
    }

    bool m_Initialized;

    GUIStyle LinkStyle
    {
        get { return m_LinkStyle; }
    }

    [SerializeField]
    GUIStyle m_LinkStyle;

    GUIStyle TitleStyle
    {
        get { return m_TitleStyle; }
    }

    [SerializeField]
    GUIStyle m_TitleStyle;

    GUIStyle HeadingStyle
    {
        get { return m_HeadingStyle; }
    }

    [SerializeField]
    GUIStyle m_HeadingStyle;

    GUIStyle BodyStyle
    {
        get { return m_BodyStyle; }
    }

    [SerializeField]
    GUIStyle m_BodyStyle;

    GUIStyle ButtonStyle
    {
        get { return m_ButtonStyle; }
    }

    [SerializeField]
    GUIStyle m_ButtonStyle;

    void Init()
    {
        if (m_Initialized)
            return;
        m_BodyStyle = new GUIStyle(EditorStyles.label);
        m_BodyStyle.wordWrap = true;
        m_BodyStyle.fontSize = 14;
        m_BodyStyle.richText = true;

        m_TitleStyle = new GUIStyle(m_BodyStyle);
        m_TitleStyle.fontSize = 26;

        m_HeadingStyle = new GUIStyle(m_BodyStyle);
        m_HeadingStyle.fontStyle = FontStyle.Bold;
        m_HeadingStyle.fontSize = 18;

        m_LinkStyle = new GUIStyle(m_BodyStyle);
        m_LinkStyle.wordWrap = false;

        // Match selection color which works nicely for both light and dark skins
        m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
        m_LinkStyle.stretchWidth = false;

        m_ButtonStyle = new GUIStyle(EditorStyles.miniButton);
        m_ButtonStyle.fontStyle = FontStyle.Bold;

        m_Initialized = true;
    }

    bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
    {
        var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

        Handles.BeginGUI();
        Handles.color = LinkStyle.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
        Handles.color = Color.white;
        Handles.EndGUI();

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, LinkStyle);
    }
}
