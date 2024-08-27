using System;
using System.Collections.Generic;
using UnityEditor;
using LobsterFramework.Utility;
using UnityEngine;

namespace LobsterFramework.Editors {
    /// <summary>
    /// A utility class that helps with drawing out options from <see cref="MenuTree{T}"/> in a collection
    /// </summary>
    /// <typeparam name="T">The type of the data stored in <see cref="MenuTree{T}"/></typeparam>
    public class MenuTreeItemCollectionDrawer<T>
    {
        private IEnumerable<T> itemsToDraw;
        private Dictionary<T, MenuTree<T>> treeMap;
        private Func<MenuTree<T>, GUIContent> guiMenuHandle;
        private Func<T, GUIContent> guiItemHandle;
        private Action<T> optionHandle;
        private Vector2 scrollPosition;

        private Dictionary<MenuTree<T>, List<T>> groups = new();
        private List<T> ungroupedItems = new();

        private Color menuColor;
        private Color itemColor;

        public MenuTreeItemCollectionDrawer(ICollection<T> items, Dictionary<T, MenuTree<T>> mapping, Func<MenuTree<T>, GUIContent> menuHandle, Func<T, GUIContent> itemHandle, Action<T> optionHandle) { 
            itemsToDraw = items;
            treeMap = mapping;
            guiMenuHandle = menuHandle;
            guiItemHandle = itemHandle;
            this.optionHandle = optionHandle;
            UpdateGroups();
        }

        public void SetColorOptions(Color menuColor, Color itemColor) {
            this.itemColor = itemColor;
            this.menuColor = menuColor;
        }

        public void UpdateGroups() {
            groups.Clear();
            ungroupedItems.Clear();

            foreach (T item in itemsToDraw) {
                try {
                    MenuTree<T> tree = treeMap[item];
                    if (groups.ContainsKey(tree)) {
                        groups[tree].Add(item);
                    }
                    else
                    {
                        groups[tree] = new() { item };
                    }
                }catch (KeyNotFoundException)
                {
                    ungroupedItems.Add(item);
                }
            }
        }

        public void Draw() {
            Color orginalColor = GUI.color;
            GUILayout.BeginVertical();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (var group in groups) {
                GUI.color = menuColor;
                GUILayout.Label(guiMenuHandle(group.Key), EditorStyles.boldLabel);
                foreach (var item in group.Value) {
                    GUI.color = itemColor;
                    var content = guiItemHandle(item);
                    if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                    {
                        optionHandle(item);
                    }
                }
            }

            if (ungroupedItems.Count > 0) {
                GUI.color = menuColor;
                EditorGUILayout.LabelField("No Label");
                foreach (var item in ungroupedItems)
                {
                    GUI.color = itemColor;
                    GUIContent content = guiItemHandle(item);
                    if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180))) {
                        optionHandle(item);
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.color = orginalColor;
        }
    }
}

