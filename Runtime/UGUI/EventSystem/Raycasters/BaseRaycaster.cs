using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    public abstract class BaseRaycaster : UIBehaviour
    {
        private BaseRaycaster m_RootRaycaster;


        public abstract void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList);

        public abstract Camera eventCamera { get; }

        public virtual int sortOrderPriority
        {
            get { return int.MinValue; }
        }

        public virtual int renderOrderPriority
        {
            get { return int.MinValue; }
        }

        public BaseRaycaster rootRaycaster
        {
            get
            {
                if (m_RootRaycaster == null)
                {
                    var baseRaycasters = GetComponentsInParent<BaseRaycaster>();
                    if (baseRaycasters.Length != 0)
                        m_RootRaycaster = baseRaycasters[baseRaycasters.Length - 1];
                }

                return m_RootRaycaster;
            }
        }

        public override string ToString()
        {
            return "Name: " + gameObject + "\n" +
                   "eventCamera: " + eventCamera + "\n" +
                   "sortOrderPriority: " + sortOrderPriority + "\n" +
                   "renderOrderPriority: " + renderOrderPriority;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RaycasterManager.AddRaycaster(this);
        }

        protected override void OnDisable()
        {
            RaycasterManager.RemoveRaycasters(this);
            base.OnDisable();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            m_RootRaycaster = null;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            m_RootRaycaster = null;
        }
    }
}