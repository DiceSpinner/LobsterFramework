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
            AbilityCoroutineContext context = (AbilityCoroutineContext)Context;
            context.coroutine = context.coroutineRunner.AddCoroutine(Coroutine());
            context.coroutine.onReset += OnCoroutineReset;
            OnCoroutineEnqueue();
        }

        /// <summary>
        /// Callback when the ability is enqueued, replaces OnEnqueue
        /// </summary>
        protected abstract void OnCoroutineEnqueue();

        protected sealed override void OnAbilityFinish() {
            AbilityCoroutineContext context = (AbilityCoroutineContext)Context;
            context.coroutine.Stop();
            OnCoroutineFinish();
        }

        protected virtual void OnCoroutineFinish() { }

        protected override sealed bool Action()
        {
            AbilityCoroutineContext context = (AbilityCoroutineContext)Context;
            context.coroutineRunner.Run();
            return !context.coroutine.IsFinished;
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
        protected abstract IEnumerable<CoroutineOption> Coroutine();
    }

    /// <summary>
    /// AbilityConfig of coroutines, subclass configs need to inherit from this to properly work
    /// </summary>
    public class AbilityCoroutineContext : AbilityContext
    {
        public CoroutineRunner coroutineRunner = new();
        public Utility.Coroutine coroutine;
    }
}
