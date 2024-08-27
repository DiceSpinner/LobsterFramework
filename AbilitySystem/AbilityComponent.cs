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
        /// Initialize the component, called when the <see cref="AbilityData"/> becomes active
        /// </summary>
        internal protected virtual void Initialize() { }

        /// <summary>
        /// Called before the <see cref="AbilityData"/> becomes inactive
        /// </summary>
        internal protected virtual void OnBecomeInactive() { }

        internal void Reset()
        {
            OnBecomeInactive();
            Initialize();
        }
    }
}
