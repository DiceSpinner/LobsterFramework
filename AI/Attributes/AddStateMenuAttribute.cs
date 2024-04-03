using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Utility;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AI
{
    /// <summary>
    /// Make this state visible to the editor script. Pass in path string to specify the menus groups that leads to this state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    
    public class AddStateMenuAttribute : Attribute
    {
        /// <summary>
        /// The root menu group
        /// </summary>
        internal static MenuGroup<Type> main = new("Main");

        /// <summary>
        /// A mapping of states with their menu groups
        /// </summary>
        internal static Dictionary<Type, MenuGroup<Type>> stateMenus = new();

        /// <summary>
        /// A mapping of states with their script icons
        /// </summary>
        internal static Dictionary<Type, Texture2D> icons = new();

        private string menuName;

        public AddStateMenuAttribute(string menuPath = "") {
            this.menuName = menuPath;
        }

        internal void Init(Type type) {
            if (!type.IsSubclassOf(typeof(State))) {
                Debug.LogWarning($"Type {type.Name} is not a valid state type!");
                return;
            }
            if (menuName == "")
            {
                main.options.Add(type);
                stateMenus[type] = main;
            }
            else
            {
                string[] path = menuName.Split('/');
                MenuGroup<Type> currentGroup = main;
                foreach (string folder in path) {
                    if (!currentGroup.subMenus.ContainsKey(folder))
                    {
                        MenuGroup<Type> group = new(folder);
                        currentGroup.AddChild(group);
                    }
                    currentGroup = currentGroup.subMenus[folder];
                }
                currentGroup.options.Add(type);
                stateMenus[type] = currentGroup;

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
