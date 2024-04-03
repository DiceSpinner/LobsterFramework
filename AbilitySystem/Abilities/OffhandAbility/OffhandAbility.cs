using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    [ComponentRequired(typeof(WeaponManager))]
    public class OffhandAbility : Ability
    {
        private WeaponManager weaponWielder;
        protected override void Initialize()
        {
            weaponWielder = abilityManager.GetComponentInBoth<WeaponManager>();
        }

        protected override bool ConditionSatisfied()
        {
            if (weaponWielder.Offhand != null && (weaponWielder.Mainhand == null || !weaponWielder.Mainhand.DoubleHanded))
            {
                ValueTuple<Type, string> setting = weaponWielder.Offhand.AbilitySetting;
                return abilityManager.IsAbilityReady(setting.Item1, setting.Item2);
            }
            return false;
        }

        protected override void OnEnqueue()
        {
            ValueTuple<Type, string> setting = weaponWielder.Offhand.AbilitySetting;
            if (abilityManager.EnqueueAbility(setting.Item1, setting.Item2))
            {
                JoinAsSecondary(setting.Item1, setting.Item2);
            }
            else {
                SuspendInstance(ConfigName);
            }
        }

        protected override bool Action()
        {
            // Wait until the ability finishes
            return true;
        }
    }
    public class OffhandAbilityChannel : AbilityChannel { }
    public class OffhandAbilityRuntime : AbilityRuntime { }
}
