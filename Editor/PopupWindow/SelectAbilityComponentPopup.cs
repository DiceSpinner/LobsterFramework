using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;
using System.Linq;

namespace LobsterFramework.Editors
{
    public class SelectAbilityComponentPopup : PopupWindowContent
    {
        private AbilityDataEditor editor;
        private AbilityData data;

        private MenuTreeItemCollectionDrawer<Type> selectionDrawer;

        public SelectAbilityComponentPopup(AbilityDataEditor editor, AbilityData data)
        {
            this.editor = editor;
            this.data = data;
            var collection = data.components.Values.Select((AbilityComponent cmp) => { return cmp.GetType(); }).ToList();
            selectionDrawer = new(collection, AddAbilityComponentMenuAttribute.componentMenus, DrawNode, DrawItem, SelectAbilityComponent);
            selectionDrawer.SetColorOptions(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.ComponentPopupColor);
        }

        private GUIContent treeContent = new();
        private GUIContent DrawNode(MenuTree<Type> tree) {
            treeContent.text = tree.path;
            return treeContent;
        }

        private GUIContent content = new();
        private GUIContent DrawItem(Type componentType) {
            content.text = componentType.Name;
            content.tooltip = componentType.FullName;
            if (AddAbilityComponentMenuAttribute.icons.TryGetValue(componentType, out Texture2D icon))
            {
                content.image = icon;
            }
            else {
                content.image = null;
            }
            return content;
        }

        private void SelectAbilityComponent(Type componentType)
        {
            editor.newSelectedAbilityComponent = data.components[componentType.AssemblyQualifiedName];
            editorWindow.Close();
        }

        public override void OnGUI(Rect rect)
        {
            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find AbilityData or Editor!");
                return;
            }
            EditorGUILayout.LabelField("Select Component", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            selectionDrawer.Draw();
        }
    }
}
