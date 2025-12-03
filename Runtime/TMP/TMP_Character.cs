using System;
using UnityEngine.TextCore;

namespace TMPro
{
    [Serializable]
    public class TMP_Character : TMP_TextElement
    {
        public TMP_Character()
        {
            scale = 1.0f;
        }

        /// <param name="unicode">Unicode value.</param>
        /// <param name="glyph">Glyph</param>
        public TMP_Character(uint unicode, Glyph glyph)
        {
            this.unicode = unicode;
            textAsset = null;
            this.glyph = glyph;
            glyphIndex = glyph.index;
            scale = 1.0f;
        }

        /// <param name="unicode">Unicode value.</param>
        /// <param name="fontAsset">The font asset to which this character belongs.</param>
        /// <param name="glyph">Glyph</param>
        public TMP_Character(uint unicode, TMP_FontAsset fontAsset, Glyph glyph)
        {
            this.unicode = unicode;
            textAsset = fontAsset;
            this.glyph = glyph;
            glyphIndex = glyph.index;
            scale = 1.0f;
        }

        /// <param name="unicode">Unicode value.</param>
        /// <param name="glyphIndex">Glyph index.</param>
        internal TMP_Character(uint unicode, uint glyphIndex)
        {
            this.unicode = unicode;
            textAsset = null;
            glyph = null;
            this.glyphIndex = glyphIndex;
            scale = 1.0f;
        }
    }
}
