using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AI
{
    /// <summary>
    /// A state that can be runned in the <see cref="StateMachine"/>. Can be added to and edited inside <see cref="StateData"/>. <br/>
    /// 
    /// Use <see cref="AddStateMenuAttribute"/> and <see cref="StateTransitionAttribute"/> to define transitions for the state and make it visible to the editor scritps to allow editing in the custom inspector of <see cref="StateData"/>
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
        /// <remarks>This is a shorthand call for <see cref="ReferenceProvider.GetComponentReference{T}(System.Type, int)"/> via <see cref="stateMachine"/></remarks>
        protected T GetComponentReference<T>(int index=0) where T : Component {
            return stateMachine.GetComponentReference<T>(GetType(), index);
        }

        /// <summary>
        /// Called to initialize references and perform setups
        /// </summary>
        internal protected abstract void InitializeFields();

        /// <summary>
        /// Called to perform clean up operations
        /// </summary>
        internal protected abstract void OnBecomeInactive();

        /// <summary>
        /// Called when the exiting the state
        /// </summary>
        internal protected abstract void OnExit();

        /// <summary>
        /// Called when entering the state
        /// </summary>
        internal protected abstract void OnEnter();

        /// <summary>
        /// Main body of the state logic, will be called every frame during Update event.
        /// </summary>
        /// <returns>The type of the state to transition to, null if not transitioning to any state.</returns>
        internal protected abstract System.Type Tick();
    }
}
