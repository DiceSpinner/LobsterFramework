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
        private bool useUnsacaledWaitTime; // Whether to use unscaled time (ignore Time.scale effect)
        private float awakeTime; // The time this coroutine should continue executing
        public event Action OnReset; // Called when this coroutine is reset

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
            if (useUnsacaledWaitTime)
            {
                if (Time.unscaledTime < awakeTime) {
                    return true;
                }
            }
            else if(Time.time < awakeTime)
            {
                return true;
            }

            // Wait for the other coroutine to finish
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

            // Check for wait condition function
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

            // Stop ability if the coroutine has reached its end
            if (!next)
            {
                IsFinished = true;
                enumerator.Dispose();
                return false;
            }

            // Handle coroutine options returned by the coroutine

            // Reset the coroutine
            if (option.reset)
            {
                enumerator.Dispose();
                enumerator = coroutine.GetEnumerator();
                OnReset?.Invoke();
                return true;
            }

            // Wait for seconds
            if (option.waitTime > 0)
            {
                useUnsacaledWaitTime = option.isUnscaledTime;
                if (useUnsacaledWaitTime)
                {
                    awakeTime = Time.unscaledTime + option.waitTime;
                    return true;
                }
                awakeTime = Time.time + option.waitTime;
                return true;
            }

            // Start a new coroutine and wait for it to finish
            if (option.subCoroutine != null) {
                waitFor = runner.AddCoroutine(option.subCoroutine);
                return true;
            }

            // Wait for existing coroutine
            if (option.coroutineToWait != null) { // Cannot wait for self
                if (option.coroutineToWait == this) {
                    Debug.LogException(new Exception("The coroutine cannot wait for itself!"));
                    enumerator.Dispose();
                    return false;
                }
                if (option.coroutineToWait.waitFor == this) {
                    Debug.LogException(new Exception("Dead lock detected!"));
                    enumerator.Dispose();
                    return false;
                }
                waitFor = option.coroutineToWait;
                return true;
            } 

            // Set up predicate handler and wait for the condition to be satisfied
            if (option.predicateCondition != null) {
                conditionSatisfied = option.predicateCondition;
                return true;
            }
            return true;
        }

        /// <summary>
        /// Mark coroutine as stopped, it will not be executed further
        /// </summary>
        public void Stop() {
            enumerator.Dispose();
            IsFinished = true;
        }
    }
}
