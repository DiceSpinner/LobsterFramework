using LobsterFramework.Init;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [RegisterInitialization(AttributeType = InitializationAttributeType.Runtime)]
    public sealed class RequireWeaponStatAttribute : InitializationAttribute
    {
        private static Dictionary<Type, HashSet<Type>> typeRequirements = new();
        private List<Type> weaponStatTypes;

        /// <summary>
        /// Stores the required <see cref="WeaponStat"/> for each <see cref="WeaponAbility"/>
        /// <br/>
        /// Key: Type of the weapon ability, Value: A set of <see cref="WeaponStat"/> types required by this ability
        /// </summary>
        public static ReadOnlyDictionary<Type, HashSet<Type>> Requirements = new(typeRequirements);

        public RequireWeaponStatAttribute( params Type[] weaponStats) {
            weaponStatTypes = new();
            foreach (Type type in weaponStats) {
                if (type == null) {
                    continue;
                }
                if (type.IsSubclassOf(typeof(WeaponStat)))
                {
                    weaponStatTypes.Add(type);
                }
                else {
                    Debug.LogWarning("Attempting to add " + type.FullName + " to weapon stat requirement which is not a valid weapon stat type.");
                }
            }
        }

        public static bool IsCompatible(Type type)
        {
            return type.IsSubclassOf(typeof(WeaponAbility));
        }

        internal protected override void Init(Type type) {
            if (!typeRequirements.ContainsKey(type)) { 
                typeRequirements.Add(type, new());
            }
            foreach (Type t in weaponStatTypes) {
                typeRequirements[type].Add(t);
            }
        }
    }
}
