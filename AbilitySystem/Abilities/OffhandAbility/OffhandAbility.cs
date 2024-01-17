using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    [ComponentRequired(typeof(WeaponWielder))]
    public class OffhandAbility : Ability
    {
        private WeaponWielder weaponWielder;
        protected override void Initialize()
        {
            weaponWielder = abilityRunner.GetComponentInBoth<WeaponWielder>();
        }

        protected override bool ConditionSatisfied()
        {
            if (weaponWielder.Offhand != null)
            {
                ValueTuple<Type, string> setting = weaponWielder.Offhand.AbilitySetting;
                return abilityRunner.IsAbilityReady(setting.Item1, setting.Item2);
            }
            return false;
        }

        protected override void OnEnqueue()
        {
            ValueTuple<Type, string> setting = weaponWielder.Offhand.AbilitySetting;
            abilityRunner.EnqueueAbility(setting.Item1, setting.Item2);
            JoinAsSecondary(setting.Item1, setting.Item2);
        }

        protected override bool Action()
        {
            // Wait until the ability finishes
            return true;
        }
    }
    public class OffhandAbilityPipe : AbilityPipe { }
    public class OffhandAbilityRuntime : AbilityRuntime { }
}
