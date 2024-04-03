using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Represents the state of Coroutine, can be used to query if the coroutine has finished.
    /// </summary>
    public class Coroutine
    {
        private CoroutineRunner runner;
        private IEnumerable<CoroutineOption> coroutine;
        private IEnumerator<CoroutineOption> enumerator;
        private Coroutine waitFor; // The child coroutine to wait for
        private Func<bool> conditionSatisfied;
        private bool unscaled; // Whether to use unscaled time
        private float awakeTime; // The time this coroutine should continue executing
        public Action onReset;

        /// <summary>
        /// True if the coroutine is finished, false otherwise
        /// </summary>
        public bool IsFinished { get; private set; }

        public Coroutine(CoroutineRunner runner, IEnumerable<CoroutineOption> coroutine) { 
            this.coroutine = coroutine;
            enumerator = coroutine.GetEnumerator();
            IsFinished = false;
            this.runner = runner;
        }

        /// <summary>
        /// Execute Coroutine until it yields
        /// </summary>
        /// <returns> False if coroutine has finished executing, otherwise true </returns>
        internal bool Advance() {
            if (IsFinished) { return false; }

            // Check for wait time
            if (unscaled)
            {
                if (Time.unscaledTime < awakeTime) {
                    return true;
                }
            }
            else if(Time.time < awakeTime)
            {
                return true;
            }

            // Check for wait coroutines
            if (waitFor != null)
            {
                if (!waitFor.IsFinished)
                {
                    return true;
                }
                else { 
                    waitFor = null;
                }
            }

            // Check for wait Condition
            if (conditionSatisfied != null) {
                if (!conditionSatisfied.Invoke())
                {
                    return true;
                }
                else { 
                    conditionSatisfied = null;
                }
            }

            bool next = enumerator.MoveNext();
            CoroutineOption option = enumerator.Current;
            if (!next)
            {
                IsFinished = true;
                return false;
            }
            if (!option.Equals(CoroutineOption.Continue))
            {
                if (option.reset)
                {
                    enumerator = coroutine.GetEnumerator();
                    onReset?.Invoke();
                    return true;
                }

                // Wait for coroutines or condition
                if (option.waitTime > 0)
                {
                    awakeTime = Time.time + option.waitTime;
                    unscaled = option.unscaled;
                }
                else if (option.waitFor != null) {
                    waitFor = runner.AddCoroutine(option.waitFor);
                } 
                else if (option.wait != null && option.wait != this) { // Cannot wait for self
                    waitFor = option.wait;
                } 
                else if (option.predicateCondition != null) {
                    conditionSatisfied = option.predicateCondition;
                }
            }
            return true;
        }

        /// <summary>
        /// Mark coroutine as stopped, it will not be executed further
        /// </summary>
        public void Stop() {
            IsFinished = true;
        }
    }
}
