using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Event/Physics Raycaster")]
    [RequireComponent(typeof(Camera))]
    public class PhysicsRaycaster : BaseRaycaster
    {
        protected const int kNoEventMaskSet = -1;

        protected Camera m_EventCamera;

        [SerializeField]
        protected LayerMask m_EventMask = kNoEventMaskSet;

        [SerializeField]
        protected int m_MaxRayIntersections = 0;
        protected int m_LastMaxRayIntersections = 0;

#if PACKAGE_PHYSICS
        RaycastHit[] m_Hits;
#endif

        protected PhysicsRaycaster()
        {}

        public override Camera eventCamera
        {
            get
            {
                if (m_EventCamera == null)
                    m_EventCamera = GetComponent<Camera>();

                if (m_EventCamera == null)
                    return Camera.main;

                return m_EventCamera ;
            }
        }


        public virtual int depth
        {
            get { return (eventCamera != null) ? (int)eventCamera.depth : 0xFFFFFF; }
        }

        public int finalEventMask
        {
            get { return (eventCamera != null) ? eventCamera.cullingMask & m_EventMask : kNoEventMaskSet; }
        }

        public LayerMask eventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }

        /// <remarks>
        /// A value of zero will represent using the allocating version of the raycast function where as any other value will use the non allocating version.
        /// </remarks>
        public int maxRayIntersections
        {
            get { return m_MaxRayIntersections; }
            set { m_MaxRayIntersections = value; }
        }

        /// <param name="eventData">The pointer event for which we will cast a ray.</param>
        /// <param name="ray">The ray to use.</param>
        /// <param name="eventDisplayIndex">The display index used.</param>
        /// <param name="distanceToClipPlane">The distance between the near and far clipping planes along the ray.</param>
        /// <returns>True if the operation was successful. false if it was not possible to compute, such as the eventPosition being outside of the view.</returns>
        protected bool ComputeRayAndDistance(PointerEventData eventData, ref Ray ray, ref int eventDisplayIndex, ref float distanceToClipPlane)
        {
            if (eventCamera == null)
                return false;

            var eventPosition = MultipleDisplayUtilities.RelativeMouseAtScaled(eventData.position, eventData.displayIndex);
            if (eventPosition != Vector3.zero)
            {
                // We support multiple display and display identification based on event position.
                eventDisplayIndex = (int)eventPosition.z;

                // Discard events that are not part of this display so the user does not interact with multiple displays at once.
                if (eventDisplayIndex != eventCamera.targetDisplay)
                    return false;
            }
            else
            {
                // The multiple display system is not supported on all platforms, when it is not supported the returned position
                // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
                eventPosition = eventData.position;
            }

            // Cull ray casts that are outside of the view rect. (case 636595)
            if (!eventCamera.pixelRect.Contains(eventPosition))
                return false;

            ray = eventCamera.ScreenPointToRay(eventPosition);
            // compensate far plane distance - see MouseEvents.cs
            float projectionDirection = ray.direction.z;
            distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                ? Mathf.Infinity
                : Mathf.Abs((eventCamera.farClipPlane - eventCamera.nearClipPlane) / projectionDirection);
            return true;
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
#if PACKAGE_PHYSICS
            Ray ray = new Ray();
            int displayIndex = 0;
            float distanceToClipPlane = 0;
            if (!ComputeRayAndDistance(eventData, ref ray, ref displayIndex, ref distanceToClipPlane))
                return;

            int hitCount = 0;

            if (m_MaxRayIntersections == 0)
            {
                if (ReflectionMethodsCache.Singleton.raycast3DAll == null)
                    return;

                m_Hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, finalEventMask);
                hitCount = m_Hits.Length;
            }
            else
            {
                if (ReflectionMethodsCache.Singleton.getRaycastNonAlloc == null)
                    return;
                if (m_LastMaxRayIntersections != m_MaxRayIntersections)
                {
                    m_Hits = new RaycastHit[m_MaxRayIntersections];
                    m_LastMaxRayIntersections = m_MaxRayIntersections;
                }

                hitCount = ReflectionMethodsCache.Singleton.getRaycastNonAlloc(ray, m_Hits, distanceToClipPlane, finalEventMask);
            }

            if (hitCount != 0)
            {
                if (hitCount > 1)
                    System.Array.Sort(m_Hits, 0, hitCount, RaycastHitComparer.instance);

                for (int b = 0, bmax = hitCount; b < bmax; ++b)
                {
                    var result = new RaycastResult
                    {
                        gameObject = m_Hits[b].collider.gameObject,
                        module = this,
                        distance = m_Hits[b].distance,
                        worldPosition = m_Hits[b].point,
                        worldNormal = m_Hits[b].normal,
                        screenPosition = eventData.position,
                        displayIndex = displayIndex,
                        index = resultAppendList.Count,
                        sortingLayer = 0,
                        sortingOrder = 0
                    };
                    resultAppendList.Add(result);
                }
            }
#endif
        }

#if PACKAGE_PHYSICS
        private class RaycastHitComparer : IComparer<RaycastHit>
        {
            public static RaycastHitComparer instance = new RaycastHitComparer();
            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
#endif
    }
}
