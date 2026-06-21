using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// Procedurally constructs the heavy benchmark UI and returns a <see cref="WidgetRegistry"/>
    /// of the elements scenarios mutate. The layout is split into three independent sections so
    /// each scenario stresses one subsystem cleanly:
    /// a dense card grid (mesh / material / raycast bulk), a set of deeply nested layout columns
    /// (LayoutRebuilder), and a free layer of unparented-by-layout images (native re-batch).
    /// </summary>
    public static class UISceneBuilder
    {
        /// <summary>Root object holding every section; destroy it to tear the rig down.</summary>
        public const string RootName = "UIBenchmark.Generated";

        /// <summary>
        /// Builds the full rig under <paramref name="canvas"/>. Allocates freely; intended to run
        /// outside the measured window.
        /// </summary>
        public static WidgetRegistry Build(Canvas canvas, BenchmarkConfig config, ProceduralSprites sprites, Font font)
        {
            var reg = new WidgetRegistry { RootCanvas = canvas };

            var root = NewRect(RootName, canvas.transform);
            Stretch(root, Vector2.zero, Vector2.one);
            reg.Root = root.gameObject;

            var images = new List<Image>(8192);
            var filled = new List<Image>(2048);
            var materialTargets = new List<Image>(2048);
            var leaves = new List<LayoutElement>(1024);
            var freeRects = new List<RectTransform>(config.freeImages);

            var rng = new System.Random(config.seed);

            BuildCardGrid(root, config, sprites, font, images, filled, materialTargets);
            BuildLayoutColumns(root, config, sprites, images, leaves);
            BuildFreeLayer(root, config, sprites, rng, images, freeRects);

            var churnParent = NewRect("ChurnParent", root);
            Stretch(churnParent, new Vector2(0f, 0f), new Vector2(0.62f, 1f));
            reg.ChurnParent = churnParent;
            reg.ChurnSprite = sprites.Solid;

            reg.AllImages = images.ToArray();
            reg.FilledImages = filled.ToArray();
            reg.MaterialTargets = materialTargets.ToArray();
            reg.LayoutLeaves = leaves.ToArray();
            reg.FreeRects = freeRects.ToArray();
            reg.TotalGraphics = images.Count;
            return reg;
        }

        static void BuildCardGrid(RectTransform root, BenchmarkConfig config, ProceduralSprites sprites, Font font,
            List<Image> images, List<Image> filled, List<Image> materialTargets)
        {
            var section = NewRect("Section_CardGrid", root);
            section.anchorMin = new Vector2(0f, 0f);
            section.anchorMax = new Vector2(0.62f, 1f);
            section.offsetMin = new Vector2(4f, 4f);
            section.offsetMax = new Vector2(-4f, -4f);

            var grid = section.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(44f, 36f);
            grid.spacing = new Vector2(2f, 2f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            int total = Mathf.Max(1, config.cardPanels) * Mathf.Max(1, config.cardsPerPanel);
            int maskedEvery = config.maskedCardFraction > 0f ? Mathf.Max(1, Mathf.RoundToInt(1f / config.maskedCardFraction)) : int.MaxValue;

            for (int i = 0; i < total; i++)
            {
                int mode = i & 3;
                Image card;
                switch (mode)
                {
                    case 1:
                        card = NewImage("Card_Sliced", section, sprites.Sliced, Image.Type.Sliced, true);
                        break;
                    case 2:
                        card = NewImage("Card_Tiled", section, sprites.Tiled, Image.Type.Tiled, true);
                        break;
                    case 3:
                        card = NewImage("Card_Filled", section, sprites.Circle, Image.Type.Filled, true);
                        card.fillMethod = Image.FillMethod.Radial360;
                        card.fillOrigin = 0;
                        card.fillAmount = 1f;
                        filled.Add(card);
                        break;
                    default:
                        card = NewImage("Card_Simple", section, sprites.Solid, Image.Type.Simple, true);
                        break;
                }

                card.color = Color.HSVToRGB((i * 0.013f) % 1f, 0.45f, 0.85f);
                images.Add(card);
                if (i % 3 == 0) materialTargets.Add(card);

                bool masked = (i % maskedEvery) == 0;
                if (masked)
                {
                    if ((i / maskedEvery) % 2 == 0)
                    {
                        card.gameObject.AddComponent<RectMask2D>();
                    }
                    else
                    {
                        var mask = card.gameObject.AddComponent<Mask>();
                        mask.showMaskGraphic = true;
                    }
                }

                var icon = NewImage("Icon", card.rectTransform, sprites.Circle, Image.Type.Simple, false);
                Stretch(icon.rectTransform, Vector2.zero, Vector2.one);
                icon.rectTransform.offsetMin = new Vector2(6f, 6f);
                icon.rectTransform.offsetMax = new Vector2(-6f, -6f);
                icon.color = Color.HSVToRGB((i * 0.013f + 0.5f) % 1f, 0.6f, 1f);
                images.Add(icon);

                if (config.includeText && font != null && (i % 8) == 0)
                {
                    var label = NewText("Label", card.rectTransform, font);
                    Stretch(label.rectTransform, Vector2.zero, Vector2.one);
                    label.text = i.ToString();
                }
            }
        }

        static void BuildLayoutColumns(RectTransform root, BenchmarkConfig config, ProceduralSprites sprites,
            List<Image> images, List<LayoutElement> leaves)
        {
            var section = NewRect("Section_DeepLayout", root);
            section.anchorMin = new Vector2(0.62f, 0.5f);
            section.anchorMax = new Vector2(1f, 1f);
            section.offsetMin = new Vector2(4f, 4f);
            section.offsetMax = new Vector2(-4f, -4f);

            var row = section.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigGroup(row);
            row.childForceExpandHeight = false;
            row.childControlHeight = false;

            int columns = Mathf.Max(1, config.layoutColumns);
            int depth = Mathf.Max(1, config.layoutDepth);
            int leavesPer = Mathf.Max(1, config.layoutLeavesPerColumn);

            for (int c = 0; c < columns; c++)
            {
                var column = NewRect("Column", section);
                var colGroup = column.gameObject.AddComponent<VerticalLayoutGroup>();
                ConfigGroup(colGroup);
                var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                RectTransform current = column;
                for (int d = 1; d < depth; d++)
                {
                    var level = NewRect("L" + d, current);
                    HorizontalOrVerticalLayoutGroup g = (d & 1) == 0
                        ? (HorizontalOrVerticalLayoutGroup)level.gameObject.AddComponent<VerticalLayoutGroup>()
                        : level.gameObject.AddComponent<HorizontalLayoutGroup>();
                    ConfigGroup(g);
                    current = level;
                }

                for (int k = 0; k < leavesPer; k++)
                {
                    var leaf = NewImage("Leaf", current, sprites.Solid, Image.Type.Simple, false);
                    leaf.color = Color.HSVToRGB((c * 0.05f + k * 0.02f) % 1f, 0.4f, 0.9f);
                    var le = leaf.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 56f;
                    le.preferredHeight = 14f + (k % 3) * 4f;
                    leaves.Add(le);
                    images.Add(leaf);
                }
            }
        }

        static void BuildFreeLayer(RectTransform root, BenchmarkConfig config, ProceduralSprites sprites,
            System.Random rng, List<Image> images, List<RectTransform> freeRects)
        {
            var section = NewRect("Section_FreeLayer", root);
            section.anchorMin = new Vector2(0.62f, 0f);
            section.anchorMax = new Vector2(1f, 0.5f);
            section.offsetMin = new Vector2(4f, 4f);
            section.offsetMax = new Vector2(-4f, -4f);

            int count = Mathf.Max(0, config.freeImages);
            for (int i = 0; i < count; i++)
            {
                var img = NewImage("Free", section, (i & 1) == 0 ? sprites.Solid : sprites.Circle, Image.Type.Simple, false);
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(20f, 20f);
                rt.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() * 2.0 - 1.0) * 300f,
                    (float)(rng.NextDouble() * 2.0 - 1.0) * 240f);
                img.color = Color.HSVToRGB((float)rng.NextDouble(), 0.5f, 1f);
                images.Add(img);
                freeRects.Add(rt);
            }
        }

        static void ConfigGroup(HorizontalOrVerticalLayoutGroup g)
        {
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            g.spacing = 2f;
            g.padding = new RectOffset(2, 2, 2, 2);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Image.Type type, bool raycastTarget)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.raycastTarget = raycastTarget;
            return img;
        }

        static Text NewText(string name, Transform parent, Font font)
        {
            var rt = NewRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
