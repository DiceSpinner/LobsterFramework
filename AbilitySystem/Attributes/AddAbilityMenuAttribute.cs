using LobsterFramework.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LobsterFramework.AbilitySystem {

    /// <summary>
    /// Applied to <see cref="Ability"/> to make it visible to editor scripts
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AddAbilityMenuAttribute : Attribute
    {
        public const string RootName = "Root";

        internal static Dictionary<Type, Texture2D> abilityIcons = new();

        /// <summary>
        /// Stores lambdas that creates and returns the AbilityConfig /AbilityChannel/AbilityContext for the corresponding ability
        /// </summary>
        internal static Dictionary<Type, (Func<AbilityConfig>, Func<AbilityChannel>, Func<AbilityContext>)> abilityHandles = new();

        /// <summary>
        /// The root menu
        /// </summary>
        internal static MenuTree<Type> root = new(RootName);

        /// <summary>
        /// A mapping of abilities with the menu they reside in.
        /// </summary>
        internal static Dictionary<Type, MenuTree<Type>> abilityMenus = new();

        /// <summary>
        /// The menu path that leads to the menu which this ability will be displayed in.
        /// </summary>
        private string menuPath;

        /// <param name="menuPath">The path leading to this item in the menu</param>
        public AddAbilityMenuAttribute(string menuPath="") {
            this.menuPath = menuPath;   
        }

        internal void AddAbility(Type abilityType) {
            if (abilityType.IsSubclassOf(typeof(Ability)))
            {
                if (abilityType.IsAbstract) {
                    Debug.LogError($"Only concrete ability can be added as to the menu! {abilityType.FullName}");
                    return;
                }

                if (!RegisterHandles(abilityType)) {
                    Debug.LogWarning($"Failed to register handle for {abilityType.FullName}");
                    return;
                }
#if UNITY_EDITOR
                MonoScript script = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(abilityType));
                try
                {
                    SerializedObject scriptObj = new(script);
                    SerializedProperty iconProperty = scriptObj.FindProperty("m_Icon");
                    Texture2D texture = (Texture2D)iconProperty.objectReferenceValue;
                    if (texture != null)
                    {
                        abilityIcons[abilityType] = texture;
                    }
                }catch (NullReferenceException)
                {
                    Debug.LogError("Null pointer exception when setting icon for script: " + abilityType.FullName);
                } 
#endif
            }
            else {
                Debug.LogError("The type specified for ability menu is not an ability:" + abilityType.FullName);
            }

            if (menuPath == "")
            {
                root.options.Add(abilityType);
                abilityMenus[abilityType] = root;
            }
            else {
                MenuTree<Type> menu = MenuTree<Type>.AddItem(root, menuPath, abilityType);
                abilityMenus[abilityType] = menu;
            }
        } 

        #region Complimentary Class Check and handles for their creation
        private static Type GetBaseConfigType(Type abilityType)
        {
            Type type = abilityType.BaseType;
            while (type != typeof(Ability))
            {
                Type t = type.Assembly.GetType(type.FullName + "Config");
                if (t != null && t.IsSubclassOf(typeof(AbilityConfig)))
                {
                    return t;
                }
                type = type.BaseType;
            }
            return typeof(AbilityConfig);
        }

        private static Type GetBaseChannelType(Type abilityType)
        {
            Type type = abilityType.BaseType;
            while (type != typeof(Ability))
            {
                Type t = type.Assembly.GetType(type.FullName + "Channel");
                if (t != null && t.IsSubclassOf(typeof(AbilityChannel)))
                {
                    return t;
                }
                type = type.BaseType;
            }
            return typeof(AbilityChannel);
        }

        private static Type GetBaseContextType(Type abilityType)
        {
            Type type = abilityType.BaseType;
            while (type != typeof(Ability))
            {
                Type t = type.Assembly.GetType(type.FullName + "Context");
                if (t != null && t.IsSubclassOf(typeof(AbilityContext)))
                {
                    return t;
                }
                type = type.BaseType;
            }
            return typeof(AbilityContext);
        }

        internal static AbilityChannel CreateAbilityChannel(Type abilityType) {
            if (abilityHandles.TryGetValue(abilityType, out var tuple)) {
                return tuple.Item2();
            }
            return null;
        }

        internal static AbilityContext CreateAbilityContext(Type abilityType)
        {
            if (abilityHandles.TryGetValue(abilityType, out var tuple))
            {
                return tuple.Item3();
            }
            return null;
        }

        internal static AbilityConfig CreateAbilityConfig(Type abilityType) {
            return abilityHandles[abilityType].Item1();
        }

        private static Func<AbilityConfig> ConfigHandle<T>() where T : AbilityConfig 
        {
            return () => { return ScriptableObject.CreateInstance<T>(); };
        }

        private static Func<AbilityChannel> ChannelHandle<T>()
            where T : AbilityChannel, new() 
        {
            return () => { return new T(); };
        }

        private static Func<AbilityContext> ContextHandle<T>()
            where T : AbilityContext, new()
        {
            return () => { return new T(); };
        }

        /// <summary>
        /// Check to see if the ability class has the channel, config and context classes defined in the same namespace
        /// </summary>
        /// <returns>true if all necessities have been defined, false otherwise</returns>
        private bool RegisterHandles(Type abilityType)
        {
            string configName = abilityType.FullName + "Config";
            string channelName = abilityType.FullName + "Channel";
            string contextName = abilityType.FullName + "Context";
            Type baseConfigType = GetBaseConfigType(abilityType);
            Type baseChannelType = GetBaseChannelType(abilityType);
            Type baseContextType = GetBaseContextType(abilityType);

            Type configType = abilityType.Assembly.GetType(configName);
            Type channelType = abilityType.Assembly.GetType(channelName);
            Type contextType = abilityType.Assembly.GetType(contextName);

            bool classError = false;
            if (configType == null || !configType.IsSubclassOf(baseConfigType) || configType.IsAbstract) {
                classError = true;
                Debug.LogError($"Ability {abilityType.FullName} is missing or has ill definition of config. Correct config should be non abstract class named {configName} and inherit from {baseConfigType.FullName}");
            }

            if (channelType == null || !channelType.IsSubclassOf(baseChannelType) || channelType.IsAbstract)
            {
                classError = true;
                Debug.LogError($"Ability {abilityType.FullName} is missing or has ill definition of channel. Correct channel should be non abstract class named {channelName} and inherit from {baseChannelType.FullName}");
            }

            if (contextType == null || !contextType.IsSubclassOf(baseContextType) || contextType.IsAbstract)
            {
                classError = true;
                Debug.LogError($"Ability {abilityType.FullName} is missing or has ill definition of context. Correct context should be non abstract class named {contextName} and inherit from {baseContextType.FullName}");
            }
            if (!classError) {
                bool constructorError = false;

                Func<AbilityChannel> channelHandle = null;
                Func<AbilityContext> contextHandle = null;

                Type type = GetType();
                #region AbilityChannel
                try
                {
                    MethodInfo genericHandle = type.GetMethod(nameof(ChannelHandle), BindingFlags.NonPublic | BindingFlags.Static);
                    var handle = genericHandle.MakeGenericMethod(channelType);
                    channelHandle = (Func<AbilityChannel>)handle.Invoke(null, null);
                } catch (Exception e) {
                    constructorError = true;
                    Debug.LogException(e);
                    Debug.LogError($"AbilityChannel {abilityType.FullName + "Channel"} must contain a default parameterless constructor.");
                }
                #endregion

                #region AbilityContext
                try
                {
                    MethodInfo genericHandle = type.GetMethod(nameof(ContextHandle), BindingFlags.NonPublic | BindingFlags.Static);
                    var handle = genericHandle.MakeGenericMethod(contextType);
                    contextHandle = (Func<AbilityContext>)handle.Invoke(null, null);
                }
                catch (Exception e)
                {
                    constructorError = true;
                    Debug.LogException(e);
                    Debug.LogError($"AbilityContext {abilityType.FullName + "Context"} must contain a default parameterless constructor.");
                }
                #endregion 

                #region AbilityConfig
                Func<AbilityConfig> configHandle;
                MethodInfo configGenericHandle = type.GetMethod(nameof(ConfigHandle), BindingFlags.NonPublic | BindingFlags.Static);
                configHandle = (Func<AbilityConfig>)configGenericHandle.MakeGenericMethod(configType).Invoke(null, null);
                #endregion

                if (!constructorError) {
                    Debug.Assert(channelHandle != null && contextHandle != null);
                    abilityHandles[abilityType] = (configHandle, channelHandle, contextHandle);
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
