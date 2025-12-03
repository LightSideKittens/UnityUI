using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    public class GraphicRegistry
    {
        private static GraphicRegistry s_Instance;

        private readonly Dictionary<Canvas, IndexedSet<Graphic>> m_Graphics = new Dictionary<Canvas, IndexedSet<Graphic>>();
        private readonly Dictionary<Canvas, IndexedSet<Graphic>> m_RaycastableGraphics = new Dictionary<Canvas, IndexedSet<Graphic>>();

        protected GraphicRegistry()
        {
            // Avoid runtime generation of these types. Some platforms are AOT only and do not support
            // JIT. What's more we actually create a instance of the required types instead of
            // just declaring an unused variable which may be optimized away by some compilers (Mono vs MS).

            // See: 877060

            System.GC.KeepAlive(new Dictionary<Graphic, int>());
            System.GC.KeepAlive(new Dictionary<ICanvasElement, int>());
            System.GC.KeepAlive(new Dictionary<IClipper, int>());
        }

        public static GraphicRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new GraphicRegistry();
                return s_Instance;
            }
        }

        /// <param name="c">The canvas being associated with the Graphic.</param>
        /// <param name="graphic">The Graphic being associated with the Canvas.</param>
        public static void RegisterGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null || graphic == null)
                return;

            IndexedSet<Graphic> graphics;
            instance.m_Graphics.TryGetValue(c, out graphics);

            if (graphics != null)
            {
                graphics.AddUnique(graphic);

                RegisterRaycastGraphicForCanvas(c, graphic);

                return;
            }

            // Dont need to AddUnique as we know its the only item in the list
            graphics = new IndexedSet<Graphic>();
            graphics.Add(graphic);
            instance.m_Graphics.Add(c, graphics);

            RegisterRaycastGraphicForCanvas(c, graphic);
        }

        /// <param name="c">The canvas being associated with the Graphic.</param>
        /// <param name="graphic">The Graphic being associated with the Canvas.</param>
        public static void RegisterRaycastGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null || graphic == null || !graphic.raycastTarget)
                return;

            IndexedSet<Graphic> graphics;
            instance.m_RaycastableGraphics.TryGetValue(c, out graphics);

            if (graphics != null)
            {
                graphics.AddUnique(graphic);

                return;
            }

            // Dont need to AddUnique as we know its the only item in the list
            graphics = new IndexedSet<Graphic>();
            graphics.Add(graphic);
            instance.m_RaycastableGraphics.Add(c, graphics);
        }

        /// <param name="c">The Canvas to dissociate from the Graphic.</param>
        /// <param name="graphic">The Graphic to dissociate from the Canvas.</param>
        public static void UnregisterGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null || graphic == null)
                return;

            IndexedSet<Graphic> graphics;
            if (instance.m_Graphics.TryGetValue(c, out graphics))
            {
                graphics.Remove(graphic);

                if (graphics.Capacity == 0)
                    instance.m_Graphics.Remove(c);

                UnregisterRaycastGraphicForCanvas(c, graphic);
            }
        }

        /// <param name="c">The Canvas to dissociate from the Graphic.</param>
        /// <param name="graphic">The Graphic to dissociate from the Canvas.</param>
        public static void UnregisterRaycastGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null || graphic == null)
                return;

            IndexedSet<Graphic> graphics;
            if (instance.m_RaycastableGraphics.TryGetValue(c, out graphics))
            {
                graphics.Remove(graphic);

                if (graphics.Count == 0)
                    instance.m_RaycastableGraphics.Remove(c);
            }
        }

        /// <param name="c">The Canvas to dissociate from the Graphic.</param>
        /// <param name="graphic">The Graphic to dissociate from the Canvas.</param>
        public static void DisableGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null)
                return;

            IndexedSet<Graphic> graphics;
            if (instance.m_Graphics.TryGetValue(c, out graphics))
            {
                graphics.DisableItem(graphic);

                if (graphics.Capacity == 0)
                    instance.m_Graphics.Remove(c);

                DisableRaycastGraphicForCanvas(c, graphic);
            }
        }

        /// <param name="c">The Canvas to dissociate from the Graphic.</param>
        /// <param name="graphic">The Graphic to dissociate from the Canvas.</param>
        public static void DisableRaycastGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null || !graphic.raycastTarget)
                return;

            IndexedSet<Graphic> graphics;
            if (instance.m_RaycastableGraphics.TryGetValue(c, out graphics))
            {
                graphics.DisableItem(graphic);

                if (graphics.Capacity == 0)
                    instance.m_RaycastableGraphics.Remove(c);
            }
        }

        private static readonly List<Graphic> s_EmptyList = new List<Graphic>();

        /// <param name="canvas">The Canvas to search</param>
        /// <returns>Returns a list of Graphics. Returns an empty list if no Graphics are associated with the specified Canvas.</returns>
        public static IList<Graphic> GetGraphicsForCanvas(Canvas canvas)
        {
            IndexedSet<Graphic> graphics;
            if (instance.m_Graphics.TryGetValue(canvas, out graphics))
                return graphics;

            return s_EmptyList;
        }

        /// <param name="canvas">The Canvas to search</param>
        /// <returns>Returns a list of Graphics. Returns an empty list if no Graphics are associated with the specified Canvas.</returns>
        public static IList<Graphic> GetRaycastableGraphicsForCanvas(Canvas canvas)
        {
            IndexedSet<Graphic> graphics;
            if (instance.m_RaycastableGraphics.TryGetValue(canvas, out graphics))
                return graphics;

            return s_EmptyList;
        }
    }
}
