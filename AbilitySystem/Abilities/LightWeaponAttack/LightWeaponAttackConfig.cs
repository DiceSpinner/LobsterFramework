using LobsterFramework.AbilitySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    public class LightWeaponAttackConfig : AbilityConfig
    {
        [HideInInspector]
        public bool signaled;
        [HideInInspector]
        public Weapon currentWeapon;
    }
}
