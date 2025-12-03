using System;
using UnityEngine;


namespace TMPro
{
    [Serializable]
    public struct GlyphAnchorPoint
    {
        public float xCoordinate { get { return m_XCoordinate; } set { m_XCoordinate = value; } }

        public float yCoordinate { get { return m_YCoordinate; } set { m_YCoordinate = value; } }

        [SerializeField]
        private float m_XCoordinate;

        [SerializeField]
        private float m_YCoordinate;
    }

    [Serializable]
    public struct MarkPositionAdjustment
    {
        public float xPositionAdjustment { get { return m_XPositionAdjustment; } set { m_XPositionAdjustment = value; } }

        public float yPositionAdjustment { get { return m_YPositionAdjustment; } set { m_YPositionAdjustment = value; } }

        /// <param name="x">The horizontal positional adjustment.</param>
        /// <param name="y">The vertical positional adjustment.</param>
        public MarkPositionAdjustment(float x, float y)
        {
            m_XPositionAdjustment = x;
            m_YPositionAdjustment = y;
        }

        [SerializeField]
        private float m_XPositionAdjustment;

        [SerializeField]
        private float m_YPositionAdjustment;
    };

    [Serializable]
    public struct MarkToBaseAdjustmentRecord
    {
        public uint baseGlyphID { get { return m_BaseGlyphID; } set { m_BaseGlyphID = value; } }

        public GlyphAnchorPoint baseGlyphAnchorPoint { get { return m_BaseGlyphAnchorPoint; } set { m_BaseGlyphAnchorPoint = value; } }

        public uint markGlyphID { get { return m_MarkGlyphID; } set { m_MarkGlyphID = value; } }

        public MarkPositionAdjustment markPositionAdjustment { get { return m_MarkPositionAdjustment; } set { m_MarkPositionAdjustment = value; } }

        [SerializeField]
        private uint m_BaseGlyphID;

        [SerializeField]
        private GlyphAnchorPoint m_BaseGlyphAnchorPoint;

        [SerializeField]
        private uint m_MarkGlyphID;

        [SerializeField]
        private MarkPositionAdjustment m_MarkPositionAdjustment;
    }

    [Serializable]
    public struct MarkToMarkAdjustmentRecord
    {
        public uint baseMarkGlyphID { get { return m_BaseMarkGlyphID; } set { m_BaseMarkGlyphID = value; } }

        public GlyphAnchorPoint baseMarkGlyphAnchorPoint { get { return m_BaseMarkGlyphAnchorPoint; } set { m_BaseMarkGlyphAnchorPoint = value; } }

        public uint combiningMarkGlyphID { get { return m_CombiningMarkGlyphID; } set { m_CombiningMarkGlyphID = value; } }

        public MarkPositionAdjustment combiningMarkPositionAdjustment { get { return m_CombiningMarkPositionAdjustment; } set { m_CombiningMarkPositionAdjustment = value; } }

        [SerializeField]
        private uint m_BaseMarkGlyphID;

        [SerializeField]
        private GlyphAnchorPoint m_BaseMarkGlyphAnchorPoint;

        [SerializeField]
        private uint m_CombiningMarkGlyphID;

        [SerializeField]
        private MarkPositionAdjustment m_CombiningMarkPositionAdjustment;
    }
}
