using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_SubMeshUI)), CanEditMultipleObjects]
    public class TMP_SubMeshUI_Editor : Editor
    {
        private SerializedProperty fontAsset_prop;

        private Material m_targetMaterial;


        public void OnEnable()
        {
            fontAsset_prop = serializedObject.FindProperty("m_fontAsset");
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(fontAsset_prop);
            GUI.enabled = true;

            EditorGUILayout.Space();
        }
    }
}
