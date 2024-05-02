using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu("LobsterFramework")]
    [ComponentRequired(typeof(WeaponManager))]
    public class OffhandAbility : Ability
    {
        private WeaponManager weaponWielder;
        protected override void InitializeSharedReferences()
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

        protected override void OnAbilityEnqueue()
        {
            ValueTuple<Type, string> setting = weaponWielder.Offhand.AbilitySetting;
            if (abilityManager.EnqueueAbility(setting.Item1, setting.Item2))
            {
                JoinAsSecondary(setting.Item1, setting.Item2);
            }
            else {
                SuspendInstance(Instance);
            }
        }

        protected override bool Action()
        {
            // Wait until the ability finishes
            return true;
        }
    }
    public class OffhandAbilityChannel : AbilityChannel { }
    public class OffhandAbilityContext : AbilityContext { }
}
