using LobsterFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    [RequireAbilityComponents(typeof(DamageModifier))]
    [ComponentRequired(typeof(WeaponWielder))]
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
            attacker = WeaponWielder.Wielder;
            moveControl = attacker.GetComponent<MovementController>();
            damageModifier = abilityRunner.GetAbilityStat<DamageModifier>();
            move = moveControl.moveSpeedModifier.MakeEffector();
            rotate = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override bool WConditionSatisfied()
        {
            return WeaponWielder.GetAbilityClip(GetType(), WeaponWielder.Mainhand.WeaponType) != null && WeaponWielder.Mainhand.state != WeaponState.Attacking;
        }

        protected override void OnCoroutineEnqueue()
        {
            LightWeaponAttackConfig c = (LightWeaponAttackConfig)Config;
            c.currentWeapon = WeaponWielder.Mainhand;
            SubscribeWeaponEvent(c.currentWeapon);
            c.signaled = false;
            move.Apply(c.currentWeapon.LMoveSpeedModifier);
            rotate.Apply(c.currentWeapon.LRotationSpeedModifier);
            abilityRunner.StartAnimation(this, ConfigName, WeaponWielder.GetAbilityClip(GetType(), WeaponWielder.Mainhand.WeaponType), c.currentWeapon.AttackSpeed);
        }

        protected override IEnumerator<CoroutineOption> Coroutine()
        {
            LightWeaponAttackConfig c = (LightWeaponAttackConfig)Config;
            // Wait for signal to attack
            while (!c.signaled)
            {
                yield return CoroutineOption.Continue;
            }
            c.signaled = false;

            c.currentWeapon.Enable();
            // Wait for signal of recovery
            while (!c.signaled)
            {
                yield return CoroutineOption.Continue;
            }
            c.signaled = false;
            c.currentWeapon.Disable();

            move.Release();
            rotate.Release();

            // Wait for animation to finish
            while (true)
            {
                yield return CoroutineOption.Continue;
            }
        }

        protected override void OnCoroutineFinish(){
            LightWeaponAttackConfig l = (LightWeaponAttackConfig)Config;
            UnSubscribeWeaponEvent(l.currentWeapon);
            l.currentWeapon.Disable();
            move.Release();
            rotate.Release();
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        protected override void Signal(AnimationEvent animationEvent)
        {
            if (animationEvent != null)
            {
                LightWeaponAttackConfig c = (LightWeaponAttackConfig)Config;
                c.signaled = true;
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
            LightWeaponAttackConfig config = (LightWeaponAttackConfig)Config;
            if (targets.IsTarget(entity))
            {
                config.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponWielder.Mainhand, damageModifier));
            }
            else {
                config.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        private void OnWeaponHit(Weapon weapon)
        {
            LightWeaponAttackConfig config = (LightWeaponAttackConfig)Config;
            
            if (targets.IsTarget(weapon.Entity))
            {
                config.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponWielder.Mainhand, damageModifier));
            }
            else
            {
                config.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }
    }

    public class LightWeaponAttackPipe : AbilityPipe { }

    public class LightWeaponAttackRuntime : AbilityCoroutineRuntime { }
}
