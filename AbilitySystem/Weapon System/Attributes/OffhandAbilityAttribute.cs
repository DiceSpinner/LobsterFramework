using System.Collections;
using System.Collections.Generic;
using System;
using LobsterFramework.Init;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Mark the WeaponAbility as an offhand WeaponAbility
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Runtime)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class OffhandWeaponAbilityAttribute : InitializationAttribute
    {
        private static readonly HashSet<Type> abilityTypes = new();

        public static bool IsOffhand(Type type)
        {
            return abilityTypes.Contains(type);
        }

        public static bool IsCompatible(Type abilityType) {
            return abilityType.IsSubclassOf(typeof(WeaponAbility)) ;
        }

        internal protected override void Init(Type abilityType)
        {
            abilityTypes.Add(abilityType);
        }
    }
}