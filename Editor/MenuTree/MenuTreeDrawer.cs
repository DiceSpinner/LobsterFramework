using System;
using LobsterFramework.Utility;
using UnityEditor;
using UnityEngine;

namespace LobsterFramework.Editors {
    /// <summary>
    /// A utility class that helps with drawing out options from <see cref="MenuTree{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of the data stored in <see cref="MenuTree{T}"/></typeparam>
    public class MenuTreeDrawer<T>
    {
        #region State
        private MenuTree<T> nextToDisplay;
        private MenuTree<T> currentNode;
        #endregion
        #region Handles
        private Action<T> optionHandle;
        private Func<T, GUIContent> guiOptionHandle;
        private Func<MenuTree<T>, GUIContent> guiNodeHandle;
        #endregion
        #region Display Options
        private Color nodeColor;
        private Color optionColor;
        private string emptyNote;
        #endregion

        private Vector2 scrollPosition;

        public MenuTreeDrawer(MenuTree<T> startNode, Action<T> optionHandle, Func<MenuTree<T>, GUIContent> guiNodeHandle, Func<T, GUIContent> guiOptionHandle)
        {
            currentNode = startNode;
            this.optionHandle = optionHandle;
            this.guiNodeHandle = guiNodeHandle;
            this.guiOptionHandle = guiOptionHandle;
        }

        public void SetColors(Color nodeColor, Color optionColor) {
            this.nodeColor = nodeColor;
            this.optionColor = optionColor;
        }

        public void SetEmptyNote(string note) {
            emptyNote = note;
        }

        public void Draw() {
            if (Event.current.type == EventType.Layout && nextToDisplay != null)
            {
                currentNode = nextToDisplay;
                nextToDisplay = null;
            }

            if (currentNode.parentMenu != null)
            {
                if (EditorUtils.Button(Color.green, $"Back: {currentNode.parentMenu.menuName}"))
                {
                    nextToDisplay = currentNode.parentMenu;
                }
            }

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            bool hasContent = false;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            Color defaultColor = GUI.color;
            GUI.color = nodeColor;
            // Display submenus
            foreach (string groupName in currentNode.subMenus.Keys) 
            {
                hasContent = true;
                MenuTree<T> subMenu = currentNode.subMenus[groupName];

                GUIContent content = guiNodeHandle(subMenu);
                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    nextToDisplay = subMenu;
                }
            }

            GUI.color = optionColor;

            // Display options
            foreach (T item in currentNode.options)
            {
                GUIContent content = guiOptionHandle(item);
                if (content == null) {
                    continue;
                }
                hasContent = true;
                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    optionHandle(item);
                }
            }
            GUI.color = defaultColor;
            GUILayout.EndScrollView();
            if (!hasContent && emptyNote != default)
            {
                GUIStyle textColor = new();
                textColor.normal.textColor = Color.yellow;
                EditorGUILayout.LabelField(emptyNote, textColor);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}