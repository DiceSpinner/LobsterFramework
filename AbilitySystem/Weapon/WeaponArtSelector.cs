using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;
using System;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [Serializable]
    public class WeaponArtSelector : SerializableType<Ability>
    {
        public WeaponType weaponType;
        [SerializeField] private string configName;
        public string ConfigName { get { return configName; } }
    }
}
