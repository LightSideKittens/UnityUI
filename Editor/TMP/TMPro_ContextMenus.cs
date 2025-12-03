using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace TMPro.EditorUtilities
{

    public class TMP_ContextMenus : Editor
    {

        private static Texture m_copiedTexture;

        private static Material m_copiedProperties;
        private static Material m_copiedAtlasProperties;


#if !TEXTCORE_1_0_OR_NEWER
        [MenuItem("CONTEXT/Texture/Copy", false, 2000)]
        static void CopyTexture(MenuCommand command)
        {
            m_copiedTexture = command.context as Texture;
        }


        [MenuItem("CONTEXT/Material/Select Material", false, 500)]
        static void SelectMaterial(MenuCommand command)
        {
            Material mat = command.context as Material;

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(mat);
        }
        #endif


        [MenuItem("CONTEXT/Material/Create Material Preset", false)]
        static void DuplicateMaterial(MenuCommand command)
        {
            Material source_Mat = (Material)command.context;
            if (!EditorUtility.IsPersistent(source_Mat))
            {
                Debug.LogWarning("Material is an instance and cannot be converted into a persistent asset.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(source_Mat).Split('.')[0];

            if (assetPath.IndexOf("Assets/", System.StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                Debug.LogWarning("Material Preset cannot be created from a material that is located outside the project.");
                return;
            }

            Material duplicate = new Material(source_Mat);

            duplicate.shaderKeywords = source_Mat.shaderKeywords;

            AssetDatabase.CreateAsset(duplicate, AssetDatabase.GenerateUniqueAssetPath(assetPath + ".mat"));

            GameObject[] selectedObjects = Selection.gameObjects;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                TMP_Text textObject = selectedObjects[i].GetComponent<TMP_Text>();

                if (textObject != null)
                {
                    textObject.fontSharedMaterial = duplicate;
                }
            }

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(duplicate);
        }


#if !TEXTCORE_1_0_OR_NEWER
        [MenuItem("CONTEXT/Material/Copy Material Properties", false)]
        static void CopyMaterialProperties(MenuCommand command)
        {
            Material mat = null;
            if (command.context.GetType() == typeof(Material))
                mat = (Material)command.context;
            else
            {
                mat = Selection.activeGameObject.GetComponent<CanvasRenderer>().GetMaterial();
            }

            m_copiedProperties = new Material(mat);

            m_copiedProperties.shaderKeywords = mat.shaderKeywords;

            m_copiedProperties.hideFlags = HideFlags.DontSave;
        }


        [MenuItem("CONTEXT/Material/Paste Material Properties", true)]
        static bool PasteMaterialPropertiesValidate(MenuCommand command)
        {
            if (m_copiedProperties == null)
                return false;

            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/Material/Paste Material Properties", false)]
        static void PasteMaterialProperties(MenuCommand command)
        {
            if (m_copiedProperties == null)
            {
                Debug.LogWarning("No Material Properties to Paste. Use Copy Material Properties first.");
                return;
            }

            Material mat = null;
            if (command.context.GetType() == typeof(Material))
                mat = (Material)command.context;
            else
            {
                mat = Selection.activeGameObject.GetComponent<CanvasRenderer>().GetMaterial();
            }

            Undo.RecordObject(mat, "Paste Material");

            ShaderUtilities.GetShaderPropertyIDs(); 
            if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                m_copiedProperties.SetTexture(ShaderUtilities.ID_MainTex, mat.GetTexture(ShaderUtilities.ID_MainTex));
                m_copiedProperties.SetFloat(ShaderUtilities.ID_GradientScale, mat.GetFloat(ShaderUtilities.ID_GradientScale));
                m_copiedProperties.SetFloat(ShaderUtilities.ID_TextureWidth, mat.GetFloat(ShaderUtilities.ID_TextureWidth));
                m_copiedProperties.SetFloat(ShaderUtilities.ID_TextureHeight, mat.GetFloat(ShaderUtilities.ID_TextureHeight));
            }

            EditorShaderUtilities.CopyMaterialProperties(m_copiedProperties, mat);

            mat.shaderKeywords = m_copiedProperties.shaderKeywords;

            TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
        }


        [MenuItem("CONTEXT/Material/Reset", true, 2100)]
        static bool ResetSettingsValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/Material/Reset", false, 2100)]
        static void ResetSettings(MenuCommand command)
        {
            Material mat = null;
            if (command.context.GetType() == typeof(Material))
                mat = (Material)command.context;
            else
            {
                mat = Selection.activeGameObject.GetComponent<CanvasRenderer>().GetMaterial();
            }

            Undo.RecordObject(mat, "Reset Material");

            ShaderUtilities.GetShaderPropertyIDs(); 
            if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                bool isSRPShader = mat.HasProperty(ShaderUtilities.ID_IsoPerimeter);

                var texture = mat.GetTexture(ShaderUtilities.ID_MainTex);
                var gradientScale = mat.GetFloat(ShaderUtilities.ID_GradientScale);

                float texWidth = 0, texHeight = 0;
                float normalWeight = 0, boldWeight = 0;

                if (!isSRPShader)
                {
                    texWidth = mat.GetFloat(ShaderUtilities.ID_TextureWidth);
                    texHeight = mat.GetFloat(ShaderUtilities.ID_TextureHeight);
                    normalWeight = mat.GetFloat(ShaderUtilities.ID_WeightNormal);
                    boldWeight = mat.GetFloat(ShaderUtilities.ID_WeightBold);
                }

                var stencilId = 0.0f;
                var stencilComp = 0.0f;

                if (mat.HasProperty(ShaderUtilities.ID_StencilID))
                {
                    stencilId = mat.GetFloat(ShaderUtilities.ID_StencilID);
                    stencilComp = mat.GetFloat(ShaderUtilities.ID_StencilComp);
                }

                Unsupported.SmartReset(mat);

                mat.shaderKeywords = new string[0]; 

                mat.SetTexture(ShaderUtilities.ID_MainTex, texture);
                mat.SetFloat(ShaderUtilities.ID_GradientScale, gradientScale);

                if (!isSRPShader)
                {
                    mat.SetFloat(ShaderUtilities.ID_TextureWidth, texWidth);
                    mat.SetFloat(ShaderUtilities.ID_TextureHeight, texHeight);
                    mat.SetFloat(ShaderUtilities.ID_WeightNormal, normalWeight);
                    mat.SetFloat(ShaderUtilities.ID_WeightBold, boldWeight);
                }

                if (mat.HasProperty(ShaderUtilities.ID_StencilID))
                {
                    mat.SetFloat(ShaderUtilities.ID_StencilID, stencilId);
                    mat.SetFloat(ShaderUtilities.ID_StencilComp, stencilComp);
                }
            }
            else
            {
                Unsupported.SmartReset(mat);
            }

            TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
        }


        [MenuItem("CONTEXT/Material/Copy Atlas", false, 2000)]
        static void CopyAtlas(MenuCommand command)
        {
            Material mat = command.context as Material;

            m_copiedAtlasProperties = new Material(mat);
            m_copiedAtlasProperties.hideFlags = HideFlags.DontSave;
        }


        [MenuItem("CONTEXT/Material/Paste Atlas", true, 2001)]
        static bool PasteAtlasValidate(MenuCommand command)
        {
            if (m_copiedAtlasProperties == null && m_copiedTexture == null)
                return false;

            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/Material/Paste Atlas", false, 2001)]
        static void PasteAtlas(MenuCommand command)
        {
            Material mat = command.context as Material;

            if (mat == null)
                return;

            if (m_copiedAtlasProperties != null)
            {
                Undo.RecordObject(mat, "Paste Texture");

                ShaderUtilities.GetShaderPropertyIDs(); 

                if (m_copiedAtlasProperties.HasProperty(ShaderUtilities.ID_MainTex))
                    mat.SetTexture(ShaderUtilities.ID_MainTex, m_copiedAtlasProperties.GetTexture(ShaderUtilities.ID_MainTex));

                if (m_copiedAtlasProperties.HasProperty(ShaderUtilities.ID_GradientScale))
                {
                    mat.SetFloat(ShaderUtilities.ID_GradientScale, m_copiedAtlasProperties.GetFloat(ShaderUtilities.ID_GradientScale));
                    mat.SetFloat(ShaderUtilities.ID_TextureWidth, m_copiedAtlasProperties.GetFloat(ShaderUtilities.ID_TextureWidth));
                    mat.SetFloat(ShaderUtilities.ID_TextureHeight, m_copiedAtlasProperties.GetFloat(ShaderUtilities.ID_TextureHeight));
                }
            }
            else if (m_copiedTexture != null)
            {
                Undo.RecordObject(mat, "Paste Texture");

                mat.SetTexture(ShaderUtilities.ID_MainTex, m_copiedTexture);
            }
        }
        #endif


        [MenuItem("CONTEXT/TMP_FontAsset/Extract Atlas", false, 2200)]
        static void ExtractAtlas(MenuCommand command)
        {
            TMP_FontAsset font = command.context as TMP_FontAsset;

            string fontPath = AssetDatabase.GetAssetPath(font);
            string texPath = Path.GetDirectoryName(fontPath) + "/" + Path.GetFileNameWithoutExtension(fontPath) + " Atlas.png";

            SerializedObject texprop = new SerializedObject(font.material.GetTexture(ShaderUtilities.ID_MainTex));
            texprop.FindProperty("m_IsReadable").boolValue = true;
            texprop.ApplyModifiedProperties();

            Texture2D tex = Instantiate(font.material.GetTexture(ShaderUtilities.ID_MainTex)) as Texture2D;

            texprop.FindProperty("m_IsReadable").boolValue = false;
            texprop.ApplyModifiedProperties();

            Debug.Log(texPath);
            var pngData = tex.EncodeToPNG();
            File.WriteAllBytes(texPath, pngData);

            AssetDatabase.Refresh();
            DestroyImmediate(tex);
        }

        /// <param name="command"></param>
        [MenuItem("CONTEXT/TMP_FontAsset/Update Atlas Texture...", false, 2000)]
        static void RegenerateFontAsset(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset != null)
            {
                TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(fontAsset);
            }
        }


        /// <param name="command"></param>
        [MenuItem("CONTEXT/TMP_FontAsset/Reset", true, 100)]
        static bool ClearFontAssetDataValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/TMP_FontAsset/Reset", false, 100)]
        static void ClearFontAssetData(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            fontAsset.ClearFontAssetData(true);

            TMP_ResourceManager.RebuildFontAssetCache();

            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        /// <param name="command"></param>
        [MenuItem("CONTEXT/TMP_FontAsset/Clear Dynamic Data", true, 2100)]
        static bool ClearFontCharacterDataValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/TMP_FontAsset/Clear Dynamic Data", false, 2100)]
        static void ClearFontCharacterData(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            fontAsset.ClearCharacterAndGlyphTablesInternal();

            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        [MenuItem("CONTEXT/TMP_FontAsset/Reset FaceInfo", priority = 101)]
        static void ResetFaceInfo(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            if (fontAsset.LoadFontFace() != FontEngineError.Success)
                return;

            fontAsset.faceInfo = FontEngine.GetFaceInfo();
            TextResourceManager.RebuildFontAssetCache();
            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
            AssetDatabase.Refresh();
        }

        /// <param name="command"></param>
        #if TEXTCORE_FONT_ENGINE_1_5_OR_NEWER
        [MenuItem("CONTEXT/TMP_FontAsset/Import Font Features", true, 2110)]
        static bool ReimportFontFeaturesValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/TMP_FontAsset/Import Font Features", false, 2110)]
        static void ReimportFontFeatures(MenuCommand command)
        {
            TMP_FontAsset fontAsset = command.context as TMP_FontAsset;

            if (fontAsset == null)
                return;

            if (Selection.activeObject != fontAsset)
                Selection.activeObject = fontAsset;

            fontAsset.ImportFontFeatures();

            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }
        #endif

        /// <param name="command"></param>
        [MenuItem("CONTEXT/TrueTypeFontImporter/Create TMP Font Asset...", false, 200)]
        static void CreateFontAsset(MenuCommand command)
        {
            TrueTypeFontImporter importer = command.context as TrueTypeFontImporter;

            if (importer != null)
            {
                Font sourceFontFile = AssetDatabase.LoadAssetAtPath<Font>(importer.assetPath);

                if (sourceFontFile)
                    TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(sourceFontFile);
            }
        }
    }
}
