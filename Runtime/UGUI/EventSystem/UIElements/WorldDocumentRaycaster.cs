using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.UIElements
{
    // This code is disabled unless the com.unity.modules.uielements module is present.
    // The UIElements module is always present in the Editor but it can be stripped from a project build if unused.
#if PACKAGE_UITOOLKIT
    [AddComponentMenu("UI Toolkit/World Document Raycaster (UI Toolkit)")]
    public class WorldDocumentRaycaster : BaseRaycaster
    {
        [SerializeField] private Camera m_EventCamera;

        public override Camera eventCamera => m_EventCamera;

        public new Camera camera
        {
            get => m_EventCamera;
            set => m_EventCamera = value;
        }

        private static PhysicsDocumentPicker worldPicker = new();


        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var currentInputModule = EventSystem.current != null ? EventSystem.current.currentInputModule : null;
            if (currentInputModule == null)
                return;

            if (!GetWorldRay(eventData, out var worldRay, out var maxDistance, out var layerMask))
                return;

            maxDistance = Mathf.Min(maxDistance, EventSystem.current.uiToolkitInterop.worldPickingMaxDistance);
            layerMask &= EventSystem.current.uiToolkitInterop.worldPickingLayers;

            var pointerId = currentInputModule.ConvertUIToolkitPointerId(eventData);

            var capturingCamera = PointerDeviceState.GetCameraWithSoftPointerCapture(pointerId);
            if (capturingCamera != null)
            {
                var cam = m_EventCamera != null ? m_EventCamera : Camera.main;
                if (capturingCamera != cam)
                    return;
            }

            if (!worldPicker.TryPickWithCapture(pointerId, worldRay, maxDistance, layerMask, out _,
                    out var document, out var elementUnderPointer, out var distance, out var captured))
                return;

            resultAppendList.Add(new RaycastResult
            {
                // Discard hits against non-UI objects. They should block UI but not hide the PhysicsRaycaster results.
                gameObject = document == null ? gameObject : document.containerPanel.selectableGameObject,
                origin = worldRay.origin,
                worldPosition = worldRay.origin + distance * worldRay.direction,
                document = document,
                element = elementUnderPointer,
                module = this,
                distance = distance,
                sortingOrder = captured ? int.MaxValue : 0,
            });
        }


        protected virtual bool GetWorldRay(PointerEventData eventData, out Ray worldRay, out float maxDistance,
            out int layerMask)
        {
            var cam = m_EventCamera != null ? m_EventCamera : Camera.main;
            if (cam == null)
            {
                worldRay = default;
                maxDistance = 0;
                layerMask = 0;
                return false;
            }

            maxDistance = cam.farClipPlane;
            layerMask = cam.cullingMask;

            Vector3 eventPosition = MultipleDisplayUtilities.GetRelativeMousePositionForRaycast(eventData);

            // Discard events that are not part of this display so the user does not interact with multiple displays at once.
            if ((int)eventPosition.z != cam.targetDisplay)
            {
                worldRay = default;
                return false;
            }

            worldRay = cam.ScreenPointToRay(eventPosition);

            return true;
        }
    }
#endif
}