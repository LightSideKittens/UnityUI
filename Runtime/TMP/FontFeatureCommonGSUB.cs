using System;
using UnityEngine;

#pragma warning disable CS0660, CS0661

namespace TMPro
{
    [Serializable]
    public struct SingleSubstitutionRecord
    {
    }

    [Serializable]
    public struct MultipleSubstitutionRecord
    {
        public uint targetGlyphID { get { return m_TargetGlyphID; } set { m_TargetGlyphID = value; } }

        public uint[] substituteGlyphIDs { get { return m_SubstituteGlyphIDs; } set { m_SubstituteGlyphIDs = value; } }

        [SerializeField]
        private uint m_TargetGlyphID;

        [SerializeField]
        private uint[] m_SubstituteGlyphIDs;
    }

    [Serializable]
    public struct AlternateSubstitutionRecord
    {

    }

    [Serializable]
    public struct LigatureSubstitutionRecord
    {
        public uint[] componentGlyphIDs { get { return m_ComponentGlyphIDs; } set { m_ComponentGlyphIDs = value; } }

        public uint ligatureGlyphID { get { return m_LigatureGlyphID; } set { m_LigatureGlyphID = value; } }

        [SerializeField]
        private uint[] m_ComponentGlyphIDs;

        [SerializeField]
        private uint m_LigatureGlyphID;

        public static bool operator==(LigatureSubstitutionRecord lhs, LigatureSubstitutionRecord rhs)
        {
            if (lhs.ligatureGlyphID != rhs.m_LigatureGlyphID)
                return false;
            
            int lhsComponentCount = lhs.m_ComponentGlyphIDs.Length;
            
            if (lhsComponentCount != rhs.m_ComponentGlyphIDs.Length)
                return false;

            for (int i = 0; i < lhsComponentCount; i++)
            {
                if (lhs.m_ComponentGlyphIDs[i] != rhs.m_ComponentGlyphIDs[i])
                    return false;
            }

            return true;
        }

        public static bool operator!=(LigatureSubstitutionRecord lhs, LigatureSubstitutionRecord rhs)
        {
            return !(lhs == rhs);
        }
    }
}
