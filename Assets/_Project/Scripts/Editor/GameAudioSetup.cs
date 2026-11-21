using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace M3P.Editor
{
    public static class GameAudioSetup
    {
        const string MixerPath = "Assets/_Project/Audio/GameAudio.mixer";
        const string LibraryPath = "Assets/_Project/Audio/MusicLibrary.asset";
        const string ConfigPath = "Assets/_Project/Configs/GameConfig.asset";
        const string MenuPrefabPath = "Assets/_Project/Prefabs/UI/Menu/MainMenu.prefab";

        const BindingFlags All =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("M3P/Audio/Setup Music and Mixers")]
        public static void Build()
        {
            string report = BuildAssets();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Game audio", report, "OK");
        }

        public static string BuildAssets()
        {
            var log = new StringBuilder();
            ConfigureClips(log);
            AudioMixer mixer = EnsureMixer(log);
            MusicLibrary library = EnsureLibrary(log);
            AssignConfig(mixer, library, log);
            EnsureMixerView(mixer, log);
            EnsureMenu(log);
            PolishMenu(log);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return log.ToString();
        }

        static void ConfigureClips(StringBuilder log)
        {
            ConfigureClip("Assets/_Project/Audio/Music/menuSong.mp3", log);
            ConfigureClip("Assets/_Project/Audio/Music/mapSong.mp3", log);
            ConfigureClip("Assets/_Project/Audio/Music/dungeonArena.mp3", log);
            ConfigureClip("Assets/_Project/Audio/Music/VolcanicArena.mp3", log);
            ConfigureClip("Assets/_Project/Audio/Music/FrostArena.mp3", log);
        }

        static void ConfigureClip(string path, StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                log.AppendLine("Missing clip " + path);
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = false;
            if (settings.loadType != AudioClipLoadType.Streaming || settings.preloadAudioData)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                changed = true;
            }

            if (!importer.loadInBackground)
            {
                importer.loadInBackground = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                log.AppendLine("Streaming " + path);
            }
        }

        static AudioMixer EnsureMixer(StringBuilder log)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Type controllerType = Require("UnityEditor.Audio.AudioMixerController");
            object controller;
            if (mixer == null)
            {
                controller = Method(controllerType, "CreateMixerControllerAtPath")
                    .Invoke(null, new object[] { MixerPath });
                mixer = (AudioMixer)controller;
                log.AppendLine("Created " + MixerPath);
            }
            else
            {
                controller = mixer;
                log.AppendLine("Mixer already present.");
            }

            object master = controller.GetType().GetProperty("masterGroup", All).GetValue(controller);
            object music = FindOrCreateGroup(mixer, controller, master, GameAudio.MusicGroupName, log);
            object sfx = FindOrCreateGroup(mixer, controller, master, GameAudio.SfxGroupName, log);
            ExposeVolume(controller, music, GameAudio.MusicVolumeParameter, log);
            ExposeVolume(controller, sfx, GameAudio.SfxVolumeParameter, log);
            EditorUtility.SetDirty(mixer);
            return mixer;
        }

        static void EnsureMixerView(AudioMixer mixer, StringBuilder log)
        {
            var serialized = new SerializedObject(mixer);
            SerializedProperty views = serialized.FindProperty("m_AudioMixerGroupViews");
            if (views == null || !views.isArray)
            {
                log.AppendLine("Mixer view property is missing.");
                return;
            }

            if (views.arraySize == 0)
                views.arraySize = 1;

            SerializedProperty view = views.GetArrayElementAtIndex(0);
            SerializedProperty name = view.FindPropertyRelative("name");
            if (name != null)
                name.stringValue = "View";

            SerializedProperty guids = view.FindPropertyRelative("guids");
            string[] ids =
            {
                GroupId(mixer, "Master"),
                GroupId(mixer, GameAudio.MusicGroupName),
                GroupId(mixer, GameAudio.SfxGroupName)
            };

            if (guids == null || !guids.isArray)
            {
                log.AppendLine("Mixer view guids property is missing.");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            if (guids.arraySize >= ids.Length && !IsEmptyGuid(guids.GetArrayElementAtIndex(0)))
            {
                log.AppendLine("Mixer view already lists groups.");
                return;
            }

            guids.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
                AssignGuid(guids.GetArrayElementAtIndex(i), ids[i], log);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mixer);
            log.AppendLine("Mixer view lists Master, Music and SFX.");
        }

        static string GroupId(AudioMixer mixer, string groupName)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null || groups[i].name != groupName)
                    continue;

                PropertyInfo property = groups[i].GetType().GetProperty("groupID", All);
                if (property == null)
                    return string.Empty;

                object value = property.GetValue(groups[i]);
                return value != null ? value.ToString().Replace("-", string.Empty) : string.Empty;
            }

            return string.Empty;
        }

        static void AssignGuid(SerializedProperty property, string hex, StringBuilder log)
        {
            Type guidType = typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.GUID");
            if (guidType == null)
                guidType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUID");

            if (guidType == null || string.IsNullOrEmpty(hex))
            {
                log.AppendLine("Could not resolve mixer group id " + hex);
                return;
            }

            object guid = null;
            ConstructorInfo constructor = guidType.GetConstructor(new[] { typeof(string) });
            if (constructor != null)
                guid = constructor.Invoke(new object[] { hex });
            else
            {
                MethodInfo parse = guidType.GetMethod("Parse", All, null, new[] { typeof(string) }, null);
                if (parse != null)
                    guid = parse.Invoke(null, new object[] { hex });
            }

            PropertyInfo boxed = typeof(SerializedProperty).GetProperty("boxedValue", All);
            if (guid == null || boxed == null)
            {
                log.AppendLine("Could not write mixer view guid.");
                return;
            }

            boxed.SetValue(property, guid);
        }

        static bool IsEmptyGuid(SerializedProperty property)
        {
            PropertyInfo boxed = typeof(SerializedProperty).GetProperty("boxedValue", All);
            if (boxed == null)
                return true;

            object value = boxed.GetValue(property);
            if (value == null)
                return true;

            string hex = value.ToString().Replace("-", string.Empty);
            return hex.Length == 0 || hex == "00000000000000000000000000000000";
        }

        static void PolishMenu(StringBuilder log)
        {
            const string framePath = "Assets/_Project/Graphics/UI/Panels/CraftTile/Frames/tile-normal.png";
            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
            Sprite white = EnsureWhiteSprite();
            GameObject root = PrefabUtility.LoadPrefabContents(MenuPrefabPath);
            try
            {
                MenuSoundSettings settings = root.GetComponentInChildren<MenuSoundSettings>(true);
                if (settings == null)
                {
                    log.AppendLine("No sound section to polish.");
                    return;
                }

                int layer = settings.transform.parent != null
                    ? settings.transform.parent.gameObject.layer
                    : 5;
                SetUiLayer(settings.transform, layer);

                Image nameField = null;
                Image[] menuImages = root.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < menuImages.Length; i++)
                {
                    if (menuImages[i] != null && menuImages[i].gameObject.name == "NameField")
                        nameField = menuImages[i];
                }

                float frameScale = nameField != null ? nameField.pixelsPerUnitMultiplier : 22f;
                Image[] images = settings.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    string objectName = images[i].gameObject.name;
                    if (objectName == "MusicSlider" || objectName == "SfxSlider")
                    {
                        images[i].sprite = frame != null ? frame : images[i].sprite;
                        images[i].type = Image.Type.Sliced;
                        images[i].pixelsPerUnitMultiplier = frameScale;
                        images[i].color = Color.white;
                    }
                    else if (objectName == "Fill" && white != null)
                    {
                        images[i].sprite = white;
                        images[i].type = Image.Type.Simple;
                        images[i].color = new Color(0.96f, 0.78f, 0.38f, 1f);
                    }
                    else if (objectName == "Handle" && white != null)
                    {
                        images[i].sprite = white;
                        images[i].type = Image.Type.Simple;
                        images[i].color = new Color(0.96f, 0.93f, 0.84f, 1f);
                    }
                }

                RectTransform rect = settings.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                if (rect.sizeDelta.y < 80f)
                    rect.sizeDelta = new Vector2(320f, 130f);

                LayoutElement element = settings.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.preferredWidth = 320f;
                    element.minHeight = 120f;
                    element.preferredHeight = Mathf.Max(130f, rect.sizeDelta.y);
                }

                PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
                log.AppendLine("Polished sound sliders. Height " + rect.sizeDelta.y.ToString("0"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite EnsureWhiteSprite()
        {
            const string path = "Assets/_Project/Graphics/UI/Common/ui-white.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void SetUiLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetUiLayer(root.GetChild(i), layer);
        }

        static object FindOrCreateGroup(
            AudioMixer mixer,
            object controller,
            object master,
            string groupName,
            StringBuilder log)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].name == groupName)
                    return groups[i];
            }

            object group = Method(controller.GetType(), "CreateNewGroup")
                .Invoke(controller, new object[] { groupName, false });
            Method(controller.GetType(), "AddChildToParent")
                .Invoke(controller, new object[] { group, master });
            try
            {
                Method(controller.GetType(), "AddGroupToCurrentView").Invoke(controller, new object[] { group });
            }
            catch (Exception exception)
            {
                log.AppendLine(groupName + " view: " + exception.GetBaseException().Message);
            }

            log.AppendLine("Added group " + groupName);
            return group;
        }

        static void ExposeVolume(
            object controller,
            object group,
            string parameterName,
            StringBuilder log)
        {
            object guid = Method(group.GetType(), "GetGUIDForVolume").Invoke(group, null);
            if (HasExposed(controller, parameterName))
            {
                log.AppendLine(parameterName + " already exposed.");
                return;
            }

            Type pathType = Require("UnityEditor.Audio.AudioGroupParameterPath");
            ConstructorInfo constructor = null;
            ConstructorInfo[] constructors = pathType.GetConstructors(All);
            for (int i = 0; i < constructors.Length; i++)
            {
                if (constructors[i].GetParameters().Length == 2)
                {
                    constructor = constructors[i];
                    break;
                }
            }

            if (constructor == null)
                throw new InvalidOperationException("AudioGroupParameterPath constructor is missing.");

            object path = constructor.Invoke(new[] { group, guid });
            Method(controller.GetType(), "AddExposedParameter").Invoke(controller, new[] { path });
            RenameExposed(controller, guid, parameterName);
            if (!HasExposed(controller, parameterName))
                log.AppendLine("WARNING: " + parameterName + " was not exposed.");
            else
                log.AppendLine("Exposed " + parameterName);
        }

        static bool HasExposed(object controller, string parameterName)
        {
            PropertyInfo exposed = controller.GetType().GetProperty("exposedParameters", All);
            if (exposed == null)
                return false;

            var parameters = (Array)exposed.GetValue(controller);
            if (parameters == null)
                return false;

            FieldInfo nameField = parameters.GetType().GetElementType().GetField("name", All);
            if (nameField == null)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                object boxed = parameters.GetValue(i);
                if ((string)nameField.GetValue(boxed) == parameterName)
                    return true;
            }

            return false;
        }

        static void RenameExposed(object controller, object guid, string parameterName)
        {
            PropertyInfo exposed = controller.GetType().GetProperty("exposedParameters", All);
            if (exposed == null)
                throw new InvalidOperationException("exposedParameters is missing.");

            var parameters = (Array)exposed.GetValue(controller);
            Type element = parameters.GetType().GetElementType();
            FieldInfo nameField = element.GetField("name", All);
            FieldInfo guidField = element.GetField("guid", All);
            if (nameField == null || guidField == null)
                throw new InvalidOperationException("Exposed parameter fields are missing.");

            for (int i = 0; i < parameters.Length; i++)
            {
                object boxed = parameters.GetValue(i);
                object parameterGuid = guidField.GetValue(boxed);
                if (parameterGuid == null || !SameGuid(parameterGuid, guid))
                    continue;

                nameField.SetValue(boxed, parameterName);
                parameters.SetValue(boxed, i);
            }

            exposed.SetValue(controller, parameters);
            MethodInfo changed = controller.GetType().GetMethod("OnChangedExposedParameter", All);
            if (changed != null)
                changed.Invoke(controller, null);
        }

        static bool SameGuid(object left, object right)
        {
            if (left.Equals(right))
                return true;

            return string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        static MusicLibrary EnsureLibrary(StringBuilder log)
        {
            MusicLibrary library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<MusicLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
                log.AppendLine("Created " + LibraryPath);
            }
            else
            {
                log.AppendLine("Music library already present.");
            }

            var serialized = new SerializedObject(library);
            AssignClip(serialized, "_menu", "Assets/_Project/Audio/Music/menuSong.mp3");
            AssignClip(serialized, "_map", "Assets/_Project/Audio/Music/mapSong.mp3");
            AssignClip(serialized, "_dungeon", "Assets/_Project/Audio/Music/dungeonArena.mp3");
            AssignClip(serialized, "_lava", "Assets/_Project/Audio/Music/VolcanicArena.mp3");
            AssignClip(serialized, "_frost", "Assets/_Project/Audio/Music/FrostArena.mp3");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            return library;
        }

        static void AssignClip(SerializedObject serialized, string propertyName, string path)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException("Missing " + propertyName);

            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static void AssignConfig(AudioMixer mixer, MusicLibrary library, StringBuilder log)
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                log.AppendLine("Missing " + ConfigPath);
                return;
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("_mixer").objectReferenceValue = mixer;
            serialized.FindProperty("_music").objectReferenceValue = library;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            log.AppendLine("Assigned mixer and music on GameConfig.");
        }

        static void EnsureMenu(StringBuilder log)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MenuPrefabPath);
            try
            {
                bool created = root.GetComponentInChildren<MenuSoundSettings>(true) == null;
                MenuSoundSettings settings = MenuSoundSettings.Ensure(root.transform);
                if (settings != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(settings.GetComponent<RectTransform>());
                PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
                log.AppendLine(created
                    ? "Added Sound section to MainMenu."
                    : "MainMenu already has a Sound section.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Type Require(string name)
        {
            Type type = typeof(UnityEditor.Editor).Assembly.GetType(name);
            if (type == null)
                throw new InvalidOperationException("Missing type " + name);
            return type;
        }

        static MethodInfo Method(Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, All);
            if (method == null)
                throw new MissingMethodException(type.FullName, name);
            return method;
        }
    }
}
