using System;
using System.Collections.Generic;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Represents options available for Coroutines, use predefined values or utility methods to create the option needed
    /// </summary>
    public struct CoroutineOption : IEquatable<CoroutineOption>
    {
        public bool reset;
        public float waitTime;
        public bool unscaled;
        public IEnumerator<CoroutineOption> waitFor;
        public Func<bool> predicateCondition;
        public Coroutine wait;

        /// <summary>
        /// The coroutine option to restart the coroutine from the beginning
        /// </summary>
        public static readonly CoroutineOption Reset = new CoroutineOption { reset = true };
        /// <summary>
        /// The coroutine option to continue execution of the current coroutine
        /// </summary>
        public static readonly CoroutineOption Continue = new CoroutineOption {};

        /// <summary>
        /// Halt the execution of coroutine for the specified seconds, will be affected by Time.timeScale
        /// </summary>
        /// <param name="time">time to halt the coroutine</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForSeconds(float time)
        {
            return new CoroutineOption { waitTime = time };
        }

        /// <summary>
        /// Halt the execution of coroutine for the specified seconds, will not be affected by Time.timeScale
        /// </summary>
        /// <param name="time">time to halt the coroutine</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForUnscaledSeconds(float time) {
            return new CoroutineOption() { unscaled = true, waitTime = time };
        }

        /// <summary>
        /// Start a subcoroutine, current coroutine will not be executed until subcoroutine has finished
        /// </summary>
        /// <param name="coroutine">The subcoroutine to be executed</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForCoroutine(IEnumerator<CoroutineOption> coroutine) {
            CoroutineOption option = new() { waitFor = coroutine};
            return option;
        }

        /// <summary>
        /// Halt the execution of coroutine until another coroutine has finished, cannot be used to wait for the current coroutine itself
        /// </summary>
        /// <param name="coroutine">The coroutine to wait for, cannot be the current coroutine itself</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForCoroutine(Coroutine coroutine)
        {
            CoroutineOption option = new() { wait = coroutine};
            return option;
        }

        /// <summary>
        /// Halt the execution of coroutine until the condition has been met
        /// </summary>
        /// <param name="predicate">The condition to test for</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForCondition(Func<bool> predicate)
        {
            CoroutineOption option = new(){ predicateCondition = predicate };
            return option;
        }

        public bool Equals(CoroutineOption other)
        {
            return wait == other.wait && reset == other.reset && predicateCondition == other.predicateCondition && 
                waitFor == other.waitFor && waitTime == other.waitTime && unscaled == other.unscaled;
        }
    }
}
