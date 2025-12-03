namespace UnityEngine.EventSystems
{
    public interface IEventSystemHandler
    {
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerMoveHandler : IEventSystemHandler
    {
        void OnPointerMove(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerEnterHandler : IEventSystemHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerExitHandler : IEventSystemHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerDownHandler : IEventSystemHandler
    {
        void OnPointerDown(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerUpHandler : IEventSystemHandler
    {
        void OnPointerUp(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    /// <remarks>
    /// Use the IPointerClickHandler Interface to handle click input using OnPointerClick callbacks. Ensure an Event System exists in the Scene to allow click detection. For click detection on non-UI GameObjects, ensure a EventSystems.PhysicsRaycaster is attached to the Camera.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// public class Example : MonoBehaviour, IPointerClickHandler
    /// {
    ///     //Detect if a click occurs
    ///     public void OnPointerClick(PointerEventData pointerEventData)
    ///     {
    ///         //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
    ///         Debug.Log(name + " Game Object Clicked!");
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface IPointerClickHandler : IEventSystemHandler
    {
        void OnPointerClick(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IBeginDragHandler : IEventSystemHandler
    {
        void OnBeginDrag(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IInitializePotentialDragHandler : IEventSystemHandler
    {
        void OnInitializePotentialDrag(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    /// using UnityEngine.UI;
    ///
    /// [RequireComponent(typeof(Image))]
    /// public class DragMe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    /// {
    ///     public bool dragOnSurfaces = true;
    ///
    ///     private GameObject m_DraggingIcon;
    ///     private RectTransform m_DraggingPlane;
    ///
    ///     public void OnBeginDrag(PointerEventData eventData)
    ///     {
    ///         var canvas = FindInParents<Canvas>(gameObject);
    ///         if (canvas == null)
    ///             return;
    ///
    ///         // We have clicked something that can be dragged.
    ///         // What we want to do is create an icon for this.
    ///         m_DraggingIcon = new GameObject("icon");
    ///
    ///         m_DraggingIcon.transform.SetParent(canvas.transform, false);
    ///         m_DraggingIcon.transform.SetAsLastSibling();
    ///
    ///         var image = m_DraggingIcon.AddComponent<Image>();
    ///
    ///         image.sprite = GetComponent<Image>().sprite;
    ///         image.SetNativeSize();
    ///
    ///         if (dragOnSurfaces)
    ///             m_DraggingPlane = transform as RectTransform;
    ///         else
    ///             m_DraggingPlane = canvas.transform as RectTransform;
    ///
    ///         SetDraggedPosition(eventData);
    ///     }
    ///
    ///     public void OnDrag(PointerEventData data)
    ///     {
    ///         if (m_DraggingIcon != null)
    ///             SetDraggedPosition(data);
    ///     }
    ///
    ///     private void SetDraggedPosition(PointerEventData data)
    ///     {
    ///         if (dragOnSurfaces && data.pointerEnter != null && data.pointerEnter.transform as RectTransform != null)
    ///             m_DraggingPlane = data.pointerEnter.transform as RectTransform;
    ///
    ///         var rt = m_DraggingIcon.GetComponent<RectTransform>();
    ///         Vector3 globalMousePos;
    ///         if (RectTransformUtility.ScreenPointToWorldPointInRectangle(m_DraggingPlane, data.position, data.pressEventCamera, out globalMousePos))
    ///         {
    ///             rt.position = globalMousePos;
    ///             rt.rotation = m_DraggingPlane.rotation;
    ///         }
    ///     }
    ///
    ///     public void OnEndDrag(PointerEventData eventData)
    ///     {
    ///         if (m_DraggingIcon != null)
    ///             Destroy(m_DraggingIcon);
    ///     }
    ///
    ///     static public T FindInParents<T>(GameObject go) where T : Component
    ///     {
    ///         if (go == null) return null;
    ///         var comp = go.GetComponent<T>();
    ///
    ///         if (comp != null)
    ///             return comp;
    ///
    ///         Transform t = go.transform.parent;
    ///         while (t != null && comp == null)
    ///         {
    ///             comp = t.gameObject.GetComponent<T>();
    ///             t = t.parent;
    ///         }
    ///         return comp;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface IDragHandler : IEventSystemHandler
    {
        void OnDrag(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IEndDragHandler : IEventSystemHandler
    {
        void OnEndDrag(PointerEventData eventData);
    }

    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// public class DropMe : MonoBehaviour, IDropHandler
    /// {
    ///     public void OnDrop(PointerEventData data)
    ///     {
    ///         if (data.pointerDrag != null)
    ///         {
    ///             Debug.Log ("Dropped object was: "  + data.pointerDrag);
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IDropHandler : IEventSystemHandler
    {
        void OnDrop(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IScrollHandler : IEventSystemHandler
    {
        void OnScroll(PointerEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IUpdateSelectedHandler : IEventSystemHandler
    {
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.EventSystems;
        ///
        /// public class UpdateSelectedExample : MonoBehaviour, IUpdateSelectedHandler
        /// {
        ///     public void OnUpdateSelected(BaseEventData data)
        ///     {
        ///         Debug.Log("OnUpdateSelected called.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        void OnUpdateSelected(BaseEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISelectHandler : IEventSystemHandler
    {
        void OnSelect(BaseEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IDeselectHandler : IEventSystemHandler
    {
        void OnDeselect(BaseEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IMoveHandler : IEventSystemHandler
    {
        void OnMove(AxisEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISubmitHandler : IEventSystemHandler
    {
        void OnSubmit(BaseEventData eventData);
    }

    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ICancelHandler : IEventSystemHandler
    {
        void OnCancel(BaseEventData eventData);
    }
}
