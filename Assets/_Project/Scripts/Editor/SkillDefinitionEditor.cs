#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace M3P.Editor
{
    [CustomEditor(typeof(SkillDefinition))]
    public class SkillDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "A Control clip scrubs its particle system to the playhead. Turn on Preview to see that frame in the prefab. Leave the particle object enabled. An Activation track only turns an object on and off. A Projectile clip releases the attack projectile. A clip on Caster plays on whoever uses the skill; until then the fallback trigger plays.",
                MessageType.Info);

            serializedObject.Update();
            SerializedProperty directorProperty = serializedObject.FindProperty("_playableDirector");
            bool hasDirector = directorProperty.objectReferenceValue != null;
            if (GUILayout.Button(hasDirector ? "Open Timeline Prefab" : "Create Timeline"))
            {
                if (hasDirector)
                    OpenTimeline(directorProperty.objectReferenceValue as PlayableDirector);
                else
                    CreateTimeline((SkillDefinition)target, directorProperty);
            }
        }

        void CreateTimeline(SkillDefinition skill, SerializedProperty directorProperty)
        {
            string skillPath = AssetDatabase.GetAssetPath(skill);
            string folder = Path.GetDirectoryName(skillPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder))
                return;

            string stem = NextFreeStem(folder, skill.name + " Timeline");
            string playablePath = folder + "/" + stem + ".playable";
            string prefabPath = folder + "/" + stem + ".prefab";

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, playablePath);

            timeline.CreateTrack<AnimationTrack>(null, "Caster");

            ProjectileTrack projectileTrack = timeline.CreateTrack<ProjectileTrack>(null, "Projectile");
            TimelineClip projectileClip = projectileTrack.CreateDefaultClip();
            if (projectileClip != null)
            {
                projectileClip.start = 0.2d;
                projectileClip.duration = 0.45d;
                projectileClip.displayName = "Launch";
            }

            GameObject root = new GameObject(stem);
            GameObject prefab = null;
            try
            {
                PlayableDirector director = root.AddComponent<PlayableDirector>();
                director.playableAsset = timeline;
                director.playOnAwake = false;
                director.extrapolationMode = DirectorWrapMode.None;

                GameObject particles = new GameObject("Particles");
                particles.transform.SetParent(root.transform, false);
                ParticleSystem particleSystem = particles.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particleSystem.main;
                main.playOnAwake = false;
                BindParticleControl(director, timeline, particles);

                prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                DestroyImmediate(root);
            }

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            PlayableDirector prefabDirector = prefab != null ? prefab.GetComponent<PlayableDirector>() : null;
            if (prefabDirector == null)
                return;

            directorProperty.objectReferenceValue = prefabDirector;
            serializedObject.ApplyModifiedProperties();
            OpenTimeline(prefabDirector);
        }

        static void OpenTimeline(PlayableDirector director)
        {
            if (director == null)
                return;

            string path = AssetDatabase.GetAssetPath(director);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError(
                    "Playable Director must be a prefab asset. Scene objects cannot be shared by player and enemies.",
                    director);
                return;
            }

            PrefabStage stage = PrefabStageUtility.OpenPrefab(path);
            if (stage == null || stage.prefabContentsRoot == null)
                return;

            PlayableDirector stageDirector = stage.prefabContentsRoot.GetComponent<PlayableDirector>();
            if (stageDirector == null)
                stageDirector = stage.prefabContentsRoot.GetComponentInChildren<PlayableDirector>(true);

            Selection.activeGameObject = stageDirector != null
                ? stageDirector.gameObject
                : stage.prefabContentsRoot;
            TimelineEditor.GetOrCreateWindow();
        }

        static void BindParticleControl(PlayableDirector director, TimelineAsset timeline, GameObject particles)
        {
            ControlTrack track = timeline.CreateTrack<ControlTrack>(null, "Particles");
            TimelineClip clip = track.CreateDefaultClip();
            if (clip == null)
                return;

            ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
            float particleDuration = particleSystem != null ? particleSystem.main.duration : 1.5f;
            clip.start = 0d;
            clip.duration = Mathf.Max(1.5f, particleDuration);
            clip.displayName = "Particles";

            ControlPlayableAsset asset = clip.asset as ControlPlayableAsset;
            if (asset == null)
                return;

            asset.updateParticle = true;
            asset.searchHierarchy = true;
            asset.active = false;
            asset.updateDirector = false;

            ExposedReference<GameObject> reference = asset.sourceGameObject;
            reference.exposedName = new PropertyName(GUID.Generate().ToString());
            asset.sourceGameObject = reference;
            director.SetReferenceValue(reference.exposedName, particles);
            EditorUtility.SetDirty(asset);
        }

        static string NextFreeStem(string folder, string stem)
        {
            string candidate = stem;
            int index = 2;
            while (AssetDatabase.LoadAssetAtPath<Object>(folder + "/" + candidate + ".playable") != null
                || AssetDatabase.LoadAssetAtPath<Object>(folder + "/" + candidate + ".prefab") != null)
            {
                candidate = stem + " " + index;
                index++;
            }

            return candidate;
        }
    }
}
#endif
