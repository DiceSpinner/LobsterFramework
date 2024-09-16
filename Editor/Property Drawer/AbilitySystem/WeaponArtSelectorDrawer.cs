using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using LobsterFramework.AbilitySystem.WeaponSystem;
using System;
using System.Linq;

namespace LobsterFramework.Editors
{
    [CustomPropertyDrawer(typeof(WeaponArtSelector))]
    public class WeaponArtSelectorDrawer : PropertyDrawer
    {
        private List<GUIContent> guiContents;
        private List<Type> weaponArtSelections;
        private int selectionIndex;

        public WeaponArtSelectorDrawer()
        {
            guiContents = new();
            guiContents.Add(new("None"));
            weaponArtSelections = new();
            selectionIndex = 0;
        }

        private void ResetOptions(WeaponType weaponType, SerializedProperty property)
        {
            guiContents.RemoveRange(1, guiContents.Count - 1); 
            weaponArtSelections.Clear();
            var weaponArts = WeaponArtAttribute.weaponArtsByWeaponType[(int)weaponType];
            for (int i = 0; i < weaponArts.Count; i++)
            {
                Type ability = weaponArts[i];
                guiContents.Add(AddAbilityMenuAttribute.abilityDisplayEntries[ability]);
                weaponArtSelections.Add(ability);
                if (ability.AssemblyQualifiedName == property.stringValue) {
                    selectionIndex = i + 1;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 2 * base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorUtils.SetPropertyPointer(property, nameof(WeaponArtSelector.weaponType));
            WeaponType weaponType = (WeaponType)property.enumValueIndex;

            EditorUtils.SetPropertyPointer(property, nameof(WeaponArtSelector.typeName));
            ResetOptions(weaponType, property);
            float height = EditorGUI.GetPropertyHeight(property);

            Rect rect1 = new(position);
            rect1.height = height;

            EditorGUI.BeginChangeCheck();
            selectionIndex = EditorGUI.Popup(rect1, label, selectionIndex, guiContents.ToArray());
            
            if (EditorGUI.EndChangeCheck()) {
                if (selectionIndex != 0) // Weapon Art selected
                {
                    property.stringValue = weaponArtSelections[selectionIndex - 1].AssemblyQualifiedName;
                }
                else
                { // None option selected
                    property.stringValue = default;
                }
            }

            Rect rect2 = new(rect1);
            rect2.y += height;

            EditorUtils.SetPropertyPointer(property, nameof(WeaponArtSelector.instance));
            EditorGUI.indentLevel++;
            property.stringValue = EditorGUI.TextField(rect2, property.displayName, property.stringValue);
            EditorGUI.indentLevel--;
        }
    }
}