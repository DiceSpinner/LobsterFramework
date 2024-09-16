using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using System;
using LobsterFramework.Utility;
using System.Reflection;

namespace LobsterFramework.Editors
{
    [CustomPropertyDrawer(typeof(AbilitySelector))]
    public sealed class AbilitySelectorDrawer : PropertyDrawer
    {
        private AbilitySelectorPopup popup;
        internal string newSelection = "";
        private bool isExpanded = false;

        public AbilitySelectorDrawer() { 
            popup = new AbilitySelectorPopup(this);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        private GUIContent mock = new(" ");
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (popup.restriction == null) {
                popup.restriction = fieldInfo.GetCustomAttribute<RestrictAbilityTypeAttribute>();
            }
            
            while(property.name != nameof(SerializableType.typeName)) {
                property.Next(true);
            }
            if (newSelection != "")
            {
                property.stringValue = newSelection;
                newSelection = "";
            }

            Rect totalSpace = new(position);
            totalSpace.height = EditorGUIUtility.singleLineHeight;
            Rect foldoutRect = new(totalSpace);
            foldoutRect.width = EditorGUIUtility.labelWidth;
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, label);            
            Rect rect1  = EditorGUI.PrefixLabel(totalSpace, GUIUtility.GetControlID(FocusType.Keyboard), mock);

            bool buttonPressed;
            Type abilityType = Utility.TypeCache.GetTypeByName(property.stringValue);
            if (abilityType == null || !AddAbilityMenuAttribute.abilityDisplayEntries.ContainsKey(abilityType)) {
                buttonPressed = GUI.Button(rect1, "None", EditorStyles.miniPullDown);
            }
            else {
                buttonPressed = GUI.Button(rect1, AddAbilityMenuAttribute.abilityDisplayEntries[abilityType], EditorStyles.miniPullDown);
            }

            if (buttonPressed) {
                PopupWindow.Show(new() { position = Event.current.mousePosition}, popup);
            }

            Rect rect = new(position);
            rect.y += EditorGUIUtility.singleLineHeight;
            rect.height = EditorGUIUtility.singleLineHeight;

            EditorUtils.SetPropertyPointer(property, nameof(AbilitySelector.instance));
            if (isExpanded) {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                string text = EditorGUI.TextField(rect, property.displayName, property.stringValue);
                if (EditorGUI.EndChangeCheck()) {
                    property.stringValue = text;
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
