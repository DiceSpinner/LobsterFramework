using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    [AddAbilityMenu]
    public class TestCoroutine : AbilityCoroutine
    {
        protected class TestCoroutineConfig : AbilityConfig { }

        protected override IEnumerable<CoroutineOption> Coroutine()
        {
            Debug.Log("1: " + Time.time);
            yield return CoroutineOption.Continue;

            Debug.Log("2: " + Time.time);
            yield return CoroutineOption.Continue;

            Debug.Log("3: " + Time.time);
            yield return CoroutineOption.Continue;

            Debug.Log("4: " + Time.time);
            yield return CoroutineOption.Continue;

            Debug.Log("5: " + Time.time);
        }

        protected override void OnCoroutineEnqueue()
        {
            Debug.Log("Coroutining!");
        }

        protected override void OnCoroutineFinish()
        {
            Debug.Log("Coroutined!");
        }

        protected override void OnCoroutineReset()
        {
            throw new System.NotImplementedException();
        }
    }
}
