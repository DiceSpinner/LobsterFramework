using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Marks this ability as requiring specified AbilityComponents to run
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class RequireAbilityComponentsAttribute : Attribute
    {
        public static Dictionary<Type, HashSet<Type>> requirement = new();
        public static Dictionary<Type, HashSet<Type>> rev_requirement = new();

        private Type[] abilityComponents;

        public RequireAbilityComponentsAttribute(params Type[] abilityComponents)
        {
            this.abilityComponents = abilityComponents;
        }

        public void Init(Type ability) {
            if (!ability.IsSubclassOf(typeof(Ability)))
            {
                Debug.LogError("Type:" + ability.ToString() + " is not an Ability!");
                return;
            }
            if (!requirement.ContainsKey(ability))
            {
                requirement[ability] = new HashSet<Type>();
            }
            
            foreach (Type t in abilityComponents)
            {
                if (!t.IsSubclassOf(typeof(AbilityComponent)))
                {
                    Debug.LogError("Cannot apply require AbilityComponent of type:" + t.ToString() + " to " + ability.ToString());
                    return;
                }
                requirement[ability].Add(t);

                if (!rev_requirement.ContainsKey(t))
                {
                    rev_requirement[t] = new HashSet<Type>();
                }
                rev_requirement[t].Add(ability);
            }
        }
    }
}
