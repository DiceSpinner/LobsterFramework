using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [ComponentRequired(typeof(WeaponManager))]
    [AddAbilityMenu]
    public class WeaponArt : Ability
    {
        private WeaponManager weaponWielder;

        protected override void InitializeSharedReferences()
        {
            weaponWielder = abilityManager.GetComponentInBoth<WeaponManager>();
        }

        protected override bool ConditionSatisfied()
        {
            if (weaponWielder.Mainhand != null) {
                ValueTuple<Type, string> setting = weaponWielder.Mainhand.AbilitySetting;
                return abilityManager.IsAbilityReady(setting.Item1, setting.Item2);
            }
            return false;
        }

        protected override void OnAbilityEnqueue()
        {
            ValueTuple<Type, string> setting = weaponWielder.Mainhand.AbilitySetting;
            abilityManager.EnqueueAbility(setting.Item1, setting.Item2);
            JoinAsSecondary(setting.Item1, setting.Item2);
        }

        protected override bool Action()
        {
            // Wait until the ability finishes
            return true;
        }
    }
    public class WeaponArtChannel : AbilityChannel { }
    public class WeaponArtContext : AbilityContext { }
}
