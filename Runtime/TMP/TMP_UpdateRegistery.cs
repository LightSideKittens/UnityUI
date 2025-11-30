using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Collections;
using System.Collections;
using System.Collections.Generic;


namespace TMPro
{
    /// <summary>
    /// Class for handling and scheduling text object updates.
    /// </summary>
    public class TMP_UpdateRegistry
    {
        private static TMP_UpdateRegistry s_Instance;

        private readonly List<ICanvasElement> m_LayoutRebuildQueue = new();
        private HashSet<int> m_LayoutQueueLookup = new();

        private readonly List<ICanvasElement> m_GraphicRebuildQueue = new();
        private HashSet<int> m_GraphicQueueLookup = new();

        /// <summary>
        /// Get a singleton instance of the registry
        /// </summary>
        public static TMP_UpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new();
                return s_Instance;
            }
        }


        /// <summary>
        /// Register to receive callback from the Canvas System.
        /// </summary>
        protected TMP_UpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdateForCanvasRendererObjects;
        }


        /// <summary>
        /// Function to register elements which require a layout rebuild.
        /// </summary>
        /// <param name="element"></param>
        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            int id = (element as Object).GetInstanceID();

            if (m_LayoutQueueLookup.Contains(id))
                return false;

            m_LayoutQueueLookup.Add(id);
            m_LayoutRebuildQueue.Add(element);

            return true;
        }


        /// <summary>
        /// Function to register elements which require a graphic rebuild.
        /// </summary>
        /// <param name="element"></param>
        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            int id = (element as Object).GetInstanceID();

            if (m_GraphicQueueLookup.Contains(id))
                return false;

            m_GraphicQueueLookup.Add(id);
            m_GraphicRebuildQueue.Add(element);

            return true;
        }


        /// <summary>
        /// Method to handle objects that need updating.
        /// </summary>
        private void PerformUpdateForCanvasRendererObjects()
        {
            for (int index = 0; index < m_LayoutRebuildQueue.Count; index++)
            {
                ICanvasElement element = instance.m_LayoutRebuildQueue[index];

                element.Rebuild(CanvasUpdate.Prelayout);
            }

            if (m_LayoutRebuildQueue.Count > 0)
            {
                m_LayoutRebuildQueue.Clear();
                m_LayoutQueueLookup.Clear();
            }


            for (int index = 0; index < m_GraphicRebuildQueue.Count; index++)
            {
                ICanvasElement element = instance.m_GraphicRebuildQueue[index];

                element.Rebuild(CanvasUpdate.PreRender);
            }

            if (m_GraphicRebuildQueue.Count > 0)
            {
                m_GraphicRebuildQueue.Clear();
                m_GraphicQueueLookup.Clear();
            }
        }


        /// <summary>
        /// Method to handle objects that need updating.
        /// </summary>
        private void PerformUpdateForMeshRendererObjects()
        {
            Debug.Log("Perform update of MeshRenderer objects.");
        }


        /// <summary>
        /// Function to unregister elements which no longer require a rebuild.
        /// </summary>
        /// <param name="element"></param>
        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }


        private void InternalUnRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            int id = (element as Object).GetInstanceID();

            instance.m_LayoutRebuildQueue.Remove(element);
            m_GraphicQueueLookup.Remove(id);
        }


        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            int id = (element as Object).GetInstanceID();

            instance.m_GraphicRebuildQueue.Remove(element);
            m_LayoutQueueLookup.Remove(id);
        }
    }
}
