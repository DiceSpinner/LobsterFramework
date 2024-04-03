using Codice.Client.BaseCommands;
using LobsterFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
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
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;
            runtime.currentWeapon = WeaponManager.Mainhand;

            AnimationClip animation = WeaponManager.AnimationData.GetAbilityClip(WeaponManager.Mainhand.WeaponType, typeof(LightWeaponAttack));
            abilityManager.StartAnimation(this, ConfigName, animation, runtime.currentWeapon.AttackSpeed);

            SubscribeWeaponEvent(runtime.currentWeapon);
            move.Apply(runtime.currentWeapon.LMoveSpeedModifier);
            rotate.Apply(runtime.currentWeapon.LRotationSpeedModifier);
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;

            do {
                // Wait for signal to attack
                while (!runtime.animationSignaled)
                {
                    yield return CoroutineOption.Continue;
                }

                runtime.currentWeapon.Enable();
                // Wait for signal of end attack
                while (!runtime.animationSignaled)
                {
                    yield return CoroutineOption.Continue;
                }
                runtime.currentWeapon.Pause();

                // Wait for animation signal of end recovery
                while (!runtime.animationSignaled) {
                    yield return CoroutineOption.Continue;
                }
            } while (runtime.inputSignaled); // Continue to perform attack if the user signal is received
        }

        protected override void OnCoroutineFinish(){
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;
            UnSubscribeWeaponEvent(runtime.currentWeapon);
            runtime.currentWeapon.Disable();
            move.Release();
            rotate.Release();
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        // Animation signal
        protected override void OnSignaled(AnimationEvent animationEvent)
        {
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;
            runtime.animationSignaled.Put(true);
        }

        // Player input
        protected override void OnSignaled()
        {
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;
            if (runtime.currentWeapon.state != WeaponState.Attacking) {
                runtime.inputSignaled.Put(true);
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
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;
            if (targets.IsTarget(entity))
            {
                runtime.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponManager.Mainhand, damageModifier));
            }
            else {
                runtime.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        private void OnWeaponHit(Weapon weapon)
        {
            LightWeaponAttackRuntime runtime = (LightWeaponAttackRuntime)Runtime;
            
            if (targets.IsTarget(weapon.Entity))
            {
                runtime.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponManager.Mainhand, damageModifier));
            }
            else
            {
                runtime.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }
    }

    public class LightWeaponAttackChannel : AbilityChannel { }

    public class LightWeaponAttackRuntime : AbilityCoroutineRuntime {
        public Weapon currentWeapon;
        public Signal<bool> animationSignaled = new();
        public Signal<bool> inputSignaled = new();
    }
}
