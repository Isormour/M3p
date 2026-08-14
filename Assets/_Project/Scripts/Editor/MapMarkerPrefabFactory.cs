#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace M3P.Editor
{
    /// <summary>One-shot helpers to author default map marker prefabs for EncounterConfig.</summary>
    public static class MapMarkerPrefabFactory
    {
        const string PrefabFolder = "Assets/_Project/Prefabs/Map";

        [MenuItem("M3P/Map/Create Default Marker Prefabs")]
        public static void CreateDefaultMarkers()
        {
            EnsureFolder(PrefabFolder);

            CreateMarker("Marker_Start", new Color(0.75f, 0.75f, 0.8f));
            CreateMarker("Marker_Battle", new Color(0.85f, 0.25f, 0.22f));
            CreateMarker("Marker_Shop", new Color(0.25f, 0.45f, 0.9f));
            CreateMarker("Marker_Chest", new Color(0.95f, 0.75f, 0.2f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created default map marker prefabs under " + PrefabFolder);
        }

        static void CreateMarker(string name, Color color)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = name;
            root.transform.localScale = Vector3.one * 1.1f;

            var renderer = root.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                renderer.sharedMaterial = new Material(shader) { color = color };
            }

            if (root.GetComponent<MapNode>() == null)
                root.AddComponent<MapNode>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
