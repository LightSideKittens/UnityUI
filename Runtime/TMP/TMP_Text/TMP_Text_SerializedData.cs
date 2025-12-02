using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore;

namespace TMPro
{
    public abstract partial class TMP_Text
    {
        const string RtlLtrStressTest =
    "RTL/LTR mixed stress test (Unity TMP + BiDi + shaping):\n" +
    "\n" +
    // Арабский + базовые знаки
    "العربية: مرحباً بالعالم! هذا نَصٌّ لاختبار الاتجاه؛ عربى، عربي، عربي؟\n" +
    "أرقام عربية-هندية: ١٢٣٤٥٦٧٨٩٠, Arabic digits: 1234567890.\n" +
    "علامات الترقيم: ، ؛ ؟ ! : ، … — (شرطة طويلة)، \"اقتباس\"، «اقتباس».\n" +
    "\n" +
    // LAM–ALEF + диакритика + ZWJ
    "لام-ألف: لا لَا لَّا ل\u064E\u200Dا (LAM + FATHA + ZWJ + ALEF).\n" +
    "سلسلة مشكولة: اَللّٰهُ رَبُّ الْعَالَمِينَ، بِسْمِ اللّٰهِ الرَّحْمٰنِ الرَّحِيمِ.\n" +
    "\n" +
    // Персидский (Farsi) с ZWNJ
    "فارسی: سلام دنیا! حروف اضافی: پ چ ژ گ ی.\n" +
    "فاصلهٔ مجازی (ZWNJ): می\u200Cروم، خانه\u200Cها، کتاب\u200Cها، نمی\u200Cخواهم.\n" +
    "\n" +
    // Урду
    "اُردو: یہ ایک جامع ٹیسٹ ٹیکسٹ ہے؛ حروف: ٹ، ڈ، ڑ، ں، ہ، ھ، ے، ں، گ، ک، ی.\n" +
    "مخلوط جملہ: اردو (Urdu) + English + اعداد ۱۲۳۴.\n" +
    "\n" +
    // Иврит + латиница + цифры
    "עברית: שלום עולם, טקסט בדיקה, מספרים 1234 ו-٥٦٧٨.\n" +
    "Mixed Hebrew/English: שלום (shalom) world 2025-12-01, test-מספר.\n" +
    "\n" +
    // Скобки, кавычки, вложенность
    "Nested brackets: (AR: مرحبا [نص {تجريبي} مع أقواس]!) and (EN: (nested [brackets] {here})).\n" +
    "Quotes: \"plain quotes\", ‘single’, “double smart”, «guillemets», „low-high“.\n" +
    "\n" +
    // Rich Text TMP
    "RichText: <b>غامق عربي مرحبا</b>, <i>שלום מודגש</i>, " +
    "<color=#FF0000>red English text</color>, " +
    "<size=150%>نص مكبر</size>, <u>خط سفلي</u>.\n" +
    "\n" +
    // ZWJ/ZWNJ + формы
    "ZWJ forms (for shaping): \u200Dب (forced final), ب\u200D (forced initial), \u200Dب\u200D (forced medial).\n" +
    "Mix with diacritics + ZWJ: \u200Dب\u064E\u200D، \u200Dن\u0651\u064E\u200D.\n" +
    "ZWNJ breaking join: با\u200Cب، لا\u200Cا، می\u200Cرود.\n" +
    "\n" +
    // Bidi control chars
    "Bidi controls (LRE/RLE/PDF, LRM/RLM):\n" +
    "English before \u202Bمرحبا بالعالم\u202C after [RLE ... PDF].\n" +
    "LRM/LRM: EN\u200E-\u200Etag, AR\u200F-\u200Fعلامة.\n" +
    "\n" +
    // RTL + LTR в одной строке
    "Mixed run: مرحبا (hello) 123 in [EN], ثم نص عربي، then Hebrew שלום, ואז again عربى.\n" +
    "Right-to-left with inner English: هذا \"test\" داخل جملة عربية (with brackets).؟\n" +
    "\n" +
    // Скобки и кавычки вокруг RTL
    "Brackets around RTL: (مرحبا)، [سلام]، {شکریہ} and around mixed [hello مرحبا 123].\n" +
    "\n" +
    // Эмодзи + RTL
    "Emojis: 😀 😃 😁 😂 🤔 👍 ❤️.\n" +
    "RTL with emojis: مرحبا 😀 بالعالم، رقم ١٢٣، نص 😃 مختلط 👍.\n" +
    "Family ZWJ emoji: \U0001F468\u200D\U0001F469\u200D\U0001F467, flags: \U0001F1EE\U0001F1F7 (IL), " +
    "\U0001F1EA\U0001F1F8 (ES), \U0001F1FA\U0001F1F8 (US).\n" +
    "\n" +
    // Комбинированная строка «всё подряд»
    "Big mixed line: لا + لَا + لَّا + فارسی پچژگ + اُردو ٹ،ڈ،ڑ + עברית שלום + English TEXT 1234 " +
    "++ emoji 😀 + brackets (عربي [EN {עברית} 42]) + ZWNJ می\u200Cروم + ZWJ \u200Dب\u200D + " +
    "bidi controls \u202Bعربي مع \u202AEN\u202C داخل\u202C done.";

        
        /// <summary>
        /// A string containing the text to be displayed.
        /// </summary>
        public virtual string text
        {
            get
            {
                m_text = RtlLtrStressTest;
                return RtlLtrStressTest;
                if (m_IsTextBackingStringDirty)
                    return InternalTextBackingArrayToString();

                return m_text;
            }
            set
            {
                if (!m_IsTextBackingStringDirty && m_text != null && value != null && m_text.Length == value.Length && m_text == value)
                    return;

                m_IsTextBackingStringDirty = false;
                m_text = value;
                _havePropertiesChanged = true;
                m_text = RtlLtrStressTest;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }
        [SerializeField]
        [TextArea(5, 10)]
        protected string m_text;
        
        /// <summary>
        ///
        /// </summary>
        private bool m_IsTextBackingStringDirty;

        /// <summary>
        /// The ITextPreprocessor component referenced by the text object (if any)
        /// </summary>
        public ITextPreprocessor textPreprocessor
        {
            get => m_TextPreprocessor;
            set => m_TextPreprocessor = value;
        }
        [SerializeField]
        protected ITextPreprocessor m_TextPreprocessor;

        public string PreprocessedText { get; private set; } 

        /// <summary>
        ///
        /// </summary>
        public bool isRightToLeftText
        {
            get => m_isRightToLeft;
            set { if (m_isRightToLeft == value) return; m_isRightToLeft = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_isRightToLeft;


        /// <summary>
        /// The Font Asset to be assigned to this text object.
        /// </summary>
        public TMP_FontAsset font
        {
            get => m_fontAsset;
            set { if (m_fontAsset == value) return; m_fontAsset = value; LoadFontAsset(); _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TMP_FontAsset m_fontAsset;
        protected TMP_FontAsset m_currentFontAsset;
        protected bool m_isSDFShader;


        /// <summary>
        /// The material to be assigned to this text object.
        /// </summary>
        public virtual Material fontSharedMaterial
        {
            get => m_sharedMaterial;
            set { if (m_sharedMaterial == value) return; SetSharedMaterial(value); _havePropertiesChanged = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material m_sharedMaterial;
        protected Material m_currentMaterial;
        protected static MaterialReference[] m_materialReferences = new MaterialReference[4];
        protected static Dictionary<int, int> m_materialReferenceIndexLookup = new();

        protected static TMP_TextProcessingStack<MaterialReference> m_materialReferenceStack = new(new MaterialReference[16]);
        protected int m_currentMaterialIndex;


        /// <summary>
        /// An array containing the materials used by the text object.
        /// </summary>
        public virtual Material[] fontSharedMaterials
        {
            get => GetSharedMaterials();
            set { SetSharedMaterials(value); _havePropertiesChanged = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material[] m_fontSharedMaterials;


        /// <summary>
        /// The material to be assigned to this text object. An instance of the material will be assigned to the object's renderer.
        /// </summary>
        public Material fontMaterial
        {
            get => GetMaterial(m_sharedMaterial);

            set
            {
                if (m_sharedMaterial != null && m_sharedMaterial.GetInstanceID() == value.GetInstanceID()) return;

                m_sharedMaterial = value;

                m_padding = GetPaddingForMaterial();
                _havePropertiesChanged = true;

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
        [SerializeField]
        protected Material m_fontMaterial;


        /// <summary>
        /// The materials to be assigned to this text object. An instance of the materials will be assigned.
        /// </summary>
        public virtual Material[] fontMaterials
        {
            get => GetMaterials(m_fontSharedMaterials);

            set { SetSharedMaterials(value); _havePropertiesChanged = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material[] m_fontMaterials;

        protected bool m_isMaterialDirty;


        /// <summary>
        /// This is the default vertex color assigned to each vertices. Color tags will override vertex colors unless the overrideColorTags is set.
        /// </summary>
        public override Color color
        {
            get => m_fontColor;
            set { if (m_fontColor == value) return; _havePropertiesChanged = true; m_fontColor = value; SetVerticesDirty(); }
        }

        [SerializeField]
        protected Color32 m_fontColor32 = Color.white;
        [SerializeField]
        protected Color m_fontColor = Color.white;
        protected static Color32 s_colorWhite = new(255, 255, 255, 255);
        protected Color32 m_underlineColor = s_colorWhite;
        protected Color32 m_strikethroughColor = s_colorWhite;
        internal HighlightState m_HighlightState = new(s_colorWhite, TMP_Offset.zero);
        internal bool m_ConvertToLinearSpace;

        /// <summary>
        /// Sets the vertex color alpha value.
        /// </summary>
        public float alpha
        {
            get => m_fontColor.a;
            set { if (m_fontColor.a == value) return; m_fontColor.a = value; _havePropertiesChanged = true; SetVerticesDirty(); }
        }


        /// <summary>
        /// Determines if Vertex Color Gradient should be used
        /// </summary>
        /// <value><c>true</c> if enable vertex gradient; otherwise, <c>false</c>.</value>
        public bool enableVertexGradient
        {
            get => m_enableVertexGradient;
            set { if (m_enableVertexGradient == value) return; _havePropertiesChanged = true; m_enableVertexGradient = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_enableVertexGradient;

        [SerializeField]
        protected ColorMode m_colorMode = ColorMode.FourCornersGradient;

        /// <summary>
        /// Sets the vertex colors for each of the 4 vertices of the character quads.
        /// </summary>
        /// <value>The color gradient.</value>
        public VertexGradient colorGradient
        {
            get => m_fontColorGradient;
            set { _havePropertiesChanged = true; m_fontColorGradient = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected VertexGradient m_fontColorGradient = new(Color.white);


        /// <summary>
        /// Set the vertex colors of the 4 vertices of each character quads.
        /// </summary>
        public TMP_ColorGradient colorGradientPreset
        {
            get => m_fontColorGradientPreset;
            set { _havePropertiesChanged = true; m_fontColorGradientPreset = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected TMP_ColorGradient m_fontColorGradientPreset;


        /// <summary>
        /// Sprite Asset used by the text object.
        /// </summary>
        public TMP_SpriteAsset spriteAsset
        {
            get => m_spriteAsset;
            set { m_spriteAsset = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TMP_SpriteAsset m_spriteAsset;


        /// <summary>
        /// Determines whether or not the sprite color is multiplies by the vertex color of the text.
        /// </summary>
        public bool tintAllSprites
        {
            get => m_tintAllSprites;
            set { if (m_tintAllSprites == value) return; m_tintAllSprites = value; _havePropertiesChanged = true; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_tintAllSprites;
        protected bool m_tintSprite;
        protected Color32 m_spriteColor;

        /// <summary>
        /// Style sheet used by the text object.
        /// </summary>
        public TMP_StyleSheet styleSheet
        {
            get => m_StyleSheet;
            set { m_StyleSheet = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TMP_StyleSheet m_StyleSheet;

        /// <summary>
        ///
        /// </summary>
        public TMP_Style textStyle
        {
            get
            {
                m_TextStyle = GetStyle(m_TextStyleHashCode);

                if (m_TextStyle == null)
                {
                    m_TextStyle = TMP_Style.NormalStyle;
                    m_TextStyleHashCode = m_TextStyle.hashCode;
                }

                return m_TextStyle;
            }

            set { m_TextStyle = value; m_TextStyleHashCode = m_TextStyle.hashCode; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        internal TMP_Style m_TextStyle;
        [SerializeField]
        protected int m_TextStyleHashCode;

        /// <summary>
        /// This overrides the color tags forcing the vertex colors to be the default font color.
        /// </summary>
        public bool overrideColorTags
        {
            get => m_overrideHtmlColors;
            set { if (m_overrideHtmlColors == value) return; _havePropertiesChanged = true; m_overrideHtmlColors = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_overrideHtmlColors;


        /// <summary>
        /// Sets the color of the _FaceColor property of the assigned material. Changing face color will result in an instance of the material.
        /// </summary>
        public Color32 faceColor
        {
            get
            {
                if (m_sharedMaterial == null) return m_faceColor;

                m_faceColor = m_sharedMaterial.GetColor(ShaderUtilities.ID_FaceColor);
                return m_faceColor;
            }

            set { if (m_faceColor.Compare(value)) return; SetFaceColor(value); _havePropertiesChanged = true; m_faceColor = value; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Color32 m_faceColor = Color.white;


        /// <summary>
        /// Sets the color of the _OutlineColor property of the assigned material. Changing outline color will result in an instance of the material.
        /// </summary>
        public Color32 outlineColor
        {
            get
            {
                if (m_sharedMaterial == null) return m_outlineColor;

                m_outlineColor = m_sharedMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
                return m_outlineColor;
            }

            set { if (m_outlineColor.Compare(value)) return; SetOutlineColor(value); _havePropertiesChanged = true; m_outlineColor = value; SetVerticesDirty(); }
        }

        protected Color32 m_outlineColor = Color.black;


        /// <summary>
        /// Sets the thickness of the outline of the font. Setting this value will result in an instance of the material.
        /// </summary>
        public float outlineWidth
        {
            get
            {
                if (m_sharedMaterial == null) return m_outlineWidth;

                m_outlineWidth = m_sharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth);
                return m_outlineWidth;
            }
            set { if (m_outlineWidth == value) return; SetOutlineThickness(value); _havePropertiesChanged = true; m_outlineWidth = value; SetVerticesDirty(); }
        }
        protected float m_outlineWidth;


        /// <summary>
        /// The rotation for the environment map lighting.
        /// </summary>
        protected Vector3 m_currentEnvMapRotation;
        /// <summary>
        /// Determine if the environment map property is valid.
        /// </summary>
        protected bool m_hasEnvMapProperty;


        /// <summary>
        /// The point size of the font.
        /// </summary>
        public float fontSize
        {
            get => m_fontSize;
            set { if (m_fontSize == value) return; _havePropertiesChanged = true; m_fontSize = value; if (!m_enableAutoSizing) m_fontSizeBase = m_fontSize; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSize = -99;

        protected float m_currentFontSize;

        [SerializeField] protected float m_fontSizeBase = 36;
        protected TMP_TextProcessingStack<float> m_sizeStack = new(16);


        /// <summary>
        /// Control the weight of the font if an alternative font asset is assigned for the given weight in the font asset editor.
        /// </summary>
        public FontWeight fontWeight
        {
            get => m_fontWeight;
            set { if (m_fontWeight == value) return; m_fontWeight = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected FontWeight m_fontWeight = FontWeight.Regular;
        protected FontWeight m_FontWeightInternal = FontWeight.Regular;
        protected TMP_TextProcessingStack<FontWeight> m_FontWeightStack = new(8);

        /// <summary>
        ///
        /// </summary>
        public float pixelsPerUnit
        {
            get
            {
                var localCanvas = canvas;
                if (!localCanvas)
                    return 1;
                if (!font)
                    return localCanvas.scaleFactor;
                if (m_currentFontAsset == null || m_currentFontAsset.faceInfo.pointSize <= 0 || m_fontSize <= 0)
                    return 1;
                return m_fontSize / m_currentFontAsset.faceInfo.pointSize;
            }
        }


        /// <summary>
        /// Enable text auto-sizing
        /// </summary>
        public bool enableAutoSizing
        {
            get => m_enableAutoSizing;
            set { if (m_enableAutoSizing == value) return; m_enableAutoSizing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_enableAutoSizing;
        protected float m_maxFontSize;
        protected float m_minFontSize;
        protected int m_AutoSizeIterationCount;
        protected int m_AutoSizeMaxIterationCount = 100;

        protected bool m_IsAutoSizePointSizeSet;


        /// <summary>
        /// Minimum point size of the font when text auto-sizing is enabled.
        /// </summary>
        public float fontSizeMin
        {
            get => m_fontSizeMin;
            set { if (m_fontSizeMin == value) return; m_fontSizeMin = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSizeMin;


        /// <summary>
        /// Maximum point size of the font when text auto-sizing is enabled.
        /// </summary>
        public float fontSizeMax
        {
            get => m_fontSizeMax;
            set { if (m_fontSizeMax == value) return; m_fontSizeMax = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSizeMax;


        /// <summary>
        /// The style of the text
        /// </summary>
        public FontStyles fontStyle
        {
            get => m_fontStyle;
            set { if (m_fontStyle == value) return; m_fontStyle = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected FontStyles m_fontStyle = FontStyles.Normal;
        protected FontStyles m_FontStyleInternal = FontStyles.Normal;
        protected TMP_FontStyleStack m_fontStyleStack;

        /// <summary>
        /// Property used in conjunction with padding calculation for the geometry.
        /// </summary>
        public bool isUsingBold => m_isUsingBold;

        protected bool m_isUsingBold = false;

        /// <summary>
        /// Horizontal alignment options
        /// </summary>
        public HorizontalAlignmentOptions horizontalAlignment
        {
            get => m_HorizontalAlignment;
            set
            {
                if (m_HorizontalAlignment == value)
                    return;

                m_HorizontalAlignment = value;

                _havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }
        [SerializeField]
        protected HorizontalAlignmentOptions m_HorizontalAlignment = HorizontalAlignmentOptions.Left;

        /// <summary>
        /// Vertical alignment options
        /// </summary>
        public VerticalAlignmentOptions verticalAlignment
        {
            get => m_VerticalAlignment;
            set
            {
                if (m_VerticalAlignment == value)
                    return;

                m_VerticalAlignment = value;

                _havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }
        [SerializeField]
        protected VerticalAlignmentOptions m_VerticalAlignment = VerticalAlignmentOptions.Top;

        /// <summary>
        /// Text alignment options
        /// </summary>
        public TextAlignmentOptions alignment
        {
            get => (TextAlignmentOptions)((int)m_HorizontalAlignment | (int)m_VerticalAlignment);
            set
            {
                HorizontalAlignmentOptions horizontalAlignment = (HorizontalAlignmentOptions)((int)value & 0xFF);
                VerticalAlignmentOptions verticalAlignment = (VerticalAlignmentOptions)((int)value & 0xFF00);

                if (m_HorizontalAlignment == horizontalAlignment && m_VerticalAlignment == verticalAlignment)
                    return;

                m_HorizontalAlignment = horizontalAlignment;
                m_VerticalAlignment = verticalAlignment;
                _havePropertiesChanged = true;
                SetVerticesDirty();
            }
        }
        [SerializeField]
        [FormerlySerializedAs("m_lineJustification")]
        protected TextAlignmentOptions m_textAlignment = TextAlignmentOptions.Converted;

        [SerializeField] protected bool autoHorizontalAlignment = true;

        protected HorizontalAlignmentOptions m_lineJustification;
        protected TMP_TextProcessingStack<HorizontalAlignmentOptions> m_lineJustificationStack = new(new HorizontalAlignmentOptions[16]);

        /// <summary>
        /// Use the extents of the text geometry for alignment instead of font metrics.
        /// </summary>


        /// <summary>
        /// The amount of additional spacing between characters.
        /// </summary>
        public float characterSpacing
        {
            get => m_characterSpacing;
            set { if (m_characterSpacing == value) return; _havePropertiesChanged = true; m_characterSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_characterSpacing;
        protected float m_cSpacing = 0;
        protected float m_monoSpacing = 0;
        protected bool m_duoSpace;

        /// <summary>
        /// The amount of additional spacing between words.
        /// </summary>
        public float wordSpacing
        {
            get => m_wordSpacing;
            set { if (m_wordSpacing == value) return; _havePropertiesChanged = true; m_wordSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_wordSpacing;

        /// <summary>
        /// The amount of additional spacing to add between each lines of text.
        /// </summary>
        public float lineSpacing
        {
            get => m_lineSpacing;
            set { if (m_lineSpacing == value) return; _havePropertiesChanged = true; m_lineSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_lineSpacing;
        protected float m_lineSpacingDelta = 0;
        protected float m_lineHeight = TMP_Math.FLOAT_UNSET;
        protected bool m_IsDrivenLineSpacing;


        /// <summary>
        /// The amount of potential line spacing adjustment before text auto sizing kicks in.
        /// </summary>
        public float lineSpacingAdjustment
        {
            get => m_lineSpacingMax;
            set { if (m_lineSpacingMax == value) return; _havePropertiesChanged = true; m_lineSpacingMax = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_lineSpacingMax;

        /// <summary>
        /// The amount of additional spacing to add between each lines of text.
        /// </summary>
        public float paragraphSpacing
        {
            get => m_paragraphSpacing;
            set { if (m_paragraphSpacing == value) return; _havePropertiesChanged = true; m_paragraphSpacing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_paragraphSpacing;


        /// <summary>
        /// Percentage the width of characters can be adjusted before text auto-sizing begins to reduce the point size.
        /// </summary>
        public float characterWidthAdjustment
        {
            get => m_charWidthMaxAdj;
            set { if (m_charWidthMaxAdj == value) return; _havePropertiesChanged = true; m_charWidthMaxAdj = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_charWidthMaxAdj;

        protected float m_charWidthAdjDelta = 0;


        /// <summary>
        /// Controls the text wrapping mode.
        /// </summary>
        public TextWrappingModes textWrappingMode
        {
            get => m_TextWrappingMode;
            set { if (m_TextWrappingMode == value) return; _havePropertiesChanged = true; m_TextWrappingMode = value; SetVerticesDirty(); SetLayoutDirty(); }
        }

        
        [SerializeField] [FormerlySerializedAs("m_enableWordWrapping")]
        protected TextWrappingModes m_TextWrappingMode;
        protected bool m_isNonBreakingSpace = false;

        /// <summary>
        /// Controls the blending between using character and word spacing to fill-in the space for justified text.
        /// </summary>
        public float wordWrappingRatios
        {
            get => m_wordWrappingRatios;
            set { if (m_wordWrappingRatios == value) return; m_wordWrappingRatios = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_wordWrappingRatios = 0.4f;


        /// <summary>
        ///
        /// </summary>


        /// <summary>
        /// Controls the Text Overflow Mode
        /// </summary>
        public TextOverflowModes overflowMode
        {
            get => m_overflowMode;
            set { if (m_overflowMode == value) return; m_overflowMode = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TextOverflowModes m_overflowMode = TextOverflowModes.Overflow;


        /// <summary>
        /// Indicates if the text exceeds the vertical bounds of its text container.
        /// </summary>
        public bool isTextOverflowing
        {
            get { if (m_firstOverflowCharacterIndex != -1) return true; return false; }
        }


        /// <summary>
        /// The first character which exceeds the vertical bounds of its text container.
        /// </summary>
        public int firstOverflowCharacterIndex => m_firstOverflowCharacterIndex;

        protected int m_firstOverflowCharacterIndex = -1;


        /// <summary>
        /// Property indicating whether the text is Truncated or using Ellipsis.
        /// </summary>
        public bool isTextTruncated => m_isTextTruncated;

        protected bool m_isTextTruncated;

        
        [SerializeField]
        protected bool m_enableKerning;
        protected int m_LastBaseGlyphIndex;

        /// <summary>
        /// List of OpenType font features that are enabled.
        /// </summary>
        public List<OTL_FeatureTag> fontFeatures
        {
            get => m_ActiveFontFeatures;
            set
            {
                if (value == null)
                    return;

                _havePropertiesChanged = true; m_ActiveFontFeatures = value; SetVerticesDirty(); SetLayoutDirty();
            }
        }
        [SerializeField]
        protected List<OTL_FeatureTag> m_ActiveFontFeatures = new() { 0 };

        /// <summary>
        /// Adds extra padding around each character. This may be necessary when the displayed text is very small to prevent clipping.
        /// </summary>
        public bool extraPadding
        {
            get => m_enableExtraPadding;
            set { if (m_enableExtraPadding == value) return; _havePropertiesChanged = true; m_enableExtraPadding = value; UpdateMeshPadding(); SetVerticesDirty();
            }
        }
        [SerializeField]
        protected bool m_enableExtraPadding;
        [SerializeField]
        protected bool checkPaddingRequired;


        /// <summary>
        /// Enables or Disables Rich Text Tags
        /// </summary>
        public bool richText
        {
            get => m_isRichText;
            set { if (m_isRichText == value) return; m_isRichText = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_isRichText = true;

        /// <summary>
        /// Enables or Disables parsing of CTRL characters in input text.
        /// </summary>
        public bool parseCtrlCharacters
        {
            get => m_parseCtrlCharacters;
            set { if (m_parseCtrlCharacters == value) return; m_parseCtrlCharacters = value; _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_parseCtrlCharacters = true;


        /// <summary>
        /// Sets the RenderQueue along with Ztest to force the text to be drawn last and on top of scene elements.
        /// </summary>
        public bool isOverlay
        {
            get => m_isOverlay;
            set { if (m_isOverlay == value) return; m_isOverlay = value; SetShaderDepth(); _havePropertiesChanged = true; SetVerticesDirty(); }
        }
        protected bool m_isOverlay;


        /// <summary>
        /// Sets Perspective Correction to Zero for Orthographic Camera mode & 0.875f for Perspective Camera mode.
        /// </summary>
        public bool isOrthographic
        {
            get => m_isOrthographic;
            set { if (m_isOrthographic == value) return; _havePropertiesChanged = true; m_isOrthographic = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_isOrthographic;


        /// <summary>
        /// Sets the culling on the shaders. Note changing this value will result in an instance of the material.
        /// </summary>
        public bool enableCulling
        {
            get => m_isCullingEnabled;
            set { if (m_isCullingEnabled == value) return; m_isCullingEnabled = value; SetCulling(); _havePropertiesChanged = true; }
        }
        [SerializeField]
        protected bool m_isCullingEnabled;

        protected bool m_isMaskingEnabled;
        protected bool isMaskUpdateRequired;

        /// <summary>
        /// Forces objects that are not visible to get refreshed.
        /// </summary>
        public bool ignoreVisibility
        {
            get => m_ignoreCulling;
            set { if (m_ignoreCulling == value) return; _havePropertiesChanged = true; m_ignoreCulling = value; }
        }

        protected bool m_ignoreCulling = true;


        /// <summary>
        /// Controls how the face and outline textures will be applied to the text object.
        /// </summary>
        public TextureMappingOptions horizontalMapping
        {
            get => m_horizontalMapping;
            set { if (m_horizontalMapping == value) return; _havePropertiesChanged = true; m_horizontalMapping = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected TextureMappingOptions m_horizontalMapping = TextureMappingOptions.Character;


        /// <summary>
        /// Controls how the face and outline textures will be applied to the text object.
        /// </summary>
        public TextureMappingOptions verticalMapping
        {
            get => m_verticalMapping;
            set { if (m_verticalMapping == value) return; _havePropertiesChanged = true; m_verticalMapping = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected TextureMappingOptions m_verticalMapping = TextureMappingOptions.Character;


        /// <summary>
        /// Controls the UV Offset for the various texture mapping mode on the text object.
        /// </summary>


        /// <summary>
        /// Controls the horizontal offset of the UV of the texture mapping mode for each line of the text object.
        /// </summary>
        public float mappingUvLineOffset
        {
            get => m_uvLineOffset;
            set { if (m_uvLineOffset == value) return; _havePropertiesChanged = true; m_uvLineOffset = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected float m_uvLineOffset;


        /// <summary>
        /// Determines if the Mesh will be rendered.
        /// </summary>
        public TextRenderFlags renderMode
        {
            get => m_renderMode;
            set { if (m_renderMode == value) return; m_renderMode = value; _havePropertiesChanged = true; }
        }
        protected TextRenderFlags m_renderMode = TextRenderFlags.Render;


        /// <summary>
        /// Determines the sorting order of the geometry of the text object.
        /// </summary>
        public VertexSortingOrder geometrySortingOrder
        {
            get => m_geometrySortingOrder;

            set { m_geometrySortingOrder = value; _havePropertiesChanged = true; SetVerticesDirty(); }

        }
        [SerializeField]
        protected VertexSortingOrder m_geometrySortingOrder;


        /// <summary>
        /// Determines if a text object will be excluded from the InternalUpdate callback used to handle updates of SDF Scale when the scale of the text object or parent(s) changes.
        /// </summary>
        public bool isTextObjectScaleStatic
        {
            get => m_IsTextObjectScaleStatic;
            set
            {
                m_IsTextObjectScaleStatic = value;

                if (!isActiveAndEnabled)
                    return;

                if (m_IsTextObjectScaleStatic)
                    TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);
                else
                    TMP_UpdateManager.RegisterTextObjectForUpdate(this);
            }
        }
        [SerializeField]
        protected bool m_IsTextObjectScaleStatic;

        /// <summary>
        /// Determines if the data structures allocated to contain the geometry of the text object will be reduced in size if the number of characters required to display the text is reduced by more than 256 characters.
        /// This reduction has the benefit of reducing the amount of vertex data being submitted to the graphic device but results in GC when it occurs.
        /// </summary>
        public bool vertexBufferAutoSizeReduction
        {
            get => m_VertexBufferAutoSizeReduction;
            set { m_VertexBufferAutoSizeReduction = value; _havePropertiesChanged = true; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_VertexBufferAutoSizeReduction;

        /// <summary>
        /// The first character which should be made visible in conjunction with the Text Overflow Linked mode.
        /// </summary>
        public int firstVisibleCharacter
        {
            get => m_firstVisibleCharacter;
            set { if (m_firstVisibleCharacter == value) return; _havePropertiesChanged = true; m_firstVisibleCharacter = value; SetVerticesDirty(); }
        }

        protected int m_firstVisibleCharacter;

        /// <summary>
        /// Allows to control how many characters are visible from the input.
        /// </summary>
        public int maxVisibleCharacters
        {
            get => m_maxVisibleCharacters;
            set { if (m_maxVisibleCharacters == value) return; _havePropertiesChanged = true; m_maxVisibleCharacters = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleCharacters = 99999;


        /// <summary>
        /// Allows to control how many words are visible from the input.
        /// </summary>
        public int maxVisibleWords
        {
            get => m_maxVisibleWords;
            set { if (m_maxVisibleWords == value) return; _havePropertiesChanged = true; m_maxVisibleWords = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleWords = 99999;


        /// <summary>
        /// Allows control over how many lines of text are displayed.
        /// </summary>
        public int maxVisibleLines
        {
            get => m_maxVisibleLines;
            set { if (m_maxVisibleLines == value) return; _havePropertiesChanged = true; m_maxVisibleLines = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleLines = 99999;


        /// <summary>
        /// Determines if the text's vertical alignment will be adjusted based on visible descender of the text.
        /// </summary>
        public bool useMaxVisibleDescender
        {
            get => m_useMaxVisibleDescender;
            set { if (m_useMaxVisibleDescender == value) return; _havePropertiesChanged = true; m_useMaxVisibleDescender = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_useMaxVisibleDescender = true;
        
        /// <summary>
        /// The margins of the text object.
        /// </summary>
        public virtual Vector4 margin
        {
            get => m_margin;
            set { if (m_margin == value) return; m_margin = value; ComputeMarginSize(); _havePropertiesChanged = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected Vector4 m_margin = new(0, 0, 0, 0);
        protected float m_marginLeft;
        protected float m_marginRight;
        protected float m_marginWidth;
        protected float m_marginHeight;
        protected float m_width = -1;


        /// <summary>
        /// Returns data about the text object which includes information about each character, word, line, link, etc.
        /// </summary>
        public TMP_TextInfo textInfo
        {
            get
            {
                if (m_textInfo == null)
                    m_textInfo = new(this);

                return m_textInfo;
            }
        }

        protected TMP_TextInfo m_textInfo;

        /// <summary>
        /// Property tracking if any of the text properties have changed. Flag is set before the text is regenerated.
        /// </summary>
        public bool havePropertiesChanged
        {
            get => m_havePropertiesChanged;
            set { if (m_havePropertiesChanged == value) return; _havePropertiesChanged = value; SetAllDirty(); }
        }

        internal bool _havePropertiesChanged
        {
            get => m_havePropertiesChanged;
            set => m_havePropertiesChanged = value;
        }

        protected bool m_havePropertiesChanged;

        /// <summary>
        /// Returns are reference to the Transform
        /// </summary>
        public new Transform transform
        {
            get
            {
                if (m_transform == null)
                    m_transform = GetComponent<Transform>();
                return m_transform;
            }
        }
        protected Transform m_transform;


        /// <summary>
        /// Returns are reference to the RectTransform
        /// </summary>
        public new RectTransform rectTransform
        {
            get
            {
                if (m_rectTransform == null)
                    m_rectTransform = GetComponent<RectTransform>();
                return m_rectTransform;
            }
        }
        protected RectTransform m_rectTransform;


        /// <summary>
        /// Used to track potential changes in RectTransform size to allow us to ignore OnRectTransformDimensionsChange getting called due to rounding errors when using Stretch Anchors.
        /// </summary>
        protected Vector2 m_PreviousRectTransformSize;

        /// <summary>
        /// Used to track potential changes in pivot position to allow us to ignore OnRectTransformDimensionsChange getting called due to rounding errors when using Stretch Anchors.
        /// </summary>
        protected Vector2 m_PreviousPivotPosition;


        /// <summary>
        /// Enables control over setting the size of the text container to match the text object.
        /// </summary>
        public virtual bool autoSizeTextContainer
        {
            get;
            set;
        }
        protected bool m_autoSizeTextContainer;


        /// <summary>
        /// The mesh used by the font asset and material assigned to the text object.
        /// </summary>
        public virtual Mesh mesh => m_mesh;

        protected Mesh m_mesh;


        /// <summary>
        /// Determines if the geometry of the characters will be quads or volumetric (cubes).
        /// </summary>
        public bool isVolumetricText
        {
            get => m_isVolumetricText;
            set { if (m_isVolumetricText == value) return; _havePropertiesChanged = value; m_textInfo.ResetVertexLayout(value); SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_isVolumetricText;
    }
}