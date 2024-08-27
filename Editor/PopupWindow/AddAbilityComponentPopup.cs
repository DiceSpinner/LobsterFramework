using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using System.Reflection;
using LobsterFramework.Utility;

namespace LobsterFramework.Editors
{
    public class AddAbilityComponentPopup : PopupWindowContent
    {
        private AbilityData data;
        private MenuTreeDrawer<Type> menuTreeDrawer;

        public AddAbilityComponentPopup(AbilityData data)
        {
            this.data = data;
            menuTreeDrawer = new(AddAbilityComponentMenuAttribute.root, AddAbilityComponent, DrawMenu, DrawItem);
            menuTreeDrawer.SetEmptyNote("Option Exhausted");
            menuTreeDrawer.SetColors(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.ComponentPopupColor);
        }

        #region Handles
        private void AddAbilityComponent(Type componentType)
        {
            data.AddAbilityComponent(componentType);
        }

        private GUIContent content = new();
        private GUIContent DrawItem(Type componentType) {
            if (data.components.ContainsKey(componentType.AssemblyQualifiedName))
            {
                return null;
            }

            content.text = componentType.Name;
            if (AddAbilityComponentMenuAttribute.icons.TryGetValue(componentType, out Texture2D icon))
            {
                content.image = icon;
            }
            else {
                content.image = null;
            }
            content.tooltip = componentType.FullName;
            return content;
        }

        private GUIContent DrawMenu(MenuTree<Type> tree) {
            content.text = tree.menuName;
            content.image = AbilityEditorConfig.GetFolderIcon(tree.path[(Constants.MenuRootName.Length + 1)..]);
            return content;
        }

        #endregion

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add Components", EditorUtils.CentredTitleLabelStyle);
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
