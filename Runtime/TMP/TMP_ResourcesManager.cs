using System.Collections.Generic;
using UnityEngine;


namespace TMPro
{
    public class TMP_ResourceManager
    {
        private static TMP_Settings s_TextSettings;

        internal static TMP_Settings GetTextSettings()
        {
            if (s_TextSettings == null)
            {
                s_TextSettings = Resources.Load<TMP_Settings>("TextSettings");

#if UNITY_EDITOR
                if (s_TextSettings == null)
                {
                    TMP_PackageResourceImporterWindow.ShowPackageImporterWindow();
                }
                #endif
            }

            return s_TextSettings;
        }

        private struct FontAssetRef
        {
            public int nameHashCode;
            public int familyNameHashCode;
            public int styleNameHashCode;
            public long familyNameAndStyleHashCode;
            public readonly TMP_FontAsset fontAsset;

            public FontAssetRef(int nameHashCode, int familyNameHashCode, int styleNameHashCode, TMP_FontAsset fontAsset)
            {
                this.nameHashCode = nameHashCode != 0 ? nameHashCode : familyNameHashCode;
                this.familyNameHashCode = familyNameHashCode;
                this.styleNameHashCode = styleNameHashCode;
                familyNameAndStyleHashCode = (long) styleNameHashCode << 32 | (uint) familyNameHashCode;
                this.fontAsset = fontAsset;
            }
        }

        private static readonly Dictionary<int, FontAssetRef> s_FontAssetReferences = new();
        private static readonly Dictionary<int, TMP_FontAsset> s_FontAssetNameReferenceLookup = new();
        private static readonly Dictionary<long, TMP_FontAsset> s_FontAssetFamilyNameAndStyleReferenceLookup = new();
        private static readonly List<int> s_FontAssetRemovalList = new(16);

        private static readonly int k_RegularStyleHashCode = TMP_TextUtilities.GetHashCode("Regular");

        /// <param name="fontAsset">Font asset to be added to the resource manager.</param>
        public static void AddFontAsset(TMP_FontAsset fontAsset)
        {
            int instanceID = fontAsset.instanceID;

            if (!s_FontAssetReferences.ContainsKey(instanceID))
            {
                FontAssetRef fontAssetRef = new(fontAsset.hashCode, fontAsset.familyNameHashCode, fontAsset.styleNameHashCode, fontAsset);
                s_FontAssetReferences.Add(instanceID, fontAssetRef);

                if (!s_FontAssetNameReferenceLookup.ContainsKey(fontAssetRef.nameHashCode))
                    s_FontAssetNameReferenceLookup.Add(fontAssetRef.nameHashCode, fontAsset);

                if (!s_FontAssetFamilyNameAndStyleReferenceLookup.ContainsKey(fontAssetRef.familyNameAndStyleHashCode))
                    s_FontAssetFamilyNameAndStyleReferenceLookup.Add(fontAssetRef.familyNameAndStyleHashCode, fontAsset);
            }
            else
            {
                FontAssetRef fontAssetRef = s_FontAssetReferences[instanceID];

                if (fontAssetRef.nameHashCode == fontAsset.hashCode && fontAssetRef.familyNameHashCode == fontAsset.familyNameHashCode && fontAssetRef.styleNameHashCode == fontAsset.styleNameHashCode)
                    return;

                if (fontAssetRef.nameHashCode != fontAsset.hashCode)
                {
                    s_FontAssetNameReferenceLookup.Remove(fontAssetRef.nameHashCode);

                    fontAssetRef.nameHashCode = fontAsset.hashCode;

                    if (!s_FontAssetNameReferenceLookup.ContainsKey(fontAssetRef.nameHashCode))
                        s_FontAssetNameReferenceLookup.Add(fontAssetRef.nameHashCode, fontAsset);
                }

                if (fontAssetRef.familyNameHashCode != fontAsset.familyNameHashCode || fontAssetRef.styleNameHashCode != fontAsset.styleNameHashCode)
                {
                    s_FontAssetFamilyNameAndStyleReferenceLookup.Remove(fontAssetRef.familyNameAndStyleHashCode);

                    fontAssetRef.familyNameHashCode = fontAsset.familyNameHashCode;
                    fontAssetRef.styleNameHashCode = fontAsset.styleNameHashCode;
                    fontAssetRef.familyNameAndStyleHashCode = (long) fontAsset.styleNameHashCode << 32 | (uint) fontAsset.familyNameHashCode;

                    if (!s_FontAssetFamilyNameAndStyleReferenceLookup.ContainsKey(fontAssetRef.familyNameAndStyleHashCode))
                        s_FontAssetFamilyNameAndStyleReferenceLookup.Add(fontAssetRef.familyNameAndStyleHashCode, fontAsset);
                }

                s_FontAssetReferences[instanceID] = fontAssetRef;
            }
        }

        /// <param name="fontAsset">Font asset to be removed from the resource manager.</param>
        public static void RemoveFontAsset(TMP_FontAsset fontAsset)
        {
            int instanceID = fontAsset.instanceID;

            if (s_FontAssetReferences.TryGetValue(instanceID, out FontAssetRef reference))
            {
                s_FontAssetNameReferenceLookup.Remove(reference.nameHashCode);
                s_FontAssetFamilyNameAndStyleReferenceLookup.Remove(reference.familyNameAndStyleHashCode);
                s_FontAssetReferences.Remove(instanceID);
            }
        }

        /// <param name="nameHashcode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        internal static bool TryGetFontAssetByName(int nameHashcode, out TMP_FontAsset fontAsset)
        {
            fontAsset = null;

            return s_FontAssetNameReferenceLookup.TryGetValue(nameHashcode, out fontAsset);
        }

        /// <param name="familyNameHashCode"></param>
        /// <param name="styleNameHashCode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        internal static bool TryGetFontAssetByFamilyName(int familyNameHashCode, int styleNameHashCode, out TMP_FontAsset fontAsset)
        {
            fontAsset = null;

            if (styleNameHashCode == 0)
                styleNameHashCode = k_RegularStyleHashCode;

            long familyAndStyleNameHashCode = (long) styleNameHashCode << 32 | (uint) familyNameHashCode;

            return s_FontAssetFamilyNameAndStyleReferenceLookup.TryGetValue(familyAndStyleNameHashCode, out fontAsset);
        }

        public static void ClearFontAssetGlyphCache()
        {
            RebuildFontAssetCache();
        }

        internal static void RebuildFontAssetCache()
        {
            foreach (var pair in s_FontAssetReferences)
            {
                FontAssetRef fontAssetRef = pair.Value;

                TMP_FontAsset fontAsset = fontAssetRef.fontAsset;

                if (fontAsset == null)
                {
                    s_FontAssetNameReferenceLookup.Remove(fontAssetRef.nameHashCode);
                    s_FontAssetFamilyNameAndStyleReferenceLookup.Remove(fontAssetRef.familyNameAndStyleHashCode);

                    s_FontAssetRemovalList.Add(pair.Key);
                    continue;
                }

                fontAsset.InitializeCharacterLookupDictionary();
                fontAsset.AddSynthesizedCharactersAndFaceMetrics();
            }

            for (int i = 0; i < s_FontAssetRemovalList.Count; i++)
            {
                s_FontAssetReferences.Remove(s_FontAssetRemovalList[i]);
            }
            s_FontAssetRemovalList.Clear();

            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, null);
        }
    }
}
