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
        internal Dictionary<string, AbilityRuntime> runtimes = new();

        private HashSet<string> executing = new();

        protected string ConfigName { get; private set; }
        protected AbilityConfig Config { get; private set; }
        protected AbilityChannel Channel { get; private set; }
        protected AbilityRuntime Runtime { get; private set; }

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
            Type t3 = GetBaseRuntimeType();
            Debug.LogError($"The necessary complementary classes for this ability is not defined, make sure to define {type.Name}Config / {type.Name}Pipe / {type.Name}Runtime and with each " +
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
#endif

        /// <summary>
        /// Callback when the gameobject is about to be disabled or destroyed
        /// </summary>
        protected virtual void Clear() { }

        // Comparer used to sort action by their priority
        public int CompareByExecutionPriority(Ability other)
        {
            return abilityPriority.Value.executionPriority - other.abilityPriority.Value.executionPriority;
        }

        public int CompareByEnqueuePriority(Ability other)
        {
            return abilityPriority.Value.enqueuePriority - other.abilityPriority.Value.enqueuePriority;
        }

        protected void StartAnimation(string configName, AnimationClip animation, float speed = 1) { 
            abilityManager.StartAnimation(this, configName, animation, speed);
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
        /// <returns>The result of this operation</returns>
        internal bool EnqueueAbility(string configName)
        {
            try{
                if (IsReady(configName))
                {
                    ConfigName = configName;
                    Config = configs[configName];
                    Channel = channels[configName];
                    Runtime = runtimes[configName];
                    Runtime.isRunning = true;
                    executing.Add(configName);
                    OnEnqueue();
                    
                    AbilityExecutor.EnqueueAction(new AbilityInstance(this, configName));
                    return true;
                }
            }catch(Exception e)
            {
                Debug.LogError($"Ability '{GetType().FullName}' Enqueue error:\n {e}", abilityManager);
                runtimes[configName].isRunning = false;
                executing.Remove(configName);
            }
            
            return false;
        }

        /// <summary>
        /// Execute the config with the provided name. Assumes the action is already in action queue and is only being called by ActionOverseer.
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        internal bool Execute(string configName)
        {
            try
            {
                ConfigName = configName;
                Config = configs[configName];
                Channel = channels[configName];
                Runtime = runtimes[configName];
                if (Runtime.isSuspended) {
                    Runtime.isSuspended = false;
                    return false;
                }

                bool result = Action();
                if (!result)
                {
                    Runtime.isRunning = false;
                    Runtime.timeWhenAvailable = Time.time + Config.CoolDown;
                    executing.Remove(configName);
                    OnActionFinish();
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ability '{GetType().FullName}' failed to execute with config  {configName}:\n {ex}", abilityManager);
                runtimes[configName].isRunning = false;
                executing.Remove(configName);
            }
            return false;
        }

        // Get the number of configs currently running
        public int Running { get { return executing.Count; } }

        /// <summary>
        /// Suspend the execution of the specified configuration and casuing it to finish at the current frame
        /// </summary>
        /// <param name="configName"> Name of the configuration to terminate </param>
        /// <returns> true if the config exists and is suspended, otherwise false </returns>
        public bool SuspendInstance(string configName)
        {
            if (!configs.ContainsKey(configName))
            {
                return false;
            }
            AbilityRuntime runtime = runtimes[configName];
            if (!runtime.isRunning)
            {
                return true;
            }
            runtime.isSuspended = true;
            runtime.isRunning = false;
            executing.Remove(configName);
            runtime.timeWhenAvailable = Time.time + configs[configName].CoolDown;

            ConfigName = configName;
            Config = configs[configName];
            Channel = channels[configName];
            Runtime = runtime;
            OnActionFinish();
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
        /// Callback to initialize the ability runtime environment
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Check if the provided config is executing, this method will return false if the config is not present
        /// </summary>
        /// <param name="name"> The name of the config to be examined </param>
        /// <returns> true if the specified config is executing, otherwise false </returns>
        public bool IsExecuting(string name)
        {
            return executing.Contains(name);
        }

        /// <summary>
        /// Check if the ability with specified config name is ready
        /// </summary>
        /// <param name="name">The name of the ability config</param>
        /// <returns>true if config with specified name exists and is ready, false otherwise</returns>
        public bool IsReady(string name)
        {
            if (!configs.ContainsKey(name))
            {
                Debug.LogError(GetType().ToString() + " does not have config with name '" + name + "'", this);
                return false;
            }
            Config = configs[name];
            Channel = channels[name];
            Runtime = runtimes[name];
            return !Runtime.IsRunning && (!Config.UseCooldown || !Runtime.OnCooldown) && ConditionSatisfied();
        }

        /// <summary>
        /// Initialize ability runtime environments
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
            Type runtimeType = type.Assembly.GetType(type.FullName + "Runtime");

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
                
                AbilityRuntime runtime = (AbilityRuntime)Activator.CreateInstance(runtimeType);
                runtime.ability = this;
                runtimes[name] = runtime;
                runtime.Initialize();
            }
            foreach (string key in removed)
            {
                configs.Remove(key);
            }
        }

        /// <summary>
        /// Clean up ability runtime environments
        /// </summary>
        internal void OnClose()
        {
            Clear();
            foreach (AbilityRuntime runtime in runtimes.Values)
            {
                runtime.Clear();
            }
        }

        // Reset, called when the parent component is resetted
        internal void ResetStatus()
        {
            Clear();
            Initialize();
        }

        /// <summary>
        /// Main body of the ability execution, implement this to create different abilities!
        /// </summary>
        /// <returns>False if the ability has finished, otherwise true</returns>
        protected abstract bool Action();

        /// <summary>
        /// Callback when the action is finished or halted, override this to clean up temporary data generated during the action.
        /// </summary>
        /// <param name="config">The config being processed</param>
        protected virtual void OnActionFinish() { }

        /// <summary>
        /// Callback when the animation of the ability is interrupted by other abilities. Useful when abilities relies on animation events.
        /// Default implementation suspends the ability.
        /// </summary>
        protected virtual void OnAnimationInterrupt(AnimancerState state) { state.Speed = 1; SuspendInstance(ConfigName); }

        /// <summary>
        /// Interrupt the animation of this ability
        /// </summary>
        /// <param name="configName">Name of the ability configuration</param>
        internal void AnimationInterrupt(string configName, AnimancerState state)
        {
            if (!configs.ContainsKey(configName)) { return; }
            ConfigName = configName;
            Config = configs[configName];
            Runtime = runtimes[configName];
            Channel = channels[configName];
            OnAnimationInterrupt(state);
        }
        /// <summary>
        /// Callback when the ability is added to the action executing queue
        /// </summary>
        protected virtual void OnEnqueue() { }


        /// <summary>
        /// Attempt to join the current running ability with another ability that is running. 
        /// On success, the current running ability will terminate no later than the joined ability.
        /// </summary>
        /// <typeparam name="T">The type of the ability to be joined with</typeparam>
        /// <param name="configName">The name of the configuration of the running ability to be joined</param>
        /// <returns> Return true on success, otherwise false </returns>
        protected bool JoinAsSecondary<T>(string configName) where T : Ability
        {
            return abilityManager.JoinAbilities(typeof(T), GetType(), configName, ConfigName);
        }

        /// <summary>
        /// Attempt to join the current running ability with another ability that is running. 
        /// On success, the current running ability will terminate no later than the joined ability.
        /// </summary>
        /// <param name="abilityType">The type of the ability to be joined with</param>
        /// <param name="configName">The name of the configuration of the ability to be joined</param>
        /// <returns> Return true on success, otherwise false</returns>
        protected bool JoinAsSecondary(Type abilityType, string configName)
        {
            return abilityManager.JoinAbilities(abilityType, GetType(), configName, ConfigName);
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

            private Type GetBaseRuntimeType() {
                Type type = GetType().BaseType;
                while (type != typeof(Ability))
                {
                    Type t = type.Assembly.GetType(type.FullName + "Runtime");
                    if (t != null && t.IsSubclassOf(typeof(AbilityRuntime)))
                    {
                        return t;
                    }
                    type = type.BaseType;
                }
                return typeof(AbilityRuntime);
            }

            /// <summary>
            /// Check to see if the ability class has the pipe, config and runtime classes defined in the same namespace
            /// </summary>
            /// <returns>true if all necessities have been defined, false otherwise</returns>
            private bool ComplementariesDefined()
            {
                Type abilityType = GetType();
                string typeName = abilityType.FullName + "Config";
                string pipeName = abilityType.FullName + "Channel";
                string runtimeName = abilityType.FullName + "Runtime";
                Type configType = GetBaseConfigType();
                Type channelType = GetBaseChannelType();
                Type runtimeType = GetBaseRuntimeType();
                bool config = false;
                bool pipe = false;
                bool runtime = false;

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
                    } else if (name.Equals(runtimeName) && type.IsSubclassOf(runtimeType)) {
                        runtime = true;
                    }
                    if (config && pipe && runtime)
                    {
                        return true;
                    }
                }
                return false;
            }
            #endregion

        public bool HasConfig(string configName)
        {
            return configs.ContainsKey(configName);
        }


        #region Signal Handlers
        /// <summary>
        /// Signal this ability.
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="animationEvent"></param>
        public void Signal(string configName, AnimationEvent animationEvent)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && runtimes[configName].isRunning)
            {
                ConfigName = configName;
                Config = config;
                Channel = channels[configName];
                Runtime = runtimes[configName];
                OnSignaled(animationEvent);
            }
        }
        public void Signal(string configName)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && runtimes[configName].isRunning)
            {
                ConfigName = configName;
                Config = config;
                Channel = channels[configName];
                Runtime = runtimes[configName];
                OnSignaled();
            }
        }
        public void Signal(string configName, int num)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && runtimes[configName].isRunning)
            {
                ConfigName = configName;
                Config = config;
                Channel = channels[configName];
                Runtime = runtimes[configName];
                OnSignaled(num);
            }
        }
        public void Signal(string configName, bool flag)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && runtimes[configName].isRunning)
            {
                ConfigName = configName;
                Config = config;
                Channel = channels[configName];
                Runtime = runtimes[configName];
                OnSignaled(flag);
            }
        }
        public void Signal(string configName, string text)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && runtimes[configName].isRunning)
            {
                ConfigName = configName;
                Config = config;
                Channel = channels[configName];
                Runtime = runtimes[configName];
                OnSignaled(text);
            }
        }
        public void Signal(string configName, UnityEngine.Object obj)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config) && runtimes[configName].isRunning)
            {
                ConfigName = configName;
                Config = config;
                Channel = channels[configName];
                Runtime = runtimes[configName];
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
    ///  A configuration of the Ability, each configuration has its own settings that affects the execution of the Ability.
    ///  This class should be subclassed inside subclasses of Ability with name 'Ability_Subclass_Name'Runtime.
    ///  i.e CircleAttack which inherit from Ability must define a class named CircleAttackConfig inherited from this class within the same namespace as CircleAttack
    /// </summary>
    public class AbilityRuntime
    {
        internal protected Ability ability;
        internal bool isRunning = false;
        internal float timeWhenAvailable = 0;
        
        public bool IsRunning { get { return isRunning; } }
        public bool OnCooldown { get { return Time.time < timeWhenAvailable; } }

        /// <summary>
        /// Flag indicate whether this ability is suspended
        /// </summary>
        internal bool isSuspended = false;

        /// <summary>
        /// Callback to clean up the runtime environment
        /// </summary>
        protected internal virtual void Clear() { }

        /// <summary>
        /// Callback to initialize the runtime environment
        /// </summary>
        protected internal virtual void Initialize() { }
    }

    /// <summary>
    /// Communication channel to interact with the ability, must be defined for each ability
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
    /// Runtime environment of an instance of the ability, must be defined for each ability
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
        public AbilityInstance(Ability ability, string config)
        {
            this.ability = ability;
            this.configName = config;
        }

        public bool StopAbility()
        {
            return ability.SuspendInstance(configName);
        }
    }

    [Serializable]
    public class AbilityDictionary : SerializableDictionary<string, Ability> { }

    [Serializable]
    public class AbilityConfigDictionary : SerializableDictionary<string, AbilityConfig> { }
    #endregion
}
