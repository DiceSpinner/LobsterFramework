using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LobsterFramework.AbilitySystem
{
    [ComponentRequired(typeof(WeaponWielder))]
    [AddAbilityMenu]
    public class WeaponArt : Ability
    {
        private WeaponWielder weaponWielder;

        protected override void Initialize()
        {
            weaponWielder = abilityRunner.GetComponentInBoth<WeaponWielder>();
        }

        protected override bool ConditionSatisfied()
        {
            if (weaponWielder.Mainhand != null) {
                ValueTuple<Type, string> setting = weaponWielder.Mainhand.AbilitySetting;
                return abilityRunner.IsAbilityReady(setting.Item1, setting.Item2);
            }
            return false;
        }

        protected override void OnEnqueue()
        {
            ValueTuple<Type, string> setting = weaponWielder.Mainhand.AbilitySetting;
            abilityRunner.EnqueueAbility(setting.Item1, setting.Item2);
            JoinAsSecondary(setting.Item1, setting.Item2);
        }

        protected override bool Action()
        {
            // Wait until the ability finishes
            return true;
        }
    }
    public class WeaponArtPipe : AbilityPipe { }
    public class WeaponArtRuntime : AbilityRuntime { }
}
