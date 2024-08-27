using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Marks this ability as requiring specified <see cref="AbilityComponent"/> to run
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class RequireAbilityComponentsAttribute : Attribute
    {
        /// <summary>
        /// Key: <see cref="Ability"/> Type, Value: The set of <see cref="AbilityComponent"/> types required by this ability. 
        /// </summary>
        internal static Dictionary<Type, HashSet<Type>> Requirement = new();

        /// <summary>
        /// Key: <see cref="AbilityComponent"/> Type, Value: The set of <see cref="Ability"/> types relies on this ability component. 
        /// </summary>
        internal static Dictionary<Type, HashSet<Type>> ReverseRequirement = new();

        private Type[] abilityComponents;

        public RequireAbilityComponentsAttribute( params Type[] abilityComponents)
        {
            this.abilityComponents = abilityComponents;
        }

        internal void Init(Type ability) {
            if (!ability.IsSubclassOf(typeof(Ability)))
            {
                Debug.LogError("Type:" + ability.ToString() + " is not an Ability!");
                return;
            }
            if (abilityComponents == null) {
                Debug.LogWarning($"Passing null argument to {nameof(RequireAbilityComponentsAttribute)} when being applied to {ability.FullName}! The attribute will be discarded!");
                return;
            }

            if (!Requirement.ContainsKey(ability))
            {
                Requirement[ability] = new HashSet<Type>();
            }
            

            foreach (Type t in abilityComponents)
            {
                if (t == null) {
                    continue;
                }
                if (!t.IsSubclassOf(typeof(AbilityComponent)) && t.IsSealed)
                {
                    Debug.LogWarning($"Cannot apply the requirement of{nameof(AbilityComponent)} of type: + {t.FullName} to {ability.FullName}");
                    continue;
                }
                Requirement[ability].Add(t);

                if (!ReverseRequirement.ContainsKey(t))
                {
                    ReverseRequirement[t] = new HashSet<Type>();
                }
                ReverseRequirement[t].Add(ability);
            }
        }
    }
}
