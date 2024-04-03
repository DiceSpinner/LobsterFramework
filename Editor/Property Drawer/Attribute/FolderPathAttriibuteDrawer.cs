using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LobsterFramework.Editors
{
    [CustomPropertyDrawer(typeof(FolderPathAttribute))]
    public class FolderPathAttriibuteDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float buttonWidth = 40;
            position.width -= buttonWidth;
            EditorGUI.PropertyField(position, property, label);
            Rect buttonPos = position; 
           
            buttonPos.width = buttonWidth;
            buttonPos.x = position.x + position.width;
            if (GUI.Button(buttonPos, "...")) {
                string path = EditorUtility.OpenFolderPanel("Select Folder Path", "Assets", "");
                if (path != "") {
                    property.stringValue = path;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
