using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public abstract partial class TMP_Text
    {
        public float flexibleHeight => m_flexibleHeight;
        protected float m_flexibleHeight = -1f;
        public float flexibleWidth => m_flexibleWidth;
        protected float m_flexibleWidth = -1f;
        public float minWidth => m_minWidth;
        protected float m_minWidth;
        public float minHeight => m_minHeight;
        protected float m_minHeight;
        public float maxWidth => m_maxWidth;
        protected float m_maxWidth;
        public float maxHeight => m_maxHeight;
        protected float m_maxHeight;
        
        protected LayoutElement layoutElement
        {
            get
            {
                if (m_LayoutElement == null)
                {
                    m_LayoutElement = GetComponent<LayoutElement>();
                }

                return m_LayoutElement;
            }
        }
        protected LayoutElement m_LayoutElement;

        /// <summary>
        /// Computed preferred width of the text object.
        /// </summary>
        public virtual float preferredWidth => preferredSize.x;

        /// <summary>
        /// Computed preferred height of the text object.
        /// </summary>
        public virtual float preferredHeight => preferredSize.y;
        private Vector2 preferredSize;
        
        /// <summary>
        /// Method returning the compound bounds of the text object and child sub objects.
        /// </summary>
        /// <returns></returns>
        protected virtual Bounds GetCompoundBounds()
        {
            return new();
        }
        
        /// <summary>
        /// Method which returns the bounds of the text object;
        /// </summary>
        /// <returns></returns>
        protected void SetTextBounds()
        {
            Extents extent = new(k_LargePositiveVector2, k_LargeNegativeVector2);

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
        
        /// <summary>
        /// Method to adjust line spacing as a result of using different fonts or font point size.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="offset"></param>
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

        /// <summary>
        /// Function to increase the size of the Line Extents Array.
        /// </summary>
        /// <param name="size"></param>
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
                    temp_lineInfo[i].lineExtents.min = k_LargePositiveVector2;
                    temp_lineInfo[i].lineExtents.max = k_LargeNegativeVector2;

                    temp_lineInfo[i].ascender = k_LargeNegativeFloat;
                    temp_lineInfo[i].descender = k_LargePositiveFloat;
                }
            }

            m_textInfo.lineInfo = temp_lineInfo;
        }

        protected static Vector2 k_LargePositiveVector2 = new(TMP_Math.INT_MAX, TMP_Math.INT_MAX);
        protected static Vector2 k_LargeNegativeVector2 = new(TMP_Math.INT_MIN, TMP_Math.INT_MIN);
        protected static float k_LargePositiveFloat = TMP_Math.FLOAT_MAX;
        protected static float k_LargeNegativeFloat = TMP_Math.FLOAT_MIN;

        /// <summary>
        /// Function to force an update of the margin size.
        /// </summary>
        public virtual void ComputeMarginSize()
        {
        }
    }
}