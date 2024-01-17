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
        public void Reset()
        {
            OnClose();
            Initialize();
        }

        /// <summary>
        /// Initialize the component
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Callback before disabled
        /// </summary>
        public virtual void OnClose() { }

        /// <summary>
        /// Callback to update internal state on each frame during the regular unity update cycle
        /// </summary>
        public virtual void Update() { }
    }
}
