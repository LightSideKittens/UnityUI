using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;


namespace TMPro.EditorUtilities
{
    public abstract class TMP_BaseEditorPanel : Editor
    {
        static readonly GUIContent k_RtlToggleLabel =
            new("Enable RTL Editor", "Reverses text direction and allows right to left editing.");

        static readonly GUIContent k_FontAssetLabel = new("Font Asset",
            "The Font Asset containing the glyphs that can be rendered for this text.");

        static readonly GUIContent k_MaterialPresetLabel = new("Material Preset",
            "The material used for rendering. Only materials created from the Font Asset can be used.");

        static readonly GUIContent k_StyleLabel =
            new("Text Style", "The style from a style sheet to be applied to the text.");

        static readonly GUIContent k_AutoSizeLabel =
            new("Auto Size", "Auto sizes the text to fit the available space.");

        static readonly GUIContent k_FontSizeLabel =
            new("Font Size", "The size the text will be rendered at in points.");

        static readonly GUIContent k_AutoSizeOptionsLabel = new("Auto Size Options");
        static readonly GUIContent k_MinLabel = new("Min", "The minimum font size.");
        static readonly GUIContent k_MaxLabel = new("Max", "The maximum font size.");

        static readonly GUIContent k_WdLabel = new("WD%",
            "Compresses character width up to this value before reducing font size.");

        static readonly GUIContent k_LineLabel = new("Line",
            "Negative value only. Compresses line height down to this value before reducing font size.");

        static readonly GUIContent k_FontStyleLabel =
            new("Font Style", "Styles to apply to the text such as Bold or Italic.");

        static readonly GUIContent k_BoldLabel = new("B", "Bold");
        static readonly GUIContent k_ItalicLabel = new("I", "Italic");
        static readonly GUIContent k_UnderlineLabel = new("U", "Underline");
        static readonly GUIContent k_StrikethroughLabel = new("S", "Strikethrough");
        static readonly GUIContent k_LowercaseLabel = new("ab", "Lowercase");
        static readonly GUIContent k_UppercaseLabel = new("AB", "Uppercase");
        static readonly GUIContent k_SmallcapsLabel = new("SC", "Smallcaps");

        static readonly GUIContent k_ColorModeLabel = new("Color Mode", "The type of gradient to use.");
        static readonly GUIContent k_BaseColorLabel = new("Vertex Color", "The base color of the text vertices.");

        static readonly GUIContent k_ColorPresetLabel =
            new("Color Preset", "A Color Preset which override the local color settings.");

        static readonly GUIContent k_ColorGradientLabel = new("Color Gradient",
            "The gradient color applied over the Vertex Color. Can be locally set or driven by a Gradient Asset.");

        static readonly GUIContent k_CorenerColorsLabel = new("Colors", "The color composition of the gradient.");

        static readonly GUIContent k_OverrideTagsLabel =
            new("Override Tags", "Whether the color settings override the <color> tag.");

        static readonly GUIContent k_SpacingOptionsLabel = new("Spacing Options (em)",
            "Spacing adjustments between different elements of the text. Values are in font units where a value of 1 equals 1/100em.");

        static readonly GUIContent k_CharacterSpacingLabel = new("Character");
        static readonly GUIContent k_WordSpacingLabel = new("Word");
        static readonly GUIContent k_LineSpacingLabel = new("Line");
        static readonly GUIContent k_ParagraphSpacingLabel = new("Paragraph");

        static readonly GUIContent k_AlignmentLabel =
            new("Alignment", "Horizontal and vertical alignment of the text within its container.");

        static readonly GUIContent k_WrapMixLabel = new("Wrap Mix (W <-> C)",
            "How much to favor words versus characters when distributing the text.");

        static readonly GUIContent k_WrappingLabel =
            new("Wrapping", "Wraps text to the next line when reaching the edge of the container.");

        static readonly GUIContent[] k_WrappingOptions = { new("Disabled"), new("Enabled") };

        static readonly GUIContent k_OverflowLabel =
            new("Overflow", "How to display text which goes past the edge of the container.");

        static readonly GUIContent k_MarginsLabel =
            new("Margins", "The space between the text and the edge of its container.");

        static readonly GUIContent k_GeometrySortingLabel = new("Geometry Sorting",
            "The order in which text geometry is sorted. Used to adjust the way overlapping characters are displayed.");

        static readonly GUIContent k_IsTextObjectScaleStatic = new("Is Scale Static",
            "Controls whether a text object will be excluded from the InteralUpdate callback to handle scale changes of the text object or its parent(s).");

        static readonly GUIContent k_RichTextLabel =
            new("Rich Text", "Enables the use of rich text tags such as <color> and <font>.");

        static readonly GUIContent k_EscapeCharactersLabel = new("Parse Escape Characters",
            "Whether to display strings such as \"\\n\" as is or replace them by the character they represent.");

        static readonly GUIContent k_VisibleDescenderLabel = new("Visible Descender",
            "Compute descender values from visible characters only. Used to adjust layout behavior when hiding and revealing characters dynamically.");

        static readonly GUIContent k_SpriteAssetLabel = new("Sprite Asset",
            "The Sprite Asset used when NOT specifically referencing one using <sprite=\"Sprite Asset Name\">.");

        static readonly GUIContent k_StyleSheetAssetLabel =
            new("Style Sheet Asset", "The Style Sheet Asset used by this text object.");

        static readonly GUIContent k_HorizontalMappingLabel = new("Horizontal Mapping",
            "Horizontal UV mapping when using a shader with a texture face option.");

        static readonly GUIContent k_VerticalMappingLabel = new("Vertical Mapping",
            "Vertical UV mapping when using a shader with a texture face option.");

        static readonly GUIContent k_LineOffsetLabel = new("Line Offset",
            "Adds an horizontal offset to each successive line. Used for slanted texturing.");

        static readonly GUIContent k_FontFeaturesLabel = new("Font Features",
            "Font features available for the primary font asset assigned to the text component.");

        static readonly GUIContent k_PaddingLabel = new("Extra Padding",
            "Adds some padding between the characters and the edge of the text mesh. Can reduce graphical errors when displaying small text.");

        protected static string[] k_UiStateLabel = { "<i>(Click to collapse)</i> ", "<i>(Click to expand)</i> " };

        static Dictionary<int, TMP_Style> k_AvailableStyles = new();
        protected Dictionary<int, int> m_TextStyleIndexLookup = new();

        protected struct Foldout
        {
            public static bool extraSettings = false;
            public static bool materialInspector = true;
        }

        protected static int s_EventId;

        protected SerializedProperty m_TextProp;

        protected SerializedProperty m_FontAssetProp;

        protected SerializedProperty m_FontSharedMaterialProp;
        protected Material[] m_MaterialPresets;
        protected GUIContent[] m_MaterialPresetNames;
        protected Dictionary<int, int> m_MaterialPresetIndexLookup = new();
        protected int m_MaterialPresetSelectionIndex;

        protected List<TMP_Style> m_Styles = new();
        protected GUIContent[] m_StyleNames;
        protected int m_StyleSelectionIndex;

        protected SerializedProperty m_FontStyleProp;

        protected SerializedProperty m_FontColorProp;
        protected SerializedProperty m_EnableVertexGradientProp;
        protected SerializedProperty m_FontColorGradientProp;
        protected SerializedProperty m_FontColorGradientPresetProp;
        protected SerializedProperty m_OverrideHtmlColorProp;

        protected SerializedProperty m_FontSizeProp;
        protected SerializedProperty m_FontSizeBaseProp;

        protected SerializedProperty m_AutoSizingProp;
        protected SerializedProperty m_FontSizeMinProp;
        protected SerializedProperty m_FontSizeMaxProp;

        protected SerializedProperty m_LineSpacingMaxProp;
        protected SerializedProperty m_CharWidthMaxAdjProp;

        protected SerializedProperty m_CharacterSpacingProp;
        protected SerializedProperty m_WordSpacingProp;
        protected SerializedProperty m_LineSpacingProp;
        protected SerializedProperty m_ParagraphSpacingProp;

        protected SerializedProperty m_HorizontalAlignmentProp;
        protected SerializedProperty autoHorizontalAlignment;
        protected SerializedProperty m_VerticalAlignmentProp;

        protected SerializedProperty m_HorizontalMappingProp;
        protected SerializedProperty m_VerticalMappingProp;
        protected SerializedProperty m_UvLineOffsetProp;

        protected SerializedProperty m_TextWrappingModeProp;
        protected SerializedProperty m_WordWrappingRatiosProp;
        protected SerializedProperty m_TextOverflowModeProp;

        protected SerializedProperty m_FontFeaturesActiveProp;

        protected SerializedProperty m_IsRichTextProp;

        protected SerializedProperty m_HasFontAssetChangedProp;

        protected SerializedProperty m_EnableExtraPaddingProp;
        protected SerializedProperty m_CheckPaddingRequiredProp;
        protected SerializedProperty m_EnableEscapeCharacterParsingProp;
        protected SerializedProperty m_UseMaxVisibleDescenderProp;
        protected SerializedProperty m_GeometrySortingOrderProp;

        protected SerializedProperty m_StyleSheetAssetProp;
        protected SerializedProperty m_TextStyleHashCodeProp;

        protected SerializedProperty m_MarginProp;

        protected SerializedProperty m_ColorModeProp;

        protected bool m_HavePropertiesChanged;

        protected TMP_Text m_TextComponent;
        protected RectTransform m_RectTransform;

        protected Material m_TargetMaterial;

        protected Vector3[] m_RectCorners = new Vector3[4];
        protected Vector3[] m_HandlePoints = new Vector3[4];
        private int fs = 12;

        private static readonly string[] k_FontFeatures = { "kern", "liga", "mark", "mkmk" };

        protected virtual void OnEnable()
        {
            m_TextProp = serializedObject.FindProperty("m_text");
            m_FontAssetProp = serializedObject.FindProperty("m_fontAsset");
            m_FontSharedMaterialProp = serializedObject.FindProperty("m_sharedMaterial");

            m_FontStyleProp = serializedObject.FindProperty("m_fontStyle");

            m_FontSizeProp = serializedObject.FindProperty("m_fontSize");
            m_FontSizeBaseProp = serializedObject.FindProperty("m_fontSizeBase");

            m_AutoSizingProp = serializedObject.FindProperty("m_enableAutoSizing");
            m_FontSizeMinProp = serializedObject.FindProperty("m_fontSizeMin");
            m_FontSizeMaxProp = serializedObject.FindProperty("m_fontSizeMax");

            m_LineSpacingMaxProp = serializedObject.FindProperty("m_lineSpacingMax");
            m_CharWidthMaxAdjProp = serializedObject.FindProperty("m_charWidthMaxAdj");

            m_FontColorProp = serializedObject.FindProperty("m_fontColor");
            m_EnableVertexGradientProp = serializedObject.FindProperty("m_enableVertexGradient");
            m_FontColorGradientProp = serializedObject.FindProperty("m_fontColorGradient");
            m_FontColorGradientPresetProp = serializedObject.FindProperty("m_fontColorGradientPreset");
            m_OverrideHtmlColorProp = serializedObject.FindProperty("m_overrideHtmlColors");

            m_CharacterSpacingProp = serializedObject.FindProperty("m_characterSpacing");
            m_WordSpacingProp = serializedObject.FindProperty("m_wordSpacing");
            m_LineSpacingProp = serializedObject.FindProperty("m_lineSpacing");
            m_ParagraphSpacingProp = serializedObject.FindProperty("m_paragraphSpacing");

            m_HorizontalAlignmentProp = serializedObject.FindProperty("m_HorizontalAlignment");
            autoHorizontalAlignment = serializedObject.FindProperty("autoHorizontalAlignment");
            m_VerticalAlignmentProp = serializedObject.FindProperty("m_VerticalAlignment");

            m_HorizontalMappingProp = serializedObject.FindProperty("m_horizontalMapping");
            m_VerticalMappingProp = serializedObject.FindProperty("m_verticalMapping");
            m_UvLineOffsetProp = serializedObject.FindProperty("m_uvLineOffset");

            m_TextWrappingModeProp = serializedObject.FindProperty("m_TextWrappingMode");
            m_WordWrappingRatiosProp = serializedObject.FindProperty("m_wordWrappingRatios");
            m_TextOverflowModeProp = serializedObject.FindProperty("m_overflowMode");

            m_FontFeaturesActiveProp = serializedObject.FindProperty("m_ActiveFontFeatures");

            m_EnableExtraPaddingProp = serializedObject.FindProperty("m_enableExtraPadding");
            m_IsRichTextProp = serializedObject.FindProperty("m_isRichText");
            m_CheckPaddingRequiredProp = serializedObject.FindProperty("checkPaddingRequired");
            m_EnableEscapeCharacterParsingProp = serializedObject.FindProperty("m_parseCtrlCharacters");
            m_UseMaxVisibleDescenderProp = serializedObject.FindProperty("m_useMaxVisibleDescender");

            m_GeometrySortingOrderProp = serializedObject.FindProperty("m_geometrySortingOrder");

            m_StyleSheetAssetProp = serializedObject.FindProperty("m_StyleSheet");
            m_TextStyleHashCodeProp = serializedObject.FindProperty("m_TextStyleHashCode");

            m_MarginProp = serializedObject.FindProperty("m_margin");

            m_HasFontAssetChangedProp = serializedObject.FindProperty("m_hasFontAssetChanged");

            m_ColorModeProp = serializedObject.FindProperty("m_colorMode");

            m_TextComponent = (TMP_Text)target;
            m_RectTransform = m_TextComponent.rectTransform;

            m_TargetMaterial = m_TextComponent.fontSharedMaterial;

            if (m_TargetMaterial != null)
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(m_TargetMaterial,
                    Foldout.materialInspector);

            m_MaterialPresetNames = GetMaterialPresets();

            if (TMP_Settings.instance != null)
                m_StyleNames = GetStyleNames();

            TMPro_EventManager.TEXT_STYLE_PROPERTY_EVENT.Add(ON_TEXT_STYLE_CHANGED);

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        protected virtual void OnDisable()
        {
            if (m_TargetMaterial != null)
                Foldout.materialInspector =
                    UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(m_TargetMaterial);

            if (Undo.undoRedoPerformed != null)
                Undo.undoRedoPerformed -= OnUndoRedo;

            TMPro_EventManager.TEXT_STYLE_PROPERTY_EVENT.Remove(ON_TEXT_STYLE_CHANGED);
        }

        void ON_TEXT_STYLE_CHANGED(bool isChanged)
        {
            m_StyleNames = GetStyleNames();
        }

        public override void OnInspectorGUI()
        {
            if (IsMixSelectionTypes()) return;

            serializedObject.Update();

            DrawTextInput();

            DrawMainSettings();

            DrawExtraSettings();

            EditorGUILayout.Space();

            if (serializedObject.ApplyModifiedProperties() || m_HavePropertiesChanged)
            {
                m_TextComponent.havePropertiesChanged = true;
                m_HavePropertiesChanged = false;
            }
        }

        public void OnSceneGUI()
        {
            if (IsMixSelectionTypes()) return;

            m_RectTransform.GetWorldCorners(m_RectCorners);
            Vector4 marginOffset = m_TextComponent.margin;
            Vector3 lossyScale = m_RectTransform.lossyScale;

            m_HandlePoints[0] = m_RectCorners[0] +
                                m_RectTransform.TransformDirection(new Vector3(marginOffset.x * lossyScale.x,
                                    marginOffset.w * lossyScale.y, 0));
            m_HandlePoints[1] = m_RectCorners[1] +
                                m_RectTransform.TransformDirection(new Vector3(marginOffset.x * lossyScale.x,
                                    -marginOffset.y * lossyScale.y, 0));
            m_HandlePoints[2] = m_RectCorners[2] +
                                m_RectTransform.TransformDirection(new Vector3(-marginOffset.z * lossyScale.x,
                                    -marginOffset.y * lossyScale.y, 0));
            m_HandlePoints[3] = m_RectCorners[3] +
                                m_RectTransform.TransformDirection(new Vector3(-marginOffset.z * lossyScale.x,
                                    marginOffset.w * lossyScale.y, 0));

            Handles.DrawSolidRectangleWithOutline(m_HandlePoints, new Color32(255, 255, 255, 0),
                new Color32(255, 255, 0, 255));

            Matrix4x4 matrix = m_RectTransform.worldToLocalMatrix;

            Vector3 oldLeft = (m_HandlePoints[0] + m_HandlePoints[1]) * 0.5f;
#if UNITY_2022_1_OR_NEWER
            Vector3 newLeft = Handles.FreeMoveHandle(oldLeft,
                HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#else
            Vector3 newLeft =
 Handles.FreeMoveHandle(oldLeft, Quaternion.identity, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#endif
            bool hasChanged = false;
            if (oldLeft != newLeft)
            {
                oldLeft = matrix.MultiplyPoint(oldLeft);
                newLeft = matrix.MultiplyPoint(newLeft);

                float delta = (oldLeft.x - newLeft.x) * lossyScale.x;
                marginOffset.x += -delta / lossyScale.x;
                hasChanged = true;
            }

            Vector3 oldTop = (m_HandlePoints[1] + m_HandlePoints[2]) * 0.5f;
#if UNITY_2022_1_OR_NEWER
            Vector3 newTop = Handles.FreeMoveHandle(oldTop,
                HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#else
            Vector3 newTop =
 Handles.FreeMoveHandle(oldTop, Quaternion.identity, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#endif
            if (oldTop != newTop)
            {
                oldTop = matrix.MultiplyPoint(oldTop);
                newTop = matrix.MultiplyPoint(newTop);

                float delta = (oldTop.y - newTop.y) * lossyScale.y;
                marginOffset.y += delta / lossyScale.y;
                hasChanged = true;
            }

            Vector3 oldRight = (m_HandlePoints[2] + m_HandlePoints[3]) * 0.5f;
#if UNITY_2022_1_OR_NEWER
            Vector3 newRight = Handles.FreeMoveHandle(oldRight,
                HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#else
            Vector3 newRight =
 Handles.FreeMoveHandle(oldRight, Quaternion.identity, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#endif
            if (oldRight != newRight)
            {
                oldRight = matrix.MultiplyPoint(oldRight);
                newRight = matrix.MultiplyPoint(newRight);

                float delta = (oldRight.x - newRight.x) * lossyScale.x;
                marginOffset.z += delta / lossyScale.x;
                hasChanged = true;
            }

            Vector3 oldBottom = (m_HandlePoints[3] + m_HandlePoints[0]) * 0.5f;
#if UNITY_2022_1_OR_NEWER
            Vector3 newBottom = Handles.FreeMoveHandle(oldBottom,
                HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#else
            Vector3 newBottom =
 Handles.FreeMoveHandle(oldBottom, Quaternion.identity, HandleUtility.GetHandleSize(m_RectTransform.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
#endif
            if (oldBottom != newBottom)
            {
                oldBottom = matrix.MultiplyPoint(oldBottom);
                newBottom = matrix.MultiplyPoint(newBottom);

                float delta = (oldBottom.y - newBottom.y) * lossyScale.y;
                marginOffset.w += -delta / lossyScale.y;
                hasChanged = true;
            }

            if (hasChanged)
            {
                Undo.RecordObjects(new Object[] { m_RectTransform, m_TextComponent }, "Margin Changes");
                m_TextComponent.margin = marginOffset;
                EditorUtility.SetDirty(target);
            }
        }

        protected void DrawTextInput()
        {
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, 22);
            GUI.Label(rect, new GUIContent("<b>Text Input</b>"), TMP_UIStyleManager.sectionHeader);

            var oldfs = EditorStyles.textArea.fontSize;
            fs = EditorGUILayout.IntSlider("Text Area Font Size (Editor)", fs, 12, 32);

            EditorGUI.indentLevel = 0;

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 110f;

            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUI.BeginChangeCheck();
            EditorStyles.textArea.fontSize = fs;
            EditorGUILayout.PropertyField(m_TextProp, GUIContent.none);
            EditorStyles.textArea.fontSize = oldfs;

            if (EditorGUI.EndChangeCheck() && m_TextProp.stringValue != m_TextComponent.text)
            {
                m_HavePropertiesChanged = true;
            }

            if (m_TextComponent.textPreprocessor != null)
            {
                GUILayout.Label("Preprocessed Text (Text Preprocessor)");
                EditorGUILayout.TextArea(m_TextComponent.PreprocessedText, TMP_UIStyleManager.wrappingTextArea,
                    GUILayout.Height(EditorGUI.GetPropertyHeight(m_TextProp) - EditorGUIUtility.singleLineHeight),
                    GUILayout.ExpandWidth(true));
            }

            if (m_StyleNames != null)
            {
                rect = EditorGUILayout.GetControlRect(false, 17);

                EditorGUI.BeginProperty(rect, k_StyleLabel, m_TextStyleHashCodeProp);

                m_TextStyleIndexLookup.TryGetValue(m_TextStyleHashCodeProp.intValue, out m_StyleSelectionIndex);

                EditorGUI.BeginChangeCheck();
                m_StyleSelectionIndex = EditorGUI.Popup(rect, k_StyleLabel, m_StyleSelectionIndex, m_StyleNames);
                if (EditorGUI.EndChangeCheck())
                {
                    m_TextStyleHashCodeProp.intValue = m_Styles[m_StyleSelectionIndex].hashCode;
                    m_TextComponent.m_TextStyle = m_Styles[m_StyleSelectionIndex];
                    m_HavePropertiesChanged = true;
                }

                EditorGUI.EndProperty();
            }
        }

        protected void DrawMainSettings()
        {
            GUILayout.Label(new GUIContent("<b>Main Settings</b>"), TMP_UIStyleManager.sectionHeader);

            DrawFont();

            DrawColor();

            DrawSpacing();

            DrawAlignment();

            DrawWrappingOverflow();

            DrawTextureMapping();

            m_TextComponent.renderMode =
                (TextRenderFlags)EditorGUILayout.EnumPopup("Render Mode", m_TextComponent.renderMode);
        }

        void DrawFont()
        {
            bool isFontAssetDirty = false;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_FontAssetProp, k_FontAssetLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
                m_HasFontAssetChangedProp.boolValue = true;

                m_MaterialPresetNames = GetMaterialPresets();
                m_MaterialPresetSelectionIndex = 0;

                isFontAssetDirty = true;
            }

            Rect rect;

            if (m_MaterialPresetNames != null && !isFontAssetDirty)
            {
                EditorGUI.BeginChangeCheck();
                rect = EditorGUILayout.GetControlRect(false, 17);

                EditorGUI.BeginProperty(rect, k_MaterialPresetLabel, m_FontSharedMaterialProp);

                float oldHeight = EditorStyles.popup.fixedHeight;
                EditorStyles.popup.fixedHeight = rect.height;

                int oldSize = EditorStyles.popup.fontSize;
                EditorStyles.popup.fontSize = 11;

                if (m_FontSharedMaterialProp.objectReferenceValue != null)
                    m_MaterialPresetIndexLookup.TryGetValue(
                        m_FontSharedMaterialProp.objectReferenceValue.GetInstanceID(),
                        out m_MaterialPresetSelectionIndex);

                m_MaterialPresetSelectionIndex = EditorGUI.Popup(rect, k_MaterialPresetLabel,
                    m_MaterialPresetSelectionIndex, m_MaterialPresetNames);

                EditorGUI.EndProperty();

                if (EditorGUI.EndChangeCheck())
                {
                    m_FontSharedMaterialProp.objectReferenceValue = m_MaterialPresets[m_MaterialPresetSelectionIndex];
                    m_HavePropertiesChanged = true;
                }

                EditorStyles.popup.fixedHeight = oldHeight;
                EditorStyles.popup.fontSize = oldSize;
            }

            EditorGUI.BeginChangeCheck();

            int v1, v2, v3, v4, v5, v6, v7;

            if (EditorGUIUtility.wideMode)
            {
                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                EditorGUI.BeginProperty(rect, k_FontStyleLabel, m_FontStyleProp);

                EditorGUI.PrefixLabel(rect, k_FontStyleLabel);

                int styleValue = m_FontStyleProp.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 7f);

                v1 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 1) == 1, k_BoldLabel,
                    TMP_UIStyleManager.alignmentButtonLeft)
                    ? 1
                    : 0;
                rect.x += rect.width;
                v2 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 2) == 2, k_ItalicLabel,
                    TMP_UIStyleManager.alignmentButtonMid)
                    ? 2
                    : 0;
                rect.x += rect.width;
                v3 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 4) == 4, k_UnderlineLabel,
                    TMP_UIStyleManager.alignmentButtonMid)
                    ? 4
                    : 0;
                rect.x += rect.width;
                v7 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 64) == 64, k_StrikethroughLabel,
                    TMP_UIStyleManager.alignmentButtonRight)
                    ? 64
                    : 0;
                rect.x += rect.width;

                int selected = 0;

                EditorGUI.BeginChangeCheck();
                v4 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 8) == 8, k_LowercaseLabel,
                    TMP_UIStyleManager.alignmentButtonLeft)
                    ? 8
                    : 0;
                if (EditorGUI.EndChangeCheck() && v4 > 0)
                {
                    selected = v4;
                }

                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v5 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 16) == 16, k_UppercaseLabel,
                    TMP_UIStyleManager.alignmentButtonMid)
                    ? 16
                    : 0;
                if (EditorGUI.EndChangeCheck() && v5 > 0)
                {
                    selected = v5;
                }

                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v6 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 32) == 32, k_SmallcapsLabel,
                    TMP_UIStyleManager.alignmentButtonRight)
                    ? 32
                    : 0;
                if (EditorGUI.EndChangeCheck() && v6 > 0)
                {
                    selected = v6;
                }

                if (selected > 0)
                {
                    v4 = selected == 8 ? 8 : 0;
                    v5 = selected == 16 ? 16 : 0;
                    v6 = selected == 32 ? 32 : 0;
                }

                EditorGUI.EndProperty();
            }
            else
            {
                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                EditorGUI.BeginProperty(rect, k_FontStyleLabel, m_FontStyleProp);

                EditorGUI.PrefixLabel(rect, k_FontStyleLabel);

                int styleValue = m_FontStyleProp.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                rect.width = Mathf.Max(25f, rect.width / 4f);

                v1 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 1) == 1, k_BoldLabel,
                    TMP_UIStyleManager.alignmentButtonLeft)
                    ? 1
                    : 0;
                rect.x += rect.width;
                v2 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 2) == 2, k_ItalicLabel,
                    TMP_UIStyleManager.alignmentButtonMid)
                    ? 2
                    : 0;
                rect.x += rect.width;
                v3 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 4) == 4, k_UnderlineLabel,
                    TMP_UIStyleManager.alignmentButtonMid)
                    ? 4
                    : 0;
                rect.x += rect.width;
                v7 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 64) == 64, k_StrikethroughLabel,
                    TMP_UIStyleManager.alignmentButtonRight)
                    ? 64
                    : 0;

                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 4f);

                int selected = 0;

                EditorGUI.BeginChangeCheck();
                v4 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 8) == 8, k_LowercaseLabel,
                    TMP_UIStyleManager.alignmentButtonLeft)
                    ? 8
                    : 0;
                if (EditorGUI.EndChangeCheck() && v4 > 0)
                {
                    selected = v4;
                }

                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v5 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 16) == 16, k_UppercaseLabel,
                    TMP_UIStyleManager.alignmentButtonMid)
                    ? 16
                    : 0;
                if (EditorGUI.EndChangeCheck() && v5 > 0)
                {
                    selected = v5;
                }

                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v6 = TMP_EditorUtility.EditorToggle(rect, (styleValue & 32) == 32, k_SmallcapsLabel,
                    TMP_UIStyleManager.alignmentButtonRight)
                    ? 32
                    : 0;
                if (EditorGUI.EndChangeCheck() && v6 > 0)
                {
                    selected = v6;
                }

                if (selected > 0)
                {
                    v4 = selected == 8 ? 8 : 0;
                    v5 = selected == 16 ? 16 : 0;
                    v6 = selected == 32 ? 32 : 0;
                }

                EditorGUI.EndProperty();
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_FontStyleProp.intValue = v1 + v2 + v3 + v4 + v5 + v6 + v7;
                m_HavePropertiesChanged = true;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(m_AutoSizingProp.boolValue);
            EditorGUILayout.PropertyField(m_FontSizeProp, k_FontSizeLabel,
                GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 50f));
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                float fontSize = Mathf.Clamp(m_FontSizeProp.floatValue, 0, 32767);

                m_FontSizeProp.floatValue = fontSize;
                m_FontSizeBaseProp.floatValue = fontSize;
                m_HavePropertiesChanged = true;
            }

            EditorGUI.indentLevel += 1;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AutoSizingProp, k_AutoSizeLabel);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_AutoSizingProp.boolValue == false)
                    m_FontSizeProp.floatValue = m_FontSizeBaseProp.floatValue;

                m_HavePropertiesChanged = true;
            }

            if (m_AutoSizingProp.boolValue)
            {
                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

                EditorGUI.PrefixLabel(rect, k_AutoSizeOptionsLabel);

                int previousIndent = EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                rect.width = (rect.width - EditorGUIUtility.labelWidth) / 4f;
                rect.x += EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 24;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, m_FontSizeMinProp, k_MinLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    float minSize = m_FontSizeMinProp.floatValue;

                    minSize = Mathf.Max(0, minSize);

                    m_FontSizeMinProp.floatValue = Mathf.Min(minSize, m_FontSizeMaxProp.floatValue);
                    m_HavePropertiesChanged = true;
                }

                rect.x += rect.width;

                EditorGUIUtility.labelWidth = 27;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, m_FontSizeMaxProp, k_MaxLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    float maxSize = Mathf.Clamp(m_FontSizeMaxProp.floatValue, 0, 32767);

                    m_FontSizeMaxProp.floatValue = Mathf.Max(m_FontSizeMinProp.floatValue, maxSize);
                    m_HavePropertiesChanged = true;
                }

                rect.x += rect.width;

                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = 36;
                EditorGUI.PropertyField(rect, m_CharWidthMaxAdjProp, k_WdLabel);
                rect.x += rect.width;
                EditorGUIUtility.labelWidth = 28;
                EditorGUI.PropertyField(rect, m_LineSpacingMaxProp, k_LineLabel);

                EditorGUIUtility.labelWidth = 0;

                if (EditorGUI.EndChangeCheck())
                {
                    m_CharWidthMaxAdjProp.floatValue = Mathf.Clamp(m_CharWidthMaxAdjProp.floatValue, 0, 50);
                    m_LineSpacingMaxProp.floatValue = Mathf.Min(0, m_LineSpacingMaxProp.floatValue);
                    m_HavePropertiesChanged = true;
                }

                EditorGUI.indentLevel = previousIndent;
            }

            EditorGUI.indentLevel -= 1;


            EditorGUILayout.Space();
        }

        void DrawColor()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_FontColorProp, k_BaseColorLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnableVertexGradientProp, k_ColorGradientLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUIUtility.fieldWidth = 0;

            if (m_EnableVertexGradientProp.boolValue)
            {
                EditorGUI.indentLevel += 1;

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(m_FontColorGradientPresetProp, k_ColorPresetLabel);

                SerializedObject obj = null;

                SerializedProperty colorMode;

                SerializedProperty topLeft;
                SerializedProperty topRight;
                SerializedProperty bottomLeft;
                SerializedProperty bottomRight;

                if (m_FontColorGradientPresetProp.objectReferenceValue == null)
                {
                    colorMode = m_ColorModeProp;
                    topLeft = m_FontColorGradientProp.FindPropertyRelative("topLeft");
                    topRight = m_FontColorGradientProp.FindPropertyRelative("topRight");
                    bottomLeft = m_FontColorGradientProp.FindPropertyRelative("bottomLeft");
                    bottomRight = m_FontColorGradientProp.FindPropertyRelative("bottomRight");
                }
                else
                {
                    obj = new SerializedObject(m_FontColorGradientPresetProp.objectReferenceValue);
                    colorMode = obj.FindProperty("colorMode");
                    topLeft = obj.FindProperty("topLeft");
                    topRight = obj.FindProperty("topRight");
                    bottomLeft = obj.FindProperty("bottomLeft");
                    bottomRight = obj.FindProperty("bottomRight");
                }

                EditorGUILayout.PropertyField(colorMode, k_ColorModeLabel);

                Rect rect = EditorGUILayout.GetControlRect(true,
                    EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));

                EditorGUI.PrefixLabel(rect, k_CorenerColorsLabel);

                rect.x += EditorGUIUtility.labelWidth;
                rect.width = rect.width - EditorGUIUtility.labelWidth;

                switch ((ColorMode)colorMode.enumValueIndex)
                {
                    case ColorMode.Single:
                        TMP_EditorUtility.DrawColorProperty(rect, topLeft);

                        topRight.colorValue = topLeft.colorValue;
                        bottomLeft.colorValue = topLeft.colorValue;
                        bottomRight.colorValue = topLeft.colorValue;
                        break;
                    case ColorMode.HorizontalGradient:
                        rect.width /= 2f;

                        TMP_EditorUtility.DrawColorProperty(rect, topLeft);

                        rect.x += rect.width;

                        TMP_EditorUtility.DrawColorProperty(rect, topRight);

                        bottomLeft.colorValue = topLeft.colorValue;
                        bottomRight.colorValue = topRight.colorValue;
                        break;
                    case ColorMode.VerticalGradient:
                        TMP_EditorUtility.DrawColorProperty(rect, topLeft);

                        rect = EditorGUILayout.GetControlRect(false,
                            EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                        rect.x += EditorGUIUtility.labelWidth;

                        TMP_EditorUtility.DrawColorProperty(rect, bottomLeft);

                        topRight.colorValue = topLeft.colorValue;
                        bottomRight.colorValue = bottomLeft.colorValue;
                        break;
                    case ColorMode.FourCornersGradient:
                        rect.width /= 2f;

                        TMP_EditorUtility.DrawColorProperty(rect, topLeft);

                        rect.x += rect.width;

                        TMP_EditorUtility.DrawColorProperty(rect, topRight);

                        rect = EditorGUILayout.GetControlRect(false,
                            EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                        rect.x += EditorGUIUtility.labelWidth;
                        rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2f;

                        TMP_EditorUtility.DrawColorProperty(rect, bottomLeft);

                        rect.x += rect.width;

                        TMP_EditorUtility.DrawColorProperty(rect, bottomRight);
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    m_HavePropertiesChanged = true;
                    if (obj != null)
                    {
                        obj.ApplyModifiedProperties();
                        TMPro_EventManager.ON_COLOR_GRADIENT_PROPERTY_CHANGED(
                            m_FontColorGradientPresetProp.objectReferenceValue as TMP_ColorGradient);
                    }
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(m_OverrideHtmlColorProp, k_OverrideTagsLabel);

            EditorGUILayout.Space();
        }

        void DrawSpacing()
        {
            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            EditorGUI.PrefixLabel(rect, k_SpacingOptionsLabel);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float currentLabelWidth = EditorGUIUtility.labelWidth;
            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth - 3f) / 2f;

            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, m_CharacterSpacingProp, k_CharacterSpacingLabel);
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, m_WordSpacingProp, k_WordSpacingLabel);

            rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth - 3f) / 2f;
            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, m_LineSpacingProp, k_LineSpacingLabel);
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, m_ParagraphSpacingProp, k_ParagraphSpacingLabel);

            EditorGUIUtility.labelWidth = currentLabelWidth;
            EditorGUI.indentLevel = oldIndent;

            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        void DrawAlignment()
        {
            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.currentViewWidth > 504 ? 20 : 40 + 3);
            EditorGUI.BeginProperty(rect, k_AlignmentLabel, m_HorizontalAlignmentProp);
            EditorGUI.BeginProperty(rect, k_AlignmentLabel, m_VerticalAlignmentProp);

            EditorGUI.PrefixLabel(rect, k_AlignmentLabel);
            rect.x += EditorGUIUtility.labelWidth;

            EditorGUI.PropertyField(rect, m_HorizontalAlignmentProp, GUIContent.none);
            EditorGUI.PropertyField(rect, m_VerticalAlignmentProp, GUIContent.none);

            if (((HorizontalAlignmentOptions)m_HorizontalAlignmentProp.intValue &
                 HorizontalAlignmentOptions.Justified) == HorizontalAlignmentOptions.Justified ||
                ((HorizontalAlignmentOptions)m_HorizontalAlignmentProp.intValue & HorizontalAlignmentOptions.Flush) ==
                HorizontalAlignmentOptions.Flush)
                DrawPropertySlider(k_WrapMixLabel, m_WordWrappingRatiosProp);

            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;

            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            EditorGUILayout.PropertyField(autoHorizontalAlignment);

            EditorGUILayout.Space();
        }

        void DrawWrappingOverflow()
        {
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, k_WrappingLabel, m_TextWrappingModeProp);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, m_TextWrappingModeProp);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUI.EndProperty();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_TextOverflowModeProp, k_OverflowLabel);

            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        protected abstract void DrawExtraSettings();

        protected void DrawMargins()
        {
            EditorGUI.BeginChangeCheck();
            DrawMarginProperty(m_MarginProp, k_MarginsLabel);
            if (EditorGUI.EndChangeCheck())
            {
                Vector4 margins = m_MarginProp.vector4Value;
                Rect textContainerSize = m_RectTransform.rect;

                margins.x = Mathf.Clamp(margins.x, -textContainerSize.width, textContainerSize.width);
                margins.z = Mathf.Clamp(margins.z, -textContainerSize.width, textContainerSize.width);

                margins.y = Mathf.Clamp(margins.y, -textContainerSize.height, textContainerSize.height);
                margins.w = Mathf.Clamp(margins.w, -textContainerSize.height, textContainerSize.height);

                m_MarginProp.vector4Value = margins;

                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        protected void DrawGeometrySorting()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_GeometrySortingOrderProp, k_GeometrySortingLabel);

            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;

            EditorGUILayout.Space();
        }

        protected void DrawRichText()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_IsRichTextProp, k_RichTextLabel);
            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;
        }

        protected void DrawParsing()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnableEscapeCharacterParsingProp, k_EscapeCharactersLabel);
            EditorGUILayout.PropertyField(m_UseMaxVisibleDescenderProp, k_VisibleDescenderLabel);

            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;

            EditorGUILayout.Space();
        }

        protected void DrawStyleSheet()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_StyleSheetAssetProp, k_StyleSheetAssetLabel, true);

            if (EditorGUI.EndChangeCheck())
            {
                m_StyleNames = GetStyleNames();
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }

        protected void DrawTextureMapping()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_HorizontalMappingProp, k_HorizontalMappingLabel);
            EditorGUILayout.PropertyField(m_VerticalMappingProp, k_VerticalMappingLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            if (m_HorizontalMappingProp.enumValueIndex > 0)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_UvLineOffsetProp, k_LineOffsetLabel, GUILayout.MinWidth(70f));
                if (EditorGUI.EndChangeCheck())
                {
                    m_HavePropertiesChanged = true;
                }
            }

            EditorGUILayout.Space();
        }

        protected void DrawFontFeatures()
        {
            int srcMask = 0;

            int featureCount = m_FontFeaturesActiveProp.arraySize;
            for (int i = 0; i < featureCount; i++)
            {
                SerializedProperty activeFeatureProperty = m_FontFeaturesActiveProp.GetArrayElementAtIndex(i);

                for (int j = 0; j < k_FontFeatures.Length; j++)
                {
                    if (activeFeatureProperty.intValue == k_FontFeatures[j].TagToInt())
                    {
                        srcMask |= 0x1 << j;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();

            int mask = EditorGUILayout.MaskField(k_FontFeaturesLabel, srcMask, k_FontFeatures);

            if (EditorGUI.EndChangeCheck())
            {
                m_FontFeaturesActiveProp.ClearArray();

                int writeIndex = 0;

                for (int i = 0; i < k_FontFeatures.Length; i++)
                {
                    int bit = 0x1 << i;
                    if ((mask & bit) == bit)
                    {
                        m_FontFeaturesActiveProp.InsertArrayElementAtIndex(writeIndex);
                        SerializedProperty newFeature = m_FontFeaturesActiveProp.GetArrayElementAtIndex(writeIndex);
                        newFeature.intValue = k_FontFeatures[i].TagToInt();

                        writeIndex += 1;
                    }
                }

                m_HavePropertiesChanged = true;
            }
        }

        protected void DrawPadding()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnableExtraPaddingProp, k_PaddingLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
                m_CheckPaddingRequiredProp.boolValue = true;
            }
        }

        protected GUIContent[] GetMaterialPresets()
        {
            TMP_FontAsset fontAsset = m_FontAssetProp.objectReferenceValue as TMP_FontAsset;
            if (fontAsset == null) return null;

            m_MaterialPresets = TMP_EditorUtility.FindMaterialReferences(fontAsset);
            m_MaterialPresetNames = new GUIContent[m_MaterialPresets.Length];

            m_MaterialPresetIndexLookup.Clear();

            for (int i = 0; i < m_MaterialPresetNames.Length; i++)
            {
                m_MaterialPresetNames[i] = new GUIContent(m_MaterialPresets[i].name);

                m_MaterialPresetIndexLookup.Add(m_MaterialPresets[i].GetInstanceID(), i);
            }

            return m_MaterialPresetNames;
        }

        protected GUIContent[] GetStyleNames()
        {
            k_AvailableStyles.Clear();
            m_TextStyleIndexLookup.Clear();
            m_Styles.Clear();

            TMP_Style styleNormal = TMP_Style.NormalStyle;

            m_Styles.Add(styleNormal);
            m_TextStyleIndexLookup.Add(styleNormal.hashCode, 0);

            k_AvailableStyles.Add(styleNormal.hashCode, styleNormal);

            TMP_StyleSheet localStyleSheet = (TMP_StyleSheet)m_StyleSheetAssetProp.objectReferenceValue;

            if (localStyleSheet != null)
            {
                int styleCount = localStyleSheet.styles.Count;

                for (int i = 0; i < styleCount; i++)
                {
                    TMP_Style style = localStyleSheet.styles[i];

                    if (k_AvailableStyles.ContainsKey(style.hashCode) == false)
                    {
                        k_AvailableStyles.Add(style.hashCode, style);
                        m_Styles.Add(style);
                        m_TextStyleIndexLookup.Add(style.hashCode, m_TextStyleIndexLookup.Count);
                    }
                }
            }

            TMP_StyleSheet globalStyleSheet = TMP_Settings.defaultStyleSheet;

            if (globalStyleSheet != null)
            {
                int styleCount = globalStyleSheet.styles.Count;

                for (int i = 0; i < styleCount; i++)
                {
                    TMP_Style style = globalStyleSheet.styles[i];

                    if (k_AvailableStyles.ContainsKey(style.hashCode) == false)
                    {
                        k_AvailableStyles.Add(style.hashCode, style);
                        m_Styles.Add(style);
                        m_TextStyleIndexLookup.Add(style.hashCode, m_TextStyleIndexLookup.Count);
                    }
                }
            }

            GUIContent[] styleNames = k_AvailableStyles.Values.Select(item => new GUIContent(item.name)).ToArray();

            m_TextStyleIndexLookup.TryGetValue(m_TextStyleHashCodeProp.intValue, out m_StyleSelectionIndex);

            return styleNames;
        }

        protected void DrawMarginProperty(SerializedProperty property, GUIContent label)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 2 * 18);

            EditorGUI.BeginProperty(rect, label, property);

            Rect pos0 = new Rect(rect.x, rect.y + 2, rect.width - 15, 18);

            float width = rect.width + 3;
            pos0.width = EditorGUIUtility.labelWidth;
            EditorGUI.PrefixLabel(pos0, label);

            Vector4 margins = property.vector4Value;

            float widthB = width - EditorGUIUtility.labelWidth;
            float fieldWidth = widthB / 4;
            pos0.width = Mathf.Max(fieldWidth - 5, 45f);

            pos0.x = EditorGUIUtility.labelWidth + 15;
            margins.x = DrawMarginField(pos0, "Left", margins.x);

            pos0.x += fieldWidth;
            margins.y = DrawMarginField(pos0, "Top", margins.y);

            pos0.x += fieldWidth;
            margins.z = DrawMarginField(pos0, "Right", margins.z);

            pos0.x += fieldWidth;
            margins.w = DrawMarginField(pos0, "Bottom", margins.w);

            property.vector4Value = margins;

            EditorGUI.EndProperty();
        }

        float DrawMarginField(Rect position, string label, float value)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
            EditorGUI.PrefixLabel(position, controlId, new GUIContent(label));

            Rect dragZone = new Rect(position.x, position.y, position.width, position.height);
            position.y += EditorGUIUtility.singleLineHeight;

            return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, position, dragZone, controlId, value,
                EditorGUI.kFloatFieldFormatString, EditorStyles.numberField, true);
        }

        protected void DrawPropertySlider(GUIContent label, SerializedProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 17);

            GUIContent content = label ?? GUIContent.none;
            EditorGUI.Slider(new Rect(rect.x, rect.y, rect.width, rect.height), property, 0.0f, 1.0f, content);
        }

        protected abstract bool IsMixSelectionTypes();

        protected abstract void OnUndoRedo();
    }
}