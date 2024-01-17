using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using LobsterFramework.Utility;
using Animancer;

namespace LobsterFramework.AbilitySystem {
    /// <summary>
    /// Abilities defines the kind of actions the parent object can make. <br/>
    /// Each subclass of Ability defines its own AbilityConfig and can be runned on multiple instances of its AbilityConfigs.
    /// </summary>
    public abstract class Ability : ScriptableObject
    {
        [HideInInspector]
        protected internal AbilityRunner abilityRunner;

        public RefAbilityPriority abilityPriority;

        [HideInInspector]
        [SerializeField] internal StringAbilityConfigDictionary configs = new();
        internal Dictionary<string, AbilityPipe> pipes = new();
        internal Dictionary<string, AbilityRuntime> runtimes = new();

        private HashSet<string> executing = new();

        protected string ConfigName { get; private set; }
        protected AbilityConfig Config { get; private set; }
        protected AbilityPipe Pipe { get; private set; }
        protected AbilityRuntime Runtime { get; private set; }

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
            Type t2 = GetBasePipeType();
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

        /// <summary>
        /// Callback when the gameobject is about to be disabled or destroyed
        /// </summary>
        public virtual void CleanUp() { }

        // Comparer used to sort action by their priority
        public int CompareByExecutionPriority(Ability other)
        {
            return abilityPriority.Value.executionPriority - other.abilityPriority.Value.executionPriority;
        }

        public int CompareByEnqueuePriority(Ability other)
        {
            return abilityPriority.Value.enqueuePriority - other.abilityPriority.Value.enqueuePriority;
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
            if (IsReady(configName))
            {
                ConfigName = configName;
                Config = configs[configName];
                Pipe = pipes[configName];
                Runtime = runtimes[configName];
                Runtime.accessKey = AbilityExecutor.EnqueueAction(new AbilityInstance(this, configName));
                executing.Add(configName);
                OnEnqueue();
                return true;
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
                Pipe = pipes[configName];
                Runtime = runtimes[configName];
                bool result = Action();
                if (!result || Runtime.isSuspended)
                {
                    Runtime.accessKey = -1;
                    Runtime.isSuspended = false;
                    executing.Remove(configName);
                    Runtime.timeWhenAvailable = Time.time + Config.CoolDown;
                    OnActionFinish();
                    return false;
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError("Action '" + GetType().ToString() + "' failed to execute with config + '" + configName + "':\n " + ex.ToString());
            }
            return false;
        }

        // Get the number of configs currently running
        public int RunningCount { get { return executing.Count; } }

        /// <summary>
        /// Suspend the execution of provided action and force it to finish at the current frame
        /// </summary>
        /// <param name="name"> Name of the configuration to terminate </param>
        /// <returns> true if the config exists and is halted, otherwise false </returns>
        public bool HaltAbilityExecution(string name)
        {
            if (!configs.ContainsKey(name))
            {
                return false;
            }
            AbilityRuntime runtime = runtimes[name];
            if (runtime.accessKey == -1)
            {
                return true;
            }
            AbilityExecutor.RemoveAction(runtime.accessKey);
            runtime.accessKey = -1;
            runtime.isSuspended = false;
            executing.Remove(name);
            runtime.timeWhenAvailable = Time.time + configs[name].CoolDown;
            OnActionFinish();
            return true;
        }

        /// <summary>
        /// Halt the execution of all configs
        /// </summary>
        public void HaltOnAllConfigs()
        {
            foreach (string name in configs.Keys)
            {
                HaltAbilityExecution(name);
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
            Pipe = pipes[name];
            Runtime = runtimes[name];
            return !Runtime.IsExecuting && (!Config.UseCooldown || !Runtime.OnCooldown) && ConditionSatisfied();
        }

        /// <summary>
        /// Initialize ability runtime environments
        /// </summary>
        internal void OnOpen()
        {
            if (!ComplementariesDefined())
            {
                Debug.LogError("The ability config or pipe for '" + GetType() + "' is not declared!");
            }
            Initialize();
            Type type = GetType();
            Type pipeType = type.Assembly.GetType(type.FullName + "Pipe");
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
                pipes[name] = (AbilityPipe)Activator.CreateInstance(pipeType);
                pipes[name].Construct(config);
                
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
            CleanUp();
            foreach (AbilityRuntime runtime in runtimes.Values)
            {
                runtime.Close();
            }
        }

        // Reset, called when the parent component is resetted
        internal void ResetStatus()
        {
            CleanUp();
            Initialize();
        }

        public void SaveConfigsAsAsset()
        {
            foreach (AbilityConfig config in configs.Values)
            {
                AssetDatabase.AddObjectToAsset(config, this);
            }
        }
        /// <summary>
        /// Main body of the ability execution, implement this to create different abilities!
        /// </summary>
        /// <param name="config">The config being executed with</param>
        /// <returns>False if the ability has finished, otherwise true</returns>
        protected abstract bool Action();

        /// <summary>
        /// Callback when the action is finished or halted, override this to clean up temporary data generated during the action.
        /// </summary>
        /// <param name="config">The config being processed</param>
        protected virtual void OnActionFinish() { }

        /// <summary>
        /// Callback when the animation of the ability is interrupted by other abilities. Useful when abilities relies on animation events.
        /// </summary>
        /// <param name="config"></param>
        protected virtual void OnAnimationInterrupt(AnimancerState state) { state.Speed = 1; HaltAbilityExecution(ConfigName); }

        /// <summary>
        /// Interrupt the animation of the currently animating AbilityConfig pair
        /// </summary>
        /// <param name="configName"></param>
        internal void AnimationInterrupt(string configName, AnimancerState state)
        {
            if (!configs.ContainsKey(configName)) { return; }
            ConfigName = configName;
            Config = configs[configName];
            OnAnimationInterrupt(state);
        }
        /// <summary>
        /// Callback when the ability is added to the action executing queue
        /// </summary>
        /// <param name="config"></param>
        /// <param name="configName"></param>
        protected virtual void OnEnqueue() { }


        /// <summary>
        /// Attempt to join the current running ability with another ability that is running. 
        /// On success, the current running ability will terminate no later than the joined ability.
        /// </summary>
        /// <typeparam name="T">The type of the ability to be joined with</typeparam>
        /// <param name="configName">The name of the config of the running ability to be joined</param>
        /// <returns> Return true on success, otherwise false </returns>
        protected bool JoinAsSecondary<T>(string configName) where T : Ability
        {
            return abilityRunner.JoinAbilities(typeof(T), GetType(), configName, ConfigName);
        }

        protected bool JoinAsSecondary(Type abilityType, string configName)
        {
            return abilityRunner.JoinAbilities(abilityType, GetType(), configName, ConfigName);
        }

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

        private Type GetBasePipeType()
        {
            Type type = GetType().BaseType;
            while (type != typeof(Ability))
            {
                Type t = type.Assembly.GetType(type.FullName + "Pipe");
                if (t != null && t.IsSubclassOf(typeof(AbilityPipe)))
                {
                    return t;
                }
                type = type.BaseType;
            }
            return typeof(AbilityPipe);
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
            string pipeName = abilityType.FullName + "Pipe";
            string runtimeName = abilityType.FullName + "Runtime";
            Type configType = GetBaseConfigType();
            Type pipeType = GetBasePipeType();
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
                else if (name.Equals(pipeName) && type.IsSubclassOf(pipeType))
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

        public bool HasConfig(string configName)
        {
            return configs.ContainsKey(configName);
        }

        public void Signal(string configName, AnimationEvent animationEvent)
        {
            if (configs.TryGetValue(configName, out AbilityConfig config))
            {
                ConfigName = configName;
                Config = config;
                Signal(animationEvent);
            }
        }

        /// <summary>
        /// Override this to implement signal event handler
        /// </summary>
        /// <param name="config">Config to be signaled</param>
        protected virtual void Signal(AnimationEvent animationEvent) { }

        public AbilityPipe GetAbilityPipe(string configName)
        {
            if (configs.ContainsKey(configName))
            {
                return pipes[configName];
            }
            return default;
        }
    }
    /// <summary>
    ///  A configuration of the Ability, each configuration has its own settings that affects the execution of the Ability.
    ///  This class should be subclassed inside subclasses of Ability with name 'Ability_Subclass_Name'Runtime.
    ///  i.e CircleAttack which inherit from Ability must define a class named CircleAttackConfig inherited from this class within the same namespace as CircleAttack
    /// </summary>
    public class AbilityRuntime
    {
        internal protected Ability ability;
        internal int accessKey = -1;
        internal float timeWhenAvailable = 0;
        
        public bool IsExecuting { get { return accessKey != -1; } }
        public bool OnCooldown { get { return Time.time < timeWhenAvailable; } }

        /// <summary>
        /// Whether this ability has been halted, set this to true while the ability is running will terminate it
        /// </summary>
        internal bool isSuspended = false;

        /// <summary>
        /// Callback to clean up the runtime environment
        /// </summary>
        protected internal virtual void Close() { }

        /// <summary>
        /// Callback to initialize the runtime environment
        /// </summary>
        protected internal virtual void Initialize() { }
    }

    /// <summary>
    /// Communication channel to interact with the ability, must be defined for each ability
    /// </summary>
    public class AbilityPipe
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

        public bool HaltAbility()
        {
            return ability.HaltAbilityExecution(configName);
        }
    }
}
