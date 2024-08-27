using System;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Use the weapon ability specified by the weapon equipped. Can be used to query the states of the weapon ability being runned.
    /// </summary>
    [RequireComponentReference(typeof(WeaponManager))]
    [AddAbilityMenu(Constants.Framework)] 
    public sealed class WeaponArt : Ability
    {
        private WeaponManager weaponManager;

        protected override void InitializeSharedReferences()
        {
            weaponManager = GetComponentReference<WeaponManager>();
        }

        protected override bool ConditionSatisfied()
        {
            if (weaponManager.Mainhand != null) {
                ValueTuple<Type, string> setting = weaponManager.Mainhand.AbilitySetting;
                return abilityManager.IsAbilityReady(setting.Item1, setting.Item2);
            }
            return false;
        }

        protected override void OnAbilityEnqueue()
        {
            (Type abilityType, string instance) = weaponManager.Mainhand.AbilitySetting;
            abilityManager.EnqueueAbility(abilityType, instance);
            JoinAsSecondary(abilityType, instance);
        }

        protected override void OnAbilityFinish()
        {
           (Type abilityType, string instance) = weaponManager.Mainhand.AbilitySetting;
            abilityManager.SuspendAbilityInstance(abilityType, instance);
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
