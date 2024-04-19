using UnityEngine;
using System.Collections.Generic;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu]
    [RequireAbilityComponents(typeof(DamageModifier))]
    [ComponentRequired(typeof(WeaponManager))]
    public class HeavyWeaponAttack : WeaponAbility
    {
        [SerializeField] private TargetSetting targets;
        
        private MovementController moveControl;
        private DamageModifier damageModifier;
        private CombinedValueEffector<float> move;
        private CombinedValueEffector<float> rotate;

        protected override void Init()
        {
            moveControl = abilityManager.GetComponentInBoth<MovementController>();
            damageModifier = abilityManager.GetAbilityComponent<DamageModifier>();
            move = moveControl.moveSpeedModifier.MakeEffector();
            rotate = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override void InitializeContext()
        {
            HeavyWeaponAttackChannel channel = (HeavyWeaponAttackChannel)Channel;
            channel.SetConfig((HeavyWeaponAttackConfig)Config);
        }

        protected override void OnCoroutineEnqueue()
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            context.currentWeapon = WeaponManager.Mainhand;
            context.chargeTimer = 0;
            context.animationSignaled.Reset();
            context.inputSignaled.Reset();

            AnimationClip animation = WeaponManager.AnimationData.GetAbilityClip(context.currentWeapon.WeaponType, typeof(HeavyWeaponAttack));

            SubscribeWeaponEvent();
            move.Apply(context.currentWeapon.HMoveSpeedModifier);
            rotate.Apply(context.currentWeapon.HRotationSpeedModifier);
            context.animationState = abilityManager.StartAnimation(this, Instance, animation, WeaponManager.Mainhand.AttackSpeed);
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            HeavyWeaponAttackConfig config = (HeavyWeaponAttackConfig)Config;
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

        protected override void OnCoroutineFinish()
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            UnSubscribeWeaponEvent();
            context.currentWeapon.Disable();
            move.Release();
            rotate.Release();
        }

        protected override void OnSignaled(AnimationEvent animationEvent)
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            context.animationSignaled.Put(true);
        }

        protected override void OnSignaled()
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            context.inputSignaled.Put(true);
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        public void OnEntityHit(Entity entity)
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            HeavyWeaponAttackConfig config = (HeavyWeaponAttackConfig)Config;
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
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            HeavyWeaponAttackConfig config = (HeavyWeaponAttackConfig)Config;
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
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            context.currentWeapon.onEntityHit += OnEntityHit;
            context.currentWeapon.onWeaponHit += OnWeaponHit;
        }

        public void UnSubscribeWeaponEvent()
        {
            HeavyWeaponAttackContext context = (HeavyWeaponAttackContext)Context;
            context.currentWeapon.onEntityHit -= OnEntityHit;
            context.currentWeapon.onWeaponHit -= OnWeaponHit;
        }  
    }

    public class HeavyWeaponAttackChannel : AbilityChannel
    {
        private HeavyWeaponAttackConfig conf;
        public float MaxChargeTime { get { return conf.ChargeMaxTime; } }
        public float MaxChargeDamageIncrease { get { return conf.MaxChargeDamageIncrease; } }
        public float BaseDamageModifier { get { return conf.BaseDamageModifier; } }

        public void SetConfig(HeavyWeaponAttackConfig config) { conf = config; }
    }

    public class HeavyWeaponAttackContext : AbilityCoroutineContext
    {
        public Signal<bool> animationSignaled = new();
        public Signal<bool> inputSignaled = new();
        public Weapon currentWeapon;
        public AnimancerState animationState;
        public float chargeTimer;
    }
}
