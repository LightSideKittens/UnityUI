using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;


namespace TMPro
{

    public enum TextContainerAnchors { TopLeft = 0, Top = 1, TopRight = 2, Left = 3, Middle = 4, Right = 5, BottomLeft = 6, Bottom = 7, BottomRight = 8, Custom = 9 };


    [RequireComponent(typeof(RectTransform))]
    public class TextContainer : UIBehaviour
    {

        #pragma warning disable 0618

        public bool hasChanged
        {
            get { return m_hasChanged; }
            set { m_hasChanged = value; }
        }
        private bool m_hasChanged;


        public Vector2 pivot
        {
            get { return m_pivot; }
            set {
                if (m_pivot != value) { m_pivot = value; m_anchorPosition = GetAnchorPosition(m_pivot); m_hasChanged = true; OnContainerChanged(); } }
        }
        [SerializeField]
        private Vector2 m_pivot;


        public TextContainerAnchors anchorPosition
        {
            get { return m_anchorPosition; }
            set {
                if (m_anchorPosition != value) { m_anchorPosition = value; m_pivot = GetPivot(m_anchorPosition); m_hasChanged = true; OnContainerChanged(); } }
        }
        [SerializeField]
        private TextContainerAnchors m_anchorPosition = TextContainerAnchors.Middle;


        public Rect rect
        {
            get { return m_rect; }
            set {
                if (m_rect != value) { m_rect = value;
                    m_hasChanged = true; OnContainerChanged(); } }
        }
        [SerializeField]
        private Rect m_rect;


        public Vector2 size
        {
            get { return new Vector2(m_rect.width, m_rect.height); }
            set {
                if (new Vector2(m_rect.width, m_rect.height) != value) { SetRect(value); m_hasChanged = true; m_isDefaultWidth = false; m_isDefaultHeight = false; OnContainerChanged(); } }
        }


        public float width
        {
            get { return m_rect.width; }
            set {
                SetRect(new Vector2(value, m_rect.height)); m_hasChanged = true; m_isDefaultWidth = false; OnContainerChanged(); }
        }


        public float height
        {
            get { return m_rect.height; }
            set { SetRect(new Vector2(m_rect.width, value)); m_hasChanged = true; m_isDefaultHeight = false; OnContainerChanged(); }
        }


        public bool isDefaultWidth
        {
            get { return m_isDefaultWidth; }
        }
        private bool m_isDefaultWidth;

        public bool isDefaultHeight
        {
            get { return m_isDefaultHeight; }
        }
        private bool m_isDefaultHeight;


        public bool isAutoFitting
        {
            get { return m_isAutoFitting; }
            set { m_isAutoFitting = value; }
        }
        private bool m_isAutoFitting = false;


        public Vector3[] corners
        {
            get { return m_corners; }
        }
        private Vector3[] m_corners = new Vector3[4];


        public Vector3[] worldCorners
        {
            get { return m_worldCorners; }
        }
        private Vector3[] m_worldCorners = new Vector3[4];


        public Vector4 margins
        {
            get { return m_margins; }
            set { if (m_margins != value) {
                m_margins = value; m_hasChanged = true; OnContainerChanged(); } }
        }
        [SerializeField]
        private Vector4 m_margins;


        /// <summary>
        /// The RectTransform used by the object
        /// </summary>
        public RectTransform rectTransform
        {
            get
            {
                if (m_rectTransform == null) m_rectTransform = GetComponent<RectTransform>();

                return m_rectTransform;
            }
        }
        private RectTransform m_rectTransform;


        private static Vector2 k_defaultSize = new Vector2(100, 100);


        /// <summary>
        /// 
        /// </summary>
        public TextMeshPro textMeshPro
        {
            get
            {
                if (m_textMeshPro == null) m_textMeshPro = GetComponent<TextMeshPro>();
                return m_textMeshPro;
            }
        }
        private TextMeshPro m_textMeshPro;


        protected override void Awake()
        {
            Debug.LogWarning("The Text Container component is now Obsolete and can safely be removed from [" + gameObject.name + "].", this);

            return;
        }


        /// <summary>
        /// 
        /// </summary>
        protected override void OnEnable()
        {
            OnContainerChanged();
        }


        /// <summary>
        /// 
        /// </summary>
        protected override void OnDisable()
        {
        }


        /// <summary>
        /// 
        /// </summary>
        private void OnContainerChanged()
        {
            UpdateCorners();

            if (this.m_rectTransform != null)
            {
                m_rectTransform.sizeDelta = this.size;
                m_rectTransform.hasChanged = true;
            }

            if (this.textMeshPro != null)
            {
                m_textMeshPro.SetVerticesDirty();
                m_textMeshPro.margin = m_margins;
            }
        }


#if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        protected override void OnValidate()
        {
            m_hasChanged = true;
            OnContainerChanged();
        }
#endif


        /// <summary>
        /// Callback from Unity to handle RectTransform changes.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            if (this.rectTransform == null) m_rectTransform = gameObject.AddComponent<RectTransform>();

            if (m_rectTransform.sizeDelta != k_defaultSize)
                this.size = m_rectTransform.sizeDelta;

            pivot = m_rectTransform.pivot;

            m_hasChanged = true;
            OnContainerChanged();
        }


        private void SetRect(Vector2 size)
        {
            m_rect = new Rect(m_rect.x, m_rect.y, size.x, size.y);
        }

        private void UpdateCorners()
        {
            m_corners[0] = new Vector3(-m_pivot.x * m_rect.width, (- m_pivot.y) * m_rect.height);
            m_corners[1] = new Vector3(-m_pivot.x * m_rect.width, (1 - m_pivot.y) * m_rect.height);
            m_corners[2] = new Vector3((1 - m_pivot.x) * m_rect.width, (1 - m_pivot.y) * m_rect.height);
            m_corners[3] = new Vector3((1 - m_pivot.x) * m_rect.width, (- m_pivot.y) * m_rect.height);

            if (m_rectTransform != null)
                m_rectTransform.pivot = m_pivot;
        }


        private Vector2 GetPivot(TextContainerAnchors anchor)
        {
            Vector2 pivot = Vector2.zero;

            switch (anchor)
            {
                case TextContainerAnchors.TopLeft:
                    pivot = new Vector2(0, 1);
                    break;
                case TextContainerAnchors.Top:
                    pivot = new Vector2(0.5f, 1);
                    break;
                case TextContainerAnchors.TopRight:
                    pivot = new Vector2(1, 1);
                    break;
                case TextContainerAnchors.Left:
                    pivot = new Vector2(0, 0.5f);
                    break;
                case TextContainerAnchors.Middle:
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case TextContainerAnchors.Right:
                    pivot = new Vector2(1, 0.5f);
                    break;
                case TextContainerAnchors.BottomLeft:
                    pivot = new Vector2(0, 0);
                    break;
                case TextContainerAnchors.Bottom:
                    pivot = new Vector2(0.5f, 0);
                    break;
                case TextContainerAnchors.BottomRight:
                    pivot = new Vector2(1, 0);
                    break;
            }

            return pivot;
        }


        private TextContainerAnchors GetAnchorPosition(Vector2 pivot)
        {

            if (pivot == new Vector2(0, 1))
                return TextContainerAnchors.TopLeft;
            else if (pivot == new Vector2(0.5f, 1))
                return TextContainerAnchors.Top;
            else if (pivot == new Vector2(1f, 1))
                return TextContainerAnchors.TopRight;
            else if (pivot == new Vector2(0, 0.5f))
                return TextContainerAnchors.Left;
            else if (pivot == new Vector2(0.5f, 0.5f))
                return TextContainerAnchors.Middle;
            else if (pivot == new Vector2(1, 0.5f))
                return TextContainerAnchors.Right;
            else if (pivot == new Vector2(0, 0))
                return TextContainerAnchors.BottomLeft;
            else if (pivot == new Vector2(0.5f, 0))
                return TextContainerAnchors.Bottom;
            else if (pivot == new Vector2(1, 0))
                return TextContainerAnchors.BottomRight;
            else
                return TextContainerAnchors.Custom;

        }
    }
}
