using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Music and SFX sliders on the main menu. Values are written to <see cref="AudioPreferences"/>.
    /// </summary>
    public sealed class MenuSoundSettings : MonoBehaviour
    {
        [SerializeField] Slider _music;
        [SerializeField] Slider _sfx;
        [SerializeField] TMP_Text _musicValue;
        [SerializeField] TMP_Text _sfxValue;

        bool _bound;

        public static MenuSoundSettings Ensure(Transform menuRoot)
        {
            if (menuRoot == null)
                return null;

            MenuSoundSettings existing = menuRoot.GetComponentInChildren<MenuSoundSettings>(true);
            if (existing != null)
                return existing;

            Transform layout = menuRoot.Find("Layout");
            if (layout == null)
                layout = menuRoot;

            return Build(layout, menuRoot);
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            Bind();
        }

        void OnDisable()
        {
            Unbind();
        }

        void Bind()
        {
            if (_bound || _music == null || _sfx == null)
                return;

            AudioPreferences.Load();
            _music.SetValueWithoutNotify(AudioPreferences.Music);
            _sfx.SetValueWithoutNotify(AudioPreferences.Sfx);
            SetPercent(_musicValue, AudioPreferences.Music);
            SetPercent(_sfxValue, AudioPreferences.Sfx);
            _music.onValueChanged.AddListener(OnMusic);
            _sfx.onValueChanged.AddListener(OnSfx);
            ReleaseHook hook = _sfx.GetComponent<ReleaseHook>();
            if (hook == null)
                hook = _sfx.gameObject.AddComponent<ReleaseHook>();
            hook.Released = OnSfxReleased;
            _bound = true;
        }

        void Unbind()
        {
            if (!_bound)
                return;

            if (_music != null)
                _music.onValueChanged.RemoveListener(OnMusic);
            if (_sfx != null)
            {
                _sfx.onValueChanged.RemoveListener(OnSfx);
                ReleaseHook hook = _sfx.GetComponent<ReleaseHook>();
                if (hook != null)
                    hook.Released = null;
            }
            _bound = false;
        }

        void OnMusic(float value)
        {
            AudioPreferences.SetMusic(value);
            SetPercent(_musicValue, value);
        }

        void OnSfx(float value)
        {
            AudioPreferences.SetSfx(value);
            SetPercent(_sfxValue, value);
        }

        void OnSfxReleased()
        {
            Sfx.Play(SfxId.MenuConfirm);
        }

        public void Setup(Slider music, Slider sfx, TMP_Text musicValue, TMP_Text sfxValue)
        {
            _music = music;
            _sfx = sfx;
            _musicValue = musicValue;
            _sfxValue = sfxValue;
            if (Application.isPlaying)
                Bind();
        }

        static void SetPercent(TMP_Text label, float value)
        {
            if (label != null)
                label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        static MenuSoundSettings Build(Transform layout, Transform menuRoot)
        {
            TMP_FontAsset font = null;
            TMP_Text sample = menuRoot.GetComponentInChildren<TMP_Text>(true);
            if (sample != null)
                font = sample.font;

            Image frameImage = null;
            Image[] images = menuRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == "NameField" && images[i].sprite != null)
                {
                    frameImage = images[i];
                    break;
                }
            }

            Sprite frame = frameImage != null ? frameImage.sprite : null;
            float frameScale = frameImage != null ? frameImage.pixelsPerUnitMultiplier : 1f;

            var sectionGo = new GameObject("SoundSettings", typeof(RectTransform));
            sectionGo.transform.SetParent(layout, false);
            sectionGo.transform.SetAsLastSibling();
            SetLayer(sectionGo.transform, layout.gameObject.layer);
            RectTransform sectionRect = sectionGo.GetComponent<RectTransform>();
            sectionRect.anchorMin = Vector2.zero;
            sectionRect.anchorMax = Vector2.zero;
            sectionRect.pivot = new Vector2(0.5f, 0.5f);
            sectionRect.sizeDelta = new Vector2(320f, 148f);

            VerticalLayoutGroup column = sectionGo.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(0, 0, 10, 0);
            column.spacing = 6f;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            LayoutElement sectionLayout = sectionGo.AddComponent<LayoutElement>();
            sectionLayout.preferredWidth = 320f;
            sectionLayout.preferredHeight = 130f;
            sectionLayout.minHeight = 120f;
            sectionRect.sizeDelta = new Vector2(320f, 130f);

            CreateLabel(sectionGo.transform, "SoundLabel", "Sound", font, 24f, TextAlignmentOptions.Center, 28f);
            Slider music = CreateRow(sectionGo.transform, "MusicRow", "Music", font, frame, frameScale, out TMP_Text musicValue);
            Slider sfx = CreateRow(sectionGo.transform, "SfxRow", "SFX", font, frame, frameScale, out TMP_Text sfxValue);
            music.gameObject.name = "MusicSlider";
            sfx.gameObject.name = "SfxSlider";

            MenuSoundSettings settings = sectionGo.AddComponent<MenuSoundSettings>();
            settings.Setup(music, sfx, musicValue, sfxValue);
            SetLayer(sectionGo.transform, layout.gameObject.layer);
            return settings;
        }

        static Slider CreateRow(
            Transform parent,
            string name,
            string caption,
            TMP_FontAsset font,
            Sprite frame,
            float frameScale,
            out TMP_Text valueLabel)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            SetLayer(row.transform, parent.gameObject.layer);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 36f;
            rowElement.minHeight = 36f;

            CreateLabel(row.transform, "Label", caption, font, 22f, TextAlignmentOptions.MidlineRight, 0f)
                .gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

            Slider slider = CreateSlider(row.transform, frame, frameScale);
            LayoutElement sliderElement = slider.gameObject.AddComponent<LayoutElement>();
            sliderElement.preferredWidth = 180f;
            sliderElement.flexibleWidth = 1f;
            sliderElement.preferredHeight = 28f;
            sliderElement.minHeight = 28f;

            valueLabel = CreateLabel(row.transform, "Value", "10%", font, 20f, TextAlignmentOptions.MidlineLeft, 0f);
            valueLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;
            return slider;
        }

        static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            TMP_FontAsset font,
            float size,
            TextAlignmentOptions alignment,
            float preferredHeight)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (preferredHeight > 0f)
            {
                LayoutElement element = go.AddComponent<LayoutElement>();
                element.preferredHeight = preferredHeight;
                element.minHeight = preferredHeight;
            }

            return label;
        }

        static Slider CreateSlider(Transform parent, Sprite frame, float frameScale)
        {
            var root = new GameObject("Slider", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SetLayer(root.transform, parent.gameObject.layer);

            Image background = root.AddComponent<Image>();
            background.sprite = frame;
            background.type = frame != null && frame.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            background.pixelsPerUnitMultiplier = frameScale > 0f ? frameScale : 1f;
            background.color = Color.white;
            background.raycastTarget = true;

            RectTransform fillArea = CreateRect(root.transform, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(10f, 0f);
            fillArea.offsetMax = new Vector2(-10f, 0f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillArea, false);
            SetLayer(fillGo.transform, parent.gameObject.layer);
            RectTransform fill = fillGo.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = new Vector2(8f, 0f);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.sprite = SolidSprite();
            fillImage.color = new Color(0.96f, 0.78f, 0.38f, 1f);
            fillImage.raycastTarget = false;

            RectTransform handleArea = CreateRect(root.transform, "Handle Slide Area");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleArea, false);
            SetLayer(handleGo.transform, parent.gameObject.layer);
            RectTransform handle = handleGo.GetComponent<RectTransform>();
            handle.sizeDelta = new Vector2(14f, 0f);
            Image handleImage = handleGo.AddComponent<Image>();
            handleImage.sprite = SolidSprite();
            handleImage.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            handleImage.raycastTarget = true;

            Slider slider = root.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = AudioPreferences.DefaultVolume;
            return slider;
        }

        static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void SetLayer(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayer(root.GetChild(i), layer);
        }

        static Sprite _solid;

        static Sprite SolidSprite()
        {
            if (_solid != null)
                return _solid;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            _solid = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            _solid.hideFlags = HideFlags.HideAndDontSave;
            return _solid;
        }

        sealed class ReleaseHook : MonoBehaviour, IPointerUpHandler
        {
            public UnityEngine.Events.UnityAction Released;

            public void OnPointerUp(PointerEventData eventData)
            {
                if (Released != null)
                    Released.Invoke();
            }
        }
    }
}
