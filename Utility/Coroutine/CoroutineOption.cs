using System;
using System.Collections.Generic;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Represents options available for Coroutines, use predefined values or utility methods to create the option needed
    /// </summary>
    public struct CoroutineOption
    {
        internal bool reset;
        internal float waitTime;
        internal bool isUnscaledTime;
        internal IEnumerable<CoroutineOption> subCoroutine;
        internal Func<bool> predicateCondition;
        internal Coroutine coroutineToWait;

        /// <summary>
        /// The coroutine option to restart the coroutine execution from the beginning
        /// </summary>
        public static readonly CoroutineOption Reset = new CoroutineOption { reset = true };
        /// <summary>
        /// The coroutine option to continue execution of the current coroutine
        /// </summary>
        public static readonly CoroutineOption Continue = new CoroutineOption {};

        /// <summary>
        /// Pause the execution of coroutine for the specified seconds, will be affected by Time.timeScale
        /// </summary>
        /// <param name="time">time to pause the coroutine</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForSeconds(float time)
        {
            return new CoroutineOption { waitTime = time };
        }

        /// <summary>
        /// Pause the execution of coroutine for the specified seconds, will not be affected by Time.timeScale
        /// </summary>
        /// <param name="time">time to pause the coroutine</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForUnscaledSeconds(float time) {
            return new CoroutineOption() { isUnscaledTime = true, waitTime = time };
        }

        /// <summary>
        /// Start a subcoroutine, current coroutine will not be executed until subcoroutine has finished
        /// </summary>
        /// <param name="subCoroutine">The subcoroutine to be executed</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption SubCoroutine(IEnumerable<CoroutineOption> subCoroutine) {
            CoroutineOption option = new() { subCoroutine = subCoroutine};
            return option;
        }

        /// <summary>
        /// Pause the execution of coroutine until another coroutine has finished, cannot be used to wait for the current coroutine itself
        /// </summary>
        /// <param name="coroutine">The coroutine to wait for, cannot be the current executing coroutine itself</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption WaitForCoroutine(Coroutine coroutine)
        {
            CoroutineOption option = new() { coroutineToWait = coroutine};
            return option;
        }

        /// <summary>
        /// Pause the execution of coroutine until the condition has been met
        /// </summary>
        /// <param name="predicate">The condition to test for</param>
        /// <returns>The CoroutineOption represents this option</returns>
        public static CoroutineOption Condition(Func<bool> predicate)
        {
            CoroutineOption option = new(){ predicateCondition = predicate };
            return option;
        }
    }
}
