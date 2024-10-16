using UnityEngine;
using System.Collections.Generic;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [AddAbilityMenu(Constants.Framework)]
    [WeaponArt(BlackList = true)]
    [WeaponAnimation(typeof(GuardAnimations))]
    [RequireComponentReference(typeof(MovementController))]
    public sealed class Guard : WeaponAbility
    {
        private MovementController moveControl;
        private CombinedValueEffector<float> moveModifier;
        private CombinedValueEffector<float> rotateModifier;
        private bool leftDeflect = false;

        protected override void InitWeaponAbilityReferences()
        {
            moveControl = WeaponManager.Wielder.GetComponent<MovementController>();
            moveModifier = moveControl.moveSpeedModifier.MakeEffector();
            rotateModifier = moveControl.rotateSpeedModifier.MakeEffector();
        }

        protected override bool WeaponAbilityReady()
        {
            return WeaponManager.Mainhand.State == WeaponState.Idle;
        }

        protected override void OnWeaponAbilityEnqueue()
        {
            GuardContext context = (GuardContext)Context;
            context.currentWeapon = WeaponManager.Mainhand;
            context.deflected = false;

            context.animationSignaled.Reset();

            // Start animation
            AnimationClip deflectAnimation;
            if (leftDeflect)
            { 
                deflectAnimation = WeaponManager.AnimationData.GetAbilityClip<Guard>(context.currentWeapon.WeaponType, (int)GuardAnimations.DeflectLeft);
            }
            else {
                deflectAnimation = WeaponManager.AnimationData.GetAbilityClip<Guard>(context.currentWeapon.WeaponType, (int)GuardAnimations.DeflectRight);
            }
            leftDeflect = !leftDeflect;
            context.animancerState = AbilityManager.StartAnimation(this, Instance, deflectAnimation, context.currentWeapon.DefenseSpeed);

            // Movement constraints
            context.currentWeapon.OnWeaponDeflect += OnDeflect;
            moveModifier.Apply(context.currentWeapon.GMoveSpeedModifier);
            rotateModifier.Apply(context.currentWeapon.GRotationSpeedModifier);
        }

        protected override void OnWeaponAbilityFinish()
        {
            GuardContext context = (GuardContext)Context;
            context.currentWeapon.Disable();
            moveModifier.Release();
            rotateModifier.Release();
        }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            GuardContext context = (GuardContext)Context; 
            GuardConfig config = (GuardConfig)Config;
            while(!context.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            context.animancerState.IsPlaying = false;
            context.currentWeapon.Enable(WeaponState.Deflecting);
            context.deflectOver = config.DelfectTime + Time.time;

            float currentClipTime = context.animancerState.Time;

            // Wait for deflect, if deflect period has passed then wait for Guard cancel
            while (!context.deflected)
            {
                if (context.currentWeapon.State == WeaponState.Deflecting && Time.time >= context.deflectOver)
                {
                    context.currentWeapon.State = WeaponState.Guarding;
                }
                yield return CoroutineOption.Continue;
            }

            // Deflect
            context.animancerState.IsPlaying = true;
            // Wait for deflect animation end
            while (!context.animationSignaled)
            {
                yield return CoroutineOption.Continue;
            }
            context.currentWeapon.State = WeaponState.Guarding;
            context.animancerState.IsPlaying = false;
            context.animancerState.Time = currentClipTime;

            // Wait for guard cancel
            while (true) {
                yield return CoroutineOption.Continue;
            }
        }

        protected override void OnSignaled(AnimationEvent animationEvent)
        {
            GuardContext context = (GuardContext)Context;
            context.animationSignaled.Put(true);
        }

        protected override void OnSignaled()
        {
            SuspendInstance(Instance);
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }

        private void OnDeflect() {
            GuardContext context = (GuardContext)Context;
            context.deflected = true;
        }

        public enum GuardAnimations : int { 
            DeflectLeft,
            DeflectRight
        }
    }

    public class GuardContext : AbilityCoroutineContext {
        public Signal<bool> animationSignaled = new();
        public Weapon currentWeapon;
        public AnimancerState animancerState;
        public bool deflected;
        public float deflectOver;
    }

    public class GuardChannel : AbilityChannel { }
}
