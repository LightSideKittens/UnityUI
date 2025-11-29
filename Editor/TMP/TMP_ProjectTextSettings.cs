#if !UNITY_2018_3_OR_NEWER
using UnityEditor;

namespace TMPro
{

    public static class TMP_ProjectTextSettings
    {
        [MenuItem("Edit/Project Settings/TextMeshPro Settings", false, 309)]
        public static void SelectProjectTextSettings()
        {
            TMP_Settings textSettings = TMP_Settings.instance;

            if (textSettings)
            {
                Selection.activeObject = textSettings;

                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(textSettings);
            }
            else
                TMPro_EventManager.RESOURCE_LOAD_EVENT.Add(ON_RESOURCES_LOADED);
        }


        static void ON_RESOURCES_LOADED()
        {
            TMPro_EventManager.RESOURCE_LOAD_EVENT.Remove(ON_RESOURCES_LOADED);

            TMP_Settings textSettings = TMP_Settings.instance;

            Selection.activeObject = textSettings;

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(textSettings);
        }
    }
}
#endif