using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Aspect Ratio Fitter", 142)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class AspectRatioFitter : UIBehaviour, ILayoutSelfController
    {
        public enum AspectMode
        {
            None,
            WidthControlsHeight,
            HeightControlsWidth,
            FitInParent,
            EnvelopeParent
        }

        [SerializeField] private AspectMode m_AspectMode = AspectMode.None;

        public AspectMode aspectMode { get { return m_AspectMode; } set { if (SetPropertyUtility.SetStruct(ref m_AspectMode, value)) SetDirty(); } }

        [SerializeField] private float m_AspectRatio = 1;

        public float aspectRatio { get { return m_AspectRatio; } set { if (SetPropertyUtility.SetStruct(ref m_AspectRatio, value)) SetDirty(); } }

        [System.NonSerialized]
        private RectTransform m_Rect;

        // This "delayed" mechanism is required for case 1014834.
        private bool m_DelayedSetDirty = false;

        //Does the gameobject has a parent for reference to enable FitToParent/EnvelopeParent modes.
        private bool m_DoesParentExist = false;

        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        // field is never assigned warning
        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649

        protected AspectRatioFitter() {}

        protected override void OnEnable()
        {
            base.OnEnable();
            m_DoesParentExist = rectTransform.parent ? true : false;
            SetDirty();
        }

        protected override void Start()
        {
            base.Start();
            //Disable the component if the aspect mode is not valid or the object state/setup is not supported with AspectRatio setup.
            if (!IsComponentValidOnObject() || !IsAspectModeValid())
                this.enabled = false;
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_DoesParentExist = rectTransform.parent ? true : false;
            SetDirty();
        }

        protected virtual void Update()
        {
            if (m_DelayedSetDirty)
            {
                m_DelayedSetDirty = false;
                SetDirty();
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            UpdateRect();
        }

        private void UpdateRect()
        {
            if (!IsActive() || !IsComponentValidOnObject())
                return;

            m_Tracker.Clear();

            switch (m_AspectMode)
            {
#if UNITY_EDITOR
                case AspectMode.None:
                {
                    if (!Application.isPlaying)
                        m_AspectRatio = Mathf.Clamp(rectTransform.rect.width / rectTransform.rect.height, 0.001f, 1000f);

                    break;
                }
#endif
                case AspectMode.HeightControlsWidth:
                {
                    m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.height * m_AspectRatio);
                    break;
                }
                case AspectMode.WidthControlsHeight:
                {
                    m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.width / m_AspectRatio);
                    break;
                }
                case AspectMode.FitInParent:
                case AspectMode.EnvelopeParent:
                {
                    if (!DoesParentExists())
                        break;

                    m_Tracker.Add(this, rectTransform,
                        DrivenTransformProperties.Anchors |
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.SizeDeltaX |
                        DrivenTransformProperties.SizeDeltaY);

                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.anchoredPosition = Vector2.zero;

                    Vector2 sizeDelta = Vector2.zero;
                    Vector2 parentSize = GetParentSize();
                    if ((parentSize.y * aspectRatio < parentSize.x) ^ (m_AspectMode == AspectMode.FitInParent))
                    {
                        sizeDelta.y = GetSizeDeltaToProduceSize(parentSize.x / aspectRatio, 1);
                    }
                    else
                    {
                        sizeDelta.x = GetSizeDeltaToProduceSize(parentSize.y * aspectRatio, 0);
                    }
                    rectTransform.sizeDelta = sizeDelta;

                    break;
                }
            }
        }

        private float GetSizeDeltaToProduceSize(float size, int axis)
        {
            return size - GetParentSize()[axis] * (rectTransform.anchorMax[axis] - rectTransform.anchorMin[axis]);
        }

        private Vector2 GetParentSize()
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            return !parent ? Vector2.zero : parent.rect.size;
        }

        public virtual void SetLayoutHorizontal() {}

        public virtual void SetLayoutVertical() {}

        protected void SetDirty()
        {
            UpdateRect();
        }

        public bool IsComponentValidOnObject()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return false;
            }
            return true;
        }

        public bool IsAspectModeValid()
        {
            if (!DoesParentExists() && (aspectMode == AspectMode.EnvelopeParent || aspectMode == AspectMode.FitInParent))
                return false;

            return true;
        }

        private bool DoesParentExists()
        {
            return m_DoesParentExist;
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_AspectRatio = Mathf.Clamp(m_AspectRatio, 0.001f, 1000f);
            m_DelayedSetDirty = true;
        }

    #endif
    }
}
