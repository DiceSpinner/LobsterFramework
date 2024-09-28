using UnityEngine;
using System.Collections.Generic;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu(Constants.Framework)]
    [WeaponArt(BlackList = true)]
    [RequireAbilityComponents(typeof(DamageModifier))]
    [RequireComponentReference(typeof(MovementController))]
    public sealed class ChargedAttack : WeaponAbility
    {
        [SerializeField] private TargetSetting targets;
        
        private MovementController moveControl;
        private DamageModifier damageModifier;
        private CombinedValueEffector<float> move;
        private CombinedValueEffector<float> rotate;

        protected override void InitWeaponAbilityReferences()
        {
            moveControl = GetComponentReference<MovementController>();
            damageModifier = AbilityManager.GetAbilityComponent<DamageModifier>();
            move = moveControl.moveSpeedModifier.MakeEffector();
            rotate = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override void InitializeContext()
        {
            ChargedAttackChannel channel = (ChargedAttackChannel)Channel;
            channel.SetConfig((ChargedAttackConfig)Config);
        }

        protected override void OnWeaponAbilityEnqueue()
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            context.currentWeapon = WeaponManager.Mainhand;
            context.chargeTimer = 0;
            context.animationSignaled.Reset();
            context.inputSignaled.Reset();

            AnimationClip animation = WeaponManager.AnimationData.GetAbilityClip<ChargedAttack>(context.currentWeapon.WeaponType);

            SubscribeWeaponEvent();
            move.Apply(context.currentWeapon.HMoveSpeedModifier);
            rotate.Apply(context.currentWeapon.HRotationSpeedModifier);
            context.animationState = AbilityManager.StartAnimation(this, Instance, animation, WeaponManager.Mainhand.AttackSpeed);
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            ChargedAttackConfig config = (ChargedAttackConfig)Config;
            // Wait for signal to charge
            while (!context.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            context.animationState.IsPlaying = false;
            // Wait for signal to attack
            while (!context.inputSignaled && context.chargeTimer < config.ChargeMaxTime)
            {
                context.chargeTimer += Time.deltaTime;
                yield return CoroutineOption.Continue;
            }

            context.animationState.IsPlaying = true;
            context.currentWeapon.Enable();

            // Wait for signal of recovery
            while (!context.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            context.currentWeapon.Disable();

            // Wait for animation to finish
            while (true)
            {
                yield return CoroutineOption.Continue;
            }
        }

        protected override void OnWeaponAbilityFinish()
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            UnSubscribeWeaponEvent();
            context.currentWeapon.Disable();
            move.Release();
            rotate.Release();
        }

        protected override void OnSignaled(AnimationEvent animationEvent)
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            context.animationSignaled.Put(true);
        }

        protected override void OnSignaled()
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            context.inputSignaled.Put(true);
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        public void OnEntityHit(Entity entity)
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            ChargedAttackConfig config = (ChargedAttackConfig)Config;
            if (targets.IsTarget(entity))
            {
                if (context.chargeTimer > config.ChargeMaxTime)
                {
                    context.chargeTimer = config.ChargeMaxTime;
                }
                context.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponManager.Mainhand, damageModifier));
            }
            else {
                context.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        public void OnWeaponHit(Weapon weapon)
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            ChargedAttackConfig config = (ChargedAttackConfig)Config;
            if (targets.IsTarget(weapon.Entity))
            {
                if (context.chargeTimer > config.ChargeMaxTime)
                {
                    context.chargeTimer = config.ChargeMaxTime;
                }
                context.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponManager.Mainhand, damageModifier));
            }
            else
            {
                context.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        public void SubscribeWeaponEvent()
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            context.currentWeapon.OnEntityHit += OnEntityHit;
            context.currentWeapon.OnWeaponHit += OnWeaponHit;
        }

        public void UnSubscribeWeaponEvent()
        {
            ChargedAttackContext context = (ChargedAttackContext)Context;
            context.currentWeapon.OnEntityHit -= OnEntityHit;
            context.currentWeapon.OnWeaponHit -= OnWeaponHit;
        }  
    }

    public class ChargedAttackChannel : AbilityChannel
    {
        private ChargedAttackConfig conf;
        public float MaxChargeTime { get { return conf.ChargeMaxTime; } }
        public float MaxChargeDamageIncrease { get { return conf.MaxChargeDamageIncrease; } }
        public float BaseDamageModifier { get { return conf.BaseDamageModifier; } }

        public void SetConfig(ChargedAttackConfig config) { conf = config; }
    }

    public class ChargedAttackContext : AbilityCoroutineContext
    {
        public Signal<bool> animationSignaled = new();
        public Signal<bool> inputSignaled = new();
        public Weapon currentWeapon;
        public AnimancerState animationState;
        public float chargeTimer;
    }
}
