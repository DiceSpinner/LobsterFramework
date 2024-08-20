using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu(Constants.Framework)]
    [RequireComponentReference(typeof(WeaponManager))]
    public sealed class OffhandAbility : Ability
    {
        private WeaponManager weaponManager;
        protected override void InitializeSharedReferences()
        {
            weaponManager = GetComponentReference<WeaponManager>();
        }

        protected override bool ConditionSatisfied()
        {
            if (weaponManager.Offhand != null && (weaponManager.Mainhand == null || !weaponManager.Mainhand.DoubleHanded))
            {
                ValueTuple<Type, string> setting = weaponManager.Offhand.AbilitySetting;
                return abilityManager.IsAbilityReady(setting.Item1, setting.Item2);
            }
            return false;
        }

        protected override void OnAbilityEnqueue()
        {
            (Type abilityType, string instance) = weaponManager.Offhand.AbilitySetting;
            if (abilityManager.EnqueueAbility(abilityType, instance))
            {
                JoinAsSecondary(abilityType, instance);
            }
            else {
                SuspendInstance(Instance);
            }
        }

        protected override void OnAbilityFinish()
        {
            (Type abilityType, string instance) = weaponManager.Offhand.AbilitySetting;
             abilityManager.SuspendAbilityInstance(abilityType, instance);
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
