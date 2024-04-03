using UnityEngine;
using System.Collections.Generic;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    [WeaponAnimation(typeof(GuardAnimations))]
    [ComponentRequired(typeof(WeaponManager))]
    public class Guard : WeaponAbility
    {
        private MovementController moveControl;
        private CombinedValueEffector<float> moveModifier;
        private CombinedValueEffector<float> rotateModifier;
        private bool leftDeflect = false;

        protected override void Init()
        {
            moveControl = WeaponManager.Wielder.GetComponent<MovementController>();
            moveModifier = moveControl.moveSpeedModifier.MakeEffector();
            rotateModifier = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override bool WConditionSatisfied()
        {
            return WeaponManager.Mainhand.state == WeaponState.Idle;
        }

        protected override void OnCoroutineEnqueue()
        {
            GuardRuntime runtime = (GuardRuntime)Runtime;
            runtime.currentWeapon = WeaponManager.Mainhand;
            runtime.deflected = false;

            // Start animation
            AnimationClip deflectAnimation;
            if (leftDeflect)
            {
                deflectAnimation = WeaponManager.AnimationData.GetAbilityClip(runtime.currentWeapon.WeaponType, GetType(), (int)GuardAnimations.DeflectLeft);
            }
            else {
                deflectAnimation = WeaponManager.AnimationData.GetAbilityClip(runtime.currentWeapon.WeaponType, GetType(), (int)GuardAnimations.DeflectRight);
            }
            leftDeflect = !leftDeflect;
            runtime.animancerState = abilityManager.StartAnimation(this, ConfigName, deflectAnimation, runtime.currentWeapon.DefenseSpeed);

            // Movement constraints
            runtime.currentWeapon.onWeaponDeflect += OnDeflect;
            moveModifier.Apply(runtime.currentWeapon.GMoveSpeedModifier);
            rotateModifier.Apply(runtime.currentWeapon.GRotationSpeedModifier);
        }

        protected override void OnCoroutineFinish()
        {
            GuardRuntime g = (GuardRuntime)Runtime;
            g.animationSignaled.Reset();
            g.currentWeapon.Disable();
            moveModifier.Release();
            rotateModifier.Release();
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            GuardRuntime runtime = (GuardRuntime)Runtime; 
            GuardConfig config = (GuardConfig)Config;
            while(!runtime.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            runtime.animancerState.IsPlaying = false;
            runtime.currentWeapon.Enable(WeaponState.Deflecting);
            runtime.deflectOver = config.DelfectTime + Time.time;

            float currentClipTime = runtime.animancerState.Time;

            // Wait for deflect, if deflect period has passed then wait for Guard cancel
            while (!runtime.deflected)
            {
                if (runtime.currentWeapon.state == WeaponState.Deflecting && Time.time >= runtime.deflectOver)
                {
                    runtime.currentWeapon.state = WeaponState.Guarding;
                }
                yield return CoroutineOption.Continue;
            }

            // Deflect
            runtime.animancerState.IsPlaying = true;
            // Wait for deflect animation end
            while (!runtime.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            runtime.currentWeapon.state = WeaponState.Guarding;
            runtime.animancerState.IsPlaying = false;
            runtime.animancerState.Time = currentClipTime;

            // Wait for guard cancel
            while (true) {
                yield return CoroutineOption.Continue;
            }
        }

        protected override void OnSignaled(AnimationEvent animationEvent)
        {
            GuardRuntime runtime = (GuardRuntime)Runtime;
            runtime.animationSignaled.Put(true);
        }

        protected override void OnSignaled()
        {
            SuspendInstance(ConfigName);
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        private void OnDeflect() {
            GuardRuntime runtime = (GuardRuntime)Runtime;
            runtime.deflected = true;
        }

        public enum GuardAnimations : int { 
            DeflectLeft,
            DeflectRight
        }
    }

    public class GuardRuntime : AbilityCoroutineRuntime {
        public Signal<bool> animationSignaled = new();
        public Weapon currentWeapon;
        public AnimancerState animancerState;
        public bool deflected;
        public float deflectOver;
    }

    public class GuardChannel : AbilityChannel { }
}
