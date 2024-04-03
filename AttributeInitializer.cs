using System;
using UnityEngine;
using System.Reflection;
using LobsterFramework.AbilitySystem;
using LobsterFramework.Interaction;
using LobsterFramework.AI;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Initializes all of the custom attributes of LobsterFramework for all assemblies
    /// </summary>
    public class AttributeInitializer
    {
        public static void Initialize(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();

            // AbilitySystem
            AddAbilityMenu(types);
            AddAbilityStatMenu(types);
            AddComponentRequirement(types); 
            AddAbilityStatRequirement(types);
            AddWeaponStatMenu(types);
            AddWeaponArtMenu(types);
            AddWeaponStatRequirement(types);
            AddOffhandAbilitySpec(types);
            AddWeaponDataEntries(types);

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
                if (info != null) {
                    info.AddAbility(type);
                }
            }
        }

        private static void AddAbilityStatMenu(Type[] types) {
            foreach (Type type in types)
            {
                AddAbilityComponentMenuAttribute info = type.GetCustomAttribute<AddAbilityComponentMenuAttribute>();
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }

        private static void AddComponentRequirement(Type[] types) {
            foreach (Type type in types)
            {
                ComponentRequiredAttribute info = type.GetCustomAttribute<ComponentRequiredAttribute>(true);
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }
        private static void AddAbilityStatRequirement(Type[] types) {
            foreach (Type type in types)
            {
                foreach (RequireAbilityComponentsAttribute info in type.GetCustomAttributes<RequireAbilityComponentsAttribute>(true)) {
                    info.Init(type);
                }
            }
        }

        private static void AddWeaponStatMenu(Type[] types) {
            foreach (Type type in types)
            {
                foreach (AddWeaponStatMenuAttribute info in type.GetCustomAttributes<AddWeaponStatMenuAttribute>())
                {
                    info.Init(type);
                }
            }
        }

        private static void AddWeaponArtMenu(Type[] types) {
            foreach (Type type in types)
            {
                foreach (AddWeaponArtMenuAttribute info in type.GetCustomAttributes<AddWeaponArtMenuAttribute>())
                {
                    info.Init(type);
                }
            }
        }

        private static void AddWeaponStatRequirement(Type[] types) {
            foreach (Type type in types)
            {
                foreach (RequireWeaponStatAttribute info in type.GetCustomAttributes<RequireWeaponStatAttribute>())
                {
                    info.Init(type);
                }
            }
        }

        private static void AddInteractions(Type[] types) {
            foreach (Type type in types)
            {
                RegisterInteractorAttribute info = type.GetCustomAttribute<RegisterInteractorAttribute>();
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }

        private static void AddOffhandAbilitySpec(Type[] types) {
            foreach (Type type in types) {
                OffhandWeaponAbilityAttribute info = type.GetCustomAttribute<OffhandWeaponAbilityAttribute>();
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }

        private static void AddWeaponDataEntries(Type[] types)
        {
            foreach (Type type in types)
            {
                WeaponAnimationAttribute info = type.GetCustomAttribute<WeaponAnimationAttribute>();
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }

        private static void AddStateMenu(Type[] types)
        {
            foreach (Type type in types)
            {
                AddStateMenuAttribute info = type.GetCustomAttribute<AddStateMenuAttribute>();
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }

        private static void AddStateTransition(Type[] types)
        {
            foreach (Type type in types)
            {
                StateTransitionAttribute info = type.GetCustomAttribute<StateTransitionAttribute>();
                if (info != null)
                {
                    info.Init(type);
                }
            }
        }
    }
}
