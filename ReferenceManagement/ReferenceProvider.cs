using System;
using UnityEngine;
using LobsterFramework.Utility;
using System.Collections.Generic;
using System.Linq;
using TypeCache = LobsterFramework.Utility.TypeCache;

namespace LobsterFramework
{
    /// <summary>
    /// Provides references to <see cref="ReferenceRequester"/> data containers.
    /// </summary>
    public abstract class ReferenceProvider : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField] internal ReferenceMap referenceMapping = new();
        [ReadOnly]
        [SerializeField] private ReferenceRequester bindedRequester;

        /**
         * Unity Events here are marked as protected to grant child class access.
         */
        #region Unity Events
        protected void OnValidate()
        {
            Bind(bindedRequester);
        }
        protected void OnDestroy()
        {
            UnBind();
        }
        #endregion

        /// <summary>
        /// Bind the data container with the reference provider to update the set of fields required to store the references required by the data container.
        /// The required references of any previous binded data container different from the currently binded one will be lost.
        /// This component will listen to changes made to the data container and react accordingly.
        /// This method should be called in the "OnValidate" message to ensure the listeners are properly hooked up
        /// </summary>
        /// <param name="referenceRequester">The data container requesting component references to be binded with</param>
        protected void Bind(ReferenceRequester referenceRequester) {
            if (referenceRequester == null) { 
                UnBind();
                return;
            }
            if (bindedRequester != referenceRequester)
            { // Check if requirement has changed after recompilation
                if (bindedRequester != null)
                {
                    bindedRequester.OnRequirementAdded -= OnRequirementAdded;
                    bindedRequester.OnRequirementRemoved -= OnRequirementRemoved;
                }
                bindedRequester = referenceRequester;
            }
            // Prevent duplicate event callbacks.
            bindedRequester.OnRequirementAdded -= OnRequirementAdded;
            bindedRequester.OnRequirementRemoved -= OnRequirementRemoved;
            bindedRequester.OnRequirementAdded += OnRequirementAdded;
            bindedRequester.OnRequirementRemoved += OnRequirementRemoved;

            ValidateFields(); 
            // QuickFillFields();
        }
        protected void UnBind() {
            referenceMapping.Clear();
            if (bindedRequester != null) {
                bindedRequester.OnRequirementAdded -= OnRequirementAdded;
                bindedRequester.OnRequirementRemoved -= OnRequirementRemoved;
            }
        }

        private HashSet<string> storedKeys = new();
        private void ValidateFields() {
            // Make sure space for storing required component references is available
            foreach (var typeRequesting in bindedRequester)
            {
                // Remove type requesting entries that are not needed
                if (!RequireComponentReferenceAttribute.Requirement.ContainsKey(typeRequesting))
                {
                    referenceMapping.Remove(typeRequesting.AssemblyQualifiedName); 
                    continue;
                }
                storedKeys.Add(typeRequesting.AssemblyQualifiedName);

                // Add in type requesting entry that is missing
                if (!referenceMapping.ContainsKey(typeRequesting.AssemblyQualifiedName))
                {
                    referenceMapping.Add(typeRequesting.AssemblyQualifiedName, new());
                }

                // Remove required type entry that is invalid after recompilation (Type name changed, removed, etc)
                var requiredTypeCollection = referenceMapping[typeRequesting.AssemblyQualifiedName];
                foreach (string requiredTypeName in referenceMapping[typeRequesting.AssemblyQualifiedName].Keys.ToList())
                {
                    Type requiredType = TypeCache.GetTypeByName(requiredTypeName);
                    if (requiredType == null) {
                        requiredTypeCollection.Remove(requiredTypeName); continue;
                    }

                    if (!RequireComponentReferenceAttribute.Requirement[typeRequesting].ContainsKey(requiredType)) {
                        requiredTypeCollection.Remove(requiredTypeName); continue;
                    }
                }

                // Add in required type entry that is missing 
                foreach (Type requriedType in RequireComponentReferenceAttribute.Requirement[typeRequesting].Keys) {
                    if (!requiredTypeCollection.ContainsKey(requriedType.AssemblyQualifiedName)) {
                        requiredTypeCollection.Add(requriedType.AssemblyQualifiedName, new());
                    }
                    var referenceCollection = requiredTypeCollection[requriedType.AssemblyQualifiedName];

                    if (referenceCollection == null) {
                        referenceCollection = new();
                        requiredTypeCollection[requriedType.AssemblyQualifiedName] = referenceCollection;
                    }

                    int numRequired = RequireComponentReferenceAttribute.Requirement[typeRequesting][requriedType].Count;

                    int numDiff = numRequired - referenceCollection.Count;
                    // Add in more fields to match the number of fields required by the type
                    if (numDiff > 0)
                    {
                        for (int i = 0;i < numDiff;i++) {
                            referenceCollection.Add(null);
                        }
                    }
                    // Remove extra fields to match the number of fields required by the type
                    else if(numDiff < 0){
                        for (int i = 0; i < -numDiff; i++)
                        {
                            referenceCollection.RemoveAt(referenceCollection.Count - 1);
                        }
                    }
                }
            }

            // Remove requester type entries that are not part of requirement of the currently binded requester
            foreach (var requesterTypeName in referenceMapping.Keys.ToList()) {
                if (!storedKeys.Contains(requesterTypeName)) { 
                    referenceMapping.Remove(requesterTypeName); continue;
                }
            }

            storedKeys.Clear();
        }

        /// <summary>
        /// Quickly fill the required references using the current gameobject
        /// </summary>
        internal void QuickFillFields() {
            foreach (var item in referenceMapping.Values) {
                foreach (var kwp in item) {
                    var lst = kwp.Value;
                    for (int i = 0;i < lst.Count;i++) {
                        if (lst[i] == null)
                        {
                            lst[i] = GetComponent(TypeCache.GetTypeByName(kwp.Key));
                        }
                    }
                }
            }
        }
        private void OnRequirementAdded(Type requesterType) {
            var requirement = RequireComponentReferenceAttribute.Requirement[requesterType];
            referenceMapping.Add(requesterType.AssemblyQualifiedName, new());
            foreach (Type requestedType in requirement.Keys) {
                ComponentList lst = new();
                referenceMapping[requesterType.AssemblyQualifiedName].Add(requestedType.AssemblyQualifiedName, lst);
                for (int i = 0;i < requirement[requestedType].Count;i++) {
                    lst.Add(null);
                }
            }
        }

        private void OnRequirementRemoved(Type requesterType)
        {
            referenceMapping.Remove(requesterType.AssemblyQualifiedName);
        }

        /// <summary>
        /// Check if the binded requester's requirements has been satisfied. Meaning all required fields must be non-null.
        /// </summary>
        /// <param name="typeRequesting">The type of the data within the binded data container requesting component references to check for</param>
        /// <returns>true if all required references are present, false otherwise</returns>
        public bool IsRequirementSatisfied(Type typeRequesting) {
            try
            {
                foreach (var lst in referenceMapping[typeRequesting.AssemblyQualifiedName].Values)
                {
                    foreach (var item in lst) {
                        if (item == null)
                        {
                            Debug.LogWarning($"Type {typeRequesting.FullName}'s component reference requirements are not completely satisfied!", gameObject);
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (KeyNotFoundException) // This type is not requesting references
            {
                return true;
            }
        }

        /// <summary>
        /// Get the reference requested by the type of the data stored in the binded data container.
        /// </summary>
        /// <typeparam name="T">The type of the component reference being requested</typeparam>
        /// <param name="requesterType">The type of the data stored in the binded data container requesting for reference</param>
        /// <returns>The requested reference if present, otherwise null</returns>
        public T GetComponentReference<T>(Type requesterType, int index=0) where T : Component{
            try {
                var item = referenceMapping[requesterType.AssemblyQualifiedName][typeof(T).AssemblyQualifiedName][index];
                if(item != null) { return (T)item; }
                return null;
            }catch(KeyNotFoundException e)
            {
                Debug.LogWarning($"Index out of bounds while attempting to acquire component reference of type {typeof(T).FullName} for {requesterType.FullName}! Make sure you're using type safe enum values as indices!");
                Debug.LogException(e);
                return default;
            }catch(IndexOutOfRangeException e)
            {
                Debug.LogWarning($"Index out of bounds while attempting to acquire component reference of type {typeof(T).FullName} for {requesterType.FullName}! Make sure you're using type safe enum values as indices!");
                Debug.LogException(e);
                return default;
            }
        }
    }

    [Serializable]
    internal class ComponentMap : SerializableDictionary<string, ComponentList> { } 

    [Serializable]
    internal class ReferenceMap : SerializableDictionary<string, ComponentMap> { }
}
