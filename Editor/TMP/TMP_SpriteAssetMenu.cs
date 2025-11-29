using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.U2D;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace TMPro.EditorUtilities
{
    public static class TMP_SpriteAssetMenu
    {
        [MenuItem("CONTEXT/TMP_SpriteAsset/Add Default Material", true, 2200)]
        static bool AddDefaultMaterialValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/TMP_SpriteAsset/Add Default Material", false, 2200)]
        static void AddDefaultMaterial(MenuCommand command)
        {
            TMP_SpriteAsset spriteAsset = (TMP_SpriteAsset)command.context;

            if (spriteAsset != null && spriteAsset.material == null)
            {
                AddDefaultMaterial(spriteAsset);
            }
        }

        [MenuItem("CONTEXT/TMP_SpriteAsset/Update Sprite Asset", true, 2100)]
        static bool UpdateSpriteAssetValidate(MenuCommand command)
        {
            return AssetDatabase.IsOpenForEdit(command.context);
        }

        [MenuItem("CONTEXT/TMP_SpriteAsset/Update Sprite Asset", false, 2100)]
        static void UpdateSpriteAsset(MenuCommand command)
        {
            TMP_SpriteAsset spriteAsset = (TMP_SpriteAsset)command.context;

            if (spriteAsset == null)
                return;

            UpdateSpriteAsset(spriteAsset);
        }


        internal static void UpdateSpriteAsset(TMP_SpriteAsset spriteAsset)
        {
            string filePath = AssetDatabase.GetAssetPath(spriteAsset.spriteSheet);

            if (string.IsNullOrEmpty(filePath))
                return;

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).ToArray();

            if (sprites.Length == 0)
            {
                Debug.Log("Sprite Asset <color=#FFFF80>[" + spriteAsset.name + "]</color>'s atlas texture does not appear to have any sprites defined in it. Use the Unity Sprite Editor to define sprites for this texture.", spriteAsset.spriteSheet);
                return;
            }

            List<TMP_SpriteGlyph> spriteGlyphTable = spriteAsset.spriteGlyphTable;

            uint[] existingGlyphIndexes = spriteGlyphTable.Select(x => x.index).ToArray();
            List<uint> availableGlyphIndexes = new List<uint>();

            uint lastGlyphIndex = existingGlyphIndexes.Length > 0 ? existingGlyphIndexes.Last() : 0;
            int elementIndex = 0;
            for (uint i = 0; i < lastGlyphIndex; i++)
            {
                uint existingGlyphIndex = existingGlyphIndexes[elementIndex];

                if (i == existingGlyphIndex)
                    elementIndex += 1;
                else
                    availableGlyphIndexes.Add(i);
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                TMP_SpriteGlyph spriteGlyph = spriteGlyphTable.FirstOrDefault(x => x.sprite == sprite);

                if (spriteGlyph != null)
                {
                    if (spriteGlyph.glyphRect.x != sprite.rect.x || spriteGlyph.glyphRect.y != sprite.rect.y || spriteGlyph.glyphRect.width != sprite.rect.width || spriteGlyph.glyphRect.height != sprite.rect.height)
                        spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                }
                else
                {
                    TMP_SpriteCharacter spriteCharacter;

                    if (spriteAsset.spriteCharacterTable != null && spriteAsset.spriteCharacterTable.Count > 0)
                    {
                        spriteCharacter = spriteAsset.spriteCharacterTable.FirstOrDefault(x => x.name == sprite.name);
                        spriteGlyph = spriteCharacter != null ? spriteGlyphTable[(int)spriteCharacter.glyphIndex] : null;

                        if (spriteGlyph != null)
                        {
                            spriteGlyph.sprite = sprite;

                            if (spriteGlyph.glyphRect.x != sprite.rect.x || spriteGlyph.glyphRect.y != sprite.rect.y || spriteGlyph.glyphRect.width != sprite.rect.width || spriteGlyph.glyphRect.height != sprite.rect.height)
                                spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                        }
                    }

                    spriteGlyph = new TMP_SpriteGlyph();

                    if (availableGlyphIndexes.Count > 0)
                    {
                        spriteGlyph.index = availableGlyphIndexes[0];
                        availableGlyphIndexes.RemoveAt(0);
                    }
                    else
                        spriteGlyph.index = (uint)spriteGlyphTable.Count;

                    spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                    spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                    spriteGlyph.scale = 1.0f;
                    spriteGlyph.sprite = sprite;

                    spriteGlyphTable.Add(spriteGlyph);

                    spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);

                    string fileNameToLowerInvariant = sprite.name.ToLowerInvariant();
                    if (fileNameToLowerInvariant == ".notdef" || fileNameToLowerInvariant == "notdef")
                    {
                        spriteCharacter.name = fileNameToLowerInvariant;
                        spriteCharacter.unicode = 0;
                    }
                    else
                    {
                        spriteCharacter.unicode = 0xFFFE;
                        if (!string.IsNullOrEmpty(sprite.name) && sprite.name.Length > 2 && sprite.name[0] == '0' && (sprite.name[1] == 'x' || sprite.name[1] == 'X'))
                        {
                            spriteCharacter.unicode = (uint)TMP_TextUtilities.StringHexToInt(sprite.name.Remove(0, 2));
                        }
                        spriteCharacter.name = sprite.name;
                    }

                    spriteCharacter.scale = 1.0f;

                    spriteAsset.spriteCharacterTable.Add(spriteCharacter);
                }
            }

            for (int i = 0; i < spriteAsset.spriteCharacterTable.Count; i++)
            {
                TMP_SpriteCharacter spriteCharacter = spriteAsset.spriteCharacterTable[i];
                if (spriteCharacter.unicode == 0)
                    spriteCharacter.unicode = 0xFFFE;
            }

            spriteAsset.SortGlyphTable();
            spriteAsset.UpdateLookupTables();
            TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, spriteAsset);
            EditorUtility.SetDirty(spriteAsset);

        }


        [MenuItem("Assets/Create/TextMeshPro/Sprite Asset", false, 200)]
        static void CreateSpriteAsset()
        {
            Object[] targets = Selection.objects;

            if (targets == null)
            {
                Debug.LogWarning("A Sprite Texture must first be selected in order to create a Sprite Asset.");
                return;
            }

            if (TMP_Settings.instance == null)
            {
                Debug.Log("Unable to create sprite asset. Please import the TMP Essential Resources.");

                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Object target = targets[i];

                if (target == null || target.GetType() != typeof(Texture2D))
                {
                    Debug.LogWarning("Selected Object [" + target.name + "] is not a Sprite Texture. A Sprite Texture must be selected in order to create a Sprite Asset.", target);
                    continue;
                }

                CreateSpriteAssetFromSelectedObject(target);
            }
        }


        static void CreateSpriteAssetFromSelectedObject(Object target)
        {
            string filePathWithName = AssetDatabase.GetAssetPath(target);
            string fileNameWithExtension = Path.GetFileName(filePathWithName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePathWithName);
            string filePath = filePathWithName.Replace(fileNameWithExtension, "");
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(filePath + fileNameWithoutExtension + ".asset");

            TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(spriteAsset, uniquePath);

            spriteAsset.version = "1.1.0";

            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);

            List<TMP_SpriteGlyph> spriteGlyphTable = new List<TMP_SpriteGlyph>();
            List<TMP_SpriteCharacter> spriteCharacterTable = new List<TMP_SpriteCharacter>();

            if (target.GetType() == typeof(Texture2D))
            {
                Texture2D sourceTex = target as Texture2D;

                spriteAsset.spriteSheet = sourceTex;

                PopulateSpriteTables(sourceTex, ref spriteCharacterTable, ref spriteGlyphTable);

                spriteAsset.spriteCharacterTable = spriteCharacterTable;
                spriteAsset.spriteGlyphTable = spriteGlyphTable;

                AddDefaultMaterial(spriteAsset);
            }
            else if (target.GetType() == typeof(SpriteAtlas))
            {
            }

            spriteAsset.UpdateLookupTables();

            EditorUtility.SetDirty(spriteAsset);

            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));
        }


        static void PopulateSpriteTables(Texture source, ref List<TMP_SpriteCharacter> spriteCharacterTable, ref List<TMP_SpriteGlyph> spriteGlyphTable)
        {
            string filePath = AssetDatabase.GetAssetPath(source);

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).OrderByDescending(x => x.rect.y).ThenBy(x => x.rect.x).ToArray();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph();
                spriteGlyph.index = (uint)i;
                spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
                spriteGlyph.scale = 1.0f;
                spriteGlyph.sprite = sprite;

                spriteGlyphTable.Add(spriteGlyph);

                TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);

                string fileNameToLowerInvariant = sprite.name.ToLowerInvariant();
                if (fileNameToLowerInvariant == ".notdef" || fileNameToLowerInvariant == "notdef")
                {
                    spriteCharacter.unicode = 0;
                    spriteCharacter.name = fileNameToLowerInvariant;
                }
                else
                {
                    if (!string.IsNullOrEmpty(sprite.name) && sprite.name.Length > 2 && sprite.name[0] == '0' && (sprite.name[1] == 'x' || sprite.name[1] == 'X'))
                    {
                        spriteCharacter.unicode = (uint)TMP_TextUtilities.StringHexToInt(sprite.name.Remove(0, 2));
                    }
                    spriteCharacter.name = sprite.name;
                }

                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }


        static void PopulateSpriteTables(SpriteAtlas spriteAtlas, ref List<TMP_SpriteCharacter> spriteCharacterTable, ref List<TMP_SpriteGlyph> spriteGlyphTable)
        {
            int spriteCount = spriteAtlas.spriteCount;
            Sprite[] sprites = new Sprite[spriteCount];

            spriteAtlas.GetSprites(sprites);

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph();
                spriteGlyph.index = (uint)i;
                spriteGlyph.metrics = new GlyphMetrics(sprite.textureRect.width, sprite.textureRect.height, -sprite.pivot.x, sprite.textureRect.height - sprite.pivot.y, sprite.textureRect.width);
                spriteGlyph.glyphRect = new GlyphRect(sprite.textureRect);
                spriteGlyph.scale = 1.0f;
                spriteGlyph.sprite = sprite;

                spriteGlyphTable.Add(spriteGlyph);

                TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);
                spriteCharacter.name = sprite.name;
                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }


        /// <summary>
        /// Create and add new default material to sprite asset.
        /// </summary>
        /// <param name="spriteAsset"></param>
        static void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
        {
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            Material material = new Material(shader);
            material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            material.name = spriteAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }


        static List<TMP_Sprite> UpdateSpriteInfo(TMP_SpriteAsset spriteAsset)
        {
            string filePath = AssetDatabase.GetAssetPath(spriteAsset.spriteSheet);

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).OrderByDescending(x => x.rect.y).ThenBy(x => x.rect.x).ToArray();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];

                int index = -1;
                if (spriteAsset.spriteInfoList.Count > i && spriteAsset.spriteInfoList[i].sprite != null)
                    index = spriteAsset.spriteInfoList.FindIndex(item => item.sprite.GetInstanceID() == sprite.GetInstanceID());

                TMP_Sprite spriteInfo = index == -1 ? new TMP_Sprite() : spriteAsset.spriteInfoList[index];

                Rect spriteRect = sprite.rect;
                spriteInfo.x = spriteRect.x;
                spriteInfo.y = spriteRect.y;
                spriteInfo.width = spriteRect.width;
                spriteInfo.height = spriteRect.height;

                Vector2 pivot = new Vector2(0 - (sprite.bounds.min.x) / (sprite.bounds.extents.x * 2), 0 - (sprite.bounds.min.y) / (sprite.bounds.extents.y * 2));

                spriteInfo.pivot = new Vector2(0 - pivot.x * spriteRect.width, spriteRect.height - pivot.y * spriteRect.height);

                if (index == -1)
                {
                    int[] ids = spriteAsset.spriteInfoList.Select(item => item.id).ToArray();

                    int id = 0;
                    for (int j = 0; j < ids.Length; j++ )
                    {
                        if (ids[0] != 0) break;

                        if (j > 0 && (ids[j] - ids[j - 1]) > 1)
                        {
                            id = ids[j - 1] + 1;
                            break;
                        }

                        id = j + 1;
                    }

                    spriteInfo.sprite = sprite;
                    spriteInfo.name = sprite.name;
                    spriteInfo.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteInfo.name);
                    spriteInfo.id = id;
                    spriteInfo.xAdvance = spriteRect.width;
                    spriteInfo.scale = 1.0f;

                    spriteInfo.xOffset = spriteInfo.pivot.x;
                    spriteInfo.yOffset = spriteInfo.pivot.y;

                    spriteAsset.spriteInfoList.Add(spriteInfo);

                    spriteAsset.spriteInfoList = spriteAsset.spriteInfoList.OrderBy(s => s.id).ToList();
                }
                else
                {
                    spriteAsset.spriteInfoList[index] = spriteInfo;
                }
            }

            return spriteAsset.spriteInfoList;
        }
    }
}
