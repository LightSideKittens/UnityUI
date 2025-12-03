using UnityEngine.EventSystems;

namespace UnityEditor.EventSystems
{
    [CustomEditor(typeof(PhysicsRaycaster), true)]
    public class PhysicsRaycasterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
#if !PACKAGE_PHYSICS
            EditorGUILayout.HelpBox("Physics module is not present. This Raycaster will have no effect", MessageType.Warning);
#endif
        }
    }
}
