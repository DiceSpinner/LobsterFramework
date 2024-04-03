using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LobsterFramework.Editors
{
    [CustomPropertyDrawer(typeof(FilePathAttribute))]
    public class FilePathAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float buttonWidth = 40;
            position.width -= buttonWidth;
            EditorGUI.PropertyField(position, property, label);
            Rect buttonPos = position;

            buttonPos.width = buttonWidth;
            buttonPos.x = position.x + position.width;
            if (GUI.Button(buttonPos, "..."))
            {
                FilePathAttribute attr = (FilePathAttribute)attribute;
                string defaultName = attr.defaultName == "" ? property.stringValue : attr.defaultName;
                string path = EditorUtility.SaveFilePanel("Select File Path", "Assets", defaultName, attr.extension);
                if (path != "")
                {
                    property.stringValue = path;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
