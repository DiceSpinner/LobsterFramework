using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace LobsterFramework.Init
{
    /// <summary>
    /// Initializes all of the custom attributes of LobsterFramework for all assemblies that reference it.
    /// </summary>
    public static class AttributeInitialization
    {
        /// <summary>
        /// Flag to indicate whether attribute initialization is completed.
        /// </summary>
        public static bool Finished = false;

        public static event Action OnInitializationComplete;

        internal static List<(InitializationAttributeType, Type, int)> runtimeAttributes = new();
        internal static List<(InitializationAttributeType, Type, int)> editorAttributes = new();

        internal static Dictionary<Type, Func<Type, bool>> compatabilityCheckers = new();
        internal static HashSet<Type> initialized = new();

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts(Constants.AttributeInitOrder)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        private static void InitializeAttributes()
        {
            if (Finished) {
                return;
            }
            Assembly frameworkAssembly = typeof(Setting).Assembly;

            List<Type> typesToInit = new(frameworkAssembly.GetTypes());

            AssemblyName assemblyName = frameworkAssembly.GetName();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName[] references = assembly.GetReferencedAssemblies();
                foreach (AssemblyName reference in references)
                {
                    if (reference.Name == assemblyName.Name)
                    {
                        typesToInit.AddRange(assembly.GetTypes());
                        // Debug.Log($"Assembly {assembly.GetName().Name} use of LobsterFramework detected!"); 
                        break;
                    }
                }
            }

            FindInitAttributes(typesToInit);

            AttributePriorityComparer comparer = new();

#if UNITY_EDITOR
            editorAttributes.Sort(comparer);
            InitializeEditorAttributes(typesToInit);
#endif

            runtimeAttributes.Sort(comparer);
            InitializeRuntimeAttributes(typesToInit);

            Finished = true;
            try
            {
                OnInitializationComplete?.Invoke();
            }catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            OnInitializationComplete = null;
        }

        private static void FindInitAttributes(List<Type> types) {
#if UNITY_EDITOR
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(InitializationAttribute)) && type.IsSealed) {
                    var attribute = type.GetCustomAttribute<RegisterInitializationAttribute>();
                    if (attribute != null)
                    {
                        MethodInfo checker = type.GetMethod(InitializationAttribute.CompatabilityCheckerMethodName, BindingFlags.Static | BindingFlags.Public);
                        if (checker == null) { Debug.LogWarning($"InitializationAttribute {type.FullName} is missing compatibility checker static method.");  continue; }
                        if (checker.ReturnType == typeof(bool)) {
                            var parameters = checker.GetParameters();
                            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Type)) {
                                Debug.LogWarning($"InitializationAttribute {type.FullName} compatibility checker static method has incorrect signature."); continue;
                            }
                        }
                        else {
                            Debug.LogWarning($"InitializationAttribute {type.FullName} compatibility checker static method has incorrect signature."); continue;
                        }
                        compatabilityCheckers[type] = (Func<Type, bool>)Delegate.CreateDelegate(typeof(Func<Type, bool>), checker);

                        if (attribute.AttributeType == InitializationAttributeType.Editor)
                        {
                            editorAttributes.Add((InitializationAttributeType.Editor, type, attribute.priority));
                        }
                        else if (attribute.AttributeType == InitializationAttributeType.Runtime)
                        {
                            runtimeAttributes.Add((InitializationAttributeType.Runtime, type, attribute.priority));
                        }
                        else {
                            editorAttributes.Add((InitializationAttributeType.Dual, type, attribute.priority));
                            runtimeAttributes.Add((InitializationAttributeType.Dual, type, attribute.priority));
                        }
                    }
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static void InitializeEditorAttributes(List<Type> types) {
            foreach ((var initType, var attributeType, int priority) in editorAttributes) {
                if (initialized.Contains(attributeType)) {
                    continue;
                }

                var compatibilityChecker = compatabilityCheckers[attributeType];
                try
                {
                    foreach (Type type in types)
                    {
                        if (compatibilityChecker(type)) 
                        {
                            foreach (InitializationAttribute attribute in type.GetCustomAttributes(attributeType).Cast<InitializationAttribute>())
                            {
                                attribute.Init(type);
                            }
                        }
                    }
                }catch (Exception ex) { Debug.LogError($"Exception occured while initializing attribute {attributeType.FullName}"); Debug.LogException(ex); }

                initialized.Add(attributeType);
            }
        }
#endif

        private static void InitializeRuntimeAttributes(List<Type> types)
        {
            foreach ((var initType, var attributeType, int priority) in runtimeAttributes)
            {
                if (initialized.Contains(attributeType))
                {
                    continue;
                }

                var compatibilityChecker = compatabilityCheckers[attributeType];
                try
                {
                    foreach (Type type in types)
                    {
                        if (compatibilityChecker(type))
                        {
                            foreach (InitializationAttribute attribute in type.GetCustomAttributes(attributeType).Cast<InitializationAttribute>())
                            {
                                attribute.Init(type);
                            }
                        }
                    }
                }catch (Exception ex) { Debug.LogError($"Exception occured while initializing attribute {attributeType.FullName}"); Debug.LogException(ex); }
                initialized.Add(attributeType);
            }
        }
    }

    /// <summary>
    /// Sort in descending order
    /// </summary>
    internal class AttributePriorityComparer : IComparer<(InitializationAttributeType, Type, int)> {
        public int Compare((InitializationAttributeType, Type, int) x, (InitializationAttributeType, Type, int) y)
        {
            if (x.Item1 == y.Item1) {
                return y.Item3 - x.Item3;
            }

            if ((x.Item1 == InitializationAttributeType.Dual || x.Item1 == InitializationAttributeType.Editor) && y.Item1 == InitializationAttributeType.Runtime)
            {
                return -1;
            }
            return 1;
        }
    }
}
