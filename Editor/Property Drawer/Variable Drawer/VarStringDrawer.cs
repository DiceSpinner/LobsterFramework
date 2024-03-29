using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(VarString))]
public class VarStringDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null || !property.isExpanded)
        {
            return base.GetPropertyHeight(property, label);
        }
        return base.GetPropertyHeight(property, label) * 2;

    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Foldout
        Rect foldOutRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        if (property.objectReferenceValue == null)
        {
            property.isExpanded = false;
            property.objectReferenceValue = (VarString)EditorGUI.ObjectField(position, label.text, property.objectReferenceValue, typeof(VarString), true);
            property.serializedObject.ApplyModifiedProperties();
            return;
        }
        else {
            property.isExpanded = EditorGUI.Foldout(foldOutRect, property.isExpanded, label);
        }

        Rect rect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.objectReferenceValue = (VarString)EditorGUI.ObjectField(rect, " ", property.objectReferenceValue, typeof(VarString), true);
        if (property.isExpanded && property.objectReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            SerializedObject obj = new SerializedObject(property.objectReferenceValue);
            SerializedProperty child = obj.FindProperty("value");
            Rect rect1 = new(position.x, rect.y + EditorGUIUtility.singleLineHeight * 1.2f, position.width, rect.height);
            child.stringValue = EditorGUI.TextField(rect1, "Value", child.stringValue);
            obj.ApplyModifiedProperties();
            EditorGUI.indentLevel--;
        }
        property.serializedObject.ApplyModifiedProperties();
    }
}
