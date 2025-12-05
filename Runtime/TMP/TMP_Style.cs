using UnityEngine;
using System.Collections;

#pragma warning disable 0649

namespace TMPro
{
    [System.Serializable]
    public class TMP_Style
    {
        public static TMP_Style NormalStyle
        {
            get
            {
                if (k_NormalStyle == null)
                    k_NormalStyle = new("Normal", string.Empty, string.Empty);

                return k_NormalStyle;
            }
        }

        internal static TMP_Style k_NormalStyle;

        public string name
        {
            get { return m_Name; }
            set
            {
                if (value != m_Name) m_Name = value;
            }
        }

        public int hashCode
        {
            get { return m_HashCode; }
            set
            {
                if (value != m_HashCode) m_HashCode = value;
            }
        }

        public string styleOpeningDefinition
        {
            get { return m_OpeningDefinition; }
        }

        public string styleClosingDefinition
        {
            get { return m_ClosingDefinition; }
        }


        public uint[] styleOpeningTagArray
        {
            get { return m_OpeningTagArray; }
        }


        public uint[] styleClosingTagArray
        {
            get { return m_ClosingTagArray; }
        }


        [SerializeField] private string m_Name;

        [SerializeField] private int m_HashCode;

        [SerializeField] private string m_OpeningDefinition;

        [SerializeField] private string m_ClosingDefinition;

        [SerializeField] private uint[] m_OpeningTagArray;

        [SerializeField] private uint[] m_ClosingTagArray;


        internal TMP_Style(string styleName, string styleOpeningDefinition, string styleClosingDefinition)
        {
            m_Name = styleName;
            m_HashCode = TMP_TextParsingUtilities.GetHashCode(styleName);
            m_OpeningDefinition = styleOpeningDefinition;
            m_ClosingDefinition = styleClosingDefinition;

            RefreshStyle();
        }


        public void RefreshStyle()
        {
            m_HashCode = TMP_TextParsingUtilities.GetHashCode(m_Name);

            int s1 = m_OpeningDefinition.Length;
            m_OpeningTagArray = new uint[s1];

            for (int i = 0; i < s1; i++)
            {
                m_OpeningTagArray[i] = m_OpeningDefinition[i];
            }

            int s2 = m_ClosingDefinition.Length;
            m_ClosingTagArray = new uint[s2];

            for (int i = 0; i < s2; i++)
            {
                m_ClosingTagArray[i] = m_ClosingDefinition[i];
            }
        }
    }
}