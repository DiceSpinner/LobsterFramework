using LobsterFramework.Utility;
using System;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityComponentMenu("LobsterFramework")]
    public sealed class DamageModifier : AbilityComponent
    {
        [NonSerialized] public readonly FloatSum flatDamageModification = new(0, false, true);
        [NonSerialized] public readonly FloatProduct percentageDamageModifcation = new(1, true);

        public Damage ModifyDamage(Damage damage) {
            damage.health *= percentageDamageModifcation.Value;
            damage.posture *= percentageDamageModifcation.Value;

            damage.health += flatDamageModification.Value;
            damage.posture += flatDamageModification.Value;

            return damage;
        }
    }
}
