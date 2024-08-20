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
        internal protected StateMachine stateMachine;

        /// <summary>
        /// Attempts to get the reference of the specified component type from <see cref="StateMachine"/>. 
        /// The type of the reference should be one of the required types applied via <see cref="RequireComponentReferenceAttribute"/> on this state class.
        /// </summary>
        /// <typeparam name="T">The type of the component looking for</typeparam>
        /// <param name="index">The index to the list of components of the type specified. Use of type safe enum is strongly recommended.</param>
        /// <returns>The component reference stored in <see cref="StateMachine"/> if it exists, otherwise null</returns>
        /// <remarks>This is a shorthand call for <see cref="ReferenceProvider.GetComponentReference{T}(Type, int)"/> via <see cref="stateMachine"/></remarks>
        protected T GetComponentReference<T>(int index=0) where T : Component {
            return stateMachine.GetComponentReference<T>(GetType(), index);
        }

        /// <summary>
        /// Callback to do context environment initialization
        /// </summary>
        internal protected abstract void InitializeFields();

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
