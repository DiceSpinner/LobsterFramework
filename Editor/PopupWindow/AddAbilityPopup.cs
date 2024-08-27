using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using System;
using System.Reflection;
using LobsterFramework.Utility;

namespace LobsterFramework.Editors
{
    public class AddAbilityPopup : PopupWindowContent
    {
        private AbilityData data;
        private MenuTreeDrawer<Type> menuTreeDrawer;

        public AddAbilityPopup(AbilityData abilityData) {
            data = abilityData;
            menuTreeDrawer = new(AddAbilityMenuAttribute.root, AddAbility, DrawMenu, DrawOption);
            menuTreeDrawer.SetColors(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.AbilityPopupColor);
            menuTreeDrawer.SetEmptyNote("Option Exhausted");
        }

        #region Handles for menu drawer
        private void AddAbility(Type type) {
            data.AddAbility(type);
        }

        private GUIContent content = new();
        private GUIContent DrawMenu(MenuTree<Type> node) {
            content.text = node.menuName;
            content.image = AbilityEditorConfig.GetFolderIcon(node.path[(Constants.MenuRootName.Length + 1)..]);
            return content;
        }

        private GUIContent DrawOption(Type type) {
            if (data.abilities.ContainsKey(type.AssemblyQualifiedName)) {
                return null;
            }
            content.text = type.Name;
            if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(type, out Texture2D icon))
            {
                content.image = icon;
            }
            else { 
                content.image = null;
            }
            content.tooltip = type.FullName;
            return content;
        }
        #endregion

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add Abilities", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (data == null)
            {
                EditorGUILayout.LabelField("Cannot Find AbilityData!");
                return;
            }
            menuTreeDrawer.Draw();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}
