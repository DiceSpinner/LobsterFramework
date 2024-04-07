using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using LobsterFramework.Utility;
using Animancer;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem {
    /// <summary>
    /// Abilities defines the kind of actions the parent object can make. <br/>
    /// Each subclass of Ability defines its own AbilityConfig and can be runned on multiple instances of its AbilityConfigs.
    /// </summary>
    public abstract class Ability : ScriptableObject
    {
        protected internal AbilityManager abilityManager;
        public RefAbilityPriority abilityPriority;

        [SerializeField] internal AbilityConfigDictionary configs = new();
        internal Dictionary<string, AbilityChannel> channels = new();
        internal Dictionary<string, AbilityContext> contexts = new();

        private HashSet<string> executing = new();

        protected string Instance { get; private set; }
        protected AbilityConfig Config { get; private set; }
        protected AbilityChannel Channel { get; private set; }
        protected AbilityContext Context { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// Add the config with specified name to this Ability, this should only be called by editor scripts
        /// </summary>
        /// <param name="name">The name of the config to be added</param>
        internal bool AddConfig(string name)
        {
            Type type = GetType();
            if (ComplementariesDefined())
            {
                if (configs.ContainsKey(name))
                {
                    Debug.LogError("The ability config with name '" + name + "' is already added!");
                    return false;
                }
                
                var m = (typeof(Ability)).GetMethod("AddConfigGeneric", BindingFlags.NonPublic | BindingFlags.Instance);
                Type configType = type.Assembly.GetType(type.FullName + "Config");
                MethodInfo method = m.MakeGenericMethod(configType);
                method.Invoke(this, new[] { name });
                return true;
            }
            Type t = GetBaseConfigType();
            Type t2 = GetBaseChannelType();
            Type t3 = GetBaseContextType();
            Debug.LogError($"The necessary complementary classes for this ability is not defined, make sure to define {type.Name}Config / {type.Name}Pipe / {type.Name}Context and with each " +
                $"inherit from {t.Name} / {t2.Name} / {t3.Name}");
            return false;
        }

        private void AddConfigGeneric<T>(string name) where T : AbilityConfig 
        {
            Type type = GetType();
            if (typeof(T).FullName != (type.FullName + "Config"))
            {
                Debug.LogError("Config of type" + typeof(T).ToString() + " cannot be added!");
                return;
            }
            T config = CreateInstance<T>();
            configs.Add(name, config);
            config.name = this.name + "-" + name;
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.AddObjectToAsset(config, this);
            }
        }

        /// <summary>
        /// Remove the config with specified name if present, this should only be called by editor scripts
        /// </summary>
        /// <param name="name">Name of the config to be removed</param>
        /// <returns>The status of this operation</returns>
        public bool RemoveConfig(string name)
        {
            if (!configs.ContainsKey(name))
            {
                return false;
            }
            AbilityConfig config = configs[name];
            configs.Remove(name);
            DestroyImmediate(config, true);
            return true;
        }

        internal void SaveConfigsAsAsset()
        {
            foreach (AbilityConfig config in configs.Values)
            {
                AssetDatabase.AddObjectToAsset(config, this);
            }
        }

        internal void DisplayCurrentExecutingInstances() {
            if (executing.Count > 0)
            {
                string name = GetType().Name;
                foreach (string configName in executing)
                {
                    EditorGUILayout.LabelField(name, configName);
                }
            }
        }
#endif

        /// <summary>
        /// Clean up ability context environments
        /// </summary>
        protected virtual void ClearSetup() { }

        /// <summary>
        /// Called when ability manager is disabled
        /// </summary>
        internal void OnClose()
        {
            
            foreach (AbilityContext context in contexts.Values)
            {
                context.ClearSetup();
            }
            ClearSetup();
        }

        // Comparer used to sort action by their priority
        public int CompareByExecutionPriority(Ability other)
        {
            return abilityPriority.Value.executionPriority - other.abilityPriority.Value.executionPriority;
        }

        public int CompareByEnqueuePriority(Ability other)
        {
            return abilityPriority.Value.enqueuePriority - other.abilityPriority.Value.enqueuePriority;
        }

        protected void StartAnimation(string instance, AnimationClip animation, float speed = 1) { 
            abilityManager.StartAnimation(this, instance, animation, speed);
        }

        /// <summary>
        /// Additionaly utility method for skill check that can be imeplemented if the ability have additional requirements, this may varies beween different configs
        /// </summary>
        /// <param name="config">The config being queried</param>
        /// <returns></returns>
        protected virtual bool ConditionSatisfied() { return true; }

        /// <summary>
        /// Enqueue the ability if its not on cooldown and conditions are satisfied.  <br/>
        /// This method should not be directly called by external modules such as play input or AI. <br/> 
        /// AbilityRunner.EnqueueAbility&lt;T&gt;(string configName) shoud be used instead.
        /// </summary>
        /// <param name="configName">Name of the config being enqueued</param>
        /// <returns>true if successully enqueued the ability for execution, false otherwise</returns>
        internal bool EnqueueAbility(string configName)
        {
            try{
                if (IsReady(configName))
                {
                    Instance = configName;
                    Config = configs[configName];
                    Channel = channels[configName];
                    Context = contexts[configName];
                    Context.isRunning = true;
                    executing.Add(configName);
                    OnEnqueue();
                    
                    AbilityExecutor.EnqueueAction(new AbilityInstance(this, configName));
                    return true;
                }
            }catch(Exception e)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' Enqueue error:", abilityManager);
                Debug.LogException(e);
                SuspendInstance(configName);
            }
            
            return false;
        }

        /// <summary>
        /// Execute the config with the provided name.
        /// </summary>
        /// <param name="configName"></param>
        /// <returns>true if the ability will continue to execute, false otherwise</returns>
        internal bool Execute(string configName)
        {
            try
            {
                Instance = configName;
                Config = configs[configName];
                Channel = channels[configName];
                Context = contexts[configName];
                return Action();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' failed to execute with config  {configName}:\n", abilityManager);
                Debug.LogException(ex);
                SuspendInstance(configName);
            }
            return false;
        }

       /// <summary>
       /// The number of instances of this ability is running
       /// </summary>
        public int Running { get { return executing.Count; } }

        /// <summary>
        /// Suspend the execution of the specified ability instance and casuing it to finish at the current frame
        /// </summary>
        /// <param name="configName"> Name of the configuration of the ability instance to terminate </param>
        /// <returns> true if the configuration exists and is not running or suspended, otherwise false </returns>
        public bool SuspendInstance(string configName)
        {
            if (!configs.ContainsKey(configName))
            {
                return false;
            }
            AbilityContext context = contexts[configName];
            if (!context.isRunning)
            {
                return true;
            }
            context.isRunning = false;
            executing.Remove(configName);
            context.timeWhenAvailable = Time.time + configs[configName].CoolDown;

            try
            {
                Instance = configName;
                Config = configs[configName];
                Channel = channels[configName];
                Context = context;
                OnAbilityFinish();
            }catch (Exception ex)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' failed to finialize with config  {configName}:\n", abilityManager);
                Debug.LogException(ex);
            }

            // Update execution info for ability manager
            abilityManager.OnAbilityFinish(new AbilityInstance(this, configName));
            return true;
        }

        /// <summary>
        /// Suspend the execution of all configs
        /// </summary>
        public void SuspendAll()
        {
            foreach (string name in configs.Keys)
            {
                SuspendInstance(name);
            }
        }

        /// <summary>
        /// Callback to initialize the ability variables
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Check if the provided config is executing, this method will return false if the config is not present
        /// </summary>
        /// <param name="configName"> The name of the config to be examined </param>
        /// <returns> true if the specified config is executing, otherwise false </returns>
        public bool IsExecuting(string configName)
        {
            return executing.Contains(configName);
        }

        /// <summary>
        /// Check if the speficied ability instance is ready
        /// </summary>
        /// <param name="instance">The name of the instance of the ability instance</param>
        /// <returns>true if config with specified name exists and is ready, false otherwise</returns>
        public bool IsReady(string instance)
        {
            if (!configs.ContainsKey(instance))
            {
                Debug.LogError(GetType().ToString() + " does not have config with name '" + instance + "'", this);
                return false;
            }
            Config = configs[instance];
            Channel = channels[instance];
            Context = contexts[instance];
            return !Context.IsRunning && (!Config.UseCooldown || !Context.OnCooldown) && ConditionSatisfied();
        }

        /// <summary>
        /// Initialize ability context environments
        /// </summary>
        internal void OnOpen()
        {
            if (!ComplementariesDefined())
            {
                Debug.LogError("The ability config or channel for '" + GetType() + "' is not declared!");
            }
            Initialize();
            Type type = GetType();
            Type channelType = type.Assembly.GetType(type.FullName + "Channel");
            Type contextType = type.Assembly.GetType(type.FullName + "Context");

            List<string> removed = new();
            foreach (KeyValuePair<string, AbilityConfig> pair in configs)
            {
                AbilityConfig config = pair.Value;
                string name = pair.Key;
                if (config == null)
                {
                    removed.Add(name);
                    continue;
                }
                channels[name] = (AbilityChannel)Activator.CreateInstance(channelType);
                channels[name].Construct(config);
                
                AbilityContext context = (AbilityContext)Activator.CreateInstance(contextType);
                context.ability = this;
                contexts[name] = context;
                context.Initialize();
            }
            foreach (string key in removed)
            {
                configs.Remove(key);
            }
        }

        // Reset, called when the parent component is resetted
        internal void ResetStatus()
        {
            ClearSetup();
            Initialize();
        }

        /// <summary>
        /// Main body of the ability execution, implement this to create different abilities!
        /// </summary>
        /// <returns>False if the ability has finished, otherwise true</returns>
        protected abstract bool Action();

        /// <summary>
        /// Callback when the ability is finished or halted.
        /// </summary>
        protected virtual void OnAbilityFinish() { }

        /// <summary>
        /// Callback when the animation of the ability is interrupted by other abilities. Useful when abilities relies on animation events.
        /// Default implementation suspends the ability.
        /// </summary>
        protected virtual void OnAnimationInterrupt(AnimancerState state) { state.Speed = 1; SuspendInstance(Instance); }

        /// <summary>
        /// Send animation interrupt signal to the ability
        /// </summary>
        /// <param name="instance">Name of the ability instance</param>
        internal void AnimationInterrupt(string instance, AnimancerState state)
        {
            if (!configs.ContainsKey(instance)) { return; }
            Instance = instance;
            Config = configs[instance];
            Context = contexts[instance];
            Channel = channels[instance];
            OnAnimationInterrupt(state);
        }
        /// <summary>
        /// Callback when the ability is added to the queue for execution
        /// </summary>
        protected virtual void OnEnqueue() { }

        /// <summary>
        /// Attempt to join the current running ability with another ability that is running. 
        /// On success, the current running ability will terminate no later than the joined ability.
        /// </summary>
        /// <typeparam name="T">The type of the ability to be joined with</typeparam>
        /// <param name="instance">The name of the instance of the running ability to be joined</param>
        /// <returns> Return true on success, otherwise false </returns>
        protected bool JoinAsSecondary<T>(string instance) where T : Ability
        {
            return abilityManager.JoinAbilities(typeof(T), GetType(), instance, Instance);
        }

        /// <summary>
        /// Attempt to join the current running ability with another ability that is running. 
        /// On success, the current running ability will terminate no later than the joined ability.
        /// </summary>
        /// <param name="abilityType">The type of the ability to be joined with</param>
        /// <param name="instance">The name of the instance of the ability to be joined</param>
        /// <returns> Return true on success, otherwise false</returns>
        protected bool JoinAsSecondary(Type abilityType, string instance)
        {
            return abilityManager.JoinAbilities(abilityType, GetType(), instance, Instance);
        }

    # region Requirement Check
            private Type GetBaseConfigType()
            {
                Type type = GetType().BaseType;
                while (type != typeof(Ability))
                {
                    Type t = type.Assembly.GetType(type.FullName + "Config");
                    if (t != null && t.IsSubclassOf(typeof(AbilityConfig)))
                    {
                        return t;
                    }
                    type = type.BaseType;
                }
                return typeof(AbilityConfig);
            }

            private Type GetBaseChannelType()
            {
                Type type = GetType().BaseType;
                while (type != typeof(Ability))
                {
                    Type t = type.Assembly.GetType(type.FullName + "Channel");
                    if (t != null && t.IsSubclassOf(typeof(AbilityChannel)))
                    {
                        return t;
                    }
                    type = type.BaseType;
                }
                return typeof(AbilityChannel);
            }

            private Type GetBaseContextType() {
                Type type = GetType().BaseType;
                while (type != typeof(Ability))
                {
                    Type t = type.Assembly.GetType(type.FullName + "Context");
                    if (t != null && t.IsSubclassOf(typeof(AbilityContext)))
                    {
                        return t;
                    }
                    type = type.BaseType;
                }
                return typeof(AbilityContext);
            }

            /// <summary>
            /// Check to see if the ability class has the pipe, config and context classes defined in the same namespace
            /// </summary>
            /// <returns>true if all necessities have been defined, false otherwise</returns>
            private bool ComplementariesDefined()
            {
                Type abilityType = GetType();
                string typeName = abilityType.FullName + "Config";
                string pipeName = abilityType.FullName + "Channel";
                string contextName = abilityType.FullName + "Context";
                Type configType = GetBaseConfigType();
                Type channelType = GetBaseChannelType();
                Type contextType = GetBaseContextType();
                bool config = false;
                bool pipe = false;
                bool context = false;

                Type[] types = abilityType.Assembly.GetTypes();
                foreach (Type type in types)
                {
                    string name = type.FullName;
                    if (name.Equals(typeName) && type.IsSubclassOf(configType))
                    {
                        config = true;
                    }
                    else if (name.Equals(pipeName) && type.IsSubclassOf(channelType))
                    {
                        pipe = true;
                    } else if (name.Equals(contextName) && type.IsSubclassOf(contextType)) {
                        context = true;
                    }
                    if (config && pipe && context)
                    {
                        return true;
                    }
                }
                return false;
            }
            #endregion

        /// <summary>
        /// Check if the ability has specified configuration
        /// </summary>
        /// <param name="instance">Name of the ability instance being queried</param>
        /// <returns>true if exists, false otherwise</returns>
        public bool HasInstance(string instance)
        {
            return configs.ContainsKey(instance);
        }

        #region Signal Handlers
        /// <summary>
        /// Signal this ability.
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="animationEvent"></param>
        public void Signal(string configName, AnimationEvent animationEvent)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && contexts[configName].isRunning)
            {
                Instance = configName;
                Config = config;
                Channel = channels[configName];
                Context = contexts[configName];
                OnSignaled(animationEvent);
            }
        }
        public void Signal(string configName)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && contexts[configName].isRunning)
            {
                Instance = configName;
                Config = config;
                Channel = channels[configName];
                Context = contexts[configName];
                OnSignaled();
            }
        }
        public void Signal(string configName, int num)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && contexts[configName].isRunning)
            {
                Instance = configName;
                Config = config;
                Channel = channels[configName];
                Context = contexts[configName];
                OnSignaled(num);
            }
        }
        public void Signal(string configName, bool flag)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && contexts[configName].isRunning)
            {
                Instance = configName;
                Config = config;
                Channel = channels[configName];
                Context = contexts[configName];
                OnSignaled(flag);
            }
        }
        public void Signal(string configName, string text)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && contexts[configName].isRunning)
            {
                Instance = configName;
                Config = config;
                Channel = channels[configName];
                Context = contexts[configName];
                OnSignaled(text);
            }
        }
        public void Signal(string configName, UnityEngine.Object obj)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && contexts[configName].isRunning)
            {
                Instance = configName;
                Config = config;
                Channel = channels[configName];
                Context = contexts[configName];
                OnSignaled(obj);
            }
        }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        protected virtual void OnSignaled(AnimationEvent animationEvent) { }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        protected virtual void OnSignaled() { }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        protected virtual void OnSignaled(int num) { }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        protected virtual void OnSignaled(bool flag) { }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        protected virtual void OnSignaled(string text) { }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        protected virtual void OnSignaled(UnityEngine.Object obj) { }
        #endregion

        internal AbilityChannel GetAbilityChannel(string configName)
        {
            if (configs.ContainsKey(configName))
            {
                return channels[configName];
            }
            return default;
        }
    }

    #region Complementary classes (Must be defined for each Ability)
    /// <summary>
    ///  The runtime context of an ability instance. Not accessible from outside.
    ///  Inheritors of this class must have name 'Ability_Subclass_Name'Context.
    /// </summary>
    public class AbilityContext
    {
        internal protected Ability ability;
        internal bool isRunning = false;
        internal float timeWhenAvailable = 0;
        
        public bool IsRunning { get { return isRunning; } }
        public bool OnCooldown { get { return Time.time < timeWhenAvailable; } }

        /// <summary>
        /// Callback to clean up the context
        /// </summary>
        protected internal virtual void ClearSetup() { }

        /// <summary>
        /// Callback to initialize the contex
        /// </summary>
        protected internal virtual void Initialize() { }
    }

    /// <summary>
    /// Communication channel of an ability Instance. Can be used to control ability behaviors at runtime.
    /// </summary>
    public class AbilityChannel
    {
        protected AbilityConfig config;
        public void Construct(AbilityConfig config)
        {
            this.config = config;
            Construct();
        }
        public virtual void Construct() { }
    }

    /// <summary>
    /// The configuration of an ability instance
    /// </summary>
    [Serializable]
    public class AbilityConfig : ScriptableObject {
        [SerializeField] private bool useCooldown = true;
        [SerializeField] private float cooldown = 0;
        public float CoolDown { get { return cooldown; } }
        public bool UseCooldown { get { return useCooldown; } }
        
        /// <summary>
        /// Override this to validate data after making changes in inspector
        /// </summary>
        protected virtual void Validate() { }

        protected void OnValidate()
        {
            if (cooldown < 0)
            {
                Debug.LogWarning("Cooldown cannot be less than 0!", this);
                cooldown = 0;
            }
            Validate();
        }
    }
    #endregion

    #region Other Structs
    /// <summary>
    /// Represents an instance of ability to be executed by ActionOverseer
    /// </summary>
    internal struct AbilityInstance
    {
        public string configName;
        public Ability ability;
        public AbilityInstance(Ability ability, string configName)
        {
            this.ability = ability;
            this.configName = configName;
        }

        public bool StopAbility()
        {
            return ability.SuspendInstance(configName);
        }

        public bool IsValid() {
            return ability.IsExecuting(configName);
        }

        public bool IsEmpty { get { return ability == null; } }
    }

    [Serializable]
    public class AbilityDictionary : SerializableDictionary<string, Ability> { }

    [Serializable]
    public class AbilityConfigDictionary : SerializableDictionary<string, AbilityConfig> { }
    #endregion
}
