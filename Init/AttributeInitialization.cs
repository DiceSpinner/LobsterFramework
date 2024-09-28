using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using StopWatch = System.Diagnostics.Stopwatch;

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
            var stopWatch = StopWatch.StartNew();
            stopWatch.Start();

            Assembly frameworkAssembly = typeof(AttributeInitialization).Assembly;
            AssemblyName frameworkName = frameworkAssembly.GetName();

            HashSet<string> keyAssemblies = new() { frameworkName.FullName };
            List<Type> typesToInit = new(frameworkAssembly.GetExportedTypes());

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Dictionary<Assembly, AssemblyName[]> referencedAssemblies = new();
            foreach (Assembly assembly in assemblies)
            {
                AssemblyName[] references = assembly.GetReferencedAssemblies();
                referencedAssemblies[assembly] = references;
                foreach (AssemblyName reference in references)
                {
                    if (reference.FullName == frameworkName.FullName)
                    {
                        // Debug.Log($"{assembly.GetName().Name} referencing LobsterFramework.");
                        if (assembly.GetCustomAttribute<AttributeProviderAttribute>() != null) {
                            keyAssemblies.Add(assembly.GetName().FullName);
                        }
                        break;
                    }
                }
            }

            foreach (Assembly assembly in assemblies) 
            {
                AssemblyName assemblyName = assembly.GetName();
                foreach (AssemblyName reference in referencedAssemblies[assembly])
                {
                    if (keyAssemblies.Contains(reference.FullName))
                    {
                        // Debug.Log($"Assembly {assemblyName.Name} will be inspected for attribute initialization!"); 
                        typesToInit.AddRange(assembly.GetExportedTypes());
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

            stopWatch.Stop();
            Debug.Log($"Attribute initialization took {stopWatch.Elapsed.TotalSeconds} seconds!");
        }

        private static void FindInitAttributes(List<Type> types) {
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
            if (x.Item1 == y.Item1 || (x.Item1 == InitializationAttributeType.Dual && y.Item1 == InitializationAttributeType.Editor) || (x.Item1 == InitializationAttributeType.Editor && y.Item1 == InitializationAttributeType.Dual)) {
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
