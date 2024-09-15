using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using LobsterFramework.Utility;
using System.Reflection;
using LobsterFramework.Init;

namespace LobsterFramework
{
    /// <summary>
    /// Indicates this class requires a reference to the specified <see cref="Component"/> to function, this attribute will be inherited by subclasses.
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Dual)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RequireComponentReferenceAttribute : InitializationAttribute
    {
        private static Dictionary<Type, Dictionary<Type, List<RequirementDescription>>> requirement = new();

        private List<(Type, string, string)> components;

        /// <summary>
        /// Contains the set of requirements according to <see cref="RequireComponentReferenceAttribute"/> applied to classes. 
        /// Access Pattern: RequesterType -> RequiredType -> Index of the field description you are looking for
        /// </summary>
        public static ReadOnlyDictionary<Type, Dictionary<Type, List<RequirementDescription>>> Requirement = new(requirement);

        public static bool IsCompatible(Type type) {
            return type.IsSealed && !type.IsSubclassOf(typeof(Component));
        }

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

        public RequireComponentReferenceAttribute(Type requiredType, string fieldName, string description)
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

        public RequireComponentReferenceAttribute(Type requiredType, Type enumList) {
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
        public RequireComponentReferenceAttribute(Type requiredType, int numOfFields)
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
            if (numOfFields <= 0 ) {
                Debug.LogWarning($"Number of fields should be a positive integer: {requiredType.FullName}");
                numOfFields = 1;
            }

            components = new();
            for (int i = 0;i < numOfFields;i++) {
                components.Add((requiredType, default, default));
            }
        }

        internal protected override void Init(Type requesterType) {
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
