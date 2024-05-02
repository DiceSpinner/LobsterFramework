using System.Collections;
using System.Collections.Generic;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// A menu tree that is used by editor scripts to display nested menu options
    /// </summary>
    /// <typeparam name="T">The type of the items to be stored in the menu tree</typeparam>
    public class MenuTree<T>
    {
        /// <summary>
        /// Name of this node
        /// </summary>
        public string menuName;
        public Dictionary<string, MenuTree<T>> subMenus = new();
        public MenuTree<T> parentMenu;
        public List<T> options = new();
        /// <summary>
        /// Path to this node
        /// </summary>
        public string path;

        public MenuTree(string name) { menuName = name; path = name; }

        /// <summary>
        /// Add the item to the menu tree
        /// </summary>
        /// <param name="root">The root of the menu tree</param>
        /// <param name="itemPath">The location of this item in the tree</param>
        /// <param name="item">The item to be added</param>
        /// <returns>The menu tree that directly holds the item</returns>
        public static MenuTree<T> AddItem(MenuTree<T> root, string itemPath, T item) {
            string[] path = itemPath.Split('/');
            MenuTree<T> currentGroup = root;
            foreach (string folder in path)
            {
                if (!currentGroup.subMenus.ContainsKey(folder))
                {
                    MenuTree<T> group = new(folder);
                    currentGroup.AddChild(group);
                }
                currentGroup = currentGroup.subMenus[folder];
            }
            currentGroup.options.Add(item);
            return currentGroup;
        }

        /// <summary>
        /// Add a child node to the tree. Circular reference is not allowed. Attempting to add ancestor nodes or nodes that already has a parent will have no effect.
        /// </summary>
        /// <param name="child">The child to be added</param>
        public void AddChild(MenuTree<T> child) {
            if (child == this || child == null || child.parentMenu != null || IsAncestor(child)) {
                return;
            }
            child.parentMenu = this;
            subMenus[child.menuName] = child;
            child.path = path + "/" + child.menuName;
        }

        /// <summary>
        /// Check if the given tree is an ancestor of this menu tree.
        /// </summary>
        /// <param name="tree">The tree to be examined</param>
        /// <returns>true if the tree is an ancestor, otherwise false</returns>
        public bool IsAncestor(MenuTree<T> tree) {
            if (tree == null) {
                return false;
            }
            if (parentMenu == null) {
                return false;
            }

            if (tree == parentMenu) {
                return true;
            }
            return parentMenu.IsAncestor(tree);
        }
    }
}
