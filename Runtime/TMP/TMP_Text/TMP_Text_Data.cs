using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public abstract partial class TMP_Text
    {
        
        /// <summary>
        /// Returns the bounds of the mesh of the text object in world space.
        /// </summary>
        public Bounds bounds
        {
            get
            {
                if (m_mesh == null) return new Bounds();

                return GetCompoundBounds();
            }
        }

        /// <summary>
        /// Returns the bounds of the text of the text object.
        /// </summary>
        public Bounds textBounds
        {
            get
            {
                if (m_textInfo == null) return new Bounds();

                return GetTextBounds();
            }
        }

        /// <summary>
        /// Event delegate to allow custom loading of TMP_FontAsset when using the <font="Font Asset Name"> tag.
        /// </summary>
        public static event Func<int, string, TMP_FontAsset> OnFontAssetRequest;

        /// <summary>
        /// Event delegate to allow custom loading of TMP_SpriteAsset when using the <sprite="Sprite Asset Name"> tag.
        /// </summary>
        public static event Func<int, string, TMP_SpriteAsset> OnSpriteAssetRequest;

        /// <summary>
        /// Delegate for the OnMissingCharacter event called when the requested Unicode character is missing from the font asset.
        /// </summary>
        /// <param name="unicode">The Unicode of the missing character.</param>
        /// <param name="stringIndex">The index of the missing character in the source string.</param>
        /// <param name="text">The source text that contains the missing character.</param>
        /// <param name="fontAsset">The font asset that is missing the requested characters.</param>
        /// <param name="textComponent">The text component where the requested character is missing.</param>
        public delegate void MissingCharacterEventCallback(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent);

        /// <summary>
        /// Event delegate to be called when the requested Unicode character is missing from the font asset.
        /// </summary>
        public static event MissingCharacterEventCallback OnMissingCharacter;

        /// <summary>
        /// Event delegate to allow modifying the text geometry before it is uploaded to the mesh and rendered.
        /// </summary>
        public virtual event Action<TMP_TextInfo> OnPreRenderText = delegate { };

        /// <summary>
        /// Component used to control wrapping of text following some arbitrary shape.
        /// </summary>


        /// <summary>
        /// Component used to control and animate sprites in the text object.
        /// </summary>
        protected TMP_SpriteAnimator spriteAnimator
        {
            get
            {
                if (m_spriteAnimator == null)
                {
                    m_spriteAnimator = GetComponent<TMP_SpriteAnimator>();
                    if (m_spriteAnimator == null) m_spriteAnimator = gameObject.AddComponent<TMP_SpriteAnimator>();
                }

                return m_spriteAnimator;
            }

        }
        protected TMP_SpriteAnimator m_spriteAnimator;


        /// <summary>
        ///
        /// </summary>

        /// <summary>
        ///
        /// </summary>
        public float flexibleHeight { get { return m_flexibleHeight; } }
        protected float m_flexibleHeight = -1f;

        /// <summary>
        ///
        /// </summary>
        public float flexibleWidth { get { return m_flexibleWidth; } }
        protected float m_flexibleWidth = -1f;

        /// <summary>
        ///
        /// </summary>
        public float minWidth { get { return m_minWidth; } }
        protected float m_minWidth;

        /// <summary>
        ///
        /// </summary>
        public float minHeight { get { return m_minHeight; } }
        protected float m_minHeight;

        /// <summary>
        ///
        /// </summary>
        public float maxWidth { get { return m_maxWidth; } }
        protected float m_maxWidth;

        /// <summary>
        ///
        /// </summary>
        public float maxHeight { get { return m_maxHeight; } }
        protected float m_maxHeight;

        /// <summary>
        ///
        /// </summary>
        protected LayoutElement layoutElement
        {
            get
            {
                if (m_LayoutElement == null)
                {
                    m_LayoutElement = GetComponent<LayoutElement>();
                }

                return m_LayoutElement;
            }
        }
        protected LayoutElement m_LayoutElement;

        /// <summary>
        /// Computed preferred width of the text object.
        /// </summary>
        public virtual float preferredWidth { get { m_preferredWidth = GetPreferredWidth(); return m_preferredWidth; } }
        protected float m_preferredWidth;
        protected float m_RenderedWidth;
        protected bool m_isPreferredWidthDirty;

        /// <summary>
        /// Computed preferred height of the text object.
        /// </summary>
        public virtual float preferredHeight { get { m_preferredHeight = GetPreferredHeight(); return m_preferredHeight; } }
        protected float m_preferredHeight;
        protected float m_RenderedHeight;
        protected bool m_isPreferredHeightDirty;

        protected bool m_isCalculatingPreferredValues;


        /// <summary>
        /// Compute the rendered width of the text object.
        /// </summary>
        public virtual float renderedWidth { get { return GetRenderedWidth(); } }


        /// <summary>
        /// Compute the rendered height of the text object.
        /// </summary>
        public virtual float renderedHeight { get { return GetRenderedHeight(); } }


        /// <summary>
        ///
        /// </summary>
        public int layoutPriority { get { return m_layoutPriority; } }
        protected int m_layoutPriority = 0;

        protected bool m_isLayoutDirty;

        protected bool m_isAwake;
        internal bool m_isWaitingOnResourceLoad;

        protected struct CharacterSubstitution
        {
            public int index;
            public uint unicode;

            public CharacterSubstitution (int index, uint unicode)
            {
                this.index = index;
                this.unicode = unicode;
            }
        }

        internal enum TextInputSources { TextInputBox = 0, SetText = 1, SetTextArray = 2, TextString = 3 };

        internal TextInputSources m_inputSource;

        protected float m_fontScaleMultiplier;

        private static char[] m_htmlTag = new char[128];
        private static RichTextTagAttribute[] m_xmlAttribute = new RichTextTagAttribute[8];
        private static float[] m_attributeParameterValues = new float[16];

        protected float tag_LineIndent = 0;
        protected float tag_Indent = 0;
        protected TMP_TextProcessingStack<float> m_indentStack = new(new float[16]);
        protected bool tag_NoParsing;

        protected bool m_isTextLayoutPhase;
        protected Quaternion m_FXRotation;
        protected Vector3 m_FXScale;

        /// <summary>
        /// Array containing the Unicode characters to be parsed.
        /// </summary>
        internal TextProcessingElement[] m_TextProcessingArray = new TextProcessingElement[8];

        /// <summary>
        /// The number of Unicode characters that have been parsed and contained in the m_InternalParsingBuffer
        /// </summary>
        internal int m_InternalTextProcessingArraySize;

        [System.Diagnostics.DebuggerDisplay("Unicode ({unicode})  '{(char)unicode}'")]
        internal struct TextProcessingElement
        {
            public TextProcessingElementType elementType;
            public uint unicode;
            public int stringIndex;
            public int length;
        }

        protected struct SpecialCharacter
        {
            public TMP_Character character;
            public TMP_FontAsset fontAsset;
            public Material material;
            public int materialIndex;

            public SpecialCharacter(TMP_Character character, int materialIndex)
            {
                this.character = character;
                this.fontAsset = character.textAsset as TMP_FontAsset;
                this.material = this.fontAsset != null ? this.fontAsset.material : null;
                this.materialIndex = materialIndex;
            }
        }

        private TMP_CharacterInfo[] m_internalCharacterInfo;
        protected int m_totalCharacterCount;

        internal static WordWrapState m_SavedWordWrapState = new();
        internal static WordWrapState m_SavedLineState = new();
        internal static WordWrapState m_SavedEllipsisState = new();
        internal static WordWrapState m_SavedLastValidState = new();
        internal static WordWrapState m_SavedSoftLineBreakState = new();

        internal static TMP_TextProcessingStack<WordWrapState> m_EllipsisInsertionCandidateStack = new(8, 8);

        protected int m_characterCount;

        protected int m_firstCharacterOfLine;
        protected int m_firstVisibleCharacterOfLine;
        protected int m_lastCharacterOfLine;
        protected int m_lastVisibleCharacterOfLine;
        protected int m_lineNumber;
        protected int m_lineVisibleCharacterCount;
        protected int m_lineVisibleSpaceCount;
        protected int m_pageNumber;
        protected float m_PageAscender;
        protected float m_maxTextAscender;
        protected float m_maxCapHeight;
        protected float m_ElementAscender;
        protected float m_ElementDescender;
        protected float m_maxLineAscender;
        protected float m_maxLineDescender;
        protected float m_startOfLineAscender;
        protected float m_startOfLineDescender;
        protected float m_lineOffset;
        protected Extents m_meshExtents;


        protected Color32 m_htmlColor = new Color(255, 255, 255, 128);
        protected TMP_TextProcessingStack<Color32> m_colorStack = new(new Color32[16]);
        protected TMP_TextProcessingStack<Color32> m_underlineColorStack = new(new Color32[16]);
        protected TMP_TextProcessingStack<Color32> m_strikethroughColorStack = new(new Color32[16]);
        protected TMP_TextProcessingStack<HighlightState> m_HighlightStateStack = new(new HighlightState[16]);

        protected TMP_ColorGradient m_colorGradientPreset;
        protected TMP_TextProcessingStack<TMP_ColorGradient> m_colorGradientStack = new(new TMP_ColorGradient[16]);
        protected bool m_colorGradientPresetIsTinted;

        protected float m_tabSpacing = 0;
        protected float m_spacing = 0;

        protected TMP_TextProcessingStack<int>[] m_TextStyleStacks = new TMP_TextProcessingStack<int>[8];
        protected int m_TextStyleStackDepth = 0;

        protected TMP_TextProcessingStack<int> m_ItalicAngleStack = new(new int[16]);
        protected int m_ItalicAngle;

        protected TMP_TextProcessingStack<int> m_actionStack = new(new int[16]);

        protected float m_padding = 0;
        protected float m_baselineOffset;
        protected TMP_TextProcessingStack<float> m_baselineOffsetStack = new(new float[16]);
        protected float m_xAdvance;

        protected TMP_TextElementType m_textElementType;
        protected TMP_TextElement m_cached_TextElement;

        protected SpecialCharacter m_Ellipsis;
        protected SpecialCharacter m_Underline;

        protected TMP_SpriteAsset m_defaultSpriteAsset;
        protected TMP_SpriteAsset m_currentSpriteAsset;
        protected int m_spriteCount = 0;
        protected int m_spriteIndex;
        protected int m_spriteAnimationID;

        private static ProfilerMarker k_ParseTextMarker = new("TMP Parse Text");
        private static ProfilerMarker k_InsertNewLineMarker = new("TMP.InsertNewLine");
    }
}