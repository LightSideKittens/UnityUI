#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    public static class GraphicRebuildTracker
    {
        static IndexedSet<Graphic> m_Tracked = new IndexedSet<Graphic>();
        static bool s_Initialized;

        /// <param name="g">The graphic to track</param>
        public static void TrackGraphic(Graphic g)
        {
            if (!s_Initialized)
            {
                CanvasRenderer.onRequestRebuild += OnRebuildRequested;
                s_Initialized = true;
            }

            m_Tracked.AddUnique(g);
        }

        /// <param name="g">The graphic to remove from tracking.</param>
        public static void UnTrackGraphic(Graphic g)
        {
            m_Tracked.Remove(g);
        }

        /// <param name="g">The graphic to remove from tracking.</param>
        public static void DisableTrackGraphic(Graphic g)
        {
            m_Tracked.DisableItem(g);
        }

        static void OnRebuildRequested()
        {
            StencilMaterial.ClearAll();
            for (int i = 0; i < m_Tracked.Count; i++)
            {
                m_Tracked[i].OnRebuildRequested();
            }
        }
    }
}
#endif // if UNITY_EDITOR
