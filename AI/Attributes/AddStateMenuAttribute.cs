using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Utility;
using System.Diagnostics.CodeAnalysis;
using LobsterFramework.Init;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AI
{
    /// <summary>
    /// Applied to <see cref="State"/> to make it visible to editor scripts.
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Editor)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AddStateMenuAttribute : InitializationAttribute
    {
        /// <summary>
        /// The root menu group
        /// </summary>
        internal static MenuTree<Type> main = new(Constants.MenuRootName);

        /// <summary>
        /// A mapping of states with their menu groups
        /// </summary>
        internal static Dictionary<Type, MenuTree<Type>> stateMenus = new();

        /// <summary>
        /// A mapping of states with their script icons
        /// </summary>
        internal static Dictionary<Type, Texture2D> icons = new();

        private string menuName;

        /// <param name="menuPath">The path leading to this item in the menu</param>
        public AddStateMenuAttribute( string menuPath = "") {
            this.menuName = menuPath;
        }

        public static bool IsCompatible(Type type)
        {
            if (!type.IsSubclassOf(typeof(State)))
            {
                return false;
            }
            if (!type.IsSealed)
            {
                Debug.LogError($"{type.FullName} must be sealed!");
                return false;
            }
            return true;
        }

        internal protected override void Init(Type type) {
            
            
            if (menuName == "")
            {
                main.options.Add(type);
                stateMenus[type] = main;
            }
            else
            {
                MenuTree<Type> menu = MenuTree<Type>.AddItem(main, menuName, type);
                stateMenus[type] = menu;

                // Store state script icon if there's any
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
        }
    }  
}
