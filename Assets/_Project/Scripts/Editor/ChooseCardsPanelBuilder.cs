#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace M3P.Editor
{
    public static class ChooseCardsPanelBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/UI/UIPanelChooseCards Variant.prefab";
        const string DeckRowPath = "Assets/_Project/Prefabs/UI/Common/DeckCardButton Variant.prefab";
        const string FontPath = "Assets/_Project/Graphics/UI/Font/MedievalSharp-Bold SDF.asset";

        const float PanelWidth = 1720f;
        const float PanelHeight = 880f;

        static readonly Color Cream = new Color(0.96f, 0.90f, 0.74f, 1f);
        static readonly Color Gold = new Color(0.91f, 0.71f, 0.29f, 1f);
        static readonly Color Ink = new Color(0.035f, 0.04f, 0.05f, 1f);

        public static string Build()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite frame = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftCard/Frames/panel-outer-frame.png");
            Sprite header = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftCard/Frames/header-empty.png");
            Sprite filterNormal = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftTile/Buttons/effect-normal.png");
            Sprite filterSelected = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftTile/Buttons/effect-selected.png");
            Sprite save = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftTile/Buttons/apply-normal.png");
            Sprite closeIcon = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftTile/Decor/close.png");
            Sprite arrow = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftTile/Decor/arrow-right.png");
            Sprite ring = LoadSprite("Assets/_Project/Graphics/UI/Panels/CraftTile/Decor/input-ring.png");

            StyleDeckRow(font, ring);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = root.GetComponent<RectTransform>();
                panel.anchorMin = new Vector2(0.5f, 0.5f);
                panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                panel.anchoredPosition = new Vector2(0f, 48f);

                StyleChrome(root.transform, frame);
                Transform headerTf = StyleHeader(root.transform, font, header);
                StyleClose(root.transform, closeIcon);
                Transform leftoverHeader = root.transform.Find("Header (1)");
            if (leftoverHeader != null)
                Object.DestroyImmediate(leftoverHeader.gameObject);

            Button filterAll = StyleFilters(root.transform, font, filterNormal, filterSelected);
                Button filterSwaps = FindButton(root.transform, "FilterSwaps");
                Button filterShifts = FindButton(root.transform, "FilterShifts");
                Button filterColors = FindButton(root.transform, "FilterColors");
                StyleOwnedCards(root.transform);
                TextMeshProUGUI deckCount = StyleDeckColumn(root.transform, font);
                Button saveButton = StyleSave(root.transform, font, save);
                Button pagePrev = StylePager(root.transform, font, arrow, out Button pageNext, out TextMeshProUGUI pageLabel);

                Transform separator = Find(root.transform, "Separator");
                if (separator != null)
                    separator.gameObject.SetActive(false);

                Transform costs = Find(root.transform, "Costs");
                if (costs != null)
                    costs.gameObject.SetActive(false);

                headerTf.SetAsLastSibling();
                Transform close = Find(root.transform, "CloseButton");
                if (close != null)
                    close.SetAsLastSibling();

                var cards = root.GetComponent<UIPanelPlayerCards>();
                var so = new SerializedObject(cards);
                so.FindProperty("_deckCountLabel").objectReferenceValue = deckCount;
                so.FindProperty("_saveButton").objectReferenceValue = saveButton;
                so.FindProperty("_filterAll").objectReferenceValue = filterAll;
                so.FindProperty("_filterSwaps").objectReferenceValue = filterSwaps;
                so.FindProperty("_filterShifts").objectReferenceValue = filterShifts;
                so.FindProperty("_filterColors").objectReferenceValue = filterColors;
                so.FindProperty("_filterSpriteNormal").objectReferenceValue = filterNormal;
                so.FindProperty("_filterSpriteSelected").objectReferenceValue = filterSelected;
                so.FindProperty("_pagePrev").objectReferenceValue = pagePrev;
                so.FindProperty("_pageNext").objectReferenceValue = pageNext;
                so.FindProperty("_pageLabel").objectReferenceValue = pageLabel;
                so.FindProperty("_deckLimit").intValue = 20;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            ApplySceneInstance();
            return "Saved " + PrefabPath;
        }

        static void StyleChrome(Transform root, Sprite frame)
        {
            Transform background = Find(root, "Background");
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                image.sprite = null;
                image.color = Ink;
                image.type = Image.Type.Simple;
                image.raycastTarget = true;
                Stretch(background as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-48f, -48f));
            }

            Transform frameTf = Find(root, "BackgroundFrame");
            if (frameTf != null)
            {
                var image = frameTf.GetComponent<Image>();
                image.sprite = frame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = false;
                image.pixelsPerUnitMultiplier = 1f;
                Stretch(frameTf as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                frameTf.SetSiblingIndex(background != null ? 1 : 0);
            }
        }

        static Transform StyleHeader(Transform root, TMP_FontAsset font, Sprite headerSprite)
        {
            Image header = GetOrCreateImage(root, "Header", headerSprite, true);
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 18f), new Vector2(760f, 174f));
            header.raycastTarget = false;

            Transform inheritedTitle = Find(root, "LabelName");
            if (inheritedTitle != null)
                inheritedTitle.gameObject.SetActive(false);

            TextMeshProUGUI tmp = GetOrCreateLabel(header.transform, "HeaderTitle", font, "TALIA KART", 40f, Cream, TextAlignmentOptions.Center);
            Place(tmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(460f, 64f));
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 6f;
            return header.transform;
        }

        static void StyleClose(Transform root, Sprite closeIcon)
        {
            Transform close = Find(root, "CloseButton");
            if (close == null)
                return;

            var rt = close as RectTransform;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            Place(rt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -22f), new Vector2(64f, 64f));

            var buttonImage = close.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = null;
                buttonImage.color = new Color(1f, 1f, 1f, 0f);
                buttonImage.raycastTarget = true;
            }

            Transform[] children = close.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == close)
                    continue;

                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;

                if (child.name == "Plate")
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                if (child.name != "Icon")
                    continue;

                Stretch(child as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, -16f));
                var image = child.GetComponent<Image>();
                if (image == null)
                    continue;

                image.sprite = closeIcon;
                image.color = Cream;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.type = Image.Type.Simple;
                EditorUtility.SetDirty(image);
            }
        }

        static Button StyleFilters(Transform root, TMP_FontAsset font, Sprite normal, Sprite selected)
        {
            Transform row = GetOrCreate(root, "Filters");
            Place(row as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -168f), new Vector2(980f, 48f));
            var layout = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            Button all = MakeTab(row, "FilterAll", "Wszystkie", font, selected);
            MakeTab(row, "FilterSwaps", "Zamiany", font, normal);
            MakeTab(row, "FilterShifts", "Przesunięcia", font, normal);
            MakeTab(row, "FilterColors", "Kolory", font, normal);
            return all;
        }

        static Button MakeTab(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite)
        {
            Transform existing = parent.Find(name);
            Image image;
            Button button;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
                button = go.GetComponent<Button>();
                existing = go.transform;
            }
            else
            {
                image = existing.GetComponent<Image>();
                button = existing.GetComponent<Button>();
            }

            var rt = existing as RectTransform;
            rt.sizeDelta = new Vector2(220f, 46f);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            TextMeshProUGUI tmp = existing.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null)
            {
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                textGo.layer = 5;
                textGo.transform.SetParent(existing, false);
                tmp = textGo.AddComponent<TextMeshProUGUI>();
            }

            Stretch(tmp.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplyFont(tmp, font, 20f, Color.white, TextAlignmentOptions.Center);
            tmp.text = label;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return button;
        }

        static void StyleOwnedCards(Transform root)
        {
            Transform owned = Find(root, "OwnedCards");
            Place(owned as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -228f), new Vector2(960f, 530f));
            var mask = owned.GetComponent<RectMask2D>();
            if (mask != null)
            {
                mask.padding = Vector4.zero;
                mask.softness = new Vector2Int(0, 12);
            }

            Transform content = owned.Find("Content");
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(8, 8, 4, 8);
            grid.cellSize = new Vector2(176f, 250f);
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
        }

        static TextMeshProUGUI StyleDeckColumn(Transform root, TMP_FontAsset font)
        {
            Transform header = GetOrCreate(root, "DeckHeader");
            Place(header as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -168f), new Vector2(430f, 48f));

            Transform title = Find(root, "LabelName (1)");
            if (title == null)
                title = GetOrCreate(header, "DeckTitle");
            else
                title.SetParent(header, false);
            title.name = "DeckTitle";
            Place(title as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-150f, 0f));
            var titleTmp = title.GetComponent<TextMeshProUGUI>() ?? title.gameObject.AddComponent<TextMeshProUGUI>();
            if (title.GetComponent<CanvasRenderer>() == null)
                title.gameObject.AddComponent<CanvasRenderer>();
            ApplyFont(titleTmp, font, 28f, Cream, TextAlignmentOptions.Left);
            titleTmp.text = "Twoja talia";
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.raycastTarget = false;

            TextMeshProUGUI count = GetOrCreateLabel(header, "DeckCount", font, "<color=#E8B44A>0</color>  /  20", 26f, Color.white, TextAlignmentOptions.Right);
            Place(count.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-4f, 0f), new Vector2(140f, 0f));
            count.richText = true;

            Transform deck = Find(root, "CardsInDeck");
            Place(deck as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -224f), new Vector2(430f, 470f));
            var deckImage = deck.GetComponent<Image>();
            if (deckImage != null)
            {
                deckImage.color = new Color(1f, 1f, 1f, 0f);
                deckImage.raycastTarget = true;
            }

            var mask = deck.GetComponent<RectMask2D>();
            if (mask != null)
            {
                mask.padding = Vector4.zero;
                mask.softness = new Vector2Int(0, 8);
            }

            Transform content = deck.Find("Content");
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.cellSize = new Vector2(430f, 48f);
            grid.spacing = new Vector2(0f, 8f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            return count;
        }

        static Button StyleSave(Transform root, TMP_FontAsset font, Sprite sprite)
        {
            Transform existing = root.Find("SaveDeckButton");
            Image image;
            Button button;
            if (existing == null)
            {
                var go = new GameObject("SaveDeckButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.layer = 5;
                go.transform.SetParent(root, false);
                image = go.GetComponent<Image>();
                button = go.GetComponent<Button>();
                existing = go.transform;
            }
            else
            {
                image = existing.GetComponent<Image>();
                button = existing.GetComponent<Button>();
            }

            Place(existing as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-130f, 96f), new Vector2(430f, 64f));
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;

            TextMeshProUGUI tmp = existing.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null)
            {
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                textGo.layer = 5;
                textGo.transform.SetParent(existing, false);
                tmp = textGo.AddComponent<TextMeshProUGUI>();
            }

            Stretch(tmp.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, -8f));
            ApplyFont(tmp, font, 24f, Color.white, TextAlignmentOptions.Center);
            tmp.text = "ZAPISZ TALIĘ";
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 2f;
            tmp.raycastTarget = false;
            return button;
        }

        static Button StylePager(Transform root, TMP_FontAsset font, Sprite arrow, out Button next, out TextMeshProUGUI label)
        {
            Transform row = GetOrCreate(root, "Pager");
            Place(row as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -766f), new Vector2(960f, 40f));

            Button prev = MakeIconButton(row, "PagePrev", arrow);
            Place(prev.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(36f, 28f));
            prev.transform.localScale = new Vector3(-1f, 1f, 1f);

            label = GetOrCreateLabel(row, "PageLabel", font, "1 / 1", 22f, Cream, TextAlignmentOptions.Center);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 36f));

            next = MakeIconButton(row, "PageNext", arrow);
            Place(next.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(36f, 28f));
            return prev;
        }

        static Button MakeIconButton(Transform parent, string name, Sprite sprite)
        {
            Transform existing = parent.Find(name);
            Image image;
            Button button;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
                button = go.GetComponent<Button>();
            }
            else
            {
                image = existing.GetComponent<Image>();
                button = existing.GetComponent<Button>();
            }

            image.sprite = sprite;
            image.color = Cream;
            image.preserveAspect = true;
            button.targetGraphic = image;
            return button;
        }

        static void StyleDeckRow(TMP_FontAsset font, Sprite ring)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(DeckRowPath);
            try
            {
                var row = root.GetComponent<RectTransform>();
                row.sizeDelta = new Vector2(430f, 48f);

                var rootButton = root.GetComponent<Button>();
                if (rootButton != null)
                    rootButton.transition = Selectable.Transition.None;

                Transform backdrop = root.transform.Find("GameObject");
                if (backdrop != null)
                {
                    var image = backdrop.GetComponent<Image>();
                    if (image != null)
                        image.color = new Color(1f, 1f, 1f, 0.18f);
                }

                Transform content = root.transform.Find("Content");
                var layout = content.GetComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(6, 6, 4, 4);
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;

                Transform icon = Find(content, "icon");
                SetLayout(icon, 36f, 0f);
                var iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                    iconImage.preserveAspect = true;

                Transform name = content.Find("Name");
                var fitter = name.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                    fitter.enabled = false;
                var nameLayout = SetLayout(name, 80f, 1f);
                nameLayout.minWidth = 40f;
                var nameTmp = name.GetComponent<TextMeshProUGUI>();
                ApplyFont(nameTmp, font, 20f, Cream, TextAlignmentOptions.Left);
                nameTmp.overflowMode = TextOverflowModes.Ellipsis;

                TextMeshProUGUI count = GetOrCreateLabel(content, "Count", font, "1", 22f, Gold, TextAlignmentOptions.Center);
                SetLayout(count.transform, 28f, 0f);

                Button minus = MakeStepButton(content, "Minus", "-", font, ring);
                Button plus = MakeStepButton(content, "Plus", "+", font, ring);
                icon.SetSiblingIndex(0);
                name.SetSiblingIndex(1);
                count.transform.SetSiblingIndex(2);
                minus.transform.SetSiblingIndex(3);
                plus.transform.SetSiblingIndex(4);

                var button = root.GetComponent<UIDeckCardButton>();
                var so = new SerializedObject(button);
                so.FindProperty("cardCount").objectReferenceValue = count;
                so.FindProperty("minusButton").objectReferenceValue = minus;
                so.FindProperty("plusButton").objectReferenceValue = plus;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, DeckRowPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Button MakeStepButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite ring)
        {
            Transform existing = parent.Find(name);
            Image image;
            Button button;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
                button = go.GetComponent<Button>();
                existing = go.transform;
            }
            else
            {
                image = existing.GetComponent<Image>();
                button = existing.GetComponent<Button>();
            }

            image.sprite = ring;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;
            SetLayout(existing, 32f, 0f);

            TextMeshProUGUI tmp = existing.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null)
            {
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                textGo.layer = 5;
                textGo.transform.SetParent(existing, false);
                tmp = textGo.AddComponent<TextMeshProUGUI>();
            }

            Stretch(tmp.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplyFont(tmp, font, 22f, Cream, TextAlignmentOptions.Center);
            tmp.text = label;
            tmp.raycastTarget = false;
            return button;
        }

        static LayoutElement SetLayout(Transform target, float preferredWidth, float flexible)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexible;
            element.minHeight = 28f;
            return element;
        }

        static void ApplySceneInstance()
        {
            UIPanelPlayerCards[] panels = Object.FindObjectsByType<UIPanelPlayerCards>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(panels[i]))
                    continue;

                var rt = panels[i].GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                rt.anchoredPosition = new Vector2(0f, 48f);
                EditorUtility.SetDirty(panels[i].gameObject);
                if (!EditorApplication.isPlaying && panels[i].gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(panels[i].gameObject.scene);
            }

            if (!EditorApplication.isPlaying && panels.Length > 0)
                EditorSceneManager.SaveOpenScenes();
        }

        static Button FindButton(Transform root, string name)
        {
            Transform found = Find(root, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Transform Find(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static Transform GetOrCreate(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Image GetOrCreateImage(Transform parent, string name, Sprite sprite, bool preserveAspect)
        {
            Transform existing = parent.Find(name);
            Image image;
            if (existing != null)
            {
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI GetOrCreateLabel(Transform parent, string name, TMP_FontAsset font, string text, float size, Color color, TextAlignmentOptions align)
        {
            Transform existing = parent.Find(name);
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                tmp = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
                if (existing.GetComponent<CanvasRenderer>() == null)
                    existing.gameObject.AddComponent<CanvasRenderer>();
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            ApplyFont(tmp, font, size, color, align);
            tmp.text = text;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            if (tmp == null)
                return;

            if (font != null)
            {
                tmp.font = font;
                if (font.material != null)
                    tmp.fontSharedMaterial = font.material;
            }

            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            float pivotX = min.x == 0f && max.x == 0f ? 0f : min.x == 1f && max.x == 1f ? 1f : 0.5f;
            float pivotY = min.y == 0f && max.y == 0f ? 0f : min.y == 1f && max.y == 1f ? 1f : 0.5f;
            rt.pivot = new Vector2(pivotX, pivotY);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
#endif
