using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace TMPro
{

    public static class TMP_DefaultControls
    {
        public struct Resources
        {
            public Sprite standard;
            public Sprite background;
            public Sprite inputField;
            public Sprite knob;
            public Sprite checkmark;
            public Sprite dropdown;
            public Sprite mask;
        }

        private const float kWidth = 160f;
        private const float kThickHeight = 30f;
        private const float kThinHeight = 20f;
        private static Vector2 s_TextElementSize = new(100f, 100f);
        private static Vector2 s_ThickElementSize = new(kWidth, kThickHeight);
        private static Vector2 s_ThinElementSize = new(kWidth, kThinHeight);

        private static Color s_DefaultSelectableColor = new(1f, 1f, 1f, 1f);
        private static Color s_TextColor = new(50f / 255f, 50f / 255f, 50f / 255f, 1f);


        private static GameObject CreateUIElementRoot(string name, Vector2 size)
        {
            GameObject root;

            #if UNITY_EDITOR
            root = ObjectFactory.CreateGameObject(name, typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            #else
            root = new GameObject(name);
            RectTransform rectTransform = root.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            #endif

            return root;
        }

        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            GameObject go;
            #if UNITY_EDITOR
            go = ObjectFactory.CreateGameObject(name, typeof(RectTransform));
            #else
            go = new GameObject(name);
            go.AddComponent<RectTransform>();
            #endif
            SetParentAndAlign(go, parent);
            return go;
        }

        private static void SetDefaultTextValues(TMP_Text lbl)
        {
            lbl.color = s_TextColor;
            lbl.fontSize = 14;
        }

        private static void SetDefaultColorTransitionValues(Selectable slider)
        {
            ColorBlock colors = slider.colors;
            colors.highlightedColor = new(0.882f, 0.882f, 0.882f);
            colors.pressedColor = new(0.698f, 0.698f, 0.698f);
            colors.disabledColor = new(0.521f, 0.521f, 0.521f);
        }

        private static void SetParentAndAlign(GameObject child, GameObject parent)
        {
            if (parent == null)
                return;

#if UNITY_EDITOR
            Undo.SetTransformParent(child.transform, parent.transform, "");
#else
            child.transform.SetParent(parent.transform, false);
#endif
            SetLayerRecursively(child, parent.layer);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        public static GameObject CreateScrollbar(Resources resources)
        {
            GameObject scrollbarRoot = CreateUIElementRoot("Scrollbar", s_ThinElementSize);

            GameObject sliderArea = CreateUIObject("Sliding Area", scrollbarRoot);
            GameObject handle = CreateUIObject("Handle", sliderArea);

            Image bgImage = AddComponent<Image>(scrollbarRoot);
            bgImage.sprite = resources.background;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = s_DefaultSelectableColor;

            Image handleImage = AddComponent<Image>(handle);
            handleImage.sprite = resources.standard;
            handleImage.type = Image.Type.Sliced;
            handleImage.color = s_DefaultSelectableColor;

            RectTransform sliderAreaRect = sliderArea.GetComponent<RectTransform>();
            sliderAreaRect.sizeDelta = new(-20, -20);
            sliderAreaRect.anchorMin = Vector2.zero;
            sliderAreaRect.anchorMax = Vector2.one;

            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new(20, 20);

            Scrollbar scrollbar = AddComponent<Scrollbar>(scrollbarRoot);
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            SetDefaultColorTransitionValues(scrollbar);

            return scrollbarRoot;
        }

        public static GameObject CreateButton(Resources resources)
        {
            GameObject buttonRoot = CreateUIElementRoot("Button", s_ThickElementSize);

            GameObject childText;
#if UNITY_EDITOR
            childText = ObjectFactory.CreateGameObject("Text (TMP)", typeof(RectTransform));
#else
            childText = new GameObject("Text (TMP)");
            childText.AddComponent<RectTransform>();
#endif

            SetParentAndAlign(childText, buttonRoot);

            Image image = AddComponent<Image>(buttonRoot);
            image.sprite = resources.standard;
            image.type = Image.Type.Sliced;
            image.color = s_DefaultSelectableColor;

            Button bt = AddComponent<Button>(buttonRoot);
            SetDefaultColorTransitionValues(bt);

            TextMeshProUGUI text = AddComponent<TextMeshProUGUI>(childText);
            text.text = "Button";
            text.alignment = TextAlignmentOptions.Center;
            SetDefaultTextValues(text);

            RectTransform textRectTransform = childText.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;

            return buttonRoot;
        }

        public static GameObject CreateText(Resources resources)
        {
            GameObject go = null;

            #if UNITY_EDITOR
                go = ObjectFactory.CreateGameObject("Text (TMP)", typeof(TextMeshProUGUI));
            #else
                go = CreateUIElementRoot("Text (TMP)", s_TextElementSize);
                go.AddComponent<TextMeshProUGUI>();
            #endif

            return go;
        }

        public static GameObject CreateInputField(Resources resources)
        {
            GameObject root = CreateUIElementRoot("InputField (TMP)", s_ThickElementSize);

            GameObject textArea = CreateUIObject("Text Area", root);
            GameObject childPlaceholder = CreateUIObject("Placeholder", textArea);
            GameObject childText = CreateUIObject("Text", textArea);

            Image image = AddComponent<Image>(root);
            image.sprite = resources.inputField;
            image.type = Image.Type.Sliced;
            image.color = s_DefaultSelectableColor;

            TMP_InputField inputField = AddComponent<TMP_InputField>(root);
            SetDefaultColorTransitionValues(inputField);

            RectMask2D rectMask = AddComponent<RectMask2D>(textArea);
            rectMask.padding = new(-8, -5, -8, -5);

            RectTransform textAreaRectTransform = textArea.GetComponent<RectTransform>();
            textAreaRectTransform.anchorMin = Vector2.zero;
            textAreaRectTransform.anchorMax = Vector2.one;
            textAreaRectTransform.sizeDelta = Vector2.zero;
            textAreaRectTransform.offsetMin = new(10, 6);
            textAreaRectTransform.offsetMax = new(-10, -7);


            TextMeshProUGUI text = AddComponent<TextMeshProUGUI>(childText);
            text.text = "";
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.extraPadding = true;
            text.richText = true;
            SetDefaultTextValues(text);

            TextMeshProUGUI placeholder = AddComponent<TextMeshProUGUI>(childPlaceholder);
            placeholder.text = "Enter text...";
            placeholder.fontSize = 14;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            placeholder.extraPadding = true;

            Color placeholderColor = text.color;
            placeholderColor.a *= 0.5f;
            placeholder.color = placeholderColor;

            AddComponent<LayoutElement>(placeholder.gameObject).ignoreLayout = true;

            RectTransform textRectTransform = childText.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.offsetMin = new(0, 0);
            textRectTransform.offsetMax = new(0, 0);

            RectTransform placeholderRectTransform = childPlaceholder.GetComponent<RectTransform>();
            placeholderRectTransform.anchorMin = Vector2.zero;
            placeholderRectTransform.anchorMax = Vector2.one;
            placeholderRectTransform.sizeDelta = Vector2.zero;
            placeholderRectTransform.offsetMin = new(0, 0);
            placeholderRectTransform.offsetMax = new(0, 0);

            inputField.textViewport = textAreaRectTransform;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.fontAsset = text.font;

            return root;
        }

        public static GameObject CreateDropdown(Resources resources)
        {
            GameObject root = CreateUIElementRoot("Dropdown", s_ThickElementSize);

            GameObject label = CreateUIObject("Label", root);
            GameObject arrow = CreateUIObject("Arrow", root);
            GameObject template = CreateUIObject("Template", root);
            GameObject viewport = CreateUIObject("Viewport", template);
            GameObject content = CreateUIObject("Content", viewport);
            GameObject item = CreateUIObject("Item", content);
            GameObject itemBackground = CreateUIObject("Item Background", item);
            GameObject itemCheckmark = CreateUIObject("Item Checkmark", item);
            GameObject itemLabel = CreateUIObject("Item Label", item);

            GameObject scrollbar = CreateScrollbar(resources);
            scrollbar.name = "Scrollbar";
            SetParentAndAlign(scrollbar, template);

            Scrollbar scrollbarScrollbar = scrollbar.GetComponent<Scrollbar>();
            scrollbarScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            RectTransform vScrollbarRT = scrollbar.GetComponent<RectTransform>();
            vScrollbarRT.anchorMin = Vector2.right;
            vScrollbarRT.anchorMax = Vector2.one;
            vScrollbarRT.pivot = Vector2.one;
            vScrollbarRT.sizeDelta = new(vScrollbarRT.sizeDelta.x, 0);

            TextMeshProUGUI itemLabelText = AddComponent<TextMeshProUGUI>(itemLabel);
            SetDefaultTextValues(itemLabelText);
            itemLabelText.alignment = TextAlignmentOptions.Left;

            Image itemBackgroundImage = AddComponent<Image>(itemBackground);
            itemBackgroundImage.color = new Color32(245, 245, 245, 255);

            Image itemCheckmarkImage = AddComponent<Image>(itemCheckmark);
            itemCheckmarkImage.sprite = resources.checkmark;

            Toggle itemToggle = AddComponent<Toggle>(item);
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmarkImage;
            itemToggle.isOn = true;

            Image templateImage = AddComponent<Image>(template);
            templateImage.sprite = resources.standard;
            templateImage.type = Image.Type.Sliced;

            ScrollRect templateScrollRect = AddComponent<ScrollRect>(template);
            templateScrollRect.content = (RectTransform)content.transform;
            templateScrollRect.viewport = (RectTransform)viewport.transform;
            templateScrollRect.horizontal = false;
            templateScrollRect.movementType = ScrollRect.MovementType.Clamped;
            templateScrollRect.verticalScrollbar = scrollbarScrollbar;
            templateScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            templateScrollRect.verticalScrollbarSpacing = -3;

            Mask scrollRectMask = AddComponent<Mask>(viewport);
            scrollRectMask.showMaskGraphic = false;

            Image viewportImage = AddComponent<Image>(viewport);
            viewportImage.sprite = resources.mask;
            viewportImage.type = Image.Type.Sliced;

            TextMeshProUGUI labelText = AddComponent<TextMeshProUGUI>(label);
            SetDefaultTextValues(labelText);
            labelText.alignment = TextAlignmentOptions.Left;

            Image arrowImage = AddComponent<Image>(arrow);
            arrowImage.sprite = resources.dropdown;

            Image backgroundImage = AddComponent<Image>(root);
            backgroundImage.sprite = resources.standard;
            backgroundImage.color = s_DefaultSelectableColor;
            backgroundImage.type = Image.Type.Sliced;

            TMP_Dropdown dropdown = AddComponent<TMP_Dropdown>(root);
            dropdown.targetGraphic = backgroundImage;
            SetDefaultColorTransitionValues(dropdown);
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;

            itemLabelText.text = "Option A";
            dropdown.options.Add(new() {text = "Option A" });
            dropdown.options.Add(new() {text = "Option B" });
            dropdown.options.Add(new() {text = "Option C" });
            dropdown.RefreshShownValue();

            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new(10, 6);
            labelRT.offsetMax = new(-25, -7);

            RectTransform arrowRT = arrow.GetComponent<RectTransform>();
            arrowRT.anchorMin = new(1, 0.5f);
            arrowRT.anchorMax = new(1, 0.5f);
            arrowRT.sizeDelta = new(20, 20);
            arrowRT.anchoredPosition = new(-15, 0);

            RectTransform templateRT = template.GetComponent<RectTransform>();
            templateRT.anchorMin = new(0, 0);
            templateRT.anchorMax = new(1, 0);
            templateRT.pivot = new(0.5f, 1);
            templateRT.anchoredPosition = new(0, 2);
            templateRT.sizeDelta = new(0, 150);

            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = new(0, 0);
            viewportRT.anchorMax = new(1, 1);
            viewportRT.sizeDelta = new(-18, 0);
            viewportRT.pivot = new(0, 1);

            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new(0f, 1);
            contentRT.anchorMax = new(1f, 1);
            contentRT.pivot = new(0.5f, 1);
            contentRT.anchoredPosition = new(0, 0);
            contentRT.sizeDelta = new(0, 28);

            RectTransform itemRT = item.GetComponent<RectTransform>();
            itemRT.anchorMin = new(0, 0.5f);
            itemRT.anchorMax = new(1, 0.5f);
            itemRT.sizeDelta = new(0, 20);

            RectTransform itemBackgroundRT = itemBackground.GetComponent<RectTransform>();
            itemBackgroundRT.anchorMin = Vector2.zero;
            itemBackgroundRT.anchorMax = Vector2.one;
            itemBackgroundRT.sizeDelta = Vector2.zero;

            RectTransform itemCheckmarkRT = itemCheckmark.GetComponent<RectTransform>();
            itemCheckmarkRT.anchorMin = new(0, 0.5f);
            itemCheckmarkRT.anchorMax = new(0, 0.5f);
            itemCheckmarkRT.sizeDelta = new(20, 20);
            itemCheckmarkRT.anchoredPosition = new(10, 0);

            RectTransform itemLabelRT = itemLabel.GetComponent<RectTransform>();
            itemLabelRT.anchorMin = Vector2.zero;
            itemLabelRT.anchorMax = Vector2.one;
            itemLabelRT.offsetMin = new(20, 1);
            itemLabelRT.offsetMax = new(-10, -2);

            template.SetActive(false);

            return root;
        }

        private static T AddComponent<T>(GameObject go) where T : Component
        {
#if UNITY_EDITOR
            return ObjectFactory.AddComponent<T>(go);
#else
            return go.AddComponent<T>();
#endif
        }
    }
}
