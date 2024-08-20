using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using Codice.CM.Common;
using LobsterFramework.Utility;
using System.Reflection;

namespace LobsterFramework
{
    /// <summary>
    /// Indicates this class requires a reference to the specified <see cref="Component"/> to function, this attribute will be inherited by subclasses.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)] 
    public class RequireComponentReferenceAttribute : Attribute
    {
        private static Dictionary<Type, Dictionary<Type, List<RequirementDescription>>> requirement = new();

        private List<(Type, string, string)> components;

        /// <summary>
        /// Contains the set of requirements according to <see cref="RequireComponentReferenceAttribute"/> applied to classes. 
        /// Access Pattern: RequesterType -> RequiredType -> Index of the field description you are looking for
        /// </summary>
        public static ReadOnlyDictionary<Type, Dictionary<Type, List<RequirementDescription>>> Requirement = new(requirement);

        
        public RequireComponentReferenceAttribute(Type requiredType)
        {
            if (requiredType == null) {
                return;
            }
            if (!requiredType.IsSubclassOf(typeof(Component)))
            {
                Debug.LogWarning($"{requiredType.FullName} is not a component type!");
                return;
            }
            components = new()
            {
                (requiredType, default, default)
            };
        }

        public RequireComponentReferenceAttribute(Type requiredType, string fieldName)
        {
            if (requiredType == null)
            {
                return;
            }
            if (!requiredType.IsSubclassOf(typeof(Component)))
            {
                Debug.LogWarning($"{requiredType.FullName} is not a component type!");
                return;
            }
            components = new()
            {
                (requiredType, fieldName, default)
            };
        }

        public RequireComponentReferenceAttribute( Type requiredType, string fieldName,  string description)
        {
            if (requiredType == null)
            {
                return;
            }
            if (!requiredType.IsSubclassOf(typeof(Component)))
            {
                Debug.LogWarning($"{requiredType.FullName} is not a component type!");
                return;
            }
            components = new()
            {
                (requiredType, fieldName, description)
            };
        }

        public RequireComponentReferenceAttribute( Type requiredType, Type enumList) {
            if (requiredType == null)
            {
                return;
            }
            if (!requiredType.IsSubclassOf(typeof(Component)))
            {
                Debug.LogWarning($"{requiredType.FullName} is not a component type!");
                return;
            }
            if (!enumList.IsEnum) {
                Debug.LogWarning($"{enumList.FullName} is not an enum type!");
                return;
            }
            components = new();
            foreach (string name in EnumCache.GetNames(enumList)) {
                var info = enumList.GetMember(name)[0];
                var description = info.GetCustomAttribute<FieldDescriptionAttribute>();
                string fieldDescription = "";
                string fieldName = "";
                if (description != null) {
                    fieldDescription = description.Description;
                    fieldName = description.Name;
                }
                components.Add((requiredType, fieldDescription, fieldName));
            }
        }

        internal void Init(Type requesterType) {
            if (!requesterType.IsSealed) { // Only register requirements for sealed classes, abstract classes cannot be constructed and therefore do not need to be registered. 
                return;
            }
            if (components == null) {
                return;
            }
            if (!requirement.ContainsKey(requesterType)) { 
                requirement[requesterType] = new();
            }
            foreach((Type componentType, string name, string description) in components)
            {
                if (!requirement[requesterType].ContainsKey(componentType)) {
                    requirement[requesterType][componentType] = new();
                }
                requirement[requesterType][componentType].Add(new() { Name = name, Description = description });
            }
        }
    }

    /// <summary>
    /// Describes the a reference requirement
    /// </summary>
    public record RequirementDescription {
        /// <summary>
        /// Displayed name of the requirement in the inspector
        /// </summary>
        public string Name;

        /// <summary>
        /// Displayed tooltip of the requirement in the inspector
        /// </summary>
        public string Description;
    }
}
