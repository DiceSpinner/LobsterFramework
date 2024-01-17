using UnityEngine;
using System.Collections.Generic;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    [RequireAbilityComponents(typeof(DamageModifier))]
    [ComponentRequired(typeof(WeaponWielder))]
    public class HeavyWeaponAttack : WeaponAbility
    {
        [SerializeField] private TargetSetting targets;
        
        private MovementController moveControl;
        private DamageModifier damageModifier;
        private CombinedValueEffector<float> move;
        private CombinedValueEffector<float> rotate;

        protected override void Init()
        {
            moveControl = abilityRunner.GetComponentInBoth<MovementController>();
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
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            runtime.currentWeapon = WeaponWielder.Mainhand;
            SubscribeWeaponEvent();
            runtime.animationSignaled = false;
            runtime.inputSignaled = false;
            runtime.chargeTimer = 0;
            move.Apply(runtime.currentWeapon.HMoveSpeedModifier);
            rotate.Apply(runtime.currentWeapon.HRotationSpeedModifier);
            runtime.animationState = abilityRunner.StartAnimation(this, ConfigName, WeaponWielder.GetAbilityClip(GetType(), runtime.currentWeapon.WeaponType), WeaponWielder.Mainhand.AttackSpeed);
        }

        protected override IEnumerator<CoroutineOption> Coroutine()
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            HeavyWeaponAttackConfig config = (HeavyWeaponAttackConfig)Config;
            // Wait for signal to charge
            while (!runtime.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            runtime.animationSignaled = false;
            runtime.animationState.IsPlaying = false;
            // Wait for signal to attack
            while (!runtime.inputSignaled && runtime.chargeTimer < config.ChargeMaxTime)
            {
                runtime.chargeTimer += Time.deltaTime;
                yield return CoroutineOption.Continue;
            }

            runtime.inputSignaled = false;
            runtime.animationState.IsPlaying = true;
            runtime.currentWeapon.Enable();

            // Wait for signal of recovery
            while (!runtime.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            runtime.animationSignaled = false;
            runtime.currentWeapon.Disable();

            // Wait for animation to finish
            while (true)
            {
                yield return CoroutineOption.Continue;
            }
        }

        protected override void OnCoroutineFinish()
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            UnSubscribeWeaponEvent();
            runtime.animationSignaled = false;
            runtime.inputSignaled = false;
            runtime.currentWeapon.Disable();
            move.Release();
            rotate.Release();
        }

        protected override void Signal(AnimationEvent animationEvent)
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            if (animationEvent != null)
            {
                runtime.animationSignaled = true;
            }
            else {
                runtime.inputSignaled = true;
            }
        }

        private void DealDamage(Entity entity, float modifier)
        {
            if (targets.IsTarget(entity))
            {
                WeaponUtility.WeaponDamage(WeaponWielder.Mainhand, entity, damageModifier, modifier);
            }
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        public void OnEntityHit(Entity entity)
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            HeavyWeaponAttackConfig config = (HeavyWeaponAttackConfig)Config;
            if (targets.IsTarget(entity))
            {
                if (runtime.chargeTimer > config.ChargeMaxTime)
                {
                    runtime.chargeTimer = config.ChargeMaxTime;
                }
                runtime.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponWielder.Mainhand, damageModifier));
            }
            else {
                runtime.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        public void OnWeaponHit(Weapon weapon)
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            HeavyWeaponAttackConfig config = (HeavyWeaponAttackConfig)Config;
            if (targets.IsTarget(weapon.Entity))
            {
                if (runtime.chargeTimer > config.ChargeMaxTime)
                {
                    runtime.chargeTimer = config.ChargeMaxTime;
                }
                runtime.currentWeapon.SetOnHitDamage(WeaponUtility.ComputeDamage(WeaponWielder.Mainhand, damageModifier));
            }
            else
            {
                runtime.currentWeapon.SetOnHitDamage(Damage.none);
            }
        }

        public void SubscribeWeaponEvent()
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            runtime.currentWeapon.onEntityHit += OnEntityHit;
            runtime.currentWeapon.onWeaponHit += OnWeaponHit;
        }

        public void UnSubscribeWeaponEvent()
        {
            HeavyWeaponAttackRuntime runtime = (HeavyWeaponAttackRuntime)Runtime;
            runtime.currentWeapon.onEntityHit -= OnEntityHit;
            runtime.currentWeapon.onWeaponHit -= OnWeaponHit;
        }  
    }

    public class HeavyWeaponAttackPipe : AbilityPipe
    {
        private HeavyWeaponAttackConfig conf;
        public float MaxChargeTime { get { return conf.ChargeMaxTime; } }
        public float MaxChargeDamageIncrease { get { return conf.MaxChargeDamageIncrease; } }
        public float BaseDamageModifier { get { return conf.BaseDamageModifier; } }

        public override void Construct()
        {
            conf = (HeavyWeaponAttackConfig)config;
        }
    }

    public class HeavyWeaponAttackRuntime : AbilityCoroutineRuntime
    {
        public bool animationSignaled;
        public bool inputSignaled;
        public Weapon currentWeapon;
        public AnimancerState animationState;
        public float chargeTimer;
    }
}
