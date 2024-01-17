using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;

namespace LobsterFramework.Effects
{
    [CreateAssetMenu(menuName = "Effect/Stun")]
    public class StunEffect : Effect
    {
        private CombinedValueEffector<bool> abilityLock;
        private CombinedValueEffector<bool> moveLock;
        private AbilityRunner abilityRunner;
        private MovementController moveControl;

        protected override void OnApply()
        {
            moveControl = processor.GetComponentInBoth<MovementController>(); 
            if (moveControl != null)
            {
                moveLock = moveControl.movementLock.MakeEffector();
                moveLock.Apply(true);
            }
            
            abilityRunner = processor.GetComponentInBoth<AbilityRunner>();
            if (abilityRunner != null) {
                abilityLock = abilityRunner.actionLock.MakeEffector();
                abilityLock.Apply(true);
            }
        }

        protected override void OnEffectOver()
        {
            if (moveControl != null)
            {
                moveLock.Release();
            }

            if (abilityRunner != null)
            {
                abilityLock.Release();
            }
        }
    }
}
