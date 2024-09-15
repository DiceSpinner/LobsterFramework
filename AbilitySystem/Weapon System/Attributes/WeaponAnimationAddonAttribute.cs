using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.ObjectModel;
using LobsterFramework.Init;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Register a ScriptableObject as the addon data asset for your <see cref="WeaponAbility"/>. It will appear inside the <see cref="CharacterWeaponAnimationData"/> inspector.
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Editor)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class WeaponAnimationAddonAttribute : InitializationAttribute
    {
        private static Dictionary<Type, Type> registeredPairs = new();
        public static ReadOnlyDictionary<Type, Type> RegisteredAddons = new(registeredPairs);

        private Type dataType;
        public WeaponAnimationAddonAttribute( Type dataType) {
            this.dataType = dataType;
        }

        public static bool IsCompatible(Type abilityType) {
            return abilityType.IsSubclassOf(typeof(WeaponAbility));
        }

        internal protected override void Init(Type abilityType) {
            if (dataType.IsAbstract || !dataType.IsSubclassOf(typeof(ScriptableObject)))
            {
                Debug.LogWarning($"Type {dataType.FullName} is not {nameof(ScriptableObject)} or is abstract and cannot be used as weapon ability setting for {abilityType.FullName}!");
                return;
            }
            registeredPairs[abilityType] = dataType;
        }
    }
}
