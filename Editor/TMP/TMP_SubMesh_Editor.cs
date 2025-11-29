using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_SubMesh)), CanEditMultipleObjects]
    public class TMP_SubMesh_Editor : Editor
    {
        private struct m_foldout
        {
            public static bool fontSettings = true;
        }

        private SerializedProperty fontAsset_prop;
        private SerializedProperty spriteAsset_prop;

        private TMP_SubMesh m_SubMeshComponent;
        private Renderer m_Renderer;

        private string[] m_SortingLayerNames;

        public void OnEnable()
        {
            fontAsset_prop = serializedObject.FindProperty("m_fontAsset");
            spriteAsset_prop = serializedObject.FindProperty("m_spriteAsset");

            m_SubMeshComponent = target as TMP_SubMesh;

            m_Renderer = m_SubMeshComponent.renderer;

            m_SortingLayerNames = SortingLayerHelper.sortingLayerNames;
        }


        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 0;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(fontAsset_prop);
            EditorGUILayout.PropertyField(spriteAsset_prop);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();

            string oldName = SortingLayer.IDToName(m_Renderer.sortingLayerID);

            int oldLayerIndex = System.Array.IndexOf(m_SortingLayerNames, oldName);

            int newLayerIndex = EditorGUILayout.Popup("Sorting Layer", oldLayerIndex, m_SortingLayerNames);

            if (newLayerIndex != oldLayerIndex)
                m_Renderer.sortingLayerID = SortingLayer.NameToID(m_SortingLayerNames[newLayerIndex]);

            int newSortingLayerOrder = EditorGUILayout.IntField("Order in Layer", m_Renderer.sortingOrder);
            if (newSortingLayerOrder != m_Renderer.sortingOrder)
                m_Renderer.sortingOrder = newSortingLayerOrder;

        }
    }
}
