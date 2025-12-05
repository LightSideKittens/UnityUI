using System.Collections.Generic;
using UnityEngine;


namespace TMPro
{
    public class MaterialReferenceManager
    {
        private static MaterialReferenceManager s_Instance;

        private Dictionary<int, Material> m_FontMaterialReferenceLookup = new();
        private Dictionary<int, TMP_FontAsset> m_FontAssetReferenceLookup = new();
        private Dictionary<int, TMP_ColorGradient> m_ColorGradientReferenceLookup = new();


        public static MaterialReferenceManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new();
                return s_Instance;
            }
        }


        public static void AddFontAsset(TMP_FontAsset fontAsset)
        {
            instance.AddFontAssetInternal(fontAsset);
        }


        private void AddFontAssetInternal(TMP_FontAsset fontAsset)
        {
            if (m_FontAssetReferenceLookup.ContainsKey(fontAsset.hashCode)) return;

            m_FontAssetReferenceLookup.Add(fontAsset.hashCode, fontAsset);

            m_FontMaterialReferenceLookup.Add(fontAsset.materialHashCode, fontAsset.material);
        }


        public static void AddFontMaterial(int hashCode, Material material)
        {
            instance.AddFontMaterialInternal(hashCode, material);
        }


        private void AddFontMaterialInternal(int hashCode, Material material)
        {
            m_FontMaterialReferenceLookup.Add(hashCode, material);
        }


        public static void AddColorGradientPreset(int hashCode, TMP_ColorGradient spriteAsset)
        {
            instance.AddColorGradientPreset_Internal(hashCode, spriteAsset);
        }


        private void AddColorGradientPreset_Internal(int hashCode, TMP_ColorGradient spriteAsset)
        {
            if (m_ColorGradientReferenceLookup.ContainsKey(hashCode)) return;

            m_ColorGradientReferenceLookup.Add(hashCode, spriteAsset);
        }


        public bool Contains(TMP_FontAsset font)
        {
            return m_FontAssetReferenceLookup.ContainsKey(font.hashCode);
        }


        public static bool TryGetFontAsset(int hashCode, out TMP_FontAsset fontAsset)
        {
            return instance.TryGetFontAssetInternal(hashCode, out fontAsset);
        }


        private bool TryGetFontAssetInternal(int hashCode, out TMP_FontAsset fontAsset)
        {
            fontAsset = null;

            return m_FontAssetReferenceLookup.TryGetValue(hashCode, out fontAsset);
        }


        public static bool TryGetColorGradientPreset(int hashCode, out TMP_ColorGradient gradientPreset)
        {
            return instance.TryGetColorGradientPresetInternal(hashCode, out gradientPreset);
        }


        private bool TryGetColorGradientPresetInternal(int hashCode, out TMP_ColorGradient gradientPreset)
        {
            gradientPreset = null;

            return m_ColorGradientReferenceLookup.TryGetValue(hashCode, out gradientPreset);
        }


        public static bool TryGetMaterial(int hashCode, out Material material)
        {
            return instance.TryGetMaterialInternal(hashCode, out material);
        }


        private bool TryGetMaterialInternal(int hashCode, out Material material)
        {
            material = null;

            return m_FontMaterialReferenceLookup.TryGetValue(hashCode, out material);
        }
    }


    public struct TMP_MaterialReference
    {
        public Material material;
        public int referenceCount;
    }


    public struct MaterialReference
    {
        public int index;
        public TMP_FontAsset fontAsset;
        public Material material;
        public bool isDefaultMaterial;
        public bool isFallbackMaterial;
        public Material fallbackMaterial;
        public float padding;
        public int referenceCount;


        public MaterialReference(int index, TMP_FontAsset fontAsset, Material material, float padding)
        {
            this.index = index;
            this.fontAsset = fontAsset;
            this.material = material;
            isDefaultMaterial = material.GetInstanceID() == fontAsset.material.GetInstanceID();
            isFallbackMaterial = false;
            fallbackMaterial = null;
            this.padding = padding;
            referenceCount = 0;
        }


        public static bool Contains(MaterialReference[] materialReferences, TMP_FontAsset fontAsset)
        {
            int id = fontAsset.GetInstanceID();

            for (int i = 0; i < materialReferences.Length && materialReferences[i].fontAsset != null; i++)
            {
                if (materialReferences[i].fontAsset.GetInstanceID() == id)
                    return true;
            }

            return false;
        }


        public static int AddMaterialReference(Material material,
            TMP_FontAsset fontAsset, ref MaterialReference[] materialReferences,
            Dictionary<int, int> materialReferenceIndexLookup)
        {
            int materialID = material.GetInstanceID();

            if (materialReferenceIndexLookup.TryGetValue(materialID, out var index))
                return index;

            index = materialReferenceIndexLookup.Count;

            materialReferenceIndexLookup[materialID] = index;

            if (index >= materialReferences.Length)
                System.Array.Resize(ref materialReferences, Mathf.NextPowerOfTwo(index + 1));

            materialReferences[index].index = index;
            materialReferences[index].fontAsset = fontAsset;
            materialReferences[index].material = material;
            materialReferences[index].isDefaultMaterial = materialID == fontAsset.material.GetInstanceID();
            materialReferences[index].referenceCount = 0;

            return index;
        }


        public static int AddMaterialReference(Material material, ref MaterialReference[] materialReferences,
            Dictionary<int, int> materialReferenceIndexLookup)
        {
            int materialID = material.GetInstanceID();

            if (materialReferenceIndexLookup.TryGetValue(materialID, out var index))
                return index;

            index = materialReferenceIndexLookup.Count;

            materialReferenceIndexLookup[materialID] = index;

            if (index >= materialReferences.Length)
                System.Array.Resize(ref materialReferences, Mathf.NextPowerOfTwo(index + 1));

            materialReferences[index].index = index;
            materialReferences[index].fontAsset = materialReferences[0].fontAsset;
            materialReferences[index].material = material;
            materialReferences[index].isDefaultMaterial = true;
            materialReferences[index].referenceCount = 0;

            return index;
        }
    }
}