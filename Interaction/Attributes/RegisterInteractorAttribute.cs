using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using LobsterFramework.Init;

namespace LobsterFramework.Interaction
{
    /// <summary>
    /// Enables interaction handlers and interactability checkers for this interactor.
    /// </summary>
    [RegisterInitialization(AttributeType = InitializationAttributeType.Runtime)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RegisterInteractorAttribute : InitializationAttribute
    {
        public static bool IsCompatible(Type interactor)
        {
            return interactor.IsSubclassOf(typeof(Interactor));
        }

        internal protected override void Init(Type interactor) {
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
