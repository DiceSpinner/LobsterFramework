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
            selectionDrawer = new(collection, AddAbilityComponentMenuAttribute.componentMenus, DrawNode, DrawItem);
            selectionDrawer.SetColorOptions(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.ComponentPopupColor);
        }

        private GUIContent content = new();
        private void DrawNode(MenuTree<Type> tree) {
            GUILayout.Label(tree.path, EditorStyles.boldLabel);
        }

        private void DrawItem(Type componentType) {
            content.text = componentType.Name;
            if (AddAbilityComponentMenuAttribute.icons.TryGetValue(componentType, out Texture2D icon))
            {
                content.image = icon;
            }
            if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
            {
                editor.newSelectedAbilityComponent = data.components[componentType.AssemblyQualifiedName];
                editorWindow.Close();
            }
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
