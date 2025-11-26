using UnityEngine;
using UnityEngine.TextCore;
using System.Collections;
using System.Collections.Generic;

namespace TMPro
{
    [DisallowMultipleComponent]
    public class TMP_SpriteAnimator : MonoBehaviour
    {
        private Dictionary<int, bool> m_animations = new Dictionary<int, bool>(16);

        private TMP_Text m_TextComponent;


        private void Awake()
        {
            m_TextComponent = GetComponent<TMP_Text>();
        }


        private void OnEnable()
        {
        }


        private void OnDisable()
        {
        }


        public void StopAllAnimations()
        {
            StopAllCoroutines();
            m_animations.Clear();
        }


        public void DoSpriteAnimation(int currentCharacter, TMP_SpriteAsset spriteAsset, int start, int end, int framerate)
        {
            bool isPlaying;

            if (!m_animations.TryGetValue(currentCharacter, out isPlaying))
            {
                StartCoroutine(DoSpriteAnimationInternal(currentCharacter, spriteAsset, start, end, framerate));
                m_animations.Add(currentCharacter, true);
            }
        }


        private IEnumerator DoSpriteAnimationInternal(int currentCharacter, TMP_SpriteAsset spriteAsset, int start, int end, int framerate)
        {
            if (m_TextComponent == null) yield break;

            yield return null;

            int currentFrame = start;

            if (end > spriteAsset.spriteCharacterTable.Count)
                end = spriteAsset.spriteCharacterTable.Count - 1;

            TMP_CharacterInfo charInfo = m_TextComponent.textInfo.characterInfo[currentCharacter];

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            TMP_MeshInfo meshInfo = m_TextComponent.textInfo.meshInfo[materialIndex];

            float baseSpriteScale = spriteAsset.spriteCharacterTable[start].scale * spriteAsset.spriteCharacterTable[start].glyph.scale;

            float elapsedTime = 0;
            float targetTime = 1f / Mathf.Abs(framerate);

            while (true)
            {
                if (elapsedTime > targetTime)
                {
                    elapsedTime = 0;

                    uint character = m_TextComponent.textInfo.characterInfo[currentCharacter].character;
                    if (character == 0x03 || character == 0x2026)
                    {
                        m_animations.Remove(currentCharacter);
                        yield break;
                    }

                    TMP_SpriteCharacter spriteCharacter = spriteAsset.spriteCharacterTable[currentFrame];

                    Vector3[] vertices = meshInfo.vertices;

                    Vector2 origin = new Vector2(charInfo.origin, charInfo.baseLine);

                    float spriteScale = charInfo.scale / baseSpriteScale * spriteCharacter.scale * spriteCharacter.glyph.scale;

                    Vector3 bl = new Vector3(origin.x + spriteCharacter.glyph.metrics.horizontalBearingX * spriteScale, origin.y + (spriteCharacter.glyph.metrics.horizontalBearingY - spriteCharacter.glyph.metrics.height) * spriteScale);
                    Vector3 tl = new Vector3(bl.x, origin.y + spriteCharacter.glyph.metrics.horizontalBearingY * spriteScale);
                    Vector3 tr = new Vector3(origin.x + (spriteCharacter.glyph.metrics.horizontalBearingX + spriteCharacter.glyph.metrics.width) * spriteScale, tl.y);
                    Vector3 br = new Vector3(tr.x, bl.y);

                    vertices[vertexIndex + 0] = bl;
                    vertices[vertexIndex + 1] = tl;
                    vertices[vertexIndex + 2] = tr;
                    vertices[vertexIndex + 3] = br;

                    Vector4[] uvs0 = meshInfo.uvs0;

                    Vector2 uv0 = new Vector2((float)spriteCharacter.glyph.glyphRect.x / spriteAsset.spriteSheet.width, (float)spriteCharacter.glyph.glyphRect.y / spriteAsset.spriteSheet.height);
                    Vector2 uv1 = new Vector2(uv0.x, (float)(spriteCharacter.glyph.glyphRect.y + spriteCharacter.glyph.glyphRect.height) / spriteAsset.spriteSheet.height);
                    Vector2 uv2 = new Vector2((float)(spriteCharacter.glyph.glyphRect.x + spriteCharacter.glyph.glyphRect.width) / spriteAsset.spriteSheet.width, uv1.y);
                    Vector2 uv3 = new Vector2(uv2.x, uv0.y);

                    uvs0[vertexIndex + 0] = uv0;
                    uvs0[vertexIndex + 1] = uv1;
                    uvs0[vertexIndex + 2] = uv2;
                    uvs0[vertexIndex + 3] = uv3;

                    meshInfo.mesh.vertices = vertices;
                    meshInfo.mesh.SetUVs(0, uvs0);
                    m_TextComponent.UpdateGeometry(meshInfo.mesh, materialIndex);


                    if (framerate > 0)
                    {
                        if (currentFrame < end)
                            currentFrame += 1;
                        else
                            currentFrame = start;
                    }
                    else
                    {
                        if (currentFrame > start)
                            currentFrame -= 1;
                        else
                            currentFrame = end;
                    }
                }

                elapsedTime += Time.deltaTime;

                yield return null;
            }
        }

    }
}
