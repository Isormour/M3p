using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UITooltipWindow : MonoBehaviour
    {
        const float Width = 380f;
        const float ScreenPadding = 16f;
        const float AnchorPadding = 12f;

        [SerializeField] Image _background;
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _meta;
        [SerializeField] TextMeshProUGUI _description;
        [SerializeField] RectTransform _linesRoot;
        [SerializeField] TMP_FontAsset _font;

        readonly List<TextMeshProUGUI> _lineLabels = new List<TextMeshProUGUI>();
        Canvas _canvas;
        RectTransform _rect;
        CanvasGroup _group;

        public bool IsVisible => _group != null && _group.alpha > 0.01f && isActiveAndEnabled;

        public static UITooltipWindow Create(Transform parent)
        {
            var root = new GameObject(
                "TooltipWindow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(CanvasGroup),
                typeof(UITooltipWindow));

            if (parent != null)
                root.transform.SetParent(parent, false);

            UITooltipWindow window = root.GetComponent<UITooltipWindow>();
            window.Build();
            return window;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        public void Show(TooltipContent content, RectTransform anchor)
        {
            EnsureBuilt();
            SetContent(content);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            Place(anchor);
            if (_group != null)
                _group.alpha = 1f;
        }

        public void Hide()
        {
            if (_group != null)
                _group.alpha = 0f;
        }

        void EnsureBuilt()
        {
            if (_title != null && _linesRoot != null)
            {
                _rect = transform as RectTransform;
                _canvas = GetComponentInParent<Canvas>();
                if (_group == null)
                    _group = GetComponent<CanvasGroup>();
                return;
            }

            Build();
        }

        void Build()
        {
            _rect = transform as RectTransform;
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0f);
            _rect.sizeDelta = new Vector2(Width, 80f);

            _background = GetComponent<Image>();
            if (_background == null)
                _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.07f, 0.07f, 0.09f, 0.94f);
            _background.raycastTarget = false;

            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement rootLayout = GetComponent<LayoutElement>();
            if (rootLayout == null)
                rootLayout = gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredWidth = Width;
            rootLayout.minWidth = Width;

            Transform header = CreateChild("Header");
            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            GameObject iconObject = CreateChild("Icon", header).gameObject;
            _icon = iconObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = 40f;
            iconLayout.minHeight = 40f;
            iconLayout.preferredWidth = 40f;
            iconLayout.preferredHeight = 40f;
            iconLayout.flexibleWidth = 0f;

            _title = CreateLabel("Title", header, 22f, FontStyles.Bold, Color.white);
            _meta = CreateLabel("Meta", transform, 15f, FontStyles.Normal, new Color(0.72f, 0.72f, 0.76f, 1f));
            _description = CreateLabel("Description", transform, 17f, FontStyles.Normal, new Color(0.9f, 0.9f, 0.92f, 1f));

            _linesRoot = CreateChild("Lines");
            VerticalLayoutGroup linesLayout = _linesRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            linesLayout.spacing = 3f;
            linesLayout.childAlignment = TextAnchor.UpperLeft;
            linesLayout.childControlWidth = true;
            linesLayout.childControlHeight = true;
            linesLayout.childForceExpandWidth = true;
            linesLayout.childForceExpandHeight = false;

            _canvas = GetComponentInParent<Canvas>();
        }

        void SetContent(TooltipContent content)
        {
            bool hasIcon = content != null && content.Icon != null;
            if (_icon != null)
            {
                _icon.sprite = hasIcon ? content.Icon : null;
                _icon.enabled = hasIcon;
                _icon.gameObject.SetActive(hasIcon);
            }

            SetLabel(_title, content != null ? content.Title : string.Empty);
            SetLabel(_meta, content != null ? content.Meta : string.Empty);
            SetLabel(_description, content != null ? content.Description : string.Empty);
            BindLines(content != null ? content.Lines : null);
        }

        void BindLines(List<TooltipLine> lines)
        {
            int count = lines != null ? lines.Count : 0;
            for (int i = 0; i < count; i++)
            {
                TextMeshProUGUI label = GetOrCreateLine(i);
                label.text = lines[i].Text;
                label.color = ColorFor(lines[i].Kind);
                label.gameObject.SetActive(true);
            }

            for (int i = count; i < _lineLabels.Count; i++)
                _lineLabels[i].gameObject.SetActive(false);

            if (_linesRoot != null)
                _linesRoot.gameObject.SetActive(count > 0);
        }

        TextMeshProUGUI GetOrCreateLine(int index)
        {
            while (_lineLabels.Count <= index)
                _lineLabels.Add(CreateLabel($"Line_{_lineLabels.Count}", _linesRoot, 16f, FontStyles.Normal, Color.white));

            return _lineLabels[index];
        }

        void Place(RectTransform anchor)
        {
            if (anchor == null || _rect == null)
                return;

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            RectTransform canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);

            Vector2 bl = WorldToCanvas(canvasRect, camera, corners[0]);
            Vector2 tl = WorldToCanvas(canvasRect, camera, corners[1]);
            Vector2 tr = WorldToCanvas(canvasRect, camera, corners[2]);
            Vector2 br = WorldToCanvas(canvasRect, camera, corners[3]);

            float left = Mathf.Min(bl.x, tl.x);
            float right = Mathf.Max(br.x, tr.x);
            float bottom = Mathf.Min(bl.y, br.y);
            float top = Mathf.Max(tl.y, tr.y);
            Vector2 center = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);

            Vector2 size = _rect.rect.size;
            if (size.x < 1f)
                size = new Vector2(Width, 120f);

            Rect canvas = canvasRect.rect;
            float spaceAbove = canvas.yMax - top;
            float spaceBelow = bottom - canvas.yMin;
            float spaceRight = canvas.xMax - right;
            float spaceLeft = left - canvas.xMin;

            Vector2 pivot;
            Vector2 position;
            if (spaceAbove >= size.y + AnchorPadding || spaceAbove >= spaceBelow && spaceAbove >= spaceRight && spaceAbove >= spaceLeft)
            {
                pivot = new Vector2(0.5f, 0f);
                position = new Vector2(center.x, top + AnchorPadding);
            }
            else if (spaceBelow >= size.y + AnchorPadding && spaceBelow >= spaceRight && spaceBelow >= spaceLeft)
            {
                pivot = new Vector2(0.5f, 1f);
                position = new Vector2(center.x, bottom - AnchorPadding);
            }
            else if (spaceRight >= spaceLeft)
            {
                pivot = new Vector2(0f, 0.5f);
                position = new Vector2(right + AnchorPadding, center.y);
            }
            else
            {
                pivot = new Vector2(1f, 0.5f);
                position = new Vector2(left - AnchorPadding, center.y);
            }

            _rect.pivot = pivot;
            position = ClampToCanvas(canvas, position, size, pivot);
            _rect.anchoredPosition = position;
        }

        static Vector2 ClampToCanvas(Rect canvas, Vector2 position, Vector2 size, Vector2 pivot)
        {
            float minX = canvas.xMin + ScreenPadding + size.x * pivot.x;
            float maxX = canvas.xMax - ScreenPadding - size.x * (1f - pivot.x);
            float minY = canvas.yMin + ScreenPadding + size.y * pivot.y;
            float maxY = canvas.yMax - ScreenPadding - size.y * (1f - pivot.y);

            if (minX > maxX)
                position.x = (canvas.xMin + canvas.xMax) * 0.5f;
            else
                position.x = Mathf.Clamp(position.x, minX, maxX);

            if (minY > maxY)
                position.y = (canvas.yMin + canvas.yMax) * 0.5f;
            else
                position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }

        static Vector2 WorldToCanvas(RectTransform canvasRect, Camera camera, Vector3 world)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, camera, out Vector2 local);
            return local;
        }

        static void SetLabel(TextMeshProUGUI label, string text)
        {
            if (label == null)
                return;

            bool hasText = !string.IsNullOrEmpty(text);
            label.text = hasText ? text : string.Empty;
            label.gameObject.SetActive(hasText);
        }

        static Color ColorFor(TooltipLineKind kind)
        {
            return kind switch
            {
                TooltipLineKind.Damage => new Color(1f, 0.55f, 0.35f, 1f),
                TooltipLineKind.Heal => new Color(0.45f, 0.85f, 0.5f, 1f),
                TooltipLineKind.Shield => new Color(0.45f, 0.72f, 1f, 1f),
                TooltipLineKind.Status => new Color(1f, 0.82f, 0.35f, 1f),
                TooltipLineKind.Draw => new Color(0.8f, 0.75f, 1f, 1f),
                _ => new Color(0.78f, 0.78f, 0.82f, 1f)
            };
        }

        TextMeshProUGUI CreateLabel(string name, Transform parent, float size, FontStyles style, Color color)
        {
            Transform child = CreateChild(name, parent);
            TextMeshProUGUI label = child.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.fontStyle = style;
            label.font = _font;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        RectTransform CreateChild(string name, Transform parent = null)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent != null ? parent : transform, false);
            return (RectTransform)child.transform;
        }
    }
}
