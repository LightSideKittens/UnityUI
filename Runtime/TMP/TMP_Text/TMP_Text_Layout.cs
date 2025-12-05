using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public abstract partial class TMP_Text : ILayoutIgnorer
    {
        protected static Vector2 largePositiveVector2 = new(TMP_Math.INT_MAX, TMP_Math.INT_MAX);
        protected static Vector2 largeNegativeVector2 = new(TMP_Math.INT_MIN, TMP_Math.INT_MIN);
        protected static float largePositiveFloat = TMP_Math.FLOAT_MAX;
        protected static float largeNegativeFloat = TMP_Math.FLOAT_MIN;

        protected Vector2 preferredSize;
        public virtual float preferredWidth => preferredSize.x;
        public virtual float preferredHeight => preferredSize.y;

        protected virtual Bounds GetCompoundBounds()
        {
            return new();
        }

        protected void SetTextBounds()
        {
            Extents extent = new(largePositiveVector2, largeNegativeVector2);

            for (int i = 0; i < m_textInfo.characterCount && i < m_textInfo.characterInfo.Length; i++)
            {
                if (!m_textInfo.characterInfo[i].isVisible)
                    continue;

                extent.min.x = Mathf.Min(extent.min.x, m_textInfo.characterInfo[i].origin);
                extent.min.y = Mathf.Min(extent.min.y, m_textInfo.characterInfo[i].descender);

                extent.max.x = Mathf.Max(extent.max.x, m_textInfo.characterInfo[i].xAdvance);
                extent.max.y = Mathf.Max(extent.max.y, m_textInfo.characterInfo[i].ascender);
            }

            Vector2 size;
            size.x = extent.max.x - extent.min.x;
            size.y = extent.max.y - extent.min.y;

            Vector3 center = (extent.min + extent.max) / 2;

            textBounds = new(center, size);
            UpdatePreferredSize();
        }

        private void UpdatePreferredSize()
        {
            preferredSize = textBounds.size;
            if (m_margin.x > 0) preferredSize.x += m_margin.x;
            if (m_margin.z > 0) preferredSize.x += m_margin.z;
            if (m_margin.y > 0) preferredSize.y += m_margin.y;
            if (m_margin.w > 0) preferredSize.y += m_margin.w;
        }

        protected void AdjustLineOffset(int startIndex, int endIndex, float offset)
        {
            Vector3 vertexOffset = new(0, offset, 0);

            for (int i = startIndex; i <= endIndex; i++)
            {
                ref var charInfo = ref m_textInfo.characterInfo[i];
                charInfo.bottomLeft -= vertexOffset;
                charInfo.topLeft -= vertexOffset;
                charInfo.topRight -= vertexOffset;
                charInfo.bottomRight -= vertexOffset;

                charInfo.ascender -= vertexOffset.y;
                charInfo.baseLine -= vertexOffset.y;
                charInfo.descender -= vertexOffset.y;

                if (charInfo.isVisible)
                {
                    charInfo.vertex_BL.position -= vertexOffset;
                    charInfo.vertex_TL.position -= vertexOffset;
                    charInfo.vertex_TR.position -= vertexOffset;
                    charInfo.vertex_BR.position -= vertexOffset;
                }
            }
        }

        protected void ResizeLineExtents(int size)
        {
            size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size + 1);

            TMP_LineInfo[] temp_lineInfo = new TMP_LineInfo[size];
            for (int i = 0; i < size; i++)
            {
                if (i < m_textInfo.lineInfo.Length)
                    temp_lineInfo[i] = m_textInfo.lineInfo[i];
                else
                {
                    temp_lineInfo[i].lineExtents.min = largePositiveVector2;
                    temp_lineInfo[i].lineExtents.max = largeNegativeVector2;

                    temp_lineInfo[i].ascender = largeNegativeFloat;
                    temp_lineInfo[i].descender = largePositiveFloat;
                }
            }

            m_textInfo.lineInfo = temp_lineInfo;
        }

        public virtual void ComputeMarginSize()
        {
        }

        public bool ignoreLayout => true;
    }
}