using LobsterFramework.Utility;
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
    public class WeaponArtAttribute : Attribute
    {
        /// <summary>
        /// A array of lists that contains the type of weapon arts that can be performed by each weapon, indexed by <see cref="WeaponType"/>
        /// </summary>
        internal static List<Type>[] weaponArtsByWeaponType;

        static WeaponArtAttribute() {
            int size = EnumCache.GetSize<WeaponType>();
            weaponArtsByWeaponType = new List<Type>[size];
            for (int i = 0; i < size; i++)
            {
                weaponArtsByWeaponType[i] = new();
            }
        }

        /// <summary>
        /// Flag to indicate whether the array of weapon types is a blacklist or not
        /// </summary>
        public bool BlackList = false;
        private List<WeaponType> inputCollection;
        /// <summary>
        /// Add the Ability to the add weapon art menu with the specified compatabilities to weapon types
        /// </summary>
        /// <param name="weaponTypes"> A white list of weapon types that the ability can run on. This can be a black list if <see cref="BlackList"/> is set to true </param>
        public WeaponArtAttribute(params WeaponType[] weaponTypes)
        {
            inputCollection = new List<WeaponType>(weaponTypes);
        }

        public void Init(Type type) {
            if (!type.IsSubclassOf(typeof(WeaponAbility))) {
                return;
            }
            if (BlackList)
            {
                for(int i = 0;i < EnumCache.GetSize<WeaponType>();i++)
                {
                    if (!inputCollection.Contains((WeaponType)i)) {
                        weaponArtsByWeaponType[i].Add(type);
                    }
                }
            }
            else {
                foreach (WeaponType weaponType in inputCollection)
                {
                    weaponArtsByWeaponType[(int)weaponType].Add(type); 
                }
            }
        }
    }
}
