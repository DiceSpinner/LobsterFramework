using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

namespace LobsterFramework.AI
{
    /// <summary>
    /// Marks the transitions of this state. The state data will be verified against the list of transition states provided here.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class StateTransitionAttribute : Attribute
    {
        internal static Dictionary<Type, HashSet<Type>> transitionTable = new();

        private Type[] transitions;

        public StateTransitionAttribute( params Type[] transitions) {
            this.transitions = transitions;
        }

        internal void Init(Type state) {
            if (!state.IsSubclassOf(typeof(State))) {
                Debug.LogError($"Attribute Application Error: {state.Name} is not valid state!" );
                return;
            }

            transitionTable[state] = new();
            foreach (Type t in transitions) {
                if (!t.IsSubclassOf(typeof(State))) {
                    Debug.LogError($"Cannot map transition for ${state.Name}, {t.Name} is not valid state!");
                    continue;
                }
                transitionTable[state].Add(t);
            }
        }
    }
}
