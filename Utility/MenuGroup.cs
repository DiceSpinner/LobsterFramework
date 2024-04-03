using System.Collections;
using System.Collections.Generic;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Represents a menu in the editor with a customizable set of options
    /// </summary>
    public class MenuGroup<T>
    {
        public string menuName;
        public Dictionary<string, MenuGroup<T>> subMenus = new();
        public MenuGroup<T> parentMenu;
        public List<T> options = new();
        public string pathName;

        public MenuGroup(string name) { menuName = name; pathName = name + "/"; }

        public void AddChild(MenuGroup<T> child) {
            if (child == this || child == parentMenu || child == null) {
                return;
            }
            child.parentMenu = this;
            subMenus[child.menuName] = child;
            child.pathName = this.pathName + child.menuName + "/";
        }
    }
}
