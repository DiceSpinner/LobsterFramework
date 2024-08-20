using UnityEngine;
using UnityEditor;
using LobsterFramework.AI;
using System;
using System.Linq;
using LobsterFramework.Utility;


namespace LobsterFramework.Editors
{
    public class SelectStatePopup : PopupWindowContent
    {
        private StateDataEditor editor;
        private StateData data;
        private MenuTreeItemCollectionDrawer<Type> selectionDrawer;

        public SelectStatePopup(StateDataEditor editor, StateData data) {
            this.editor = editor;
            this.data = data;

            selectionDrawer = new(data.states.Values.Select((State state)=>{ return state.GetType(); }).ToList(), AddStateMenuAttribute.stateMenus, DrawMenu, DrawItem);
            selectionDrawer.SetColorOptions(StateEditorConfig.MenuPopupColor, StateEditorConfig.StatePopupColor);
        }

        private void DrawMenu(MenuTree<Type> tree) {
            GUILayout.Label(tree.path, EditorStyles.boldLabel);
        }

        private GUIContent content = new();
        private void DrawItem(Type stateType) {
            if (AddStateMenuAttribute.icons.TryGetValue(stateType, out Texture2D icon))
            {
                content.image = icon;
            }
            else { 
                content.image = null;
            }

            if (data.initialState != null && stateType == data.initialState.GetType())
            {
                GUI.color = Color.green;
                content.text = $"{stateType.Name} (Initial State)";
            }
            else {
                content.text = stateType.Name;
            }

            if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
            {
                editor.nextState = data.states[stateType.AssemblyQualifiedName];
                editorWindow.Close();
            }
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Select State", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find StateData or Editor!");
                return;
            }

            selectionDrawer.Draw();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}
