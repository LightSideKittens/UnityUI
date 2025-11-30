using UnityEngine;
using UnityEngine.TextCore;
using System;


namespace TMPro
{
    /// <summary>
    /// Flags to control what vertex data is pushed to the mesh and renderer.
    /// </summary>
    public enum TMP_VertexDataUpdateFlags
    {
        None = 0x0,
        Vertices = 0x1,
        Uv0 = 0x2,
        Uv2 = 0x4,
        Uv4 = 0x8,
        Colors32 = 0x10,
        All = 0xFF
    };


    /// <summary>
    /// TMP custom data type to represent 32 bit characters.
    /// </summary>
    [Serializable]
    public struct VertexGradient
    {
        public Color topLeft;
        public Color topRight;
        public Color bottomLeft;
        public Color bottomRight;

        public VertexGradient (Color color)
        {
            topLeft = color;
            topRight = color;
            bottomLeft = color;
            bottomRight = color;
        }

        /// <summary>
        /// The vertex colors at the corners of the characters.
        /// </summary>
        /// <param name="color0">Top left color.</param>
        /// <param name="color1">Top right color.</param>
        /// <param name="color2">Bottom left color.</param>
        /// <param name="color3">Bottom right color.</param>
        public VertexGradient(Color color0, Color color1, Color color2, Color color3)
        {
            topLeft = color0;
            topRight = color1;
            bottomLeft = color2;
            bottomRight = color3;
        }
    }


    public struct TMP_PageInfo
    {
        public int firstCharacterIndex;
        public int lastCharacterIndex;
        public float ascender;
        public float baseLine;
        public float descender;
    }


    /// <summary>
    /// Structure containing information about individual links contained in the text object.
    /// </summary>
    public struct TMP_LinkInfo
    {
        public TMP_Text textComponent;

        public int hashCode;

        public int linkIdFirstCharacterIndex;
        public int linkIdLength;
        public int linkTextfirstCharacterIndex;
        public int linkTextLength;

        internal char[] linkID;


        internal void SetLinkID(char[] text, int startIndex, int length)
        {
            if (linkID == null || linkID.Length < length) linkID = new char[length];

            for (int i = 0; i < length; i++)
                linkID[i] = text[startIndex + i];

            linkIdLength = length;
        }

        /// <summary>
        /// Function which returns the text contained in a link.
        /// </summary>
        /// <param name="textInfo"></param>
        /// <returns></returns>
        public string GetLinkText()
        {
            string text = string.Empty;
            TMP_TextInfo textInfo = textComponent.textInfo;

            for (int i = linkTextfirstCharacterIndex; i < linkTextfirstCharacterIndex + linkTextLength; i++)
                text += textInfo.characterInfo[i].character;

            return text;
        }

        /// <summary>
        /// Function which returns the link as a string.
        /// </summary>
        /// <returns></returns>
        public string GetLink()
        {
            return GetLinkID();
        }

        /// <summary>
        /// Function which returns the link ID as a string.
        /// </summary>
        /// <param name="text">The source input text.</param>
        /// <returns></returns>
        public string GetLinkID()
        {
            if (textComponent == null)
                return string.Empty;

            return new(linkID, 0, linkIdLength);
        }
    }


    /// <summary>
    /// Structure containing information about the individual words contained in the text object.
    /// </summary>
    public struct TMP_WordInfo
    {
        public TMP_Text textComponent;

        public int firstCharacterIndex;
        public int lastCharacterIndex;
        public int characterCount;

        /// <summary>
        /// Returns the word as a string.
        /// </summary>
        /// <returns></returns>
        public string GetWord()
        {
            string word = string.Empty;
            TMP_CharacterInfo[] charInfo = textComponent.textInfo.characterInfo;

            for (int i = firstCharacterIndex; i < lastCharacterIndex + 1; i++)
            {
                word += charInfo[i].character;
            }

            return word;
        }
    }


    public struct TMP_SpriteInfo
    {
        public int spriteIndex;
        public int characterIndex;
        public int vertexIndex;
    }


    public struct Extents
    {
        internal static Extents zero = new(Vector2.zero, Vector2.zero);
        internal static Extents uninitialized = new(new(32767, 32767), new(-32767, -32767));

        public Vector2 min;
        public Vector2 max;

        public Extents(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            string s = "Min (" + min.x.ToString("f2") + ", " + min.y.ToString("f2") + ")   Max (" + max.x.ToString("f2") + ", " + max.y.ToString("f2") + ")";
            return s;
        }
    }


    [Serializable]
    public struct Mesh_Extents
    {
        public Vector2 min;
        public Vector2 max;


        public Mesh_Extents(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            string s = "Min (" + min.x.ToString("f2") + ", " + min.y.ToString("f2") + ")   Max (" + max.x.ToString("f2") + ", " + max.y.ToString("f2") + ")";
            return s;
        }
    }


    internal struct WordWrapState
    {
        public int previous_WordBreak;
        public int total_CharacterCount;
        public int visible_CharacterCount;
        public int visibleSpaceCount;
        public int visible_SpriteCount;
        public int visible_LinkCount;
        public int firstCharacterIndex;
        public int firstVisibleCharacterIndex;
        public int lastCharacterIndex;
        public int lastVisibleCharIndex;
        public int lineNumber;

        public float maxCapHeight;
        public float maxAscender;
        public float maxDescender;
        public float startOfLineAscender;
        public float maxLineAscender;
        public float maxLineDescender;

        public HorizontalAlignmentOptions horizontalAlignment;
        public float marginLeft;
        public float marginRight;

        public float xAdvance;
        public float preferredWidth;
        public float preferredHeight;
        public float renderedWidth;
        public float renderedHeight;

        public float previousLineScale;

        public int wordCount;
        public FontStyles fontStyle;
        public int italicAngle;
        public float fontScaleMultiplier;

        public float currentFontSize;
        public float baselineOffset;
        public float lineOffset;
        public bool isDrivenLineSpacing;
        public int lastBaseGlyphIndex;

        public float cSpace;
        public float mSpace;

        public TMP_TextInfo textInfo;
        public TMP_LineInfo lineInfo;

        public Color32 vertexColor;
        public Color32 underlineColor;
        public Color32 strikethroughColor;
        public HighlightState highlightState;
        public TMP_FontStyleStack basicStyleStack;
        public TMP_TextProcessingStack<int> italicAngleStack;
        public TMP_TextProcessingStack<Color32> colorStack;
        public TMP_TextProcessingStack<Color32> underlineColorStack;
        public TMP_TextProcessingStack<Color32> strikethroughColorStack;
        public TMP_TextProcessingStack<Color32> highlightColorStack;
        public TMP_TextProcessingStack<HighlightState> highlightStateStack;
        public TMP_TextProcessingStack<TMP_ColorGradient> colorGradientStack;
        public TMP_TextProcessingStack<float> sizeStack;
        public TMP_TextProcessingStack<float> indentStack;
        public TMP_TextProcessingStack<FontWeight> fontWeightStack;
        public TMP_TextProcessingStack<int> styleStack;
        public TMP_TextProcessingStack<float> baselineStack;
        public TMP_TextProcessingStack<int> actionStack;
        public TMP_TextProcessingStack<MaterialReference> materialReferenceStack;
        public TMP_TextProcessingStack<HorizontalAlignmentOptions> lineJustificationStack;
        public int spriteAnimationID;

        public TMP_FontAsset currentFontAsset;
        public TMP_SpriteAsset currentSpriteAsset;
        public Material currentMaterial;
        public int currentMaterialIndex;

        public Extents meshExtents;

        public bool tagNoParsing;
        public bool isNonBreakingSpace;

        public Quaternion fxRotation;
        public Vector3 fxScale;
    }


    /// <summary>
    /// Structure used to store retrieve the name and hashcode of the font and material
    /// </summary>
    internal struct TagAttribute
    {
        public int startIndex;
        public int length;
        public int hashCode;
    }

    internal struct RichTextTagAttribute
    {
        public int nameHashCode;
        public int valueHashCode;
        public TagValueType valueType;
        public int valueStartIndex;
        public int valueLength;
        public TagUnitType unitType;
    }

}
