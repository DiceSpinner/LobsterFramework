using System;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [RequireComponentReference(typeof(WeaponManager))]
    /// <summary>
    /// Convenient template for creating weapon abilities. Weapon abilities can only have one instance running at any given moment.
    /// Inheriting from this will provide automatic weapon stats, state, compatibility check when attempting to enqueue the ability.
    /// </summary>
    public abstract class WeaponAbility : AbilityCoroutine
    {
        protected WeaponManager WeaponManager { get; private set; }
        protected bool IsMainhanded { get;private set; }
        protected bool InstanceExecuting = false;

        protected sealed override void InitializeSharedReferences()
        {
            WeaponManager = GetComponentReference<WeaponManager>();
            IsMainhanded = !OffhandWeaponAbilityAttribute.IsOffhand(GetType());
            InitWeaponAbilityReferences();
        }

        /// <summary>
        /// Replaces <see cref="InitializeSharedReferences"/>
        /// </summary>
        protected virtual void InitWeaponAbilityReferences() { }

        /// <summary>
        /// Replaced by <see cref="WeaponAbilityReady"/>
        /// </summary>
        /// <returns></returns>

        protected sealed override bool ConditionSatisfied()
        {
            if (InstanceExecuting) {
                return false;
            }
            Weapon query;
            if (IsMainhanded) {
                query = WeaponManager.Mainhand;
                if (query.State != WeaponState.Idle) {
                    return false;
                }
            }
            else {
                query = WeaponManager.Offhand;
                if (WeaponManager.Mainhand != null && WeaponManager.Mainhand.State != WeaponState.Idle) {
                    return false;
                }
            }
            if (RequireWeaponStatAttribute.Requirements.TryGetValue(GetType(), out var set)) {
                foreach (Type statType in set) {
                    if (!query.HasWeaponStat(statType)) {
                        return false;
                    }
                }
            }

            return query != null && WeaponAbilityReady();
        }

        /// <summary>
        /// Use this to implement custom weapon ability rules, replaces <see cref="ConditionSatisfied"/>
        /// </summary>
        /// <returns>true if the ability is ready, otherwise false</returns>
        protected virtual bool WeaponAbilityReady() { return true; } 

        /// <summary>
        /// Replaced by <see cref="OnWeaponAbilityEnqueue"/>
        /// </summary>
        protected sealed override void OnCoroutineEnqueue() { 
            InstanceExecuting = true;
            OnWeaponAbilityEnqueue();
        }

        /// <summary>
        /// Replaces <see cref="OnCoroutineEnqueue"/>
        /// </summary>
        protected virtual void OnWeaponAbilityEnqueue() { }

        /// <summary>
        /// Replaced by <see cref="OnWeaponAbilityFinish"/>
        /// </summary>
        protected sealed override void OnCoroutineFinish()
        {
            InstanceExecuting = false;
            OnWeaponAbilityFinish();
        }

        /// <summary>
        /// Repalces <see cref="OnCoroutineFinish"/>
        /// </summary>
        protected virtual void OnWeaponAbilityFinish() { }
    }
}
