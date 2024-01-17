using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace LobsterFramework.Interaction
{
    /// <summary>
    /// Registers a method as interactability checker for the interactor with respect to the specified interactable object. The method must have the correct signature:<br/>
    /// 1. Accepts a <see cref="InteractableObject"/> as the only argument <br/>
    /// 2. Have a return type of <see cref="bool"/> <br/>
    /// 3. Method must be private
    /// If mutiple checkers are registered for the same interactable object, only the last one will be considered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)] 
    public class InteractabilityCheckerAttribute : Attribute
    {
        /// <summary>
        /// Stores interaction checkers for all interactions, use type of interactor to locate the set of checkers specific to that interactor and tuple type of InteractableObject with InteractionType to look for the checker in that set.
        /// </summary>
        internal static Dictionary<Type, Dictionary<Type, Func<InteractionPrompt>>> interactabilityCheckers = new();
        private static Interactor interactor;
        private static InteractableObject interactableObject;

        internal static InteractionPrompt GetInteractionPrompts(Interactor actor, InteractableObject obj)
        {
            interactor = actor;
            interactableObject = obj;
            Type t = actor.GetType();
            Type v = obj.GetType();
            try
            {
                return interactabilityCheckers[t][v].Invoke();
            }
            catch (KeyNotFoundException)
            {
                Debug.Log($"No checker found for interaction between {t.Name} and {v.Name}, use default interactability.");
                return InteractionPrompt.none;
            }
        }

        private static Func<InteractionPrompt> ConvertMethod<T, V>(MethodInfo method) where T : Interactor where V : InteractableObject
        {
            Func<T, V, InteractionPrompt> delegateMethod = (Func<T, V, InteractionPrompt>)Delegate.CreateDelegate(typeof(Func<T, V, InteractionPrompt>), method, true);
            return () => { return delegateMethod.Invoke((T)interactor, (V)interactableObject); };
        }

        internal static void RegisterChecker(MethodInfo method, InteractabilityCheckerAttribute attribute, Type interactorType)
        {
            Dictionary<Type, Func<InteractionPrompt>> checkers = new();
            if (attribute.interactableType == null || !attribute.interactableType.IsSubclassOf(typeof(InteractableObject)))
            {
                Debug.LogError("Failed to register interactablity checker for " + interactorType.Name + ": Bad interactable object type!");
                return;
            }

            if (!CheckCheckerSignature(method))
            {
                Debug.LogError("Interactability checker of " + interactorType.Name + " for " + attribute.interactableType.Name + " has bad signature!"); 
                return;
            }
            MethodInfo conversion = typeof(InteractabilityCheckerAttribute).GetMethod("ConvertMethod", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo converted = conversion.MakeGenericMethod(interactorType, attribute.interactableType); 
            checkers[attribute.interactableType] = (Func<InteractionPrompt>)converted.Invoke(null, new object[] { method });
            interactabilityCheckers[interactorType] = checkers;
        }

        private static bool CheckCheckerSignature(MethodInfo method)
        {
            if (method.ReturnType != typeof(InteractionPrompt))
            {
                return false;
            }

            ParameterInfo[] info = method.GetParameters();
            if (info == null || info.Length != 1)
            {
                return false;
            }
            return info[0].ParameterType.IsSubclassOf(typeof(InteractableObject));
        }

        public Type interactableType = default;

        public InteractabilityCheckerAttribute(Type interactable)
        {
            interactableType = interactable;
        }
    }
}
