using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Utility;
using System.Diagnostics.CodeAnalysis;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Applied to <see cref="WeaponStat"/> to make it visible to editor scripts
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AddWeaponStatMenuAttribute : Attribute
    {
        public static HashSet<Type> types = new HashSet<Type>();
        public static Dictionary<Type, Texture2D> icons = new();

        /// <summary>
        /// The root menu
        /// </summary>
        internal static MenuTree<Type> root = new(Constants.MenuRootName);

        /// <summary>
        /// A mapping of WeaponStats with the menu they reside in.
        /// </summary>
        internal static Dictionary<Type, MenuTree<Type>> menus = new();

        /// <summary>
        /// The menu path that leads to the menu which this ability will be displayed in.
        /// </summary>
        private string menuPath;

        /// <param name="menuPath">The path leading to this item in the menu</param>
        public AddWeaponStatMenuAttribute( string menuPath = "")
        {
            this.menuPath = menuPath;
        }
        public void Init(Type type)
        {
            if (type.IsSubclassOf(typeof(WeaponStat)))
            {
                if (!type.IsSealed)
                {
                    Debug.LogError($"{type.FullName} must be sealed!");
                    return;
                }
                types.Add(type);
#if UNITY_EDITOR
                MonoScript script = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(type));
                try
                {
                    SerializedObject scriptObj = new(script);
                    SerializedProperty iconProperty = scriptObj.FindProperty("m_Icon");
                    Texture2D texture = (Texture2D)iconProperty.objectReferenceValue;
                    if (texture != null)
                    {
                        icons[type] = texture;
                    }
                }
                catch (NullReferenceException)
                {
                    Debug.LogError("Null pointer exception when setting icon for script: " + type.FullName);
                }
#endif
            }
            else
            {
                Debug.LogError("Attempting to apply WeaponStat Attribute on invalid type: " + type.Name);
                return;
            }

            if (menuPath == "")
            {
                root.options.Add(type);
                menus[type] = root;
            }
            else
            {
                MenuTree<Type> menu = MenuTree<Type>.AddItem(root, menuPath, type);
                menus[type] = menu;
            }
        }
    }
}
