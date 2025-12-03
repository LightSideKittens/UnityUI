using UnityEngine.UIElements;

namespace UnityEngine.EventSystems
{
    public struct RaycastResult
    {
        private GameObject m_GameObject; // Game object hit by the raycast

        public GameObject gameObject
        {
            get { return m_GameObject; }
            set { m_GameObject = value; }
        }

        public BaseRaycaster module;

        public float distance;

        public float index;

        public int depth;

        /// <remarks>
        /// For UI.Graphic elements will always be 0.
        /// For 3D objects this will always be 0.
        /// For 2D objects if a SortingOrder is influencing the same object as the hit collider then the renderers sortingGroupID will be used; otherwise SortingGroup.invalidSortingGroupID.
        /// </remarks>
        public int sortingGroupID;

        /// <remarks>
        /// For UI.Graphic elements this will always be 0.
        /// For 3D objects this will always be 0.
        /// For 2D objects if a SortingOrder is influencing the same object as the hit collider then the renderers sortingGroupOrder will be used.
        /// </remarks>
        public int sortingGroupOrder;

        /// <remarks>
        /// For UI.Graphic elements this will be the values from that graphic's Canvas
        /// For 3D objects this will always be 0.
        /// For 2D objects if a 2D Renderer (Sprite, Tilemap, SpriteShape) is attached to the same object as the hit collider that sortingLayerID will be used.
        /// </remarks>
        public int sortingLayer;

        /// <remarks>
        /// For Graphic elements this will be the values from that graphics Canvas
        /// For 3D objects this will always be 0.
        /// For 2D objects if a 2D Renderer (Sprite, Tilemap, SpriteShape) is attached to the same object as the hit collider that sortingOrder will be used.
        /// </remarks>
        public int sortingOrder;

        public Vector3 origin;

        public Vector3 worldPosition;

        public Vector3 worldNormal;

        public Vector2 screenPosition;

        public int displayIndex;

        public bool isValid
        {
            get { return module != null && gameObject != null; }
        }

        // This code is disabled unless the com.unity.modules.uielements module is present.
        // The UIElements module is always present in the Editor but it can be stripped from a project build if unused.
#if PACKAGE_UITOOLKIT
        /// <remarks>This is only useful in the context of EventSystem UI Toolkit interoperability.</remarks>
        /// <seealso cref="UnityEngine.UIElements.EventSystemUIToolkitInteroperabilityBridge"/>
        public UIDocument document;

        /// <remarks>This is only useful in the context of EventSystem UI Toolkit interoperability.</remarks>
        /// <seealso cref="UnityEngine.UIElements.EventSystemUIToolkitInteroperabilityBridge"/>
        public VisualElement element;
#endif

        public void Clear()
        {
            gameObject = null;
            module = null;
            distance = 0;
            index = 0;
            depth = 0;
            sortingLayer = 0;
            sortingOrder = 0;
            origin = Vector3.zero;
            worldNormal = Vector3.up;
            worldPosition = Vector3.zero;
            screenPosition = Vector3.zero;
            displayIndex = 0;
#if PACKAGE_UITOOLKIT
            document = null;
            element = null;
#endif
        }

        public override string ToString()
        {
            if (!isValid)
                return "";

            return "Name: " + gameObject + "\n" +
                "module: " + module + "\n" +
                "distance: " + distance + "\n" +
                "index: " + index + "\n" +
                "depth: " + depth + "\n" +
                "worldNormal: " + worldNormal + "\n" +
                "worldPosition: " + worldPosition + "\n" +
                "screenPosition: " + screenPosition + "\n" +
                "module.sortOrderPriority: " + module.sortOrderPriority + "\n" +
                "module.renderOrderPriority: " + module.renderOrderPriority + "\n" +
                "sortingLayer: " + sortingLayer + "\n" +
                "sortingOrder: " + sortingOrder;
        }
    }
}
