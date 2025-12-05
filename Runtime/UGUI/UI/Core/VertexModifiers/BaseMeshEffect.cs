using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    ///
    ///
    ///
    ///
    [ExecuteAlways]
    public abstract class BaseMeshEffect : UIBehaviour, IMeshModifier
    {
        [NonSerialized] private Graphic m_Graphic;

        protected Graphic graphic
        {
            get
            {
                if (m_Graphic == null)
                    m_Graphic = GetComponent<Graphic>();

                return m_Graphic;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
            base.OnDidApplyAnimationProperties();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

#endif

        public abstract void ModifyMesh(VertexHelper vh);
    }
}