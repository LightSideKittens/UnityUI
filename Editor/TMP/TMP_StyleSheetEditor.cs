using System;
using UnityEngine;
using UnityEditor;


namespace TMPro.EditorUtilities
{
    [CustomPropertyDrawer(typeof(TMP_Style))]
    public class StyleDrawer : PropertyDrawer
    {
        public static readonly float height = 95f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative("m_Name");
            SerializedProperty hashCodeProperty = property.FindPropertyRelative("m_HashCode");
            SerializedProperty openingDefinitionProperty = property.FindPropertyRelative("m_OpeningDefinition");
            SerializedProperty closingDefinitionProperty = property.FindPropertyRelative("m_ClosingDefinition");
            SerializedProperty openingDefinitionArray = property.FindPropertyRelative("m_OpeningTagArray");
            SerializedProperty closingDefinitionArray = property.FindPropertyRelative("m_ClosingTagArray");


            EditorGUIUtility.labelWidth = 86;
            position.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float labelHeight = position.height + 2f;

            EditorGUI.BeginChangeCheck();
            Rect rect0 = new Rect(position.x, position.y, (position.width) / 2 + 5, position.height);
            EditorGUI.PropertyField(rect0, nameProperty);
            if (EditorGUI.EndChangeCheck())
            {
                hashCodeProperty.intValue = TMP_TextUtilities.GetSimpleHashCode(nameProperty.stringValue);

                property.serializedObject.ApplyModifiedProperties();

                TMP_StyleSheet styleSheet = property.serializedObject.targetObject as TMP_StyleSheet;
                styleSheet.RefreshStyles();
            }

            Rect rect1 = new Rect(rect0.x + rect0.width + 5, position.y, 65, position.height);
            GUI.Label(rect1, "HashCode");
            GUI.enabled = false;
            rect1.x += 65;
            rect1.width = position.width / 2 - 75;
            EditorGUI.PropertyField(rect1, hashCodeProperty, GUIContent.none);

            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();

            position.y += labelHeight;
            GUI.Label(position, "Opening Tags");
            Rect textRect1 = new Rect(110, position.y, position.width - 86, 35);
            openingDefinitionProperty.stringValue =
                EditorGUI.TextArea(textRect1, openingDefinitionProperty.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                int size = openingDefinitionProperty.stringValue.Length;

                if (openingDefinitionArray.arraySize != size) openingDefinitionArray.arraySize = size;

                for (int i = 0; i < size; i++)
                {
                    SerializedProperty element = openingDefinitionArray.GetArrayElementAtIndex(i);
                    element.intValue = openingDefinitionProperty.stringValue[i];
                }
            }

            EditorGUI.BeginChangeCheck();

            position.y += 38;
            GUI.Label(position, "Closing Tags");
            Rect textRect2 = new Rect(110, position.y, position.width - 86, 35);
            closingDefinitionProperty.stringValue =
                EditorGUI.TextArea(textRect2, closingDefinitionProperty.stringValue);

            if (EditorGUI.EndChangeCheck())
            {
                int size = closingDefinitionProperty.stringValue.Length;

                if (closingDefinitionArray.arraySize != size) closingDefinitionArray.arraySize = size;

                for (int i = 0; i < size; i++)
                {
                    SerializedProperty element = closingDefinitionArray.GetArrayElementAtIndex(i);
                    element.intValue = closingDefinitionProperty.stringValue[i];
                }
            }
        }
    }


    [CustomEditor(typeof(TMP_StyleSheet)), CanEditMultipleObjects]
    public class TMP_StyleEditor : Editor
    {
        TMP_StyleSheet m_StyleSheet;
        SerializedProperty m_StyleListProp;

        int m_SelectedElement = -1;
        int m_Page;

        bool m_IsStyleSheetDirty;


        void OnEnable()
        {
            m_StyleSheet = target as TMP_StyleSheet;
            m_StyleListProp = serializedObject.FindProperty("m_StyleList");
        }


        public override void OnInspectorGUI()
        {
            Event currentEvent = Event.current;

            serializedObject.Update();

            m_IsStyleSheetDirty = false;
            int elementCount = m_StyleListProp.arraySize;
            int itemsPerPage = (Screen.height - 100) / 110;

            if (elementCount > 0)
            {
                for (int i = itemsPerPage * m_Page; i < elementCount && i < itemsPerPage * (m_Page + 1); i++)
                {
                    Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    SerializedProperty styleProperty = m_StyleListProp.GetArrayElementAtIndex(i);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(styleProperty);
                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                    }

                    Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                    Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width,
                        elementEndRegion.y - elementStartRegion.y);
                    if (DoSelectionCheck(selectionArea))
                    {
                        if (m_SelectedElement == i)
                        {
                            m_SelectedElement = -1;
                        }
                        else
                        {
                            m_SelectedElement = i;
                            GUIUtility.keyboardControl = 0;
                        }
                    }

                    if (m_SelectedElement == i)
                        TMP_EditorUtility.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));
                }
            }

            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            float totalWidth = rect.width;
            rect.width = totalWidth * 0.175f;

            bool guiEnabled = GUI.enabled;
            if (m_SelectedElement == -1 || m_SelectedElement == 0)
            {
                GUI.enabled = false;
            }

            if (GUI.Button(rect, "Up"))
            {
                SwapStyleElements(m_SelectedElement, m_SelectedElement - 1);
            }

            GUI.enabled = guiEnabled;

            rect.x += rect.width;
            if (m_SelectedElement == elementCount - 1)
            {
                GUI.enabled = false;
            }

            if (GUI.Button(rect, "Down"))
            {
                SwapStyleElements(m_SelectedElement, m_SelectedElement + 1);
            }

            GUI.enabled = guiEnabled;

            rect.x += rect.width + totalWidth * 0.3f;
            if (GUI.Button(rect, "+"))
            {
                int index = m_SelectedElement == -1 ? elementCount : m_SelectedElement;

                if (index > elementCount)
                    index = elementCount;

                m_StyleListProp.InsertArrayElementAtIndex(index);

                m_SelectedElement = index + 1;

                serializedObject.ApplyModifiedProperties();
                m_StyleSheet.RefreshStyles();
            }

            rect.x += rect.width;
            if (m_SelectedElement == -1 || m_SelectedElement >= elementCount) GUI.enabled = false;
            if (GUI.Button(rect, "-"))
            {
                int index = m_SelectedElement == -1 ? 0 : m_SelectedElement;

                m_StyleListProp.DeleteArrayElementAtIndex(index);

                m_SelectedElement = -1;
                serializedObject.ApplyModifiedProperties();
                m_StyleSheet.RefreshStyles();
                return;
            }

            if (itemsPerPage == 0) return;

            int shiftMultiplier = currentEvent.shift ? 10 : 1;

            Rect pagePos = EditorGUILayout.GetControlRect(false, 20);
            pagePos.width = totalWidth * 0.35f;

            if (m_Page > 0) GUI.enabled = true;
            else GUI.enabled = false;

            if (GUI.Button(pagePos, "Previous"))
                m_Page -= 1 * shiftMultiplier;

            GUI.enabled = true;
            pagePos.x += pagePos.width;
            pagePos.width = totalWidth * 0.30f;
            int totalPages = (int)(elementCount / (float)itemsPerPage + 0.999f);
            GUI.Label(pagePos, "Page " + (m_Page + 1) + " / " + totalPages, TMP_UIStyleManager.centeredLabel);

            pagePos.x += pagePos.width;
            pagePos.width = totalWidth * 0.35f;
            if (itemsPerPage * (m_Page + 1) < elementCount) GUI.enabled = true;
            else GUI.enabled = false;

            if (GUI.Button(pagePos, "Next"))
                m_Page += 1 * shiftMultiplier;

            m_Page = Mathf.Clamp(m_Page, 0, elementCount / itemsPerPage);


            if (serializedObject.ApplyModifiedProperties())
            {
                TMPro_EventManager.ON_TEXT_STYLE_PROPERTY_CHANGED(true);

                if (m_IsStyleSheetDirty)
                {
                    m_IsStyleSheetDirty = false;
                    m_StyleSheet.RefreshStyles();
                }
            }

            GUI.enabled = true;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                m_SelectedElement = -1;
        }


        static bool DoSelectionCheck(Rect selectionArea)
        {
            Event currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (selectionArea.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        currentEvent.Use();
                        return true;
                    }

                    break;
            }

            return false;
        }

        void SwapStyleElements(int selectedIndex, int newIndex)
        {
            m_StyleListProp.MoveArrayElement(selectedIndex, newIndex);
            m_SelectedElement = newIndex;
            m_IsStyleSheetDirty = true;
        }
    }
}