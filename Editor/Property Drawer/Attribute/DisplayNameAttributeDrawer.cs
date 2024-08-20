using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LobsterFramework.Editors
{
    [CustomPropertyDrawer(typeof(DisplayNameAttribute))]
    public class DisplayNameAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string name = ((DisplayNameAttribute)attribute).name;
            label.text = name;
            EditorGUI.PropertyField(position, property, label);
        }
    }
}

