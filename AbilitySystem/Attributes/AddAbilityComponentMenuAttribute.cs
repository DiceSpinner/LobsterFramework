using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Utility;
using LobsterFramework.Init;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Applied to <see cref="AbilityComponent"/> to make it visible to editor scripts
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Dual)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AddAbilityComponentMenuAttribute : InitializationAttribute
    {
        
        public static HashSet<Type> types = new HashSet<Type>();
        public static Dictionary<Type, Texture2D> icons = new();

        /// <summary>
        /// The root menu
        /// </summary>
        internal static MenuTree<Type> root = new(Constants.MenuRootName);

        /// <summary>
        /// A mapping of abilities with the menu they reside in.
        /// </summary>
        internal static Dictionary<Type, MenuTree<Type>> componentMenus = new();

        /// <summary>
        /// The menu path that leads to the menu which this ability will be displayed in.
        /// </summary>
        private string menuPath;

        /// <param name="menuPath">The path leading to this item in the menu</param>
        public AddAbilityComponentMenuAttribute(string menuPath = "")
        {
            this.menuPath = menuPath;
        }

        public static bool IsCompatible(Type componentType)
        {
            if (componentType.IsSubclassOf(typeof(AbilityComponent)))
            {
                if (!componentType.IsSealed)
                {
                    Debug.LogError($"{componentType.FullName} must be sealed!");
                    return false;
                }
                types.Add(componentType);
#if UNITY_EDITOR
                MonoScript script = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(componentType));
                try
                {
                    SerializedObject scriptObj = new(script);
                    SerializedProperty iconProperty = scriptObj.FindProperty("m_Icon");
                    Texture2D texture = (Texture2D)iconProperty.objectReferenceValue;
                    if (texture != null)
                    {
                        icons[componentType] = texture;
                    }
                }
                catch (NullReferenceException)
                {
                    Debug.LogError("Null pointer exception when setting icon for script: " + componentType.FullName);
                }
#endif
                return true;
            }
            return false;
        }

        internal protected override void Init(Type componentType) {
            if (menuPath == "")
            {
                root.options.Add(componentType);
                componentMenus[componentType] = root;
            }
            else
            {
                MenuTree<Type> menu = MenuTree<Type>.AddItem(root, menuPath, componentType);
                componentMenus[componentType] = menu;
            }
        }
    }
}
