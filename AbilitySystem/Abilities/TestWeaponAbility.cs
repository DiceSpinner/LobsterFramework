using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    [WeaponAnimation(typeof(TestAnimationEntries))]
    [AddWeaponArtMenu(false, WeaponType.EmptyHand, WeaponType.Firearm)]
    public class TestWeaponAbility : WeaponAbility
    {
        protected class TestWeaponAbilityConfig : AbilityConfig { 
        }

        public class TestWeaponAbilityPipe : AbilityPipe { }

        protected override IEnumerator<CoroutineOption> Coroutine()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnCoroutineEnqueue()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnCoroutineFinish()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }
    }

    public enum TestAnimationEntries { 
        Entry1,
        Entry2, Entry3
    }
}
