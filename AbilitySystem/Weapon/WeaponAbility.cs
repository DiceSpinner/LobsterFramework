using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    public abstract class WeaponAbility : AbilityCoroutine
    {
        protected WeaponWielder WeaponWielder { get; private set; }
        protected bool IsMainhanded { get;private set; }

        protected sealed override void Initialize()
        {
            WeaponWielder = abilityRunner.GetComponentInBoth<WeaponWielder>();
            IsMainhanded = !OffhandWeaponAbilityAttribute.IsOffhand(GetType());
            Init();
        }

        protected virtual void Init() { }

        protected sealed override bool ConditionSatisfied()
        {
            Weapon query;
            if (IsMainhanded) {
                query = WeaponWielder.Mainhand;
            }
            else {
                query = WeaponWielder.Offhand;
                if (WeaponWielder.Mainhand != null && WeaponWielder.Mainhand.state != WeaponState.Idle) {
                    return false;
                }
            }
            return query != null && RunningCount == 0 && RequireWeaponStatAttribute.HasWeaponStats(GetType(), WeaponWielder) && WConditionSatisfied();
        }

        protected virtual bool WConditionSatisfied() { return true; } 
    }
}
