using LobsterFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu]
    [RequireAbilityComponents(typeof(DamageModifier))]
    [ComponentRequired(typeof(WeaponManager))]
    public class LightWeaponAttack : WeaponAbility
    {
        [SerializeField] private TargetSetting targets;
        private Entity attacker;
        private MovementController moveControl;
        private DamageModifier damageModifier;
        private CombinedValueEffector<float> move;
        private CombinedValueEffector<float> rotate;

        private AnimancerState state;

        protected override void Init()
        {
            attacker = WeaponManager.Wielder;
            moveControl = attacker.GetComponent<MovementController>();
            damageModifier = abilityManager.GetAbilityComponent<DamageModifier>();
            move = moveControl.moveSpeedModifier.MakeEffector();
            rotate = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override void OnCoroutineEnqueue()
        {
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;
            context.currentWeapon = WeaponManager.Mainhand;
            context.animationSignaled.Reset();
            context.inputSignaled.Reset();

            AnimationClip animation = WeaponManager.AnimationData.GetAbilityClip(WeaponManager.Mainhand.WeaponType, typeof(LightWeaponAttack));
            state = abilityManager.StartAnimation(this, Instance, animation, context.currentWeapon.AttackSpeed);

            SubscribeWeaponEvent(context.currentWeapon);
            move.Apply(context.currentWeapon.LMoveSpeedModifier);
            rotate.Apply(context.currentWeapon.LRotationSpeedModifier);
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;

            do {
                // Wait for signal to attack
                while (!context.animationSignaled)
                {
                    yield return CoroutineOption.Continue;
                }

                context.currentWeapon.Enable();
                // Wait for signal of end attack
                while (!context.animationSignaled)
                {
                    yield return CoroutineOption.Continue;
                }
                context.currentWeapon.Pause();

                // Wait for animation signal of end recovery
                while (!context.animationSignaled) {
                    yield return CoroutineOption.Continue;
                }
            } while (context.inputSignaled); // Continue to perform attack if the user signal is received
        }

        protected override void OnCoroutineFinish(){
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;
            UnSubscribeWeaponEvent(context.currentWeapon);
            context.currentWeapon.Disable();
            move.Release();
            rotate.Release();
            state = null;
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        // Animation signal
        protected override void OnSignaled(AnimationEvent animationEvent)
        {
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;
            context.animationSignaled.Put(true);
        }

        // Player input
        protected override void OnSignaled()
        {
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;
            if (context.currentWeapon.state != WeaponState.Attacking) {
                context.inputSignaled.Put(true);
            }
        }

        private void SubscribeWeaponEvent(Weapon weapon)
        {
            weapon.onEntityHit += OnEntityHit;
            weapon.onWeaponHit += OnWeaponHit;
        }

        private void UnSubscribeWeaponEvent(Weapon weapon)
        {
            weapon.onEntityHit -= OnEntityHit;
            weapon.onWeaponHit -= OnWeaponHit;
        }

        private void OnEntityHit(Entity entity)
        {
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;
            if (targets.IsTarget(entity))
            {
                context.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponManager.Mainhand, damageModifier));
            }
            else {
                context.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        private void OnWeaponHit(Weapon weapon)
        {
            LightWeaponAttackContext context = (LightWeaponAttackContext)Context;
            
            if (targets.IsTarget(weapon.Entity))
            {
                context.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponManager.Mainhand, damageModifier));
            }
            else
            {
                context.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }
    }

    public class LightWeaponAttackChannel : AbilityChannel { }

    public class LightWeaponAttackContext : AbilityCoroutineContext {
        public Weapon currentWeapon;
        public Signal<bool> animationSignaled = new();
        public Signal<bool> inputSignaled = new();
    }
}
