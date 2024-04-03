using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace LobsterFramework.Interaction
{
    /// <summary>
    /// Registers a method as interaction handler for the interactor. The method must have the correct signature:<br/>
    /// 1. Accepts a interactable object as the only argument <br/>
    /// 2. Does not have a return type <br/>
    /// 3. Method must be private
    /// If mutiple handlers are registered for the same interactable object, only the last one will be considered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class InteractionHandlerAttribute : Attribute
    {
        /// <summary>
        /// Stores interaction handlers for all interactions, use type of interactor to locate the set of handlers specific to that interactor and tuple type of InteractableObject with InteractionType to look for the handler in that set.
        /// </summary>
        internal static Dictionary<Type, Dictionary<(Type, InteractionType), Func<string>>> interactionHandlers = new();
        private static Interactor interactor;
        private static IInteractable interactableObject;

        internal static string HandleInteraction(Interactor actor, IInteractable obj, InteractionType interactionType) {
            interactor = actor;
            interactableObject = obj;
            Type t = actor.GetType();
            Type v = obj.GetType();
            try{
                return interactionHandlers[t][(v, interactionType)].Invoke();
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError($"Handler not found for interaction between {t.Name} and {v.Name}");
                return default;
            }
        }

        private static Func<string> ConvertMethod<T, V>(MethodInfo method) where T : Interactor  where V : IInteractable {
           Func<T, V, string> delegateMethod = (Func<T, V, string>)Delegate.CreateDelegate(typeof(Func<T, V, string>), method, true); 
            return () => { return delegateMethod.Invoke((T)interactor, (V)interactableObject); }; 
        }

        internal static void RegisterHandler(MethodInfo method, InteractionHandlerAttribute attribute, Type interactorType) {
            Dictionary<(Type, InteractionType), Func<string>> handlers = new();
            if (attribute.interactableType == null || !typeof(IInteractable).IsAssignableFrom(attribute.interactableType))
            {
                Debug.LogError("Failed to register interaction handler for " + interactorType.Name + $": Bad interactable object type {attribute.interactableType}!");
                return;
            }
            if (!CheckHandlerSignature(method))
            {
                Debug.LogError("Interaction handler of " + interactorType.Name + " for " + attribute.interactableType.Name + " has bad signature!");
                return;
            }
            MethodInfo conversion = typeof(InteractionHandlerAttribute).GetMethod("ConvertMethod", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo converted = conversion.MakeGenericMethod(interactorType, attribute.interactableType);
            handlers[(attribute.interactableType, attribute.interactionType)] = (Func<string>)converted.Invoke(null, new object[] {method});
            interactionHandlers[interactorType] = handlers;
        }

        private static bool CheckHandlerSignature(MethodInfo method)
        {
            if (method.ReturnType != typeof(string)) {
                return false;
            }
            ParameterInfo[] info = method.GetParameters();
            if (info == null || info.Length != 1)
            {
                return false;
            }
            return typeof(IInteractable).IsAssignableFrom(info[0].ParameterType);
        }

        public Type interactableType = default;
        public InteractionType interactionType;

        public InteractionHandlerAttribute(Type interactable, InteractionType interactionType) {
            interactableType = interactable;
            this.interactionType = interactionType;
        }
    }
}
