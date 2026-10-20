#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace M3P.Editor
{
    public static class ChooseNewPerkTreePanelBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/UI/Map/UIPanelChooseNewPerkTree Variant.prefab";
        const string BasePath = "Assets/_Project/Prefabs/UI/Common/UIPanel.prefab";
        const string FontPath = "Assets/_Project/Graphics/UI/Font/MedievalSharp-Bold SDF.asset";
        const string ConfigPath = "Assets/_Project/Configs/Progression/StatProgressionConfig.asset";
        const string CheckPath = "Assets/_Project/Graphics/UI/Panels/CraftTile/Decor/check.png";

        const float PanelWidth = 1120f;
        const float PanelHeight = 1000f;

        static readonly Color Ink = new Color(0.055f, 0.058f, 0.068f, 0.98f);
        static readonly Color Gold = new Color(0.84f, 0.68f, 0.36f, 1f);
        static readonly Color Cream = new Color(0.94f, 0.89f, 0.76f, 1f);
        static readonly Color Muted = new Color(0.62f, 0.64f, 0.70f, 1f);
        static readonly Color FrameIdle = new Color(0.42f, 0.34f, 0.18f, 0.45f);
        static readonly Color FrameSelected = new Color(0.86f, 0.68f, 0.32f, 1f);
        static readonly Color FillIdle = new Color(0.075f, 0.078f, 0.09f, 1f);
        static readonly Color FillSelected = new Color(0.12f, 0.105f, 0.068f, 1f);
        static readonly Color StatusIdle = new Color(0.62f, 0.56f, 0.38f, 1f);
        static readonly Color StatusSelected = new Color(0.90f, 0.74f, 0.40f, 1f);
        static readonly Color Line = new Color(0.62f, 0.50f, 0.26f, 0.9f);
        static readonly Color Cyan = new Color(0.40f, 0.86f, 0.90f, 1f);
        static readonly Color PipEmpty = new Color(0.72f, 0.62f, 0.38f, 0.9f);

        [MenuItem("M3P/UI/Build Choose Perk Tree Panel")]
        public static void BuildMenu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite plate = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite check = LoadSprite(CheckPath);
            if (font == null)
                return "Missing font " + FontPath;

            EnsureVariant();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BuildContents(root, font, plate, check);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return "Saved " + PrefabPath;
        }

        static void EnsureVariant()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = "UIPanelChooseNewPerkTree Variant";
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.ImportAsset(PrefabPath);
        }

        static void BuildContents(GameObject root, TMP_FontAsset font, Sprite plate, Sprite check)
        {
            root.name = "UIPanelChooseNewPerkTree Variant";
            var panelRect = root.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchoredPosition = Vector2.zero;

            Hide("LabelName");
            Hide("Costs");

            Image frame = root.transform.Find("BackgroundFrame").GetComponent<Image>();
            frame.gameObject.SetActive(true);
            StylePlate(frame, plate, Gold, false);
            Stretch((RectTransform)frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image background = root.transform.Find("Background").GetComponent<Image>();
            StylePlate(background, plate, Ink, true);
            Stretch((RectTransform)background.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18f, -18f));

            DestroyNamed(root.transform, "PanelFill");
            DestroyNamed(root.transform, "Header");
            DestroyNamed(root.transform, "Offers");
            DestroyNamed(root.transform, "ConfirmLabel");
            DestroyNamed(root.transform, "DoneButton");
            DestroyNamed(root.transform, "Hint");
            DestroyNamed(root.transform, "Ownership");
            DestroyNamed(root.transform, "CornerTL");
            DestroyNamed(root.transform, "CornerTR");
            DestroyNamed(root.transform, "CornerBL");
            DestroyNamed(root.transform, "CornerBR");

            Image panelFill = MakeImage(root.transform, "PanelFill", plate, true);
            panelFill.color = Ink;
            Stretch(panelFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, -16f));

            TextMeshProUGUI eyebrow = MakeLabel(root.transform, "Header", font, "NOWA ŚCIEŻKA ROZWOJU", 15f, Gold, TextAlignmentOptions.Center);
            PlaceTop(eyebrow.rectTransform, -18f, new Vector2(720f, 26f));
            eyebrow.characterSpacing = 8f;

            TextMeshProUGUI title = MakeLabel(root.transform, "Title", font, "Drzewko odblokowane", 40f, Cream, TextAlignmentOptions.Center);
            PlaceTop(title.rectTransform, -46f, new Vector2(760f, 52f));

            TextMeshProUGUI subtitle = MakeLabel(root.transform, "Subtitle", font, "Możesz teraz rozwijać drzewko: Kondycja.", 16f, Muted, TextAlignmentOptions.Center);
            PlaceTop(subtitle.rectTransform, -100f, new Vector2(820f, 28f));

            Image ornament = MakeImage(root.transform, "Ornament", plate, false);
            ornament.color = Gold;
            PlaceTop(ornament.rectTransform, -132f, new Vector2(10f, 10f));
            ornament.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Transform offers = MakeRect(root.transform, "Offers");
            var offersRect = (RectTransform)offers;
            offersRect.anchorMin = Vector2.zero;
            offersRect.anchorMax = Vector2.one;
            offersRect.offsetMin = new Vector2(28f, 206f);
            offersRect.offsetMax = new Vector2(-28f, -158f);
            var rowLayout = offers.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            UIPerkTreeOfferCard card = BuildCard(offers, font, plate, check);

            TextMeshProUGUI confirm = MakeLabel(root.transform, "ConfirmLabel", font, "Odblokowano drzewko: Kondycja.", 20f, Cream, TextAlignmentOptions.Center);
            PlaceBottom(confirm.rectTransform, 178f, new Vector2(760f, 32f));

            Button done = BuildButton(root.transform, font, plate);
            PlaceBottom((RectTransform)done.transform, 128f, new Vector2(400f, 56f));

            TextMeshProUGUI hint = MakeLabel(
                root.transform,
                "Hint",
                font,
                "Po odblokowaniu możesz inwestować punkty i aktywować kolejne perki.",
                15f,
                Muted,
                TextAlignmentOptions.Center);
            hint.textWrappingMode = TextWrappingModes.Normal;
            PlaceBottom(hint.rectTransform, 74f, new Vector2(860f, 40f));

            BuildOwnership(root.transform, font, plate, out TextMeshProUGUI ownershipLabel, out TextMeshProUGUI ownershipCount, out UIPerkTreeCountPip pipTemplate, out Transform pipParent);

            MakeCorner(root.transform, "CornerTL", plate, new Vector2(0f, 1f), new Vector2(14f, -14f));
            MakeCorner(root.transform, "CornerTR", plate, new Vector2(1f, 1f), new Vector2(-14f, -14f));
            MakeCorner(root.transform, "CornerBL", plate, new Vector2(0f, 0f), new Vector2(14f, 14f));
            MakeCorner(root.transform, "CornerBR", plate, new Vector2(1f, 0f), new Vector2(-14f, 14f));

            var panel = root.GetComponent<UIPerkTreeUnlockPanel>() ?? root.AddComponent<UIPerkTreeUnlockPanel>();
            var so = new SerializedObject(panel);
            SetRef(so, "_cardTemplate", card);
            SetRef(so, "_cardParent", offers);
            SetRef(so, "_pipTemplate", pipTemplate);
            SetRef(so, "_pipParent", pipParent);
            SetRef(so, "_eyebrow", eyebrow);
            SetRef(so, "_title", title);
            SetRef(so, "_subtitle", subtitle);
            SetRef(so, "_confirmLabel", confirm);
            SetRef(so, "_hint", hint);
            SetRef(so, "_ownershipLabel", ownershipLabel);
            SetRef(so, "_ownershipCount", ownershipCount);
            SetRef(so, "_confirmButton", done);
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            Preview(panel);

            void Hide(string childName)
            {
                Transform child = root.transform.Find(childName);
                if (child != null)
                    child.gameObject.SetActive(false);
            }
        }

        static void Preview(UIPerkTreeUnlockPanel panel)
        {
            StatProgressionConfig config = AssetDatabase.LoadAssetAtPath<StatProgressionConfig>(ConfigPath);
            if (config == null)
                return;

            var offer = new List<PerkTree>(3);
            PerkTree[] trees = config.PerkTrees;
            int max = 0;
            for (int i = 0; i < trees.Length; i++)
            {
                if (trees[i] == null)
                    continue;

                max++;
                if (offer.Count < 3)
                    offer.Add(trees[i]);
            }

            panel.Bind(offer, config.Perks, 1, 0, max);
        }

        static UIPerkTreeOfferCard BuildCard(Transform parent, TMP_FontAsset font, Sprite plate, Sprite check)
        {
            Image frame = MakeImage(parent, "CardTemplate", plate, true);
            frame.color = FrameIdle;
            frame.raycastTarget = true;
            var rect = frame.rectTransform;
            rect.sizeDelta = new Vector2(318f, 610f);

            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Image fill = MakeImage(frame.transform, "Fill", plate, false);
            fill.color = FillIdle;
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6f, -6f));

            GameObject checkRoot = BuildCheck(frame.transform, plate, check);

            Image icon = MakeImage(frame.transform, "Icon", null, true);
            icon.color = Color.white;
            PlaceTop(icon.rectTransform, -18f, new Vector2(132f, 132f));

            TextMeshProUGUI name = MakeLabel(frame.transform, "Name", font, "Kondycja", 32f, Cream, TextAlignmentOptions.Center);
            PlaceTop(name.rectTransform, -156f, new Vector2(286f, 42f));

            TextMeshProUGUI subtitle = MakeLabel(frame.transform, "Subtitle", font, "MAKSYMALNE ZDROWIE", 13f, Gold, TextAlignmentOptions.Center);
            subtitle.characterSpacing = 2.5f;
            PlaceTop(subtitle.rectTransform, -198f, new Vector2(286f, 22f));

            TextMeshProUGUI description = MakeLabel(frame.transform, "Description", font, string.Empty, 15f, Muted, TextAlignmentOptions.Center);
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Ellipsis;
            PlaceTop(description.rectTransform, -226f, new Vector2(276f, 68f));

            Transform rows = MakeRect(frame.transform, "Perks");
            PlaceTop((RectTransform)rows, -304f, new Vector2(286f, 168f));
            UIPerkTreeOfferRow row = BuildRow(rows, font, plate);

            TextMeshProUGUI status = MakeLabel(frame.transform, "Status", font, "Wybierz drzewko", 16f, StatusIdle, TextAlignmentOptions.Center);
            PlaceBottom(status.rectTransform, 28f, new Vector2(270f, 28f));

            var card = frame.gameObject.AddComponent<UIPerkTreeOfferCard>();
            var so = new SerializedObject(card);
            SetRef(so, "_frame", frame);
            SetRef(so, "_fill", fill);
            SetRef(so, "_check", checkRoot);
            SetRef(so, "_icon", icon);
            SetRef(so, "_name", name);
            SetRef(so, "_subtitle", subtitle);
            SetRef(so, "_description", description);
            SetRef(so, "_status", status);
            SetRef(so, "_rowTemplate", row);
            SetRef(so, "_rowParent", rows);
            SetRef(so, "_button", button);
            SetColor(so, "_frameIdle", FrameIdle);
            SetColor(so, "_frameSelected", FrameSelected);
            SetColor(so, "_fillIdle", FillIdle);
            SetColor(so, "_fillSelected", FillSelected);
            SetColor(so, "_statusIdle", StatusIdle);
            SetColor(so, "_statusSelected", StatusSelected);
            so.ApplyModifiedPropertiesWithoutUndo();

            row.gameObject.SetActive(false);
            return card;
        }

        static UIPerkTreeOfferRow BuildRow(Transform parent, TMP_FontAsset font, Sprite plate)
        {
            Transform row = MakeRect(parent, "RowTemplate");
            var rect = (RectTransform)row;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-12f, 50f);
            rect.anchoredPosition = Vector2.zero;

            Image icon = MakeImage(row, "Icon", null, true);
            icon.color = Color.white;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(6f, 2f);
            iconRect.sizeDelta = new Vector2(40f, 40f);

            Image link = MakeImage(icon.transform, "Link", plate, false);
            link.color = Line;
            var linkRect = link.rectTransform;
            linkRect.anchorMin = new Vector2(0.5f, 0f);
            linkRect.anchorMax = new Vector2(0.5f, 0f);
            linkRect.pivot = new Vector2(0.5f, 1f);
            linkRect.anchoredPosition = new Vector2(0f, -1f);
            linkRect.sizeDelta = new Vector2(2f, 18f);

            TextMeshProUGUI threshold = MakeLabel(row, "Threshold", font, "Próg 2", 13f, Muted, TextAlignmentOptions.Left);
            var thresholdRect = threshold.rectTransform;
            thresholdRect.anchorMin = new Vector2(0f, 1f);
            thresholdRect.anchorMax = new Vector2(1f, 1f);
            thresholdRect.pivot = new Vector2(0f, 1f);
            thresholdRect.anchoredPosition = new Vector2(54f, -4f);
            thresholdRect.sizeDelta = new Vector2(-62f, 18f);

            TextMeshProUGUI effect = MakeLabel(row, "Effect", font, "+5 maks. HP", 17f, Cream, TextAlignmentOptions.Left);
            var effectRect = effect.rectTransform;
            effectRect.anchorMin = new Vector2(0f, 1f);
            effectRect.anchorMax = new Vector2(1f, 1f);
            effectRect.pivot = new Vector2(0f, 1f);
            effectRect.anchoredPosition = new Vector2(54f, -22f);
            effectRect.sizeDelta = new Vector2(-62f, 24f);

            var component = row.gameObject.AddComponent<UIPerkTreeOfferRow>();
            var so = new SerializedObject(component);
            SetRef(so, "_icon", icon);
            SetRef(so, "_threshold", threshold);
            SetRef(so, "_effect", effect);
            SetRef(so, "_link", link.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        static GameObject BuildCheck(Transform parent, Sprite plate, Sprite check)
        {
            Image border = MakeImage(parent, "Check", plate, false);
            border.color = Cream;
            var rect = border.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -14f);
            rect.sizeDelta = new Vector2(30f, 30f);

            Image plateImage = MakeImage(border.transform, "Plate", plate, false);
            plateImage.color = new Color(0.10f, 0.09f, 0.07f, 1f);
            Stretch(plateImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -4f));

            Image mark = MakeImage(border.transform, "Mark", check, true);
            mark.color = Cream;
            Stretch(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -16f));
            mark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(16f, 16f);
            return border.gameObject;
        }

        static Button BuildButton(Transform parent, TMP_FontAsset font, Sprite plate)
        {
            Image border = MakeImage(parent, "DoneButton", plate, true);
            border.color = Gold;
            border.raycastTarget = true;

            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.82f, 1f);
            colors.pressedColor = new Color(0.82f, 0.72f, 0.48f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Image fill = MakeImage(border.transform, "Fill", plate, false);
            fill.color = new Color(0.08f, 0.075f, 0.06f, 1f);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -4f));

            TextMeshProUGUI label = MakeLabel(border.transform, "Label", font, "Gotowe", 26f, Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, -1f), new Vector2(-24f, -8f));
            return button;
        }

        static void BuildOwnership(
            Transform parent,
            TMP_FontAsset font,
            Sprite plate,
            out TextMeshProUGUI caption,
            out TextMeshProUGUI count,
            out UIPerkTreeCountPip pipTemplate,
            out Transform pipParent)
        {
            Transform bar = MakeRect(parent, "Ownership");
            PlaceBottom((RectTransform)bar, 30f, new Vector2(520f, 32f));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            caption = MakeLabel(bar, "Caption", font, "Twoje drzewka", 16f, Cream, TextAlignmentOptions.Right);
            caption.rectTransform.sizeDelta = new Vector2(180f, 28f);

            pipParent = MakeRect(bar, "Pips");
            ((RectTransform)pipParent).sizeDelta = new Vector2(120f, 28f);
            var pipLayout = pipParent.gameObject.AddComponent<HorizontalLayoutGroup>();
            pipLayout.spacing = 8f;
            pipLayout.childAlignment = TextAnchor.MiddleCenter;
            pipLayout.childControlWidth = false;
            pipLayout.childControlHeight = false;
            pipLayout.childForceExpandWidth = false;
            pipLayout.childForceExpandHeight = false;

            pipTemplate = BuildPip(pipParent, plate);

            count = MakeLabel(bar, "Count", font, "1 / 4", 16f, Cream, TextAlignmentOptions.Left);
            count.rectTransform.sizeDelta = new Vector2(72f, 28f);
        }

        static UIPerkTreeCountPip BuildPip(Transform parent, Sprite plate)
        {
            Image border = MakeImage(parent, "PipTemplate", plate, false);
            border.color = PipEmpty;
            border.rectTransform.sizeDelta = new Vector2(13f, 13f);
            border.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image fill = MakeImage(border.transform, "Fill", plate, false);
            fill.color = Cyan;
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6f, -6f));

            var pip = border.gameObject.AddComponent<UIPerkTreeCountPip>();
            var so = new SerializedObject(pip);
            SetRef(so, "_fill", fill.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            pip.gameObject.SetActive(false);
            return pip;
        }

        static void MakeCorner(Transform parent, string name, Sprite plate, Vector2 anchor, Vector2 position)
        {
            Image gem = MakeImage(parent, name, plate, false);
            gem.color = Gold;
            var rect = gem.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(12f, 12f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        static void DestroyNamed(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        static Transform MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Image MakeImage(Transform parent, string name, Sprite sprite, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = raycast;
            image.preserveAspect = sprite != null && image.type == Image.Type.Simple;
            return image;
        }

        static void StylePlate(Image image, Sprite sprite, Color color, bool raycast)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycast;
            image.pixelsPerUnitMultiplier = 1f;
        }

        static TextMeshProUGUI MakeLabel(
            Transform parent,
            string name,
            TMP_FontAsset font,
            string text,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
                if (font.material != null)
                    tmp.fontSharedMaterial = font.material;
            }

            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        static void PlaceTop(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        static void PlaceBottom(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError("ChooseNewPerkTreePanelBuilder: missing " + propertyName);
                return;
            }

            property.objectReferenceValue = value;
        }

        static void SetColor(SerializedObject so, string propertyName, Color value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                return;

            property.colorValue = value;
        }

        static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite found)
                    return found;
            }

            return null;
        }
    }
}
#endif
