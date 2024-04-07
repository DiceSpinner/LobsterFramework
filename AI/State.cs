using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AI
{
    /// <summary>
    /// A state that can be runned in the StateMachine. Can be added to and edited inside StateData.
    /// </summary>
    public abstract class State : ScriptableObject
    {
        [HideInInspector]
        internal protected AIController controller;
        [HideInInspector]
        internal protected StateMachine stateMachine;

        /// <summary>
        /// Callback to do context environment initialization
        /// </summary>
        /// <param name="obj">The gameobject where StateMachine is attached to.</param>
        internal protected abstract void InitializeFields(GameObject obj);

        /// <summary>
        /// Callback to clean up context environment
        /// </summary>
        internal protected abstract void Close();

        /// <summary>
        /// Callback when the state is exiting
        /// </summary>
        internal protected abstract void OnExit();

        /// <summary>
        /// Callback when entering the state
        /// </summary>
        internal protected abstract void OnEnter();

        /// <summary>
        /// Main body of the state logic, will be called every frame during Update event.
        /// </summary>
        /// <returns>The type of the state to transition to, null if not transitioning to any state.</returns>
        internal protected abstract System.Type Tick();
    }
}
