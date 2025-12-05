using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


namespace TMPro
{
    public static class TMP_MaterialManager
    {
        private static List<MaskingMaterial> m_materialList = new();

        private static Dictionary<long, FallbackMaterial> m_fallbackMaterials = new();
        private static Dictionary<int, long> m_fallbackMaterialLookup = new();
        private static List<FallbackMaterial> m_fallbackCleanupList = new();

        private static bool isFallbackListDirty;

        static TMP_MaterialManager()
        {
            Canvas.willRenderCanvases += OnPreRender;
        }

        private static void OnPreRender()
        {
            if (isFallbackListDirty)
            {
                CleanupFallbackMaterials();
                isFallbackListDirty = false;
            }
        }


        public static Material GetStencilMaterial(Material baseMaterial, int stencilID)
        {
            if (!baseMaterial.HasProperty(ShaderUtilities.ID_StencilID))
            {
                Debug.LogWarning(
                    "Selected Shader does not support Stencil Masking. Please select the Distance Field or Mobile Distance Field Shader.");
                return baseMaterial;
            }

            int baseMaterialID = baseMaterial.GetInstanceID();

            for (int i = 0; i < m_materialList.Count; i++)
            {
                if (m_materialList[i].baseMaterial.GetInstanceID() == baseMaterialID &&
                    m_materialList[i].stencilID == stencilID)
                {
                    m_materialList[i].count += 1;

#if TMP_DEBUG_MODE
                    ListMaterials();
#endif

                    return m_materialList[i].stencilMaterial;
                }
            }

            Material stencilMaterial;

            stencilMaterial = new(baseMaterial);
            stencilMaterial.hideFlags = HideFlags.HideAndDontSave;

#if UNITY_EDITOR
            stencilMaterial.name += " Masking ID:" + stencilID;
#endif

            stencilMaterial.shaderKeywords = baseMaterial.shaderKeywords;

            ShaderUtilities.GetShaderPropertyIDs();
            stencilMaterial.SetFloat(ShaderUtilities.ID_StencilID, stencilID);
            stencilMaterial.SetFloat(ShaderUtilities.ID_StencilComp, 4);

            MaskingMaterial temp = new();
            temp.baseMaterial = baseMaterial;
            temp.stencilMaterial = stencilMaterial;
            temp.stencilID = stencilID;
            temp.count = 1;

            m_materialList.Add(temp);

#if TMP_DEBUG_MODE
            ListMaterials();
#endif

            return stencilMaterial;
        }


        public static void ReleaseStencilMaterial(Material stencilMaterial)
        {
            int stencilMaterialID = stencilMaterial.GetInstanceID();

            for (int i = 0; i < m_materialList.Count; i++)
            {
                if (m_materialList[i].stencilMaterial.GetInstanceID() == stencilMaterialID)
                {
                    if (m_materialList[i].count > 1)
                        m_materialList[i].count -= 1;
                    else
                    {
                        Object.DestroyImmediate(m_materialList[i].stencilMaterial);
                        m_materialList.RemoveAt(i);
                        stencilMaterial = null;
                    }

                    break;
                }
            }


#if TMP_DEBUG_MODE
            ListMaterials();
#endif
        }


        public static Material GetBaseMaterial(Material stencilMaterial)
        {
            int index = m_materialList.FindIndex(item => item.stencilMaterial == stencilMaterial);

            if (index == -1)
                return null;
            else
                return m_materialList[index].baseMaterial;
        }


        public static Material SetStencil(Material material, int stencilID)
        {
            material.SetFloat(ShaderUtilities.ID_StencilID, stencilID);

            if (stencilID == 0)
                material.SetFloat(ShaderUtilities.ID_StencilComp, 8);
            else
                material.SetFloat(ShaderUtilities.ID_StencilComp, 4);

            return material;
        }


        public static void AddMaskingMaterial(Material baseMaterial, Material stencilMaterial, int stencilID)
        {
            int index = m_materialList.FindIndex(item => item.stencilMaterial == stencilMaterial);

            if (index == -1)
            {
                MaskingMaterial temp = new();
                temp.baseMaterial = baseMaterial;
                temp.stencilMaterial = stencilMaterial;
                temp.stencilID = stencilID;
                temp.count = 1;

                m_materialList.Add(temp);
            }
            else
            {
                stencilMaterial = m_materialList[index].stencilMaterial;
                m_materialList[index].count += 1;
            }
        }


        public static void RemoveStencilMaterial(Material stencilMaterial)
        {
            int index = m_materialList.FindIndex(item => item.stencilMaterial == stencilMaterial);

            if (index != -1)
            {
                m_materialList.RemoveAt(index);
            }

#if TMP_DEBUG_MODE
            ListMaterials();
#endif
        }


        public static void ReleaseBaseMaterial(Material baseMaterial)
        {
            int index = m_materialList.FindIndex(item => item.baseMaterial == baseMaterial);

            if (index == -1)
            {
                Debug.Log("No Masking Material exists for " + baseMaterial.name);
            }
            else
            {
                if (m_materialList[index].count > 1)
                {
                    m_materialList[index].count -= 1;
                    Debug.Log("Removed (1) reference to " + m_materialList[index].stencilMaterial.name +
                              ". There are " + m_materialList[index].count + " references left.");
                }
                else
                {
                    Debug.Log("Removed last reference to " + m_materialList[index].stencilMaterial.name + " with ID " +
                              m_materialList[index].stencilMaterial.GetInstanceID());
                    Object.DestroyImmediate(m_materialList[index].stencilMaterial);
                    m_materialList.RemoveAt(index);
                }
            }

#if TMP_DEBUG_MODE
            ListMaterials();
#endif
        }


        public static void ClearMaterials()
        {
            if (m_materialList.Count == 0)
            {
                Debug.Log("Material List has already been cleared.");
                return;
            }

            for (int i = 0; i < m_materialList.Count; i++)
            {
                Material stencilMaterial = m_materialList[i].stencilMaterial;

                Object.DestroyImmediate(stencilMaterial);
            }

            m_materialList.Clear();
        }


        public static int GetStencilID(GameObject obj)
        {
            var count = 0;

            var transform = obj.transform;
            var stopAfter = FindRootSortOverrideCanvas(transform);
            if (transform == stopAfter)
                return count;

            var t = transform.parent;
            var components = TMP_ListPool<Mask>.Get();
            while (t != null)
            {
                t.GetComponents<Mask>(components);
                for (var i = 0; i < components.Count; ++i)
                {
                    var mask = components[i];
                    if (mask != null && mask.MaskEnabled() && mask.graphic.IsActive())
                    {
                        ++count;
                        break;
                    }
                }

                if (t == stopAfter)
                    break;

                t = t.parent;
            }

            TMP_ListPool<Mask>.Release(components);

            return Mathf.Min((1 << count) - 1, 255);
        }


        public static Material GetMaterialForRendering(MaskableGraphic graphic, Material baseMaterial)
        {
            if (baseMaterial == null)
                return null;

            var modifiers = TMP_ListPool<IMaterialModifier>.Get();
            graphic.GetComponents(modifiers);

            var result = baseMaterial;
            for (int i = 0; i < modifiers.Count; i++)
                result = modifiers[i].GetModifiedMaterial(result);

            TMP_ListPool<IMaterialModifier>.Release(modifiers);

            return result;
        }

        private static Transform FindRootSortOverrideCanvas(Transform start)
        {
            var canvasList = TMP_ListPool<Canvas>.Get();
            start.GetComponentsInParent(false, canvasList);
            Canvas canvas = null;

            for (int i = 0; i < canvasList.Count; ++i)
            {
                canvas = canvasList[i];

                if (canvas.overrideSorting)
                    break;
            }

            TMP_ListPool<Canvas>.Release(canvasList);

            return canvas != null ? canvas.transform : null;
        }


        internal static Material GetFallbackMaterial(TMP_FontAsset fontAsset, Material sourceMaterial, int atlasIndex)
        {
            int sourceMaterialID = sourceMaterial.GetInstanceID();
            Texture tex = fontAsset.atlasTextures[atlasIndex];
            int texID = tex.GetInstanceID();
            long key = (long)sourceMaterialID << 32 | (long)(uint)texID;

            if (m_fallbackMaterials.TryGetValue(key, out var fallback))
            {
                int sourceMaterialCRC = sourceMaterial.ComputeCRC();
                if (sourceMaterialCRC == fallback.sourceMaterialCRC)
                    return fallback.fallbackMaterial;

                CopyMaterialPresetProperties(sourceMaterial, fallback.fallbackMaterial);
                fallback.sourceMaterialCRC = sourceMaterialCRC;
                return fallback.fallbackMaterial;
            }

            Material fallbackMaterial = new(sourceMaterial);
            fallbackMaterial.SetTexture(ShaderUtilities.ID_MainTex, tex);

            fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

#if UNITY_EDITOR
            fallbackMaterial.name += " + " + tex.name;
#endif

            fallback = new();
            fallback.fallbackID = key;
            fallback.sourceMaterial = fontAsset.material;
            fallback.sourceMaterialCRC = sourceMaterial.ComputeCRC();
            fallback.fallbackMaterial = fallbackMaterial;
            fallback.count = 0;

            m_fallbackMaterials.Add(key, fallback);
            m_fallbackMaterialLookup.Add(fallbackMaterial.GetInstanceID(), key);

#if TMP_DEBUG_MODE
            ListFallbackMaterials();
#endif

            return fallbackMaterial;
        }


        public static Material GetFallbackMaterial(Material sourceMaterial, Material targetMaterial)
        {
            int sourceID = sourceMaterial.GetInstanceID();
            Texture tex = targetMaterial.GetTexture(ShaderUtilities.ID_MainTex);
            int texID = tex.GetInstanceID();
            long key = (long)sourceID << 32 | (long)(uint)texID;

            if (m_fallbackMaterials.TryGetValue(key, out var fallback))
            {
                int sourceMaterialCRC = sourceMaterial.ComputeCRC();
                if (sourceMaterialCRC == fallback.sourceMaterialCRC)
                    return fallback.fallbackMaterial;

                CopyMaterialPresetProperties(sourceMaterial, fallback.fallbackMaterial);
                fallback.sourceMaterialCRC = sourceMaterialCRC;
                return fallback.fallbackMaterial;
            }

            Material fallbackMaterial;
            if (sourceMaterial.HasProperty(ShaderUtilities.ID_GradientScale) &&
                targetMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                fallbackMaterial = new(sourceMaterial);
                fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

#if UNITY_EDITOR
                fallbackMaterial.name += " + " + tex.name;
#endif

                fallbackMaterial.SetTexture(ShaderUtilities.ID_MainTex, tex);
                fallbackMaterial.SetFloat(ShaderUtilities.ID_GradientScale,
                    targetMaterial.GetFloat(ShaderUtilities.ID_GradientScale));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_TextureWidth,
                    targetMaterial.GetFloat(ShaderUtilities.ID_TextureWidth));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_TextureHeight,
                    targetMaterial.GetFloat(ShaderUtilities.ID_TextureHeight));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_WeightNormal,
                    targetMaterial.GetFloat(ShaderUtilities.ID_WeightNormal));
                fallbackMaterial.SetFloat(ShaderUtilities.ID_WeightBold,
                    targetMaterial.GetFloat(ShaderUtilities.ID_WeightBold));
            }
            else
            {
                fallbackMaterial = new(targetMaterial);
                fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

#if UNITY_EDITOR
                fallbackMaterial.name += " + " + tex.name;
#endif
            }

            fallback = new();
            fallback.fallbackID = key;
            fallback.sourceMaterial = sourceMaterial;
            fallback.sourceMaterialCRC = sourceMaterial.ComputeCRC();
            fallback.fallbackMaterial = fallbackMaterial;
            fallback.count = 0;

            m_fallbackMaterials.Add(key, fallback);
            m_fallbackMaterialLookup.Add(fallbackMaterial.GetInstanceID(), key);

#if TMP_DEBUG_MODE
            ListFallbackMaterials();
#endif

            return fallbackMaterial;
        }


        public static void AddFallbackMaterialReference(Material targetMaterial)
        {
            if (targetMaterial == null) return;

            int sourceID = targetMaterial.GetInstanceID();

            if (m_fallbackMaterialLookup.TryGetValue(sourceID, out var key))
            {
                if (m_fallbackMaterials.TryGetValue(key, out var fallback))
                {
                    fallback.count += 1;
                }
            }
        }


        public static void RemoveFallbackMaterialReference(Material targetMaterial)
        {
            if (targetMaterial == null) return;

            int sourceID = targetMaterial.GetInstanceID();

            if (m_fallbackMaterialLookup.TryGetValue(sourceID, out var key))
            {
                if (m_fallbackMaterials.TryGetValue(key, out var fallback))
                {
                    fallback.count -= 1;

                    if (fallback.count < 1)
                        m_fallbackCleanupList.Add(fallback);
                }
            }
        }


        public static void CleanupFallbackMaterials()
        {
            if (m_fallbackCleanupList.Count == 0) return;

            for (int i = 0; i < m_fallbackCleanupList.Count; i++)
            {
                FallbackMaterial fallback = m_fallbackCleanupList[i];

                if (fallback.count < 1)
                {
                    Material mat = fallback.fallbackMaterial;
                    m_fallbackMaterials.Remove(fallback.fallbackID);
                    m_fallbackMaterialLookup.Remove(mat.GetInstanceID());
                    Object.DestroyImmediate(mat);
                    mat = null;
                }
            }

            m_fallbackCleanupList.Clear();
        }


        public static void ReleaseFallbackMaterial(Material fallbackMaterial)
        {
            if (fallbackMaterial == null) return;

            int materialID = fallbackMaterial.GetInstanceID();

            if (m_fallbackMaterialLookup.TryGetValue(materialID, out var key))
            {
                if (m_fallbackMaterials.TryGetValue(key, out var fallback))
                {
                    fallback.count -= 1;

                    if (fallback.count < 1)
                        m_fallbackCleanupList.Add(fallback);
                }
            }

            isFallbackListDirty = true;

#if TMP_DEBUG_MODE
            ListFallbackMaterials();
#endif
        }


        private class FallbackMaterial
        {
            public long fallbackID;
            public Material sourceMaterial;
            internal int sourceMaterialCRC;
            public Material fallbackMaterial;
            public int count;
        }


        private class MaskingMaterial
        {
            public Material baseMaterial;
            public Material stencilMaterial;
            public int count;
            public int stencilID;
        }


        public static void CopyMaterialPresetProperties(Material source, Material destination)
        {
            if (!source.HasProperty(ShaderUtilities.ID_GradientScale) ||
                !destination.HasProperty(ShaderUtilities.ID_GradientScale))
                return;

            Texture dst_texture = destination.GetTexture(ShaderUtilities.ID_MainTex);
            float dst_gradientScale = destination.GetFloat(ShaderUtilities.ID_GradientScale);
            float dst_texWidth = destination.GetFloat(ShaderUtilities.ID_TextureWidth);
            float dst_texHeight = destination.GetFloat(ShaderUtilities.ID_TextureHeight);
            float dst_weightNormal = destination.GetFloat(ShaderUtilities.ID_WeightNormal);
            float dst_weightBold = destination.GetFloat(ShaderUtilities.ID_WeightBold);

            destination.shader = source.shader;

            destination.CopyPropertiesFromMaterial(source);

            destination.shaderKeywords = source.shaderKeywords;

            destination.SetTexture(ShaderUtilities.ID_MainTex, dst_texture);
            destination.SetFloat(ShaderUtilities.ID_GradientScale, dst_gradientScale);
            destination.SetFloat(ShaderUtilities.ID_TextureWidth, dst_texWidth);
            destination.SetFloat(ShaderUtilities.ID_TextureHeight, dst_texHeight);
            destination.SetFloat(ShaderUtilities.ID_WeightNormal, dst_weightNormal);
            destination.SetFloat(ShaderUtilities.ID_WeightBold, dst_weightBold);
        }


#if TMP_DEBUG_MODE
        public static void ListMaterials()
        {

            if (m_materialList.Count == 0)
            {
                Debug.Log("Material List is empty.");
                return;
            }

            for (int i = 0; i < m_materialList.Count; i++)
            {
                Material baseMaterial = m_materialList[i].baseMaterial;
                Material stencilMaterial = m_materialList[i].stencilMaterial;

                Debug.Log("Item #" + (i + 1) + " - Base Material is [" + baseMaterial.name + "] with ID " + baseMaterial.GetInstanceID() + " is associated with [" + (stencilMaterial != null ? stencilMaterial.name : "Null") + "] Stencil ID " + m_materialList[i].stencilID + " with ID " + (stencilMaterial != null ? stencilMaterial.GetInstanceID() : 0) + " and is referenced " + m_materialList[i].count + " time(s).");
            }
        }


        public static void ListFallbackMaterials()
        {

            if (m_fallbackMaterials.Count == 0)
            {
                Debug.Log("Material List is empty.");
                return;
            }

            Debug.Log("List contains " + m_fallbackMaterials.Count + " items.");

            int count = 0;
            foreach (var fallback in m_fallbackMaterials)
            {
                Material baseMaterial = fallback.Value.baseMaterial;
                Material fallbackMaterial = fallback.Value.fallbackMaterial;

                string output = "Item #" + (count++);
                if (baseMaterial != null)
                    output += " - Base Material is [" + baseMaterial.name + "] with ID " + baseMaterial.GetInstanceID();
                if (fallbackMaterial != null)
                    output +=
 " is associated with [" + fallbackMaterial.name + "] with ID " + fallbackMaterial.GetInstanceID() + " and is referenced " + fallback.Value.count + " time(s).";

                Debug.Log(output);
            }
        }
#endif
    }
}