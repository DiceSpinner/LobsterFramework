using System;
using UnityEngine;
using System.Reflection;
using LobsterFramework.AbilitySystem;
using LobsterFramework.AbilitySystem.WeaponSystem;
using LobsterFramework.Interaction;
using LobsterFramework.AI;

namespace LobsterFramework
{
    /// <summary>
    /// Initializes all of the custom attributes of LobsterFramework for all assemblies that reference it.
    /// </summary>
    public class AttributeInitializer
    {
        /// <summary>
        /// Flag to indicate whether attribute initialization is completed.
        /// </summary>
        public static bool Finished = false;

        public static event Action OnInitializationComplete;

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts(Constants.AttributeInitOrder)]
#else
        [RuntimeInitializeOnLoadMethod()]
#endif
        private static void InitializeAttributes()
        {
            if (Finished) {
                return;
            }
            Assembly frameworkAssembly = typeof(Setting).Assembly;
            InitializeAssemblyAttributes(frameworkAssembly);

            AssemblyName assemblyName = frameworkAssembly.GetName();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName[] references = assembly.GetReferencedAssemblies();
                foreach (AssemblyName reference in references)
                {
                    if (reference.Name == assemblyName.Name)
                    {
                        InitializeAssemblyAttributes(assembly);
                        // Debug.Log($"Assembly {assembly.GetName().Name} use of LobsterFramework detected!"); 
                        break;
                    }
                }
            }
            Finished = true;
            try
            {
                OnInitializationComplete?.Invoke();
            }catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            OnInitializationComplete = null;
        }
        private static void InitializeAssemblyAttributes(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();

            // AbilitySystem
            AddAbilityMenu(types);
            AddAbilityComponentMenu(types);
            AddComponentRequirement(types); 
            AddAbilityStatRequirement(types);

            // Weapon System
            AddWeaponStatMenu(types);
            AddWeaponArtMenu(types);
            AddWeaponStatRequirement(types);
            AddWeaponDataEntries(types);
            MarkOffhandAbilities(types);

            // Interaction
            AddInteractions(types);

            // AI
            AddStateMenu(types);
            AddStateTransition(types);
        }

        private static void AddAbilityMenu(Type[] types)
        {
            foreach (Type type in types)
            {
                AddAbilityMenuAttribute info = type.GetCustomAttribute<AddAbilityMenuAttribute>();
                try
                {
                    info?.AddAbility(type);
                }catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddAbilityComponentMenu(Type[] types) {
            foreach (Type type in types)
            {
                AddAbilityComponentMenuAttribute info = type.GetCustomAttribute<AddAbilityComponentMenuAttribute>();
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddComponentRequirement(Type[] types) {
            foreach (Type type in types)
            {
                foreach (var info in type.GetCustomAttributes<RequireComponentReferenceAttribute>(true)) {
                    try
                    {
                        info?.Init(type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Exception occured while initializing attribute!");
                        Debug.LogException(ex);
                    }
                }
            }
        }
        private static void AddAbilityStatRequirement(Type[] types) {
            foreach (Type type in types)
            {
                foreach (RequireAbilityComponentsAttribute info in type.GetCustomAttributes<RequireAbilityComponentsAttribute>(true)) {
                    try
                    {
                        info?.Init(type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Exception occured while initializing attribute!");
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private static void MarkOffhandAbilities(Type[] types) {
            foreach (Type type in types)
            {
                OffhandWeaponAbilityAttribute info = type.GetCustomAttribute<OffhandWeaponAbilityAttribute>(true);
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddWeaponStatMenu(Type[] types) {
            foreach (Type type in types)
            {
                foreach (AddWeaponStatMenuAttribute info in type.GetCustomAttributes<AddWeaponStatMenuAttribute>())
                {
                    try
                    {
                        info?.Init(type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Exception occured while initializing attribute!");
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private static void AddWeaponArtMenu(Type[] types) {
            foreach (Type type in types)
            {
                var info = type.GetCustomAttribute<WeaponArtAttribute>();
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddWeaponStatRequirement(Type[] types) {
            foreach (Type type in types)
            {
                foreach (RequireWeaponStatAttribute info in type.GetCustomAttributes<RequireWeaponStatAttribute>())
                {
                    try
                    {
                        info?.Init(type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Exception occured while initializing attribute!");
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private static void AddInteractions(Type[] types) {
            foreach (Type type in types)
            {
                RegisterInteractorAttribute info = type.GetCustomAttribute<RegisterInteractorAttribute>();
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddWeaponDataEntries(Type[] types)
        {
            foreach (Type type in types)
            {
                WeaponAnimationAttribute info = type.GetCustomAttribute<WeaponAnimationAttribute>();
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }

                WeaponAnimationAddonAttribute setting = type.GetCustomAttribute<WeaponAnimationAddonAttribute>();
                try
                {
                    setting?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddStateMenu(Type[] types)
        {
            foreach (Type type in types)
            {
                AddStateMenuAttribute info = type.GetCustomAttribute<AddStateMenuAttribute>();
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }

        private static void AddStateTransition(Type[] types)
        {
            foreach (Type type in types)
            {
                StateTransitionAttribute info = type.GetCustomAttribute<StateTransitionAttribute>();
                try
                {
                    info?.Init(type);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception occured while initializing attribute!");
                    Debug.LogException(ex);
                }
            }
        }
    }
}
