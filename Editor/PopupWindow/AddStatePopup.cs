using UnityEngine;
using UnityEditor;
using LobsterFramework.AI;
using LobsterFramework.Utility;
using System;
using System.Collections.Generic;

namespace LobsterFramework.Editors
{
    public class AddStatePopup : PopupWindowContent
    {
        public StateData data;
        private Vector2 scrollPosition;
        private MenuGroup<Type> newGroup;
        private MenuGroup<Type> currentGroup = AddStateMenuAttribute.main;
        private Stack<string> nameStack = new();

        public AddStatePopup(StateData data) {
            this.data = data;
        }

        public override void OnGUI(Rect rect)
        {
            if (data == null)
            {
                EditorGUILayout.LabelField("Cannot Find StateData!");
                return;
            }

            if (Event.current.type == EventType.Layout && newGroup != null) {
                currentGroup = newGroup;
                newGroup = null;
            }

            // Compute current path from the stack
            string currentPath = "";
            foreach (string name in nameStack)
            {
                currentPath += name + "/";
            }

            if (currentGroup.parentMenu != null) {
                if (EditorUtils.Button(Color.green, $"Back: {currentGroup.parentMenu.menuName}")) { 
                    newGroup = currentGroup.parentMenu;
                    nameStack.Pop();
                }
            }

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            bool hasContent = false;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

           

            Color defaultColor = GUI.color;
            GUI.color = StateEditorConfig.MenuColor;
            // Display options for sub folders
            foreach (string groupName in currentGroup.subMenus.Keys) {
                hasContent = true;
                GUIContent content = new();
                content.text = groupName;

                Texture2D icon = StateEditorConfig.GetFolderIcon(currentPath + groupName);
                if (icon != null) 
                {
                    content.image = icon;
                }
                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    newGroup = currentGroup.subMenus[groupName];
                    nameStack.Push(groupName);
                }
            }
           
            
            GUI.color = StateEditorConfig.StateColor;
            // Display options for current group
            foreach (Type type in currentGroup.options)
            {
                // Display icon in options if there's one for the state script
                if (data.states.ContainsKey(type.AssemblyQualifiedName))
                {
                    continue;
                }
                hasContent = true;
                GUIContent content = new();
                content.text = type.Name;

                if (AddStateMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
                {
                    content.image = icon;
                }

                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    data.AddState(type);
                }
            }
            GUI.color = defaultColor;
            GUILayout.EndScrollView();
            if (!hasContent)
            {
                GUIStyle textColor = new();
                textColor.normal.textColor = Color.yellow;
                EditorGUILayout.LabelField("No State to add for this folder!", textColor);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}
