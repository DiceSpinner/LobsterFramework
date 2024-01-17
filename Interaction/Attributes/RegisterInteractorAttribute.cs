using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace LobsterFramework.Interaction
{
    /// <summary>
    /// Enables interaction handlers and interactability checkers for this interactor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RegisterInteractorAttribute : Attribute
    {
        public void Init(Type interactor) {
            if (!interactor.IsSubclassOf(typeof(Interactor))) {
                Debug.LogError("Type " + interactor.Name + " is not an interactor!");
                return;
            }
            // Loop through all methods and register handlers and checkers
            foreach (MethodInfo methodInfo in interactor.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                InteractionHandlerAttribute handlerAttribute = methodInfo.GetCustomAttribute<InteractionHandlerAttribute>(true);
                if (handlerAttribute != null) {
                    InteractionHandlerAttribute.RegisterHandler(methodInfo, handlerAttribute, interactor);
                    continue;
                }

                InteractabilityCheckerAttribute checkerAttribute = methodInfo.GetCustomAttribute<InteractabilityCheckerAttribute>();
                if (checkerAttribute != null) {
                   InteractabilityCheckerAttribute.RegisterChecker(methodInfo, checkerAttribute, interactor);
                }
            }
        }
    }
}
