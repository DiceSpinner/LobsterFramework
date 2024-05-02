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
            selectionDrawer = new(collection, AddAbilityMenuAttribute.abilityMenus, DrawMenu, DrawItem);
            selectionDrawer.SetColorOptions(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.AbilityPopupColor);
        }

        private GUIContent content = new();
        private void DrawItem(Type abilityType) {
            content.text = abilityType.Name;
            if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(abilityType, out Texture2D icon))
            {
                content.image = icon;
            }
            if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
            {
                editor.newSelectedAbility = data.abilities[abilityType.AssemblyQualifiedName];
                editorWindow.Close();
            }
        }

        private void DrawMenu(MenuTree<Type> tree) {
            GUILayout.Label(tree.path, EditorStyles.boldLabel);
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
