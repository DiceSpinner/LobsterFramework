using UnityEngine;
using UnityEditor;
using LobsterFramework.AI;
using LobsterFramework.Utility;
using System;

namespace LobsterFramework.Editors
{
    public class AddStatePopup : PopupWindowContent
    {
        private StateData data;
        private MenuTreeDrawer<Type> menuTreeDrawer;

        public AddStatePopup(StateData data) {
            this.data = data;
            menuTreeDrawer = new(AddStateMenuAttribute.main, AddState, DrawNode, DrawOption);
            menuTreeDrawer.SetColors(StateEditorConfig.MenuPopupColor, StateEditorConfig.StatePopupColor);
            menuTreeDrawer.SetEmptyNote("Option Exhausted");
        }

        #region Handles for MenuTreeDrawer
        private GUIContent content = new();
        private GUIContent DrawNode(MenuTree<Type> node) {
            content.text = node.menuName;
            content.image = StateEditorConfig.GetFolderIcon(node.path[(Constants.MenuRootName.Length + 1)..]);
            return content;
        }
        private GUIContent DrawOption(Type stateType) {
            if (data.states.ContainsKey(stateType.AssemblyQualifiedName))
            {
                return null;
            }
            content.text = stateType.Name;

            if (AddStateMenuAttribute.icons.TryGetValue(stateType, out Texture2D icon))
            {
                content.image = icon;
            }
            else {
                content.image = null;
            }
            return content;
        }

        private void AddState(Type stateType) {
            data.AddState(stateType);
        }
        #endregion

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add AI States", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (data == null)
            {
                EditorGUILayout.LabelField("Cannot Find StateData!");
                return;
            }

            menuTreeDrawer.Draw();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}
