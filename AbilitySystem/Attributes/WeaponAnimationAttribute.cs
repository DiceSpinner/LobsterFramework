using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    public class WeaponAnimationAttribute : Attribute
    {
        public static Dictionary<Type, Type> abilityAnimationEntry = new();

        private Type enumType;

        public WeaponAnimationAttribute(Type enumType) {
            this.enumType = enumType;
        }

        public void Init(Type abilityType) {
            if (enumType.IsEnum && Enum.GetUnderlyingType(enumType) == typeof(int))
            {
                abilityAnimationEntry[abilityType] = enumType;
            }
            else {
                Debug.LogError("You must assign a enum type backed by integer for ability " + abilityType.Name);
            }
        }
    }
}
