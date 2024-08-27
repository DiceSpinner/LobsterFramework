using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace LobsterFramework.Editors
{
    public class SelectAbilityPopup : PopupWindowContent
    {
        private AbilityData data;
        private AbilityDataEditor editor;

        private MenuTreeItemCollectionDrawer<Type> selectionDrawer;

        public SelectAbilityPopup(AbilityData abilityData, AbilityDataEditor editor) { 
            data = abilityData;
            this.editor = editor;
            var collection = abilityData.abilities.Values.Select((Ability item) => { return item.GetType(); }).ToArray();
            selectionDrawer = new(collection, AddAbilityMenuAttribute.abilityMenus, DrawMenu, DrawItem, SelectAbility);
            selectionDrawer.SetColorOptions(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.AbilityPopupColor);
        }

        private GUIContent content = new();
        private GUIContent DrawItem(Type abilityType) {
            content.text = abilityType.Name;
            content.tooltip = abilityType.FullName;
            if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(abilityType, out Texture2D icon))
            {
                content.image = icon; 
            }
            return content;
        }

        private void SelectAbility(Type abilityType) {
            editor.newSelectedAbility = data.abilities[abilityType.AssemblyQualifiedName];
            editorWindow.Close();
        }

        GUIContent treeGUI = new();
        private GUIContent DrawMenu(MenuTree<Type> tree) {
            treeGUI.text = tree.path;
            return treeGUI;
        }

        public override void OnGUI(Rect rect)
        {
            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find AbilityData or Editor!");
                return;
            }
            EditorGUILayout.LabelField("Select Ability", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            selectionDrawer.Draw();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}
