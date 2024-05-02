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
    /// Represents an ability in the ability system. Allows multiple instances to be defined. Can be customized via <see cref="AbilityData"/> inspector.
    /// </summary>
    public abstract class Ability : ScriptableObject
    {
        [Tooltip("The priority in which this ability will be executed in relation with other abilities. Higher prioritied abilities will be executed first.")]
        [field: SerializeField] internal int executionPriority;
        [SerializeField, HideInInspector] internal AbilityConfigDictionary configs = new();
        internal Dictionary<string, AbilityChannel> channels = new();
        internal Dictionary<string, AbilityContext> contexts = new();

        protected internal AbilityManager abilityManager;
        private HashSet<string> runningInstances = new();

        /// <summary>
        /// The priority in which this ability will be executed. Higher number means it will be executed earlier.
        /// </summary>
        public int ExecutionPriority { get { return executionPriority; } }
        protected string Instance { get; private set; }
        protected AbilityConfig Config { get; private set; }
        protected AbilityChannel Channel { get; private set; }
        protected AbilityContext Context { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// Add an ability instance to this ability.
        /// </summary>
        /// <param name="name">Name of the ability instance</param>
        /// <returns>true if the ability instance with specified name did not exist already and is successfully created and added, false otherwise</returns>
        internal bool AddInstance(string name) {
            if (configs.ContainsKey(name) || !AssetDatabase.Contains(this)) {
                return false;
            }
            AbilityConfig config = AddAbilityMenuAttribute.CreateAbilityConfig(GetType());
            configs[name] = config;
            AssetDatabase.AddObjectToAsset(config, this);
            return true;
        }

        /// <summary>
        /// Remove the ability instance with specified name if present, this should only be called by editor scripts
        /// </summary>
        /// <param name="name">Name of the ability instance to be removed</param>
        /// <returns>True if ability instance exists and successfully removed it, false otherwise</returns>
        internal bool RemoveInstance(string name)
        {
            if (!configs.ContainsKey(name) || !AssetDatabase.Contains(this))
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
            if (runningInstances.Count > 0)
            {
                string ability = GetType().Name;
                foreach (string instance in runningInstances)
                {
                    EditorGUILayout.LabelField(ability, instance);
                }
            }
        }
#endif

        #region Startup and termination handling
        /// <summary>
        /// Initialize local references, sets up ability contexts and channels.
        /// </summary>
        internal void Begin()
        {
            SetContext(default);
            InitializeSharedReferences();
            Type type = GetType();

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
                AbilityChannel channel = AddAbilityMenuAttribute.CreateAbilityChannel(type);
                channels[name] = channel;

                AbilityContext context = AddAbilityMenuAttribute.CreateAbilityContext(type); ;
                contexts[name] = context;

                SetContext(name);
                InitializeContext();
            }
            foreach (string key in removed)
            {
                configs.Remove(key);
            }
        }

        /// <summary>
        /// Callback to initialize the references shared by all ability instances
        /// </summary>
        protected virtual void InitializeSharedReferences() { }

        /// <summary>
        /// Callback to initialize ability context and channels
        /// </summary>
        protected virtual void InitializeContext() { }

        /// <summary>
        /// Called when ability manager is disabled
        /// </summary>
        internal void OnClose()
        {
            foreach (string instance in configs.Keys) {
                SetContext(instance);
                FinalizeContext();
            }

            SetContext(default);
            FinalizeSharedReferences();
        }
        /// <summary>
        /// Callback to finialize the references shared by all ability instances
        /// </summary>
        protected virtual void FinalizeSharedReferences() { }

        /// <summary>
        /// Callback to finialize the ability context and channels
        /// </summary>
        protected virtual void FinalizeContext() { }

        internal void ResetStatus()
        {
            FinalizeSharedReferences();
            InitializeSharedReferences();
        }
        #endregion

        #region Comparer
        // Comparer used to sort action by their priority
        public int CompareByExecutionPriority(Ability other)
        {
            return executionPriority - other.executionPriority;
        }
        #endregion

        #region Public Actions
        protected void StartAnimation(string instance, AnimationClip animation, float speed = 1)
        {
            abilityManager.StartAnimation(this, instance, animation, speed);
        }

        /// <summary>
        /// Suspend the execution of the specified ability instance and casuing it to finish at the current frame
        /// </summary>
        /// <param name="instance"> Name of the configuration of the ability instance to terminate </param>
        /// <returns> true if the configuration exists and is not running or suspended, otherwise false </returns>
        public bool SuspendInstance(string instance)
        {
            try
            {
                SetContext(instance);
                if (!Context.isRunning)
                {
                    return true;
                }
                Context.isRunning = false;
                runningInstances.Remove(instance);
                Context.timeWhenAvailable = Time.time + Config.CoolDown;
                OnAbilityFinish();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' failed to finialize with instance {instance}:\n", abilityManager);
                Debug.LogException(ex);
                return false;
            }

            // Update execution info for ability manager
            abilityManager.OnAbilityFinish(new AbilityInstance(this, instance));
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
        #endregion

        #region Internal Actions
        internal void SetContext(string instance) {
            if (instance != default)
            {
                Instance = instance;
                Config = configs[instance];
                Channel = channels[instance];
                Context = contexts[instance];
            }
            else
            {
                Instance = default;
                Config = null;
                Channel = null;
                Context = null;
            }
        }

        /// <summary>
        /// Enqueue the ability if its not on cooldown and conditions are satisfied.  <br/>
        /// This method should not be directly called by external modules such as play input or AI. <br/> 
        /// AbilityRunner.EnqueueAbility&lt;T&gt;(string configName) shoud be used instead.
        /// </summary>
        /// <param name="instance">Name of the ability instance being enqueued</param>
        /// <returns>true if successully enqueued the ability instance for execution, false otherwise</returns>
        internal bool EnqueueAbility(string instance)
        {
            try
            {
                if (IsReady(instance))
                {
                    SetContext(instance);
                    Context.isRunning = true;
                    runningInstances.Add(instance);
                    AbilityExecutor.EnqueueAction(new AbilityInstance(this, instance));

                    OnAbilityEnqueue();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' Enqueue error:", abilityManager);
                Debug.LogException(e);
                SuspendInstance(instance);
            }

            return false;
        }


        /// <summary>
        /// Execute the ability instance
        /// </summary>
        /// <param name="instanceName">Name of the ability instance to be executed</param>
        /// <returns>true if the ability will continue to execute, false otherwise</returns>
        internal bool Execute(string instanceName)
        {
            try
            {
                SetContext(instanceName);
                return Action();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' failed to execute with instance {instanceName}:\n", abilityManager);
                Debug.LogException(ex);
                SuspendInstance(instanceName);
            }
            return false;
        }

        /// <summary>
        /// Send animation interrupt signal to the ability
        /// </summary>
        /// <param name="instance">Name of the ability instance</param>
        internal void AnimationInterrupt(string instance, AnimancerState state)
        {
            if (!configs.ContainsKey(instance)) { return; }
            SetContext(instance);
            OnAnimationInterrupt(state);
        }

        internal AbilityChannel GetAbilityChannel(string configName)
        {
            if (configs.ContainsKey(configName))
            {
                return channels[configName];
            }
            return default;
        }
        #endregion

        #region Query
        /// <summary>
        /// The number of instances of this ability is running
        /// </summary>
        public int InstancesRunning { get { return runningInstances.Count; } }

        /// <summary>
        /// Check if the ability instance is executing, this method will return false if the instance is not present
        /// </summary>
        /// <param name="instance"> The name of the instance to be examined </param>
        /// <returns> true if the specified instance is executing, otherwise false </returns>
        public bool IsRunning(string instance)
        {
            return runningInstances.Contains(instance);
        }

        /// <summary>
        /// Check if the speficied ability instance is ready
        /// </summary>
        /// <param name="instance">The name of the instance of the ability instance</param>
        /// <returns>true if config with specified name exists and is ready, false otherwise</returns>
        public bool IsReady(string instance)
        {
            try {
                SetContext(instance);
                return !Context.IsRunning && (!Config.UseCooldown || !Context.OnCooldown) && ConditionSatisfied();
            } catch (KeyNotFoundException) {
                Debug.LogError(GetType().ToString() + " does not have config with name '" + instance + "'", this);
                return false;
            }
        }

        /// <summary>
        /// Check if the ability has specified configuration
        /// </summary>
        /// <param name="instance">Name of the ability instance being queried</param>
        /// <returns>true if exists, false otherwise</returns>
        public bool HasInstance(string instance)
        {
            return configs.ContainsKey(instance);
        }
        #endregion

        #region Core Ability Implementations
        /// <summary>
        /// Used for doing additional requirement check for running the ability.
        /// </summary>
        /// <returns>true if the condition for this ability has been satisfied, otherwise false</returns>
        protected virtual bool ConditionSatisfied() { return true; }

        /// <summary>
        /// Called every frame while the ability instance remains in the execution queue.
        /// </summary>
        /// <returns>false if the ability has finished and should not execute further, otherwise true</returns>
        protected abstract bool Action();

        /// <summary>
        /// Callback when the ability is added to the queue for execution
        /// </summary>
        protected virtual void OnAbilityEnqueue() { }

        /// <summary>
        /// Callback when the ability is finished or halted.
        /// </summary>
        protected virtual void OnAbilityFinish() { }

        /// <summary>
        /// Callback when the animation of the ability is interrupted by other abilities. Useful when abilities relies on animation events.
        /// Default implementation suspends the ability.
        /// </summary>
        protected virtual void OnAnimationInterrupt(AnimancerState state) { state.Speed = 1; SuspendInstance(Instance); }

        #region Signal Handlers
        /// <summary>
        /// Send an animation signal to this ability.
        /// </summary>
        internal void Signal(string instanceName, AnimationEvent animationEvent)
        {
            try
            {
                SetContext(instanceName);
                if (Context.isRunning)
                {
                    OnSignaled(animationEvent);
                }
            }
            catch (KeyNotFoundException) { }
        }

        /// <summary>
        /// Send a signal to this ability.
        /// </summary>
        internal void Signal(string instanceName)
        {
            try
            {
                SetContext(instanceName);
                if (Context.isRunning)
                {
                    OnSignaled();
                }
            }
            catch (KeyNotFoundException) { }
        }

        /// <summary>
        /// Signal handler for animation event.
        /// </summary>
        protected virtual void OnSignaled(AnimationEvent animationEvent) { }

        /// <summary>
        /// Signal handler for user event.
        /// </summary>
        protected virtual void OnSignaled() { }
        #endregion

        #endregion
    }

    #region Complementary classes (Must be defined for each Ability)
    /// <summary>
    ///  The runtime context of an ability instance. Not accessible from outside.
    ///  Inheritors of this class must have name 'Ability_Subclass_Name'Context.
    /// </summary>
    public class AbilityContext
    {
        internal bool isRunning = false;
        internal float timeWhenAvailable = 0;
        
        public bool IsRunning { get { return isRunning; } }
        public bool OnCooldown { get { return Time.time < timeWhenAvailable; } }
    }

    /// <summary>
    /// Communication channel of an ability Instance. Can be used to control ability behaviors at runtime.
    /// </summary>
    public class AbilityChannel
    {
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
            return ability.IsRunning(configName);
        }

        public bool IsEmpty { get { return ability == null; } }
    }

    [Serializable]
    public class AbilityDictionary : SerializableDictionary<string, Ability> { }

    [Serializable]
    public class AbilityConfigDictionary : SerializableDictionary<string, AbilityConfig> { }
    #endregion
}
