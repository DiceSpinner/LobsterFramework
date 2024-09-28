using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.LowLevel;

namespace LobsterFramework.Init
{
    /// <summary>
    /// Initializes a new player loop event group. Sealed classes implementing <see cref="IPlayerLoopEventGroup"/> applied with this attribute will be inspected for <see cref="PlayerLoopEventAttribute"/> on its static methods for player loop event injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    [RegisterInitialization(AttributeType = InitializationAttributeType.Dual)]
    public sealed class PlayerLoopEventGroupAttribute : InitializationAttribute
    {
        internal static List<PlayerLoopEventGroupAttribute> EventGroups = new(); 
        internal Type NeighbourEvent;
        internal Type Type;
        public bool InjectAfter;
        public int Priority;
        internal List<PlayerLoopSystem.UpdateFunction> UpdateEvents = new();
        internal Dictionary<PlayerLoopSystem.UpdateFunction, Type> EventTypes = new();

        public static bool IsCompatible(Type type)
        {
            return type.IsSealed && typeof(IPlayerLoopEventGroup).IsAssignableFrom(type);
        }

        public PlayerLoopEventGroupAttribute(Type neighbourEvent)
        {
            NeighbourEvent = neighbourEvent;
        }

        protected internal override void Init(Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); 
            foreach (var method in methods) {
                if (method.ReturnType == typeof(void) && !method.IsGenericMethodDefinition && method.GetParameters().Length == 0) {
                    var attr = method.GetCustomAttribute<PlayerLoopEventAttribute>();
                    if (attr != null) {
                        var func = Delegate.CreateDelegate(typeof(PlayerLoopSystem.UpdateFunction), method) as PlayerLoopSystem.UpdateFunction;
                        UpdateEvents.Add(func);
                        EventTypes.Add(func, attr.EventType);
                    }
                }
            }
            EventGroups.Add(this);
            Type = type;
        }
    }

    /// <summary>
    /// Sort in descending order
    /// </summary>
    internal class PlayerLoopEventGroupPriorityComparer : IComparer<PlayerLoopEventGroupAttribute>
    {
        public int Compare(PlayerLoopEventGroupAttribute x, PlayerLoopEventGroupAttribute y)
        {
            return y.Priority - x.Priority;
        }
    }
}
