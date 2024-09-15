using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;
using LobsterFramework.Init;

namespace LobsterFramework.AI
{
    /// <summary>
    /// Marks the transitions of this state. The state data will be verified against the list of transition states provided here.
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Dual)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class StateTransitionAttribute : InitializationAttribute
    {
        internal static Dictionary<Type, HashSet<Type>> transitionTable = new();

        private Type[] transitions;

        public StateTransitionAttribute( params Type[] transitions) {
            this.transitions = transitions;
        }

        public static bool IsCompatible(Type state)
        {
            return state.IsSubclassOf(typeof(State));
        }

        internal protected override void Init(Type state) {
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
