using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Indicate the weapon ability has an array of animations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class WeaponAnimationAttribute : Attribute
    {
        /// <summary>
        /// Key => Ability Type, Value => Enum Type indicates the number of animations and their names
        /// </summary>
        internal static Dictionary<Type, Type> AbilityAnimationInfo = new();

        private Type enumType;

        /// <param name="enumType">The default integer backed enum that defines the size of the animation clip array. Indexed by enum entries.</param>
        public WeaponAnimationAttribute( Type enumType) {
            this.enumType = enumType;
        }

        internal void Init(Type abilityType) {
            if (!abilityType.IsSubclassOf(typeof(WeaponAbility))) {
                Debug.LogError($"Type {abilityType.FullName} is not a valid {nameof(WeaponAbility)} type!");
                return;
            }
            if (!abilityType.IsSealed) {
                Debug.LogError($"Type {abilityType.FullName} must be sealed!");
                return;
            }

            if (enumType.IsEnum && Enum.GetUnderlyingType(enumType) == typeof(int))
            {
                AbilityAnimationInfo[abilityType] = enumType;
            }
            else {
                Debug.LogError("You must assign a enum type backed by integer without explicit values assignments for ability " + abilityType.Name);
            }
        }
    }
}
