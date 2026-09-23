#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace M3P.Editor
{
    public static class CraftCardPanelBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/UI/Map/UIPanelCardCrafting Variant.prefab";
        const string Art = "Assets/_Project/Graphics/UI/Panels/";
        const string FontPath = "Assets/_Project/Graphics/UI/Font/MedievalSharp-Bold SDF.asset";

        static readonly Color Cream = new Color(0.95f, 0.86f, 0.62f, 1f);
        static readonly Color Body = new Color(0.93f, 0.94f, 0.95f, 1f);
        static readonly Color Muted = new Color(0.76f, 0.78f, 0.82f, 1f);
        static readonly Color Panel = new Color(0.05f, 0.045f, 0.04f, 0.82f);

        public static void Build()
        {
            Sprite frame = LoadSprite(Art + "CraftCard/Frames/panel-outer-frame.png");
            Sprite header = LoadSprite(Art + "CraftCard/Frames/header-empty.png");
            Sprite forge = LoadSprite(Art + "CraftCard/Backgrounds/forge-stage-empty.png");
            Sprite craft = LoadSprite(Art + "CraftCard/Buttons/craft-normal.png");
            Sprite selected = LoadSprite(Art + "CraftCard/Buttons/recipe-selected.png");
            Sprite normal = LoadSprite(Art + "CraftTile/Buttons/effect-normal.png");
            Sprite badge = LoadSprite(Art + "CraftCard/Forge/forge-building-badge.png");
            Sprite plate = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform background = Find(root.transform, "Background");
                Image bg = background.GetComponent<Image>();
                bg.sprite = plate;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.07f, 0.05f, 0.04f, 1f);
                bg.raycastTarget = true;
                Stretch((RectTransform)background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                Image forgeImg = GetOrCreateImage(root.transform, "ForgeStage", forge, true);
                Place((RectTransform)forgeImg.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(640f, 800f));
                forgeImg.raycastTarget = false;

                Transform cardTypes = Find(root.transform, "CardTypes");
                var typesRect = (RectTransform)cardTypes;
                typesRect.anchorMin = new Vector2(0f, 0f);
                typesRect.anchorMax = new Vector2(0f, 1f);
                typesRect.pivot = new Vector2(0f, 0.5f);
                typesRect.anchoredPosition = new Vector2(58f, -24f);
                typesRect.sizeDelta = new Vector2(248f, -150f);
                Image typesImg = cardTypes.GetComponent<Image>();
                typesImg.sprite = plate;
                typesImg.type = Image.Type.Sliced;
                typesImg.color = Panel;
                typesImg.raycastTarget = false;

                TextMeshProUGUI recipes = GetOrCreateLabel(cardTypes, "LabelRecipes", font, "RECEPTURY", 20f, Cream, TextAlignmentOptions.Left);
                var recipesRect = recipes.rectTransform;
                recipesRect.anchorMin = new Vector2(0f, 1f);
                recipesRect.anchorMax = new Vector2(0f, 1f);
                recipesRect.pivot = new Vector2(0f, 1f);
                recipesRect.anchoredPosition = new Vector2(96f, -10f);
                recipesRect.sizeDelta = new Vector2(160f, 32f);

                Transform content = Find(root.transform, "Content");
                GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
                if (grid != null)
                    Object.DestroyImmediate(grid);
                VerticalLayoutGroup rows = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
                rows.spacing = 6f;
                rows.padding = new RectOffset(4, 4, 4, 4);
                rows.childAlignment = TextAnchor.UpperCenter;
                rows.childControlWidth = true;
                rows.childControlHeight = true;
                rows.childForceExpandWidth = true;
                rows.childForceExpandHeight = false;
                ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Transform scrollTf = GetOrCreate(cardTypes, "RecipeScroll");
                var scrollRect = (RectTransform)scrollTf;
                scrollRect.anchorMin = Vector2.zero;
                scrollRect.anchorMax = Vector2.one;
                scrollRect.pivot = new Vector2(0.5f, 0.5f);
                scrollRect.offsetMin = new Vector2(8f, 10f);
                scrollRect.offsetMax = new Vector2(-8f, -46f);
                Image scrollImg = scrollTf.GetComponent<Image>() ?? scrollTf.gameObject.AddComponent<Image>();
                scrollImg.sprite = plate;
                scrollImg.color = new Color(1f, 1f, 1f, 0.01f);
                scrollImg.raycastTarget = true;
                ScrollRect scroll = scrollTf.GetComponent<ScrollRect>() ?? scrollTf.gameObject.AddComponent<ScrollRect>();

                Transform viewport = GetOrCreate(scrollTf, "Viewport");
                Stretch((RectTransform)viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                if (viewport.GetComponent<RectMask2D>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
                content.SetParent(viewport, false);
                var contentRect = (RectTransform)content;
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, 0f);
                scroll.content = contentRect;
                scroll.viewport = (RectTransform)viewport;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 28f;
                scroll.inertia = true;

                Button recipeButton = BuildRecipeTemplate(cardTypes, normal, font);

                Transform cardCost = Find(root.transform, "CardCost");
                var costRect = (RectTransform)cardCost;
                costRect.anchorMin = new Vector2(1f, 0f);
                costRect.anchorMax = new Vector2(1f, 1f);
                costRect.pivot = new Vector2(1f, 0.5f);
                costRect.anchoredPosition = new Vector2(-36f, -24f);
                costRect.sizeDelta = new Vector2(270f, -150f);
                Image costBg = cardCost.GetComponent<Image>() ?? cardCost.gameObject.AddComponent<Image>();
                if (cardCost.GetComponent<CanvasRenderer>() == null)
                    cardCost.gameObject.AddComponent<CanvasRenderer>();
                costBg.sprite = plate;
                costBg.type = Image.Type.Sliced;
                costBg.color = Panel;
                costBg.raycastTarget = false;

                TextMeshProUGUI selectedTitle = GetOrCreateLabel(cardCost, "SelectedTitle", font, string.Empty, 26f, Cream, TextAlignmentOptions.Left);
                PinTop(selectedTitle.rectTransform, -14f, 40f);
                selectedTitle.textWrappingMode = TextWrappingModes.Normal;
                selectedTitle.overflowMode = TextOverflowModes.Ellipsis;

                TextMeshProUGUI selectedDescription = GetOrCreateLabel(cardCost, "SelectedDescription", font, string.Empty, 16f, Muted, TextAlignmentOptions.TopLeft);
                PinTop(selectedDescription.rectTransform, -58f, 78f);
                selectedDescription.textWrappingMode = TextWrappingModes.Normal;
                selectedDescription.overflowMode = TextOverflowModes.Ellipsis;

                TextMeshProUGUI costLabel = Find(cardCost, "LabelCost").GetComponent<TextMeshProUGUI>();
                ApplyFont(costLabel, font, 15f, Cream, TextAlignmentOptions.Left);
                costLabel.text = "KOSZT WYTWORZENIA";
                PinTop(costLabel.rectTransform, -146f, 26f);

                Transform costList = Find(cardCost, "CostList") ?? Find(cardCost, "GameObject");
                costList.name = "CostList";
                var costListRect = (RectTransform)costList;
                costListRect.anchorMin = Vector2.zero;
                costListRect.anchorMax = Vector2.one;
                costListRect.offsetMin = new Vector2(14f, 96f);
                costListRect.offsetMax = new Vector2(-14f, -178f);
                VerticalLayoutGroup costLayout = costList.GetComponent<VerticalLayoutGroup>();
                costLayout.spacing = 8f;
                costLayout.childAlignment = TextAnchor.UpperLeft;
                costLayout.childControlWidth = true;
                costLayout.childControlHeight = false;
                costLayout.childForceExpandWidth = true;
                costLayout.childForceExpandHeight = false;

                Transform craftButton = Find(root.transform, "CraftButton");
                craftButton.SetParent(cardCost, false);
                var craftRect = (RectTransform)craftButton;
                craftRect.anchorMin = new Vector2(0.5f, 0f);
                craftRect.anchorMax = new Vector2(0.5f, 0f);
                craftRect.pivot = new Vector2(0.5f, 0f);
                craftRect.anchoredPosition = new Vector2(0f, 16f);
                craftRect.sizeDelta = new Vector2(236f, 64f);
                craftRect.localScale = Vector3.one;
                Image craftImg = craftButton.GetComponent<Image>();
                craftImg.sprite = craft;
                craftImg.type = Image.Type.Simple;
                craftImg.preserveAspect = false;
                craftImg.color = Color.white;
                TextMeshProUGUI craftText = craftButton.GetComponentInChildren<TextMeshProUGUI>(true);
                ApplyFont(craftText, font, 18f, new Color(1f, 0.96f, 0.88f, 1f), TextAlignmentOptions.Center);
                craftText.text = "WYKUJ KARTĘ";

                Transform card = Find(root.transform, "CardToBeCrafted");
                var cardRect = (RectTransform)card;
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(-8f, 48f);
                cardRect.sizeDelta = new Vector2(200f, 300f);
                cardRect.localScale = new Vector3(0.66f, 0.66f, 0.66f);

                Image frameImg = GetOrCreateImage(root.transform, "OuterFrame", frame, false);
                frameImg.type = Image.Type.Sliced;
                frameImg.pixelsPerUnitMultiplier = 2.4f;
                frameImg.raycastTarget = false;
                Stretch((RectTransform)frameImg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                Image headerImg = GetOrCreateImage(root.transform, "Header", header, true);
                var headerRect = (RectTransform)headerImg.transform;
                headerRect.anchorMin = new Vector2(0.5f, 1f);
                headerRect.anchorMax = new Vector2(0.5f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = new Vector2(24f, 12f);
                headerRect.sizeDelta = new Vector2(560f, 128f);
                headerImg.raycastTarget = false;

                Image badgeImg = GetOrCreateImage(root.transform, "ForgeBadge", badge, true);
                var badgeRect = (RectTransform)badgeImg.transform;
                badgeRect.anchorMin = new Vector2(0f, 1f);
                badgeRect.anchorMax = new Vector2(0f, 1f);
                badgeRect.pivot = new Vector2(0f, 1f);
                badgeRect.anchoredPosition = new Vector2(-8f, 26f);
                badgeRect.sizeDelta = new Vector2(142f, 147f);
                badgeImg.raycastTarget = false;

                TextMeshProUGUI title = Find(root.transform, "LabelName").GetComponent<TextMeshProUGUI>();
                ApplyFont(title, font, 30f, Cream, TextAlignmentOptions.Center);
                title.text = "KUŹNIA KART";
                var titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(24f, -64f);
                titleRect.sizeDelta = new Vector2(360f, 44f);

                Image normalHolder = GetOrCreateImage(root.transform, "RecipeSpriteNormal", normal, true);
                normalHolder.gameObject.SetActive(false);
                Image selectedHolder = GetOrCreateImage(root.transform, "RecipeSpriteSelected", selected, true);
                selectedHolder.gameObject.SetActive(false);

                Order(root.transform, "Background", "BackgroundFrame", "ForgeStage", "CardTypes", "CardCost", "CardToBeCrafted", "OuterFrame", "Header", "ForgeBadge", "LabelName", "CloseButton");

                UIPanelCardCrafting crafting = root.GetComponent<UIPanelCardCrafting>();
                SerializedObject so = new SerializedObject(crafting);
                so.FindProperty("CardTypePrefab").objectReferenceValue = recipeButton;
                so.FindProperty("CostParent").objectReferenceValue = costList;
                so.FindProperty("_selectedTitle").objectReferenceValue = selectedTitle;
                so.FindProperty("_selectedDescription").objectReferenceValue = selectedDescription;
                so.FindProperty("_recipeNormalSprite").objectReferenceValue = normal;
                so.FindProperty("_recipeSelectedSprite").objectReferenceValue = selected;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Button BuildRecipeTemplate(Transform parent, Sprite normal, TMP_FontAsset font)
        {
            Transform existing = parent.Find("CraftRecipeButton");
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = new GameObject("CraftRecipeButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.layer = 5;
                go.transform.SetParent(parent, false);
            }

            LayoutElement element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.preferredHeight = 54f;
            element.minHeight = 54f;
            element.flexibleWidth = 1f;

            Image image = go.GetComponent<Image>();
            image.sprite = normal;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.04f, 0.96f, 1f);
            colors.pressedColor = new Color(0.78f, 0.74f, 0.68f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            TextMeshProUGUI label = GetOrCreateLabel(go.transform, "Label", font, "Receptura", 16f, Body, TextAlignmentOptions.Left);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.rectTransform.offsetMin = new Vector2(44f, 4f);
            label.rectTransform.offsetMax = new Vector2(-10f, -4f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            Image icon = GetOrCreateImage(go.transform, "Icon", null, true);
            var iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(24f, 0f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            icon.raycastTarget = false;
            icon.enabled = false;

            go.SetActive(false);
            go.transform.SetAsLastSibling();
            return button;
        }

        static void PinTop(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-28f, height);
        }

        static void Order(Transform root, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = root.Find(names[i]);
                if (child != null)
                    child.SetSiblingIndex(i);
            }
        }

        static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            return null;
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
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
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
                tmp = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
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
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }
    }
}
#endif
