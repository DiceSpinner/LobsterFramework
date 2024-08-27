using System.Collections;
using System.Collections.Generic;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Abilities that must be continously runned across multiple frames
    /// </summary>
    public abstract class AbilityCoroutine : Ability
    {
        protected sealed override void OnAbilityEnqueue()
        {
            AbilityCoroutineContext context = (AbilityCoroutineContext)Context;
            context.coroutine = context.coroutineRunner.AddCoroutine(Coroutine());
            context.coroutine.OnReset += OnCoroutineReset;
            OnCoroutineEnqueue();
        }

        /// <summary>
        /// Called when the ability is enqueued, replaces <see cref="OnAbilityEnqueue"/>
        /// </summary>
        protected abstract void OnCoroutineEnqueue();

        protected sealed override void OnAbilityFinish() {
            AbilityCoroutineContext context = (AbilityCoroutineContext)Context;
            context.coroutine.Stop();
            context.coroutine.OnReset -= OnCoroutineReset;
            OnCoroutineFinish();
        }

        /// <summary>
        /// Called when the ability is finished, replaces <see cref="OnAbilityFinish"/>
        /// </summary>
        protected virtual void OnCoroutineFinish() { }

        protected override sealed bool Action()
        {
            AbilityCoroutineContext context = (AbilityCoroutineContext)Context;
            context.coroutineRunner.Run();
            return !context.coroutine.IsFinished;
        }

        /// <summary>
        /// Calleback when <see cref="CoroutineOption.Reset"/> is yielded by <see cref="Coroutine"/>
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
        internal CoroutineRunner coroutineRunner = new();
        internal Coroutine coroutine;
    }
}
