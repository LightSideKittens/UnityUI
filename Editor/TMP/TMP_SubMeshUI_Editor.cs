using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_SubMeshUI)), CanEditMultipleObjects]
    public class TMP_SubMeshUI_Editor : Editor
    {
        private struct m_foldout
        {
            public static bool fontSettings = true;
        }

        private SerializedProperty fontAsset_prop;
        private SerializedProperty spriteAsset_prop;

        private Material m_targetMaterial;


        public void OnEnable()
        {
            fontAsset_prop = serializedObject.FindProperty("m_fontAsset");
            spriteAsset_prop = serializedObject.FindProperty("m_spriteAsset");
        }


        public void OnDisable()
        {
        }



        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(fontAsset_prop);
            EditorGUILayout.PropertyField(spriteAsset_prop);
            GUI.enabled = true;

            EditorGUILayout.Space();
        }

    }
}
