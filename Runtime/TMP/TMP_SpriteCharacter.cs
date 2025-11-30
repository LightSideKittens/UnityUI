using System;
using UnityEngine;

namespace TMPro
{
    /// <summary>
    /// A basic element of text representing a pictograph, image, sprite.
    /// </summary>
    [Serializable]
    public class TMP_SpriteCharacter : TMP_TextElement
    {
        /// <summary>
        /// The name of the sprite element.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        [SerializeField]
        private string m_Name;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TMP_SpriteCharacter()
        {
            m_ElementType = TextElementType.Sprite;
        }

        /// <summary>
        /// Constructor for new sprite character.
        /// </summary>
        /// <param name="unicode">Unicode value of the sprite character.</param>
        /// <param name="glyph">Glyph used by the sprite character.</param>
        public TMP_SpriteCharacter(uint unicode, TMP_SpriteGlyph glyph)
        {
            m_ElementType = TextElementType.Sprite;

            this.unicode = unicode;
            glyphIndex = glyph.index;
            this.glyph = glyph;
            scale = 1.0f;
        }

        /// <summary>
        /// Constructor for new sprite character.
        /// </summary>
        /// <param name="unicode">Unicode value of the sprite character.</param>
        /// <param name="spriteAsset">Sprite Asset used by this sprite character.</param>
        /// <param name="glyph">Glyph used by the sprite character.</param>
        public TMP_SpriteCharacter(uint unicode, TMP_SpriteAsset spriteAsset, TMP_SpriteGlyph glyph)
        {
            m_ElementType = TextElementType.Sprite;

            this.unicode = unicode;
            textAsset = spriteAsset;
            this.glyph = glyph;
            glyphIndex = glyph.index;
            scale = 1.0f;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="glyphIndex"></param>
        internal TMP_SpriteCharacter(uint unicode, uint glyphIndex)
        {
            m_ElementType = TextElementType.Sprite;

            this.unicode = unicode;
            textAsset = null;
            glyph = null;
            this.glyphIndex = glyphIndex;
            scale = 1.0f;
        }
    }
}
