using System;
using System.Diagnostics;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    public enum CanvasUpdate
    {
        Prelayout = 0,
        Layout = 1,
        PostLayout = 2,
        PreRender = 3
    }

    public interface ICanvasElement
    {
        void Rebuild(CanvasUpdate executing);
        Transform transform { get; }
    }

    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        private bool m_PerformingLayoutUpdate;
        private bool m_PerformingGraphicUpdate;

        private string[] m_CanvasUpdateProfilerStrings = new string[]
            { "CanvasUpdate.Prelayout", "CanvasUpdate.Layout", "CanvasUpdate.PostLayout", "CanvasUpdate.PreRender" };

        private const string m_CullingUpdateProfilerString = "ClipperRegistry.Cull";

        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new IndexedSet<ICanvasElement>();
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        public static event Action Updated;

        protected CanvasUpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdate;
        }

        public static CanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        private static readonly Comparison<ICanvasElement> s_SortLayoutFunction = SortLayoutList;

        private void PerformUpdate()
        {
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);

            m_PerformingLayoutUpdate = true;

            RemoveDestroyed(m_LayoutRebuildQueue);
            m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);

            for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
            {
                Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);

                for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
                {
                    var rebuild = m_LayoutRebuildQueue[j];
                    try
                    {
                        rebuild.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, rebuild.transform);
                    }
                }

                Profiling.Profiler.EndSample();
            }

            m_LayoutRebuildQueue.Clear();
            m_PerformingLayoutUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);
            Profiling.Profiler.BeginSample(m_CullingUpdateProfilerString);
            ClipperRegistry.instance.Cull();
            Profiling.Profiler.EndSample();

            m_PerformingGraphicUpdate = true;
            RemoveDestroyed(m_GraphicRebuildQueue);

            Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[3]);
            for (var k = 0; k < m_GraphicRebuildQueue.Count; k++)
            {
                try
                {
                    var element = m_GraphicRebuildQueue[k];
                    element.Rebuild(CanvasUpdate.PreRender);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, m_GraphicRebuildQueue[k].transform);
                }
            }

            Profiling.Profiler.EndSample();

            m_GraphicRebuildQueue.Clear();
            m_PerformingGraphicUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
            Updated?.Invoke();
        }

        private static bool lastIsPlaying = false;

        [Conditional("UNITY_EDITOR")]
        private static void RemoveDestroyed(IndexedSet<ICanvasElement> list)
        {
            if (!Application.isPlaying)
            {
                lastIsPlaying = false;
            }

            if (lastIsPlaying) return;

            lastIsPlaying = Application.isPlaying;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Object obj)
                {
                    if (obj == null)
                    {
                        list.RemoveAt(i--);
                    }
                }
            }
        }

        private static int ParentCount(Transform child)
        {
            if (child == null)
                return 0;

            var parent = child.parent;
            int count = 0;
            while (parent != null)
            {
                count++;
                parent = parent.parent;
            }

            return count;
        }

        private static int SortLayoutList(ICanvasElement x, ICanvasElement y)
        {
            Transform t1 = x.transform;
            Transform t2 = y.transform;

            return ParentCount(t1) - ParentCount(t2);
        }

        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_LayoutRebuildQueue.Contains(element))
                return false;

            /* TODO: this likely should be here but causes the error to show just resizing the game view (case 739376)
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for layout rebuild while we are already inside a layout rebuild loop. This is not supported.", element));
                return false;
            }*/

            return m_LayoutRebuildQueue.AddUnique(element);
        }

        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        public static bool TryRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.",
                    element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        public static void DisableCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalDisableCanvasElementForLayoutRebuild(element);
            instance.InternalDisableCanvasElementForGraphicRebuild(element);
        }

        private void InternalUnRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.",
                    element));
                return;
            }

            instance.m_LayoutRebuildQueue.Remove(element);
        }

        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.",
                    element));
                return;
            }

            instance.m_GraphicRebuildQueue.Remove(element);
        }

        private void InternalDisableCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.",
                    element));
                return;
            }

            instance.m_LayoutRebuildQueue.DisableItem(element);
        }

        private void InternalDisableCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.",
                    element));
                return;
            }

            instance.m_GraphicRebuildQueue.DisableItem(element);
        }

        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}