using LobsterFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu(Constants.Framework)]
    [WeaponArt(BlackList = true)]
    [RequireAbilityComponents(typeof(DamageModifier))]
    [RequireComponentReference(typeof(MovementController))]
    public sealed class Attack : WeaponAbility
    {
        [SerializeField] private TargetSetting targets;
        private Entity attacker;
        private MovementController moveControl;
        private DamageModifier damageModifier;
        private CombinedValueEffector<float> move;
        private CombinedValueEffector<float> rotate;

        private AnimancerState state;

        protected override void InitWeaponAbilityReferences()
        {
            attacker = WeaponManager.Wielder;
            moveControl = attacker.GetComponent<MovementController>();
            damageModifier = abilityManager.GetAbilityComponent<DamageModifier>();
            move = moveControl.moveSpeedModifier.MakeEffector();
            rotate = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override void OnCoroutineEnqueue()
        {
            AttackContext context = (AttackContext)Context;
            context.currentWeapon = WeaponManager.Mainhand;
            context.animationSignaled.Reset();
            context.inputSignaled.Reset();

            AnimationClip animation = WeaponManager.AnimationData.GetAbilityClip<Attack>(WeaponManager.Mainhand.WeaponType);
            state = abilityManager.StartAnimation(this, Instance, animation, context.currentWeapon.AttackSpeed);

            SubscribeWeaponEvent(context.currentWeapon);
            move.Apply(context.currentWeapon.LMoveSpeedModifier);
            rotate.Apply(context.currentWeapon.LRotationSpeedModifier);
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            AttackContext context = (AttackContext)Context;

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
            AttackContext context = (AttackContext)Context;
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
            AttackContext context = (AttackContext)Context;
            context.animationSignaled.Put(true);
        }

        // Player input
        protected override void OnSignaled()
        {
            AttackContext context = (AttackContext)Context;
            if (context.currentWeapon.state != WeaponState.Attacking) {
                context.inputSignaled.Put(true);
            }
        }

        private void SubscribeWeaponEvent(Weapon weapon)
        {
            weapon.OnEntityHit += OnEntityHit;
            weapon.OnWeaponHit += OnWeaponHit;
        }

        private void UnSubscribeWeaponEvent(Weapon weapon)
        {
            weapon.OnEntityHit -= OnEntityHit;
            weapon.OnWeaponHit -= OnWeaponHit;
        }

        private void OnEntityHit(Entity entity)
        {
            AttackContext context = (AttackContext)Context;
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
            AttackContext context = (AttackContext)Context;
            
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

    public class AttackChannel : AbilityChannel { }

    public class AttackContext : AbilityCoroutineContext {
        public Weapon currentWeapon;
        public Signal<bool> animationSignaled = new();
        public Signal<bool> inputSignaled = new();
    }

    public class Test : ScriptableObject {
        public int a;
        public int b;
        public string c;
    }
}
