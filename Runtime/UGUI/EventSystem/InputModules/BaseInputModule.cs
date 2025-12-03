using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    [RequireComponent(typeof(EventSystem))]
    /// <remarks>
    /// An Input Module is a component of the EventSystem that is responsible for raising events and sending them to GameObjects for handling. The BaseInputModule is a class that all Input Modules in the EventSystem inherit from. Examples of provided modules are TouchInputModule and StandaloneInputModule, if these are inadequate for your project you can create your own by extending from the BaseInputModule.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// /**
    ///  * Create a module that every tick sends a 'Move' event to
    ///  * the target object
    ///  */
    /// public class MyInputModule : BaseInputModule
    /// {
    ///     public GameObject m_TargetObject;
    ///
    ///     public override void Process()
    ///     {
    ///         if (m_TargetObject == null)
    ///             return;
    ///         ExecuteEvents.Execute (m_TargetObject, new BaseEventData (eventSystem), ExecuteEvents.moveHandler);
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public abstract class BaseInputModule : UIBehaviour
    {
        [NonSerialized]
        protected List<RaycastResult> m_RaycastResultCache = new List<RaycastResult>();

        [SerializeField] private bool m_SendPointerHoverToParent = true;

        //This is needed for testing
        protected internal bool sendPointerHoverToParent { get { return m_SendPointerHoverToParent; } set { m_SendPointerHoverToParent = value; } }

        private AxisEventData m_AxisEventData;

        private EventSystem m_EventSystem;
        private BaseEventData m_BaseEventData;

        protected BaseInput m_InputOverride;
        private BaseInput m_DefaultInput;

        public BaseInput input
        {
            get
            {
                if (m_InputOverride != null)
                    return m_InputOverride;

                if (m_DefaultInput == null)
                {
                    var inputs = GetComponents<BaseInput>();
                    foreach (var baseInput in inputs)
                    {
                        // We dont want to use any classes that derrive from BaseInput for default.
                        if (baseInput != null && baseInput.GetType() == typeof(BaseInput))
                        {
                            m_DefaultInput = baseInput;
                            break;
                        }
                    }

                    if (m_DefaultInput == null)
                        m_DefaultInput = gameObject.AddComponent<BaseInput>();
                }

                return m_DefaultInput;
            }
        }

        /// <remarks>
        /// With this it is possible to bypass the Input system with your own but still use the same InputModule. For example this can be used to feed fake input into the UI or interface with a different input system.
        /// </remarks>
        public BaseInput inputOverride
        {
            get { return m_InputOverride; }
            set { m_InputOverride = value; }
        }

        protected EventSystem eventSystem
        {
            get { return m_EventSystem; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventSystem = GetComponent<EventSystem>();
            m_EventSystem.UpdateModules();
        }

        protected override void OnDisable()
        {
            m_EventSystem.UpdateModules();
            base.OnDisable();
        }

        public abstract void Process();

        protected static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
        {
            var candidatesCount = candidates.Count;
            for (var i = 0; i < candidatesCount; ++i)
            {
                if (candidates[i].gameObject == null)
                    continue;

                return candidates[i];
            }
            return new RaycastResult();
        }

        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        protected static MoveDirection DetermineMoveDirection(float x, float y)
        {
            return DetermineMoveDirection(x, y, 0.6f);
        }

        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// <param name="deadZone">Dead zone.</param>
        protected static MoveDirection DetermineMoveDirection(float x, float y, float deadZone)
        {
            // if vector is too small... just return
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return MoveDirection.None;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                return x > 0 ? MoveDirection.Right : MoveDirection.Left;
            }

            return y > 0 ? MoveDirection.Up : MoveDirection.Down;
        }

        /// <param name="g1">GameObject to compare</param>
        /// <param name="g2">GameObject to compare</param>
        /// <returns></returns>
        protected static GameObject FindCommonRoot(GameObject g1, GameObject g2)
        {
            if (g1 == null || g2 == null)
                return null;

            var t1 = g1.transform;
            while (t1 != null)
            {
                var t2 = g2.transform;
                while (t2 != null)
                {
                    if (t1 == t2)
                        return t1.gameObject;
                    t2 = t2.parent;
                }
                t1 = t1.parent;
            }
            return null;
        }

        // walk up the tree till a common root between the last entered and the current entered is found
        // send exit events up to (but not including) the common root. Then send enter events up to
        // (but not including) the common root.
        // Send move events before exit, after enter, and on hovered objects when pointer data has changed.
        protected void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
        {
            // if we have no target / pointerEnter has been deleted
            // just send exit events to anything we are tracking
            // then exit
            if (newEnterTarget == null || currentPointerData.pointerEnter == null)
            {
                var hoveredCount = currentPointerData.hovered.Count;
                for (var i = 0; i < hoveredCount; ++i)
                {
                    currentPointerData.fullyExited = true;
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerMoveHandler);
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerExitHandler);
                }

                currentPointerData.hovered.Clear();

                if (newEnterTarget == null)
                {
                    currentPointerData.pointerEnter = null;
                    return;
                }
            }

            // if we have not changed hover target
            if (currentPointerData.pointerEnter == newEnterTarget && newEnterTarget)
            {
                if (currentPointerData.IsPointerMoving())
                {
                    var hoveredCount = currentPointerData.hovered.Count;
                    for (var i = 0; i < hoveredCount; ++i)
                        ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerMoveHandler);
                }
                return;
            }

            GameObject commonRoot = FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);
            GameObject pointerParent = ((Component)newEnterTarget.GetComponentInParent<IPointerExitHandler>())?.gameObject;

            // and we already an entered object from last time
            if (currentPointerData.pointerEnter != null)
            {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                // ** or when !m_SendPointerEnterToParent, stop when meeting a gameobject with an exit event handler
                Transform t = currentPointerData.pointerEnter.transform;

                while (t != null)
                {
                    // if we reach the common root break out!
                    if (m_SendPointerHoverToParent && commonRoot != null && commonRoot.transform == t)
                        break;

                    // if we reach a PointerExitEvent break out!
                    if (!m_SendPointerHoverToParent && pointerParent == t.gameObject)
                        break;

                    currentPointerData.fullyExited = t.gameObject != commonRoot && currentPointerData.pointerEnter != newEnterTarget;
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerMoveHandler);
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    currentPointerData.hovered.Remove(t.gameObject);

                    if (m_SendPointerHoverToParent) t = t.parent;

                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    if (!m_SendPointerHoverToParent) t = t.parent;
                }
            }

            // now issue the enter call up to but not including the common root
            var oldPointerEnter = currentPointerData.pointerEnter;
            currentPointerData.pointerEnter = newEnterTarget;
            if (newEnterTarget != null)
            {
                Transform t = newEnterTarget.transform;

                while (t != null)
                {
                    currentPointerData.reentered = t.gameObject == commonRoot && t.gameObject != oldPointerEnter;
                    // if we are sending the event to parent, they are already in hover mode at that point. No need to bubble up the event.
                    if (m_SendPointerHoverToParent && currentPointerData.reentered)
                        break;

                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerMoveHandler);
                    currentPointerData.hovered.Add(t.gameObject);

                    // stop when encountering an object with the pointerEnterHandler
                    if (!m_SendPointerHoverToParent && t.gameObject.GetComponent<IPointerEnterHandler>() != null)
                        break;

                    if (m_SendPointerHoverToParent) t = t.parent;

                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    if (!m_SendPointerHoverToParent) t = t.parent;
                }
            }
        }

        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// <param name="deadZone">Dead zone.</param>
        protected virtual AxisEventData GetAxisEventData(float x, float y, float moveDeadZone)
        {
            if (m_AxisEventData == null)
                m_AxisEventData = new AxisEventData(eventSystem);

            m_AxisEventData.Reset();
            m_AxisEventData.moveVector = new Vector2(x, y);
            m_AxisEventData.moveDir = DetermineMoveDirection(x, y, moveDeadZone);
            return m_AxisEventData;
        }

        protected virtual BaseEventData GetBaseEventData()
        {
            if (m_BaseEventData == null)
                m_BaseEventData = new BaseEventData(eventSystem);

            m_BaseEventData.Reset();
            return m_BaseEventData;
        }

        /// <param name="pointerId">Pointer ID</param>
        /// <returns>Is the given pointer over an event system object?</returns>
        public virtual bool IsPointerOverGameObject(int pointerId)
        {
            return false;
        }

        public virtual bool ShouldActivateModule()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        public virtual void DeactivateModule()
        {}

        public virtual void ActivateModule()
        {}

        public virtual void UpdateModule()
        {}

        /// <returns>Is the module supported.</returns>
        public virtual bool IsModuleSupported()
        {
            return true;
        }

        /// <param name="sourcePointerData">PointerEventData whose pointerId will be converted to UI Toolkit pointer convention.</param>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public virtual int ConvertUIToolkitPointerId(PointerEventData sourcePointerData)
        {
#if PACKAGE_UITOOLKIT
            return sourcePointerData.pointerId < 0 ?
                UIElements.PointerId.mousePointerId :
                UIElements.PointerId.touchPointerIdBase + sourcePointerData.pointerId;
#else
            return -1;
#endif
        }

        /// <remarks>
        /// Input Module implementations are free to apply a scaling factor to their PointerEventData's scrollDelta.
        /// This method can be used when a system needs to treat scaling-independent input values.
        /// </remarks>
        public virtual Vector2 ConvertPointerEventScrollDeltaToTicks(Vector2 scrollDelta)
        {
            return scrollDelta / input.mouseScrollDeltaPerTick;
        }

        /// <remarks>
        /// This method is used by UI Toolkit's TextField to allow navigation in and out
        /// of the TextField when using a device other than a keyboard.
        ///
        /// If we don't know what the source of the event is, this method should return Unknown.
        /// Input Modules with more information on the event source should override this method
        /// to reflect that information.
        ///
        /// This method's behavior is undefined for events other than Move, Submit, or Cancel.
        /// </remarks>
        /// <param name="eventData">An event generated by this input module.</param>
        /// <returns>The type of device that generated this event.</returns>
        public virtual NavigationDeviceType GetNavigationEventDeviceType(BaseEventData eventData)
        {
            return NavigationDeviceType.Unknown;
        }
    }

    /// <remarks>
    /// This can help avoid duplicated treatment of events when some controls react to keyboard input
    /// and navigation events at the same time.
    /// </remarks>
    public enum NavigationDeviceType
    {
        /// <remarks>
        /// Controls reacting to navigation events from an unknown device should react conservatively.
        /// For example, if there is a conflict between a keyboard event and a subsequent navigation event,
        /// a control could assume that the device type is a keyboard and conservatively block the navigation event.
        /// </remarks>
        Unknown = 0,
        /// <remarks>
        /// This device sends keyboard events along with any navigation event it generates.
        /// </remarks>
        Keyboard,
        /// <remarks>
        /// This device never sends keyboard events along with any navigation event it generates.
        /// </remarks>
        NonKeyboard
    }
}
