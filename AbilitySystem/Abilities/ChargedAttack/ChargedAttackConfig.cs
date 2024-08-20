using System;
using UnityEngine;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [Serializable]
    public class ChargedAttackConfig : AbilityConfig
    {
        [field:SerializeField] public float BaseDamageModifier { get; private set; }
        [field: SerializeField] public float MaxChargeDamageIncrease { get; private set; }
        [field: SerializeField] public float ChargeMaxTime { get; private set; }
    }
}
