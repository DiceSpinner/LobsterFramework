using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using LobsterFramework.AbilitySystem.WeaponSystem;
using System;

namespace LobsterFramework.Editors
{
    [CustomPropertyDrawer(typeof(WeaponArtSelector))]
    public class WeaponArtSelectorDrawer : PropertyDrawer
    {
        private List<Type> selectionMapping;
        private GUIContent[] options;
        private List<GUIContent> currentOptions;
        private List<string> currentNames;
        private int selectionIndex;

        public WeaponArtSelectorDrawer()
        {
            currentOptions = new();
            currentNames = new();
            selectionMapping = new();
            selectionMapping.AddRange(AddAbilityMenuAttribute.abilityMenus.Keys);
            selectionIndex = -1;
            options = new GUIContent[selectionMapping.Count + 1];
            options[0] = new("None");
            for (int i = 1; i < selectionMapping.Count + 1; i++)
            {
                Type type = selectionMapping[i - 1];
                GUIContent content = new(type.Name);
                if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(type, out Texture2D icon))
                {
                    content.image = icon;
                }
                options[i] = content;
            }
        }

        private void ResetOptions(WeaponType weaponType) {
            currentOptions.Clear();
            currentNames.Clear();
            currentOptions.Add(options[0]);
            currentNames.Add("None");
            if (AddWeaponArtMenuAttribute.compatibleAbilities.TryGetValue(weaponType, out var ability))
            {
                for (int i = 0; i < selectionMapping.Count; i++)
                {
                    Type type = selectionMapping[i];
                    if (ability.Contains(type))
                    {
                        currentOptions.Add(options[i + 1]);
                        currentNames.Add(type.AssemblyQualifiedName);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 2 * base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (selectionMapping.Count == 0)
            {
                return;
            }

            EditorUtils.SetPropertyPointer(property, nameof(WeaponArtSelector.weaponType));
            WeaponType weaponType = (WeaponType)property.enumValueIndex;
            ResetOptions(weaponType);
            float height = EditorGUI.GetPropertyHeight(property);
            EditorUtils.SetPropertyPointer(property, nameof(WeaponArtSelector.typeName));
            if (currentNames.Contains(property.stringValue))
            {
                selectionIndex = currentNames.IndexOf(property.stringValue);
            }
            else {
                selectionIndex = 0;
                property.stringValue = default;
            }

            Rect rect1 = new(position);
            rect1.height = height;

            selectionIndex = EditorGUI.Popup(rect1, label, selectionIndex, currentOptions.ToArray());
            string abilityName = currentNames[selectionIndex];
            property.stringValue = abilityName;

            Rect rect2 = new(rect1);
            rect2.y += height;
            
            EditorUtils.SetPropertyPointer(property, nameof(WeaponArtSelector.instance));
            EditorGUI.indentLevel++;
            property.stringValue = EditorGUI.TextField(rect2, property.displayName, property.stringValue);
            EditorGUI.indentLevel--;
        }
    }
}
