using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Applied to <see cref="WeaponAbility"/> to mark it as a weapon art that can be called by <see cref="WeaponArt"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AddWeaponArtMenuAttribute : Attribute
    {
        public static Dictionary<WeaponType, HashSet<Type>> compatibleAbilities = new();

        private List<WeaponType> compatibleTypes;
        /// <summary>
        /// Add the Ability to the add weapon art menu with the specified compatabilities to weapon types
        /// </summary>
        /// <param name="ignoreMode">If set to true, the following array will be treated as black list, 
        /// meaning the ability will be compatible to every weapon type except for the ones in the array</param>
        /// <param name="weaponTypes"> A white list of weapon types that the ability can run on. This can be a black list if <paramref name="ignoreMode"/> is set to true </param>
        public AddWeaponArtMenuAttribute(bool ignoreMode, params WeaponType[] weaponTypes)
        {
            compatibleTypes = new List<WeaponType>();
            if (ignoreMode)
            {
                foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
                {
                    if (!weaponTypes.Contains(weaponType))
                    {
                        compatibleTypes.Add(weaponType);
                    }
                }
            }
            else
            {
                foreach (WeaponType weaponType in weaponTypes)
                {
                    compatibleTypes.Add(weaponType);
                }
            }
        }

        /// <summary>
        /// Add the Ability to the add weapon art menu with the specified compatabilities to weapon types
        /// </summary>
        /// <param name="weaponTypes"> A white list of weapon types that the ability can run on. </param>
        public AddWeaponArtMenuAttribute(params WeaponType[] weaponTypes)  {
            compatibleTypes = new List<WeaponType>();
            foreach (WeaponType weaponType in weaponTypes)
            {
                compatibleTypes.Add(weaponType);
            }
        }

        public void Init(Type type) {
            foreach (WeaponType weaponType in compatibleTypes) {
                if (!compatibleAbilities.ContainsKey(weaponType)) {
                    compatibleAbilities.Add(weaponType, new());
                }
                compatibleAbilities[weaponType].Add(type);
            }
        }
    }
}
