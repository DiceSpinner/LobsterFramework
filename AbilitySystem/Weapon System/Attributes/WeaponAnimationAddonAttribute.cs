using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.ObjectModel;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Register a ScriptableObject as the addon data asset for your <see cref="WeaponAbility"/>. It will appear inside the <see cref="CharacterWeaponAnimationData"/> inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class WeaponAnimationAddonAttribute : Attribute
    {
        private static Dictionary<Type, Type> registeredPairs = new();
        public static ReadOnlyDictionary<Type, Type> RegisteredAddons = new(registeredPairs);

        private Type dataType;
        public WeaponAnimationAddonAttribute( Type dataType) {
            this.dataType = dataType;
        }

        public void Init(Type abilityType) {
            if (abilityType.IsAbstract || !abilityType.IsSubclassOf(typeof(WeaponAbility))) {
                Debug.LogWarning($"Cannot use {nameof(WeaponAnimationAddonAttribute)} on type {abilityType.FullName} since it's not a valid {nameof(WeaponAbility)} type!");
                return;
            }
            if (dataType.IsAbstract || !dataType.IsSubclassOf(typeof(ScriptableObject))) {
                Debug.LogWarning($"Type {dataType.FullName} is not {nameof(ScriptableObject)} or is abstract and cannot be used as weapon ability setting for {abilityType.FullName}!");
                return;
            }
            registeredPairs[abilityType] = dataType;
        }
    }
}
