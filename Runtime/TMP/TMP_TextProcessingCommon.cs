using System.Diagnostics;
using UnityEngine;


namespace TMPro
{
    internal enum TextProcessingElementType
    {
        Undefined = 0x0,
        TextCharacterElement = 0x1,
        TextMarkupElement = 0x2
    }

    internal struct CharacterElement
    {
        public uint Unicode
        {
            get { return m_Unicode; }
            set { m_Unicode = value; }
        }

        public CharacterElement(TMP_TextElement textElement)
        {
            m_Unicode = textElement.unicode;
            m_TextElement = textElement;
        }

        private uint m_Unicode;
        private TMP_TextElement m_TextElement;
    }

    internal struct MarkupAttribute
    {
        public int NameHashCode
        {
            get { return m_NameHashCode; }
            set { m_NameHashCode = value; }
        }

        public int ValueHashCode
        {
            get { return m_ValueHashCode; }
            set { m_ValueHashCode = value; }
        }

        public int ValueStartIndex
        {
            get { return m_ValueStartIndex; }
            set { m_ValueStartIndex = value; }
        }

        public int ValueLength
        {
            get { return m_ValueLength; }
            set { m_ValueLength = value; }
        }

        private int m_NameHashCode;
        private int m_ValueHashCode;
        private int m_ValueStartIndex;
        private int m_ValueLength;
    }

    internal struct MarkupElement
    {
        public int NameHashCode
        {
            get
            {
                return m_Attributes == null ? 0 : m_Attributes[0].NameHashCode;
            }
            set
            {
                if (m_Attributes == null)
                    m_Attributes = new MarkupAttribute[8];

                m_Attributes[0].NameHashCode = value;
            }
        }

        public int ValueHashCode
        {
            get { return m_Attributes == null ? 0 : m_Attributes[0].ValueHashCode; }
            set { m_Attributes[0].ValueHashCode = value; }
        }

        public int ValueStartIndex
        {
            get { return m_Attributes == null ? 0 : m_Attributes[0].ValueStartIndex; }
            set { m_Attributes[0].ValueStartIndex = value; }
        }

        public int ValueLength
        {
            get { return m_Attributes == null ? 0 : m_Attributes[0].ValueLength; }
            set { m_Attributes[0].ValueLength = value; }
        }

        public MarkupAttribute[] Attributes
        {
            get { return m_Attributes; }
            set { m_Attributes = value; }
        }

        /// <param name="nameHashCode"></param>
        public MarkupElement(int nameHashCode, int startIndex, int length)
        {
            m_Attributes = new MarkupAttribute[8];

            m_Attributes[0].NameHashCode = nameHashCode;
            m_Attributes[0].ValueStartIndex = startIndex;
            m_Attributes[0].ValueLength = length;
        }

        private MarkupAttribute[] m_Attributes;
    }

    [DebuggerDisplay("{DebuggerDisplay()}")]
    internal struct TextProcessingElement
    {
        public TextProcessingElementType ElementType
        {
            get { return m_ElementType; }
            set { m_ElementType = value; }
        }

        public int StartIndex
        {
            get { return m_StartIndex; }
            set { m_StartIndex = value; }
        }

        public int Length
        {
            get { return m_Length; }
            set { m_Length = value; }
        }

        public CharacterElement CharacterElement
        {
            get { return m_CharacterElement; }
        }

        public MarkupElement MarkupElement
        {
            get { return m_MarkupElement; }
            set { m_MarkupElement = value; }
        }

        public TextProcessingElement(TextProcessingElementType elementType, int startIndex, int length)
        {
            m_ElementType = elementType;
            m_StartIndex = startIndex;
            m_Length = length;

            m_CharacterElement = new();
            m_MarkupElement = new();
        }

        public TextProcessingElement(TMP_TextElement textElement, int startIndex, int length)
        {
            m_ElementType = TextProcessingElementType.TextCharacterElement;
            m_StartIndex = startIndex;
            m_Length = length;

            m_CharacterElement = new(textElement);
            m_MarkupElement = new();
        }

        public TextProcessingElement(CharacterElement characterElement, int startIndex, int length)
        {
            m_ElementType = TextProcessingElementType.TextCharacterElement;
            m_StartIndex = startIndex;
            m_Length = length;

            m_CharacterElement = characterElement;
            m_MarkupElement = new();
        }

        public TextProcessingElement(MarkupElement markupElement)
        {
            m_ElementType = TextProcessingElementType.TextMarkupElement;
            m_StartIndex = markupElement.ValueStartIndex;
            m_Length = markupElement.ValueLength;

            m_CharacterElement = new();
            m_MarkupElement = markupElement;
        }

        public static TextProcessingElement Undefined => new() { ElementType = TextProcessingElementType.Undefined };


        private string DebuggerDisplay()
        {
            return m_ElementType == TextProcessingElementType.TextCharacterElement ? $"Unicode ({m_CharacterElement.Unicode})   '{(char)m_CharacterElement.Unicode}' " : $"Markup = {(MarkupTag)m_MarkupElement.NameHashCode}";
        }

        private TextProcessingElementType m_ElementType;
        private int m_StartIndex;
        private int m_Length;

        private CharacterElement m_CharacterElement;
        private MarkupElement m_MarkupElement;
    }


}
