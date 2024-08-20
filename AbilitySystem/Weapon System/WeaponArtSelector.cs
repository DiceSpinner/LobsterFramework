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
        [SerializeField] internal WeaponType weaponType;
        [SerializeField] internal string instance=Ability.DefaultAbilityInstance;
        public string Instance { get { return instance; } }
        public WeaponType WeaponType { get {  return weaponType; }}
    }
}
