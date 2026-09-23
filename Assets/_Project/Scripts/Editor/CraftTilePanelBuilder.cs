#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace M3P.Editor
{
    public static class CraftTilePanelBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/UI/Map/UIPanelCraftTles Variant.prefab";
        const string Art = "Assets/_Project/Graphics/UI/Panels/CraftTile/";
        const string FontPath = "Assets/_Project/Graphics/UI/Font/MedievalSharp-Bold SDF.asset";

        static readonly Color Cream = new Color(0.91f, 0.82f, 0.62f, 1f);
        static readonly Color Cyan = new Color(0.42f, 0.82f, 0.92f, 1f);
        static readonly Color Body = new Color(0.90f, 0.93f, 0.96f, 1f);
        static readonly Color Muted = new Color(0.72f, 0.76f, 0.82f, 1f);

        public static string Build()
        {
            var log = new List<string>();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = root.GetComponent<RectTransform>();
                Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                panel.pivot = new Vector2(0.5f, 0.5f);

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                Sprite workshop = LoadSprite("Backgrounds/workshop-background.png");
                Sprite scrim = LoadSprite("Backgrounds/left-scrim.png");
                Sprite resourceBar = LoadSprite("Backgrounds/resource-bar.png");
                Sprite forge = LoadSprite("Forge/forge-empty.png");
                Sprite enchant = LoadSprite("Frames/enchantment-panel.png");
                Sprite tileNormal = LoadSprite("Frames/tile-normal.png");
                Sprite tileSelected = LoadSprite("Frames/tile-selected.png");
                Sprite effectNormal = LoadSprite("Buttons/effect-normal.png");
                Sprite effectSelected = LoadSprite("Buttons/effect-selected.png");
                Sprite apply = LoadSprite("Buttons/apply-normal.png");
                Sprite slotEmpty = LoadSprite("Slots/slot-empty.png");
                Sprite slotSelected = LoadSprite("Slots/slot-selected.png");
                Sprite arrow = LoadSprite("Decor/arrow-right.png");
                Sprite closeIcon = LoadSprite("Decor/close.png");
                Sprite helpIcon = LoadSprite("Decor/help-menu.png");
                Sprite ring = LoadSprite("Decor/input-ring.png");
                Sprite divider = LoadSprite("Decor/divider.png");

                Transform background = Find(root.transform, "Background");
                if (background != null)
                {
                    var bg = background.GetComponent<Image>();
                    bg.sprite = workshop;
                    bg.type = Image.Type.Simple;
                    bg.preserveAspect = false;
                    bg.color = Color.white;
                    bg.raycastTarget = true;
                    Stretch(background as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }

                Transform frame = Find(root.transform, "BackgroundFrame");
                if (frame != null)
                    frame.gameObject.SetActive(false);

                Transform costs = Find(root.transform, "Costs");
                if (costs != null)
                    costs.gameObject.SetActive(false);

                Image scrimImg = GetOrCreateImage(root.transform, "LeftScrim", scrim, false);
                Stretch(scrimImg.rectTransform, new Vector2(0f, 0f), new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);
                scrimImg.preserveAspect = false;
                scrimImg.color = new Color(1f, 1f, 1f, 0.92f);
                scrimImg.rectTransform.SetSiblingIndex(1);

                Image forgeImg = GetOrCreateImage(root.transform, "Forge", forge, true);
                Place(forgeImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 24f), new Vector2(760f, 880f));
                forgeImg.rectTransform.SetSiblingIndex(2);

                Transform showTile = Find(root.transform, "ShowTile");
                Place(showTile as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 24f), new Vector2(760f, 880f));
                showTile.SetSiblingIndex(3);

                Transform baseTf = Find(showTile, "Base");
                Place(baseTf as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, -168f), new Vector2(228f, 156f));
                var baseImage = baseTf.GetComponent<Image>();
                baseImage.preserveAspect = true;
                baseImage.raycastTarget = false;
                baseImage.color = Color.white;

                StyleTitle(root.transform, font, divider);
                StyleTilePicker(root.transform, font, tileNormal);
                StyleEffectPicker(root.transform, font, effectNormal);
                StyleEnchantment(root.transform, font, enchant, arrow, apply, ring, slotEmpty);
                StyleResources(root.transform, font, resourceBar);
                StyleCloseAndHelp(root.transform, font, closeIcon, helpIcon, ring);

                Button tileTemplate = BuildTileTemplate(root.transform, tileNormal);
                Button effectTemplate = BuildEffectTemplate(root.transform, effectNormal, font);

                var crafting = root.GetComponent<UIPanelTileCrafting>()
                    ?? root.GetComponentInChildren<UIPanelTileCrafting>(true);
                var show = showTile.GetComponent<UIPanelCraftTileShowTile>();
                BindCrafting(crafting, show, tileTemplate, effectTemplate, tileNormal, tileSelected, effectNormal, effectSelected, slotEmpty, slotSelected);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                log.Add("Saved " + PrefabPath);

                ApplySceneInstance();
                log.Add("Updated scene instance anchors if present.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return string.Join("\n", log);
        }

        static void StyleTitle(Transform root, TMP_FontAsset font, Sprite divider)
        {
            Transform label = Find(root, "LabelName");
            Place(label as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -28f), new Vector2(520f, 70f));
            var tmp = label.GetComponent<TextMeshProUGUI>();
            ApplyFont(tmp, font, 54f, Cream, TextAlignmentOptions.Left);
            tmp.text = "TILE FORGE";
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;

            Image div = GetOrCreateImage(root, "TitleDivider", divider, true);
            Place(div.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -100f), new Vector2(36f, 36f));
            div.color = Cream;
        }

        static void StyleTilePicker(Transform root, TMP_FontAsset font, Sprite tileNormal)
        {
            TextMeshProUGUI choose = GetOrCreateLabel(root, "LabelChooseTile", font, "Choose Tile", 26f, Body, TextAlignmentOptions.Left);
            Place(choose.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -128f), new Vector2(360f, 32f));

            Transform tileType = Find(root, "TileType");
            Place(tileType as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -164f), new Vector2(560f, 128f));
            var scroll = tileType.GetComponent<ScrollRect>();
            if (scroll != null)
                scroll.horizontal = false;

            Transform parent = Find(tileType, "TileTypesParent");
            var grid = parent.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(92f, 118f);
            grid.spacing = new Vector2(10f, 0f);
            grid.childAlignment = TextAnchor.MiddleLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            Stretch(parent as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        static void StyleEffectPicker(Transform root, TMP_FontAsset font, Sprite effectNormal)
        {
            TextMeshProUGUI choose = GetOrCreateLabel(root, "LabelChooseEffect", font, "Choose Effect", 26f, Body, TextAlignmentOptions.Left);
            Place(choose.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 428f), new Vector2(360f, 32f));

            Transform upgradeTypes = Find(root, "UpgradeTypes");
            var rt = upgradeTypes as RectTransform;
            Place(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 118f), new Vector2(470f, 300f));
            var image = upgradeTypes.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
            }

            Transform parent = Find(upgradeTypes, "UpgradeTypesParent");
            var grid = parent.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(224f, 50f);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var parentRt = parent as RectTransform;
            parentRt.anchorMin = new Vector2(0f, 1f);
            parentRt.anchorMax = new Vector2(1f, 1f);
            parentRt.pivot = new Vector2(0.5f, 1f);
            parentRt.anchoredPosition = Vector2.zero;
            parentRt.sizeDelta = new Vector2(0f, 0f);
        }

        static void StyleEnchantment(Transform root, TMP_FontAsset font, Sprite panel, Sprite arrow, Sprite apply, Sprite ring, Sprite slotEmpty)
        {
            Transform costParent = Find(root, "CostParent");
            var rt = costParent as RectTransform;
            Place(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 18f), new Vector2(392f, 940f));
            rt.pivot = new Vector2(1f, 0.5f);

            Image panelImg = costParent.GetComponent<Image>();
            if (panelImg == null)
                panelImg = costParent.gameObject.AddComponent<Image>();
            panelImg.sprite = panel;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;
            panelImg.raycastTarget = true;
            if (costParent.GetComponent<CanvasRenderer>() == null)
                costParent.gameObject.AddComponent<CanvasRenderer>();

            TextMeshProUGUI title = GetOrCreateLabel(costParent, "LabelEnchantment", font, "Enchantment", 34f, Cyan, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(320f, 44f));

            Transform preview = GetOrCreate(costParent, "PreviewRow");
            Place(preview as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(340f, 140f));

            BuildPreviewSide(preview, "Before", "BeforeImg", font, -110f);
            Image arrowImg = GetOrCreateImage(preview, "Arrow", arrow, true);
            Place(arrowImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 24f));
            arrowImg.color = Cream;
            Transform after = BuildPreviewSide(preview, "After", "AfterImg", font, 110f);
            Image afterBonus = GetOrCreateImage(Find(after, "AfterImg"), "AfterBonusImg", null, true);
            Stretch(afterBonus.rectTransform, new Vector2(0.55f, 0f), new Vector2(1.05f, 0.5f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI effectName = GetOrCreateLabel(costParent, "LabelEffectName", font, string.Empty, 30f, Cyan, TextAlignmentOptions.Center);
            Place(effectName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(320f, 36f));

            TextMeshProUGUI effectDesc = GetOrCreateLabel(costParent, "LabelEffectDescription", font, string.Empty, 20f, Muted, TextAlignmentOptions.Center);
            Place(effectDesc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -276f), new Vector2(300f, 64f));
            effectDesc.textWrappingMode = TextWrappingModes.Normal;
            effectDesc.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI slotsLabel = GetOrCreateLabel(costParent, "LabelEffectSlots", font, "Effect Slots", 20f, Muted, TextAlignmentOptions.Center);
            Place(slotsLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -348f), new Vector2(240f, 28f));

            Transform slotsRow = GetOrCreate(costParent, "SlotsRow");
            Place(slotsRow as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -392f), new Vector2(280f, 56f));
            string[] slotNames = { "SlotUp", "SlotDown", "SlotLeft", "SlotRight" };
            for (int i = 0; i < slotNames.Length; i++)
            {
                Transform slot = Find(root, slotNames[i]);
                slot.SetParent(slotsRow, false);
                Place(slot as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-105f + i * 70f, 0f), new Vector2(56f, 56f));
                var slotImage = slot.GetComponent<Image>();
                slotImage.sprite = slotEmpty;
                slotImage.color = Color.white;
                slotImage.preserveAspect = true;
                slotImage.type = Image.Type.Simple;
                Transform bonus = slot.Find("BonusImg");
                if (bonus != null)
                {
                    var bonusImage = bonus.GetComponent<UnityEngine.UI.Image>();
                    if (bonusImage != null)
                    {
                        bonusImage.enabled = false;
                        bonusImage.sprite = null;
                    }
                }
            }

            Transform labelCost = Find(costParent, "LabelCost");
            Place(labelCost as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -468f), new Vector2(200f, 32f));
            var costTmp = labelCost.GetComponent<TextMeshProUGUI>();
            ApplyFont(costTmp, font, 22f, Muted, TextAlignmentOptions.Center);
            costTmp.text = "Cost:";

            Transform costList = Find(costParent, "CostList");
            Place(costList as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -508f), new Vector2(280f, 180f));
            costList.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;

            Transform craftButton = Find(root, "CraftButton");
            craftButton.SetParent(costParent, false);
            Place(craftButton as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(320f, 58f));
            var craftImage = craftButton.GetComponent<Image>();
            craftImage.sprite = apply;
            craftImage.type = Image.Type.Simple;
            craftImage.preserveAspect = true;
            craftImage.color = Color.white;
            var craftText = craftButton.GetComponentInChildren<TextMeshProUGUI>(true);
            ApplyFont(craftText, font, 24f, Color.white, TextAlignmentOptions.Center);
            craftText.text = "Apply Effect";
            craftText.fontStyle = FontStyles.Bold;

            Image badge = GetOrCreateImage(craftButton, "InputBadge", ring, true);
            Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(28f, 28f));
            TextMeshProUGUI badgeLabel = GetOrCreateLabel(badge.rectTransform, "Label", font, "A", 16f, Cyan, TextAlignmentOptions.Center);
            Stretch(badgeLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        static Transform BuildPreviewSide(Transform parent, string title, string imageName, TMP_FontAsset font, float x)
        {
            Transform group = GetOrCreate(parent, title + "Group");
            Place(group as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(110f, 130f));
            TextMeshProUGUI label = GetOrCreateLabel(group, "Label" + title, font, title, 18f, Muted, TextAlignmentOptions.Center);
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(100f, 24f));
            Image img = GetOrCreateImage(group, imageName, null, true);
            Place(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(88f, 88f));
            img.color = Color.white;
            return group;
        }

        static void StyleResources(Transform root, TMP_FontAsset font, Sprite bar)
        {
            Image barImg = GetOrCreateImage(root, "ResourceBar", bar, false);
            Place(barImg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 92f));
            barImg.rectTransform.pivot = new Vector2(0.5f, 0f);
            barImg.preserveAspect = false;
            barImg.raycastTarget = false;
            barImg.color = new Color(1f, 1f, 1f, 0.95f);

            TextMeshProUGUI owned = GetOrCreateLabel(root, "LabelOwnedResources", font, "Owned Resources", 22f, Body, TextAlignmentOptions.Left);
            Place(owned.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 58f), new Vector2(280f, 28f));

            Transform shards = Find(root, "Shards");
            Place(shards as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(330f, 28f), new Vector2(720f, 70f));
            shards.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        }

        static void StyleCloseAndHelp(Transform root, TMP_FontAsset font, Sprite closeIcon, Sprite helpIcon, Sprite ring)
        {
            Transform close = Find(root, "CloseButton");
            Place(close as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-148f, 26f), new Vector2(120f, 44f));
            var closeRt = close as RectTransform;
            closeRt.pivot = new Vector2(1f, 0f);
            var closeImage = close.GetComponent<Image>();
            closeImage.sprite = null;
            closeImage.type = Image.Type.Simple;
            closeImage.color = new Color(1f, 1f, 1f, 0f);
            Transform plate = close.Find("Plate");
            if (plate != null)
                plate.gameObject.SetActive(false);
            Transform icon = close.Find("Icon");
            if (icon != null)
                icon.gameObject.SetActive(false);

            var closeText = close.GetComponentInChildren<TextMeshProUGUI>(true);
            closeText.gameObject.SetActive(true);
            ApplyFont(closeText, font, 22f, Body, TextAlignmentOptions.Left);
            closeText.text = "Back";
            var textRt = closeText.rectTransform;
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(44f, 0f);
            textRt.offsetMax = Vector2.zero;

            Image badge = GetOrCreateImage(close, "InputBadge", ring, true);
            Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(26f, 26f));
            TextMeshProUGUI badgeLabel = GetOrCreateLabel(badge.rectTransform, "Label", font, "B", 14f, Body, TextAlignmentOptions.Center);
            Stretch(badgeLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Transform oldHelpLabel = root.Find("LabelHelp");
            if (oldHelpLabel != null)
                Object.DestroyImmediate(oldHelpLabel.gameObject);

            Transform help = GetOrCreate(root, "HelpButton");
            Place(help as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 26f), new Vector2(110f, 44f));
            (help as RectTransform).pivot = new Vector2(1f, 0f);
            Image helpImg = help.GetComponent<Image>();
            if (helpImg != null)
            {
                helpImg.sprite = null;
                helpImg.color = new Color(1f, 1f, 1f, 0f);
                helpImg.raycastTarget = false;
            }

            Image helpIconImg = GetOrCreateImage(help, "Icon", helpIcon, true);
            Place(helpIconImg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(28f, 28f));
            TextMeshProUGUI helpText = GetOrCreateLabel(help, "LabelHelp", font, "Help", 20f, Body, TextAlignmentOptions.Left);
            Place(helpText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            helpText.rectTransform.offsetMin = new Vector2(48f, 0f);
            helpText.rectTransform.offsetMax = Vector2.zero;
        }

        static Button BuildTileTemplate(Transform root, Sprite frame)
        {
            Transform templates = GetOrCreate(root, "Templates");
            templates.gameObject.SetActive(false);
            Transform buttonTf = GetOrCreate(templates, "CraftTileButton");
            Place(buttonTf as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 118f));
            Image image = EnsureImage(buttonTf, frame, true);
            image.raycastTarget = true;
            Button button = buttonTf.GetComponent<Button>() ?? buttonTf.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Image icon = GetOrCreateImage(buttonTf, "Icon", null, true);
            Stretch(icon.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18f, -22f));
            icon.raycastTarget = false;
            return button;
        }

        static Button BuildEffectTemplate(Transform root, Sprite frame, TMP_FontAsset font)
        {
            Transform templates = GetOrCreate(root, "Templates");
            templates.gameObject.SetActive(false);
            Transform buttonTf = GetOrCreate(templates, "CraftEffectButton");
            Place(buttonTf as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(224f, 50f));
            Image image = EnsureImage(buttonTf, frame, true);
            image.raycastTarget = true;
            Button button = buttonTf.GetComponent<Button>() ?? buttonTf.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Image icon = GetOrCreateImage(buttonTf, "Icon", null, true);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(28f, 28f));
            icon.raycastTarget = false;
            TextMeshProUGUI name = GetOrCreateLabel(buttonTf, "Name", font, "Effect", 18f, Body, TextAlignmentOptions.Left);
            Place(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(-58f, 0f));
            name.rectTransform.offsetMin = new Vector2(58f, 4f);
            name.rectTransform.offsetMax = new Vector2(-12f, -4f);
            return button;
        }

        static void BindCrafting(
            UIPanelTileCrafting crafting,
            UIPanelCraftTileShowTile show,
            Button tileTemplate,
            Button effectTemplate,
            Sprite tileNormal,
            Sprite tileSelected,
            Sprite effectNormal,
            Sprite effectSelected,
            Sprite slotEmpty,
            Sprite slotSelected)
        {
            if (crafting == null)
                return;

            SerializedObject so = new SerializedObject(crafting);
            so.FindProperty("TileTypePrefab").objectReferenceValue = tileTemplate;
            so.FindProperty("UpgradeTypePrefab").objectReferenceValue = effectTemplate;
            so.FindProperty("TileFrameNormal").objectReferenceValue = tileNormal;
            so.FindProperty("TileFrameSelected").objectReferenceValue = tileSelected;
            so.FindProperty("EffectFrameNormal").objectReferenceValue = effectNormal;
            so.FindProperty("EffectFrameSelected").objectReferenceValue = effectSelected;
            AssignSpriteArray(so.FindProperty("CatalogueTileSprites"), LoadAllSprites("Tiles"));
            AssignSpriteArray(so.FindProperty("CatalogueEffectSprites"), LoadAllSprites("Icons/Monochrome"));
            AssignSpriteArray(so.FindProperty("ShardSprites"), LoadAllSprites("Shards"));
            so.ApplyModifiedPropertiesWithoutUndo();

            if (show == null)
                return;

            SerializedObject showSo = new SerializedObject(show);
            AssignSpriteArray(showSo.FindProperty("WorkpieceSprites"), LoadAllSprites("Forge"));
            showSo.FindProperty("SlotEmptySprite").objectReferenceValue = slotEmpty;
            showSo.FindProperty("SlotSelectedSprite").objectReferenceValue = slotSelected;
            showSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignSpriteArray(SerializedProperty property, Sprite[] sprites)
        {
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        static Sprite[] LoadAllSprites(string relativeFolder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { Art + relativeFolder });
            var list = new List<Sprite>();
            for (int i = 0; i <  guids.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (sprite != null)
                    list.Add(sprite);
            }

            return list.ToArray();
        }

        static void ApplySceneInstance()
        {
            UIPanelTileCrafting[] panels = Object.FindObjectsByType<UIPanelTileCrafting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
            {
                var rt = panels[i].transform as RectTransform;
                if (rt == null)
                    continue;
                Stretch(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                EditorUtility.SetDirty(panels[i]);
            }
        }

        static Sprite LoadSprite(string relative)
        {
            string path = Art + relative;
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
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
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
            if (image.GetComponent<CanvasRenderer>() == null)
                image.gameObject.AddComponent<CanvasRenderer>();
            return image;
        }

        static Image EnsureImage(Transform tf, Sprite sprite, bool preserveAspect)
        {
            Image image = tf.GetComponent<Image>();
            if (image == null)
                image = tf.gameObject.AddComponent<Image>();
            if (tf.GetComponent<CanvasRenderer>() == null)
                tf.gameObject.AddComponent<CanvasRenderer>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            return image;
        }

        static TextMeshProUGUI GetOrCreateLabel(Transform parent, string name, TMP_FontAsset font, string text, float size, Color color, TextAlignmentOptions align)
        {
            Transform existing = parent.Find(name);
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                tmp = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
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
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(min.x == 0f && max.x == 0f ? 0f : min.x == 1f && max.x == 1f ? 1f : 0.5f,
                min.y == 0f && max.y == 0f ? 0f : min.y == 1f && max.y == 1f ? 1f : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
#endif
