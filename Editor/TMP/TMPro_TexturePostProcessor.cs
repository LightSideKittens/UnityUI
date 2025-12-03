using System;
using UnityEngine;
using UnityEditor;


namespace TMPro.EditorUtilities
{
    internal class TMPro_TexturePostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Time.frameCount == 0)
                return;

            bool textureImported = false;

            foreach (var asset in importedAssets)
            {
                if (asset.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(asset);

                if (assetType == typeof(TMP_FontAsset))
                {
                    TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath(asset, typeof(TMP_FontAsset)) as TMP_FontAsset;

                    if (fontAsset != null && fontAsset.m_CharacterLookupDictionary != null)
                        TMP_EditorResourceManager.RegisterFontAssetForDefinitionRefresh(fontAsset);

                    continue;
                }

                if (assetType == typeof(Texture2D))
                    textureImported = true;
            }

            if (textureImported)
                TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, null);
        }
    }

    internal class TMP_FontAssetPostProcessor : UnityEditor.AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(TMP_FontAsset))
                TMP_ResourceManager.RebuildFontAssetCache();

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
