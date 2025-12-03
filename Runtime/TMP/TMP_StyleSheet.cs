using UnityEngine;
using System;
using System.Collections.Generic;


namespace TMPro
{

    [Serializable][ExcludeFromPresetAttribute]
    public class TMP_StyleSheet : ScriptableObject
    {
        internal List<TMP_Style> styles
        {
            get { return m_StyleList; }
        }

        [SerializeField]
        private List<TMP_Style> m_StyleList = new(1);
        private Dictionary<int, TMP_Style> m_StyleLookupDictionary;

        private void Reset()
        {
            LoadStyleDictionaryInternal();
        }

        /// <param name="hashCode">Hash code of the style.</param>
        /// <returns>The style matching the hash code.</returns>
        public TMP_Style GetStyle(int hashCode)
        {
            if (m_StyleLookupDictionary == null)
                LoadStyleDictionaryInternal();

            if (m_StyleLookupDictionary.TryGetValue(hashCode, out var style))
                return style;

            return null;
        }

        /// <param name="name">The name of the style.</param>
        /// <returns>The style if found.</returns>
        public TMP_Style GetStyle(string name)
        {
            if (m_StyleLookupDictionary == null)
                LoadStyleDictionaryInternal();

            int hashCode = TMP_TextParsingUtilities.GetHashCode(name);

            if (m_StyleLookupDictionary.TryGetValue(hashCode, out var style))
                return style;

            return null;
        }

        public void RefreshStyles()
        {
            LoadStyleDictionaryInternal();
        }

        private void LoadStyleDictionaryInternal()
        {
            if (m_StyleLookupDictionary == null)
                m_StyleLookupDictionary = new();
            else
                m_StyleLookupDictionary.Clear();

            for (int i = 0; i < m_StyleList.Count; i++)
            {
                m_StyleList[i].RefreshStyle();

                if (!m_StyleLookupDictionary.ContainsKey(m_StyleList[i].hashCode))
                    m_StyleLookupDictionary.Add(m_StyleList[i].hashCode, m_StyleList[i]);
            }

            int normalStyleHashCode = TMP_TextParsingUtilities.GetHashCode("Normal");
            if (!m_StyleLookupDictionary.ContainsKey(normalStyleHashCode))
            {
                TMP_Style style = new("Normal", string.Empty, string.Empty);
                m_StyleList.Add(style);
                m_StyleLookupDictionary.Add(normalStyleHashCode, style);
            }

            #if UNITY_EDITOR
            TMPro_EventManager.ON_TEXT_STYLE_PROPERTY_CHANGED(true);
            #endif
        }
    }

}
