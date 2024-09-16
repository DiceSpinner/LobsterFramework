using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;

namespace LobsterFramework.Editors
{
    public class AbilitySelectorPopup : PopupWindowContent
    {
        private MenuTreeDrawer<Type> menuTreeDrawer;
        internal RestrictAbilityTypeAttribute restriction;
        private AbilitySelectorDrawer drawer;

        public AbilitySelectorPopup(AbilitySelectorDrawer drawer) {
            this.drawer = drawer;
            menuTreeDrawer = new(AddAbilityMenuAttribute.root, SetAbilityType, DrawMenu, DrawOption);
            menuTreeDrawer.SetColors(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.AbilityPopupColor);
            menuTreeDrawer.SetEmptyNote("Option Exhausted"); 
        }

        #region Handles for menu drawer
        private void SetAbilityType(Type type)
        {
            drawer.newSelection = type.AssemblyQualifiedName;
            editorWindow.Close();
        }

        private GUIContent content = new();
        private GUIContent DrawMenu(MenuTree<Type> node)
        {
            content.text = node.menuName;
            content.image = AbilityEditorConfig.GetFolderIcon(node.path[(Constants.MenuRootName.Length + 1)..]);
            return content;
        }

        private GUIContent DrawOption(Type type)
        {
            if (restriction != null && !type.IsSubclassOf(restriction.ParentType)) {
                if (!restriction.IncludeParent || restriction.ParentType != type) {
                    return null;
                }
            }
            content.text = type.Name;
            if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(type, out Texture2D icon))
            {
                content.image = icon;
            }
            else
            {
                content.image = null;
            }
            content.tooltip = type.FullName;
            return content;
        }
        #endregion

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Choose Ability", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            menuTreeDrawer.Draw();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}
