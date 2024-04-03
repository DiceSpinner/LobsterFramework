using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Components that can be attached to ability data objects to run additional logic and host shared data across all abilities
    /// </summary>
    public abstract class AbilityComponent : ScriptableObject
    {
        /// <summary>
        /// Initialize the component
        /// </summary>
        internal protected virtual void Initialize() { }

        /// <summary>
        /// Callback before disabled
        /// </summary>
        internal protected virtual void OnClose() { }

        /// <summary>
        /// Callback to update internal state on each frame during the regular unity update cycle
        /// </summary>
        internal protected virtual void Update() { }

        internal void Reset()
        {
            OnClose();
            Initialize();
        }
    }
}
