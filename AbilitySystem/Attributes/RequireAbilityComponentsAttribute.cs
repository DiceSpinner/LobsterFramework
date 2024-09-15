using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Init;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Marks this ability as requiring specified <see cref="AbilityComponent"/> to run
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Dual)]
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class RequireAbilityComponentsAttribute : InitializationAttribute
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

        public static bool IsCompatible(Type ability) {
            return ability.IsSubclassOf(typeof(Ability));
        }

        internal protected override void Init(Type ability) {
            
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
