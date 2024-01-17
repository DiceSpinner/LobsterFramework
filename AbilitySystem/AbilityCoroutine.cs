using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using UnityEngine;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Abilities that must be continously runned across multiple frames
    /// </summary>
    public abstract class AbilityCoroutine : Ability
    {
        protected sealed override void OnEnqueue()
        {
            AbilityCoroutineRuntime runtime = (AbilityCoroutineRuntime)Runtime;
            runtime.coroutine = runtime.coroutineRunner.AddCoroutine(Coroutine());
            runtime.coroutine.onReset += OnCoroutineReset;
            OnCoroutineEnqueue();
        }

        /// <summary>
        /// Callback when the ability is enqueued, replaces OnEnqueue
        /// </summary>
        protected abstract void OnCoroutineEnqueue();

        protected sealed override void OnActionFinish() {
            AbilityCoroutineRuntime runtime = (AbilityCoroutineRuntime)Runtime;
            runtime.coroutine.Stop();
            OnCoroutineFinish();
        }

        protected virtual void OnCoroutineFinish() { }

        protected override sealed bool Action()
        {
            AbilityCoroutineRuntime runtime = (AbilityCoroutineRuntime)Runtime;
            runtime.coroutineRunner.Run();
            return !runtime.coroutine.IsFinished;
        }

        /// <summary>
        /// Calleback for when the coroutine is reset to the start position
        /// </summary>
        protected abstract void OnCoroutineReset();

        /// <summary>
        /// The body of the ability execution, replaces Action method
        /// </summary>
        /// <param name="config">The ability configuration to execute on</param>
        /// <returns></returns>
        protected abstract IEnumerator<CoroutineOption> Coroutine();
    }

    /// <summary>
    /// AbilityConfig of coroutines, subclass configs need to inherit from this to properly work
    /// </summary>
    public class AbilityCoroutineRuntime : AbilityRuntime
    {
        public CoroutineRunner coroutineRunner = new();
        public Utility.Coroutine coroutine;
    }
}
