using System;
using System.Text;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    public class PointerEventData : BaseEventData
    {
        public enum InputButton
        {
            Left = 0,

            Right = 1,

            Middle = 2
        }

        public enum FramePressState
        {
            Pressed,

            Released,

            PressedAndReleased,

            NotChanged
        }

        public GameObject pointerEnter { get; set; }

        // The object that received OnPointerDown
        private GameObject m_PointerPress;

        public GameObject lastPress { get; private set; }

        public GameObject rawPointerPress { get; set; }

        public GameObject pointerDrag { get; set; }

        public GameObject pointerClick { get; set; }

        public RaycastResult pointerCurrentRaycast { get; set; }

        public RaycastResult pointerPressRaycast { get; set; }

        public List<GameObject> hovered = new List<GameObject>();

        public bool eligibleForClick { get; set; }

        public int displayIndex { get; set; }

        public int pointerId { get; set; }

        public Vector2 position { get; set; }

        public Vector2 delta { get; set; }

        public Vector2 pressPosition { get; set; }

        public float clickTime { get; set; }

        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerDownHandler
        /// {
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///         //Grab the number of consecutive clicks and assign it to an integer varible.
        ///         int i = eventData.clickCount;
        ///         //Display the click count.
        ///         Debug.Log(i);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public int clickCount { get; set; }

        public Vector2 scrollDelta { get; set; }

        /// <remarks>
        /// If you do not want a drag threshold set this to false in IInitializePotentialDragHandler.OnInitializePotentialDrag.
        /// </remarks>
        public bool useDragThreshold { get; set; }

        public bool dragging { get; set; }

        public InputButton button { get; set; }


        /// <remarks>
        /// If the device does not report pressure, the value of this property is 1.0f.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float pressure { get; set; }
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float tangentialPressure { get; set; }
        /// <remarks>
        /// A value of 0 indicates that the stylus is parallel to the surface. A value of pi/2 indicates that it is perpendicular to the surface.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float altitudeAngle { get; set; }
        /// <remarks>
        /// A value of 0 indicates that the stylus is pointed along the x-axis of the device.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float azimuthAngle { get; set; }
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float twist { get; set; }
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public Vector2 tilt { get; set; }
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public PenStatus penStatus { get; set; }
        /// <remarks>
        /// Add `radiusVariance` to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public Vector2 radius { get; set; }
        /// <remarks>
        /// Add this value to the radius to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        public Vector2 radiusVariance { get; set; }
        public bool fullyExited { get; set; }
        public bool reentered { get; set; }
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />

        public PointerEventData(EventSystem eventSystem) : base(eventSystem)
        {
            eligibleForClick = false;

            displayIndex = 0;
            pointerId = -1;
            position = Vector2.zero; // Current position of the mouse or touch event
            delta = Vector2.zero; // Delta since last update
            pressPosition = Vector2.zero; // Delta since the event started being tracked
            clickTime = 0.0f; // The last time a click event was sent out (used for double-clicks)
            clickCount = 0; // Number of clicks in a row. 2 for a double-click for example.

            scrollDelta = Vector2.zero;
            useDragThreshold = true;
            dragging = false;
            button = InputButton.Left;

            pressure = 0f;
            tangentialPressure = 0f;
            altitudeAngle = 0f;
            azimuthAngle = 0f;
            twist = 0f;
            tilt = new Vector2(0f, 0f);
            penStatus = PenStatus.None;
            radius = Vector2.zero;
            radiusVariance = Vector2.zero;
        }

        public bool IsPointerMoving()
        {
            return delta.sqrMagnitude > 0.0f;
        }

        public bool IsScrolling()
        {
            return scrollDelta.sqrMagnitude > 0.0f;
        }

        public Camera enterEventCamera
        {
            get { return pointerCurrentRaycast.module == null ? null : pointerCurrentRaycast.module.eventCamera; }
        }

        public Camera pressEventCamera
        {
            get { return pointerPressRaycast.module == null ? null : pointerPressRaycast.module.eventCamera; }
        }

        public GameObject pointerPress
        {
            get { return m_PointerPress; }
            set
            {
                if (m_PointerPress == value)
                    return;

                lastPress = m_PointerPress;
                m_PointerPress = value;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Position</b>: " + position);
            sb.AppendLine("<b>delta</b>: " + delta);
            sb.AppendLine("<b>eligibleForClick</b>: " + eligibleForClick);
            sb.AppendLine("<b>pointerEnter</b>: " + pointerEnter);
            sb.AppendLine("<b>pointerPress</b>: " + pointerPress);
            sb.AppendLine("<b>lastPointerPress</b>: " + lastPress);
            sb.AppendLine("<b>pointerDrag</b>: " + pointerDrag);
            sb.AppendLine("<b>Use Drag Threshold</b>: " + useDragThreshold);
            sb.AppendLine("<b>Current Raycast:</b>");
            sb.AppendLine(pointerCurrentRaycast.ToString());
            sb.AppendLine("<b>Press Raycast:</b>");
            sb.AppendLine(pointerPressRaycast.ToString());
            sb.AppendLine("<b>Display Index:</b>");
            sb.AppendLine(displayIndex.ToString());
            sb.AppendLine("<b>pressure</b>: " + pressure);
            sb.AppendLine("<b>tangentialPressure</b>: " + tangentialPressure);
            sb.AppendLine("<b>altitudeAngle</b>: " + altitudeAngle);
            sb.AppendLine("<b>azimuthAngle</b>: " + azimuthAngle);
            sb.AppendLine("<b>twist</b>: " + twist);
            sb.AppendLine("<b>tilt</b>: " + tilt);
            sb.AppendLine("<b>penStatus</b>: " + penStatus);
            sb.AppendLine("<b>radius</b>: " + radius);
            sb.AppendLine("<b>radiusVariance</b>: " + radiusVariance);
            return sb.ToString();
        }
    }
}
