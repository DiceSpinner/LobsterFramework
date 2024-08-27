using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework
{
    /// <summary>
    /// Inherited by data containers to request references to monobehaviors at runtime. The monobehaviors that operate on these data containers should inherit from <see cref="ReferenceProvider"/> to be able to provide these references.
    /// </summary>
    public abstract class ReferenceRequester : ScriptableObject, IEnumerable<Type>
    {
        /// <summary>
        /// This event is raised when an instance of type with <see cref="RequireComponentReferenceAttribute"/> applied is added from the data container.
        /// </summary>
        internal event Action<Type> OnRequirementAdded;

        /// <summary>
        /// This event is raised when an instance of type with <see cref="RequireComponentReferenceAttribute"/> applied is removed from the data container.
        /// </summary>
        internal event Action<Type> OnRequirementRemoved;

        protected void RaiseRequirementAddedEvent(Type type) {
            if (RequireComponentReferenceAttribute.Requirement.ContainsKey(type)) 
            {
                OnRequirementAdded?.Invoke(type);
            }
        }
        protected void RaiseRequirementRemovedEvent(Type type) {
            if (RequireComponentReferenceAttribute.Requirement.ContainsKey(type)) { 
                OnRequirementRemoved?.Invoke(type);
            }
        }

        /// <summary>
        /// Implement this to expose the set of types with <see cref="RequireComponentReferenceAttribute"/> applied within the data container,
        /// </summary>
        /// <returns>The set of requester types this data container has</returns>
        public abstract IEnumerator<Type> GetRequestingTypes();

        IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
        {
            return GetRequestingTypes();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetRequestingTypes();
        }
    }
}
