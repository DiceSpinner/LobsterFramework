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

        protected sealed override void InitializeSharedReferences()
        {
            WeaponManager = GetComponentReference<WeaponManager>();
            IsMainhanded = !OffhandWeaponAbilityAttribute.IsOffhand(GetType());
            InitWeaponAbilityReferences();
        }

        /// <summary>
        /// Use this to implement custom initialization routines
        /// </summary>
        protected virtual void InitWeaponAbilityReferences() { }

        protected sealed override bool ConditionSatisfied()
        {
            Weapon query;
            if (IsMainhanded) {
                query = WeaponManager.Mainhand;
            }
            else {
                query = WeaponManager.Offhand;
                if (WeaponManager.Mainhand != null && WeaponManager.Mainhand.state != WeaponState.Idle) {
                    return false;
                }
            }
            return query != null && InstancesRunning == 0 && RequireWeaponStatAttribute.HasWeaponStats(GetType(), WeaponManager) && WeaponAbilityReady();
        }

        /// <summary>
        /// Use this to implement custom weapon ability rules
        /// </summary>
        /// <returns>true if the ability is ready, otherwise false</returns>
        protected virtual bool WeaponAbilityReady() { return true; } 
    }
}
