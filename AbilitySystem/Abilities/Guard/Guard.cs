using UnityEngine;
using System.Collections.Generic;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    [ComponentRequired(typeof(WeaponWielder))]
    [AddAbilityMenu]
    public class Guard : WeaponAbility
    {
        private MovementController moveControl;
        private CombinedValueEffector<float> moveModifier;
        private CombinedValueEffector<float> rotateModifier;

        protected override void Init()
        {
            moveControl = WeaponWielder.Wielder.GetComponent<MovementController>();
            moveModifier = moveControl.moveSpeedModifier.MakeEffector();
            rotateModifier = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override bool WConditionSatisfied()
        {
            return WeaponWielder.GetAbilityClip(GetType(), WeaponWielder.Mainhand.WeaponType) != null && WeaponWielder.Mainhand.state == WeaponState.Idle;
        }

        protected override void OnCoroutineEnqueue()
        {
            GuardRuntime runtime = (GuardRuntime)Runtime;
            runtime.currentWeapon = WeaponWielder.Mainhand;
            runtime.animationSignaled = false;
            runtime.deflected = false;

            moveModifier.Apply(runtime.currentWeapon.GMoveSpeedModifier);
            rotateModifier.Apply(runtime.currentWeapon.GRotationSpeedModifier);
            runtime.animancerState = abilityRunner.StartAnimation(this, ConfigName, WeaponWielder.GetAbilityClip(GetType(), runtime.currentWeapon.WeaponType), runtime.currentWeapon.DefenseSpeed);

            runtime.currentWeapon.onWeaponDeflect += OnDeflect;
        }

        protected override void OnCoroutineFinish()
        {
            GuardRuntime g = (GuardRuntime)Runtime;
            g.currentWeapon.Disable();
            moveModifier.Release();
            rotateModifier.Release();
        }

        protected override IEnumerator<CoroutineOption> Coroutine()
        {
            GuardRuntime runtime = (GuardRuntime)Runtime; 
            GuardConfig config = (GuardConfig)Config;
            while(!runtime.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            runtime.animationSignaled = false;
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
            runtime.animationSignaled = false;

            // Wait for guard cancel
            while (true) {
                yield return CoroutineOption.Continue;
            }
        }

        protected override void Signal(AnimationEvent animationEvent)
        {
            GuardRuntime runtime = (GuardRuntime)Runtime;
            if (animationEvent != null)
            {
                runtime.animationSignaled = true;
            }
            else
            {
                HaltAbilityExecution(ConfigName);
            }
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        private void OnDeflect() {
            GuardRuntime runtime = (GuardRuntime)Runtime;
            runtime.deflected = true;
        }
    }

    public class GuardRuntime : AbilityCoroutineRuntime {
        public bool animationSignaled;
        public Weapon currentWeapon;
        public AnimancerState animancerState;
        public bool deflected;
        public float deflectOver;
    }

    public class GuardPipe : AbilityPipe { }
}
