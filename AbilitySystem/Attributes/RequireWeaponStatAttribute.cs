using System;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireWeaponStatAttribute : Attribute
    {
        private static Dictionary<Type, HashSet<Type>> typeRequirements = new();
        private List<Type> weaponStatTypes;

        public RequireWeaponStatAttribute(params Type[] weaponStats) {
            weaponStatTypes = new();
            foreach (Type type in weaponStats) {
                if (type.IsSubclassOf(typeof(WeaponStat)))
                {
                    weaponStatTypes.Add(type);
                }
                else {
                    Debug.LogWarning("Attempting to add " + type.FullName + " to weapon stat requirement which is not a valid weapon stat type.");
                }
            }
        }

        public void Init(Type type) {
            if (!typeRequirements.ContainsKey(type)) { 
                typeRequirements.Add(type, new());
            }
            foreach (Type t in weaponStatTypes) {
                typeRequirements[type].Add(t);
            }
        }

        /// <summary>
        /// Check to see if the weapon contains all the WeaponStats required by the ability
        /// </summary>
        /// <param name="abilityType">The type of the ability being queried</param>
        /// <param name="weapon">The weapon being queried</param>
        /// <returns>True if the weapon contains all of the required stats, otherwise false</returns>
        public static bool HasWeaponStats(Type abilityType, WeaponManager weaponWielder) {
            if (!abilityType.IsSubclassOf(typeof(WeaponAbility))) {
                Debug.LogWarning("The ability type being queried is not a WeaponAbility!");
                return false;
            }
            if (weaponWielder == null) {
                return false;
            }

            if (typeRequirements.TryGetValue(abilityType, out var requirement)) {
                Weapon querying;
                if (OffhandWeaponAbilityAttribute.IsOffhand(abilityType)) {
                    querying = weaponWielder.Offhand;
                }
                else {
                    querying = weaponWielder.Mainhand;
                }
                if (querying == null)
                {
                    return false;
                }

                foreach (Type type in requirement) {
                    if (!querying.HasWeaponStat(type)) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
