using LobsterFramework.Utility;
using System;
using UnityEngine;
using System.Collections.Generic;
using LobsterFramework.AbilitySystem;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AI
{
    /// <summary>
    /// Represents the state information used by the state machine.
    /// </summary>
    [CreateAssetMenu(menuName = "StateMachine/StateData")]
    public class StateData : ReferenceRequester
    {
        [SerializeField] internal StateDicationary states = new();
        [SerializeField] internal State initialState;

#if UNITY_EDITOR
        /// <summary>
        /// Add the state of specified type, do nothing if the state already exists. This method should only be called by editor scripts.
        /// </summary>
        internal void AddState(Type stateType){
            if (states.ContainsKey(stateType.AssemblyQualifiedName) || !stateType.IsSubclassOf(typeof(State))) {
                return;
            }
            State state = (State)CreateInstance(stateType);
            states[stateType.AssemblyQualifiedName] = state;
            state.name = stateType.FullName;
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.AddObjectToAsset(state, this);
            }
        }

        /// <summary>
        /// Remove the state of specified type, do nothing if the state is not present. This method should only be called by editor scripts.
        /// </summary>
        /// <typeparam name="T">The type of the state to remove</typeparam>
        internal void RemoveState(Type stateType)
        {
            if (!states.ContainsKey(stateType.AssemblyQualifiedName))
            {
                Debug.LogWarning("The state you are trying to remove does not exist!");
                return;
            }
            DestroyImmediate(states[stateType.AssemblyQualifiedName], true);
            states.Remove(stateType.AssemblyQualifiedName);
        }

        /// <summary>
        /// Save this state data to the asset database
        /// </summary>
        public void SaveAsAsset(string path)
        {
            AssetDatabase.CreateAsset(this, path);
            foreach (State state in states.Values)
            {
                AssetDatabase.AddObjectToAsset(state, this);
            }
        }
#endif

        /// <summary>
        /// Produce a copy of this state data
        /// </summary>
        /// <returns>The copied state data that contains a copy of all states</returns>
        public StateData Clone() {
            StateData copied = Instantiate(this);
            foreach (var kwp in states) {
                copied.states[kwp.Key] = Instantiate(kwp.Value);
            }
            copied.initialState = copied.states[initialState.GetType().AssemblyQualifiedName];
            copied.name = this.name;
            return copied;
        }

        /// <summary>
        /// Check if each state has its transitions defined and the initial state is defined
        /// </summary>
        /// <returns></returns>
        internal bool Validate() {
            if (initialState == null) {
                return false;
            }
            foreach (State state in states.Values) {
                Type stateType = state.GetType();
                if (!StateTransitionAttribute.transitionTable.ContainsKey(stateType)) {
                    continue;
                }
                foreach (Type transition in StateTransitionAttribute.transitionTable[stateType]) {
                    if (!states.ContainsKey(transition.AssemblyQualifiedName)) {
                        return false;
                    }
                }
            }
            return true;
        }

        internal void Initialize(StateMachine machine) {
            foreach (State state in states.Values) {
                state.stateMachine = machine;
                state.InitializeFields();
            }
        }

        internal void Close() {
            foreach (State state in states.Values)
            {
                state.Close();
            }
        }

        public override IEnumerator<Type> GetRequests()
        {
            foreach (State state in states.Values) {
                Type type = state.GetType();
                if (RequireComponentReferenceAttribute.Requirement.ContainsKey(type))
                {
                    yield return type;
                }
            }
        }
    }

    [Serializable]
    public class StateDicationary : SerializableDictionary<string, State> { }

}
