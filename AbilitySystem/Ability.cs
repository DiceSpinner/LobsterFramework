using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
        public const string DefaultAbilityInstance = "default";

        [Tooltip("The priority in which this ability will be executed in relation with other abilities. Higher prioritied abilities will be executed first.")]
        [SerializeField] internal int executionPriority;
        [SerializeField, HideInInspector] internal AbilityConfigDictionary configs = new();
        internal Dictionary<string, AbilityChannel> channels = new();
        internal Dictionary<string, AbilityContext> contexts = new();
        internal HashSet<AbilityInstance> joined = new();
        internal HashSet<AbilityInstance> joinedBy = new();

        internal protected AbilityManager AbilityManager { get; internal set; }

        /// <summary>
        /// The priority in which this ability will be executed. Higher number means earlier execution in relation to other abilities.
        /// </summary>
        public int ExecutionPriority { get { return executionPriority; } }

        /// <summary>
        /// The name of the currently running ability instance
        /// </summary>
        protected string Instance { get; private set; }
        /// <summary>
        /// The configuration of the currently executing ability instance
        /// </summary>
        protected AbilityConfig Config { get; private set; }

        /// <summary>
        /// The communication channel with client code of the currently executing ability instance
        /// </summary>
        protected AbilityChannel Channel { get; private set; }

        /// <summary>
        /// The runtime context object of the currently executing ability instance
        /// </summary>
        protected AbilityContext Context { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// Add an ability instance to this ability.
        /// </summary>
        /// <param name="name">Name of the ability instance</param>
        /// <returns>true if the ability instance with specified name did not exist already and is successfully created and added, false otherwise</returns>
        internal bool AddInstance(string name) {
            if (configs.ContainsKey(name)) {
                return false;
            }
            AbilityConfig config = AddAbilityMenuAttribute.CreateAbilityConfig(GetType());
            configs[name] = config;
            if (EditorUtility.IsPersistent(this)) {
                AssetDatabase.AddObjectToAsset(config, this);
            }
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

        internal void RemoveAllInstances() { 
            foreach(var item in configs.Values) { DestroyImmediate(item, true); }
            configs.Clear();
        }

        internal void SaveConfigsAsAsset()
        {
            foreach (AbilityConfig config in configs.Values)
            {
                AssetDatabase.AddObjectToAsset(config, this);
            }
        }

        internal void DisplayCurrentExecutingInstances() {
            string ability = GetType().Name;
            foreach (var channel in channels)
            {
                if (channel.Value.IsRunning) {
                    EditorGUILayout.LabelField(ability, channel.Key);
                }
            }
        }

#endif

        #region Startup and termination handling
        /// <summary>
        /// Initialize local references, sets up ability contexts and channels.
        /// </summary>
        internal void Activate()
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

                AbilityContext context = AddAbilityMenuAttribute.CreateAbilityContext(type); ;
                contexts[name] = context;

                AbilityChannel channel = AddAbilityMenuAttribute.CreateAbilityChannel(type);
                channels[name] = channel;
                channel.TimeWhenAvailable = 0;
                channel.config = config;
                channel.context = context;

                SetContext(name);
                InitializeContext();
            }
            foreach (string key in removed)
            {
                configs.Remove(key);
            }
        }

        /// <summary>
        /// Called to initialize the references shared by all ability instances
        /// </summary>
        protected virtual void InitializeSharedReferences() { }

        /// <summary>
        /// Called to initialize ability context and channels
        /// </summary>
        protected virtual void InitializeContext() { }

        /// <summary>
        /// Called when <see cref="AbilityData"/> becomes inactive, this happens when <see cref="AbilitySystem.AbilityManager"/> is disabled.
        /// </summary>
        internal void OnBecomeInactive()
        {
            foreach (string instance in configs.Keys) {
                SetContext(instance);
                FinalizeContext();
            }

            SetContext(default);
            FinalizeSharedReferences();
        }
        /// <summary>
        /// Called to finialize the references shared by all ability instances
        /// </summary>
        protected virtual void FinalizeSharedReferences() { }

        /// <summary>
        /// Called to finialize the ability context and channels
        /// </summary>
        protected virtual void FinalizeContext() { }

        internal void ResetStatus()
        {
            FinalizeSharedReferences();
            InitializeSharedReferences();
        }
        #endregion

        #region Actions
        protected AnimancerState StartAnimation(AnimationClip animation, float speed = 1)
        {
            return AbilityManager.StartAnimation(this, Instance, animation, speed);
        }

        /// <summary>
        /// Attempts to get the reference of the specified component type from <see cref="AbilitySystem.AbilityManager"/>. 
        /// The type of the reference should be one of the required types applied via <see cref="RequireComponentReferenceAttribute"/> on this ability class.
        /// </summary>
        /// <typeparam name="T">The type of the component looking for</typeparam>
        /// <param name="index">The index to the list of components of the type specified. Use of type safe enum is strongly recommended.</param>
        /// <returns>The component reference stored in <see cref="AbilitySystem.AbilityManager"/> if it exists, otherwise null</returns>
        /// <remarks>This is a shorthand call for <see cref="ReferenceProvider.GetComponentReference{T}(Type, int)"/> via <see cref="AbilityManager"/></remarks>
        protected T GetComponentReference<T>(int index=0) where T : Component {
            return AbilityManager.GetComponentReference<T>(GetType(), index);
        }

        /// <summary>
        /// Attempts to get the reference of the specified <see cref="AbilityComponent"/> stored in the same <see cref="AbilityData"/>
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="AbilityComponent"/> being asked for</typeparam>
        /// <returns>The reference to the <see cref="AbilityComponent"/> if exists</returns>
        /// <remarks>This is a shorthand call for <see cref="AbilityManager.GetAbilityComponent{T}"/> via <see cref="AbilityManager"/></remarks>
        protected T GetAbilityComponent<T>() where T : AbilityComponent { 
            return AbilityManager.GetAbilityComponent<T>();
        }

        /// <summary>
        /// Suspend the execution of the specified ability instance and causing it to finish during the next suspension event.
        /// </summary>
        /// <param name="instance"> Name of the configuration of the ability instance to terminate </param>
        /// <returns> true if the configuration exists and is not running or suspended, otherwise false </returns>
        internal protected bool SuspendInstance(string instance)
        {
            try {
                var channel = channels[instance];
                if (!channel.IsRunning || channel.IsSuspended) {
                    return false;
                }
                channel.IsSuspended = true;
                AbilityInstanceManagement.SuspendInstance(new(this, instance));
                foreach (var abilityInstance in joined) {
                    abilityInstance.ability.joinedBy.Remove(new(this, instance));
                }

                foreach(var abilityInstance in joinedBy) {
                    abilityInstance.ability.joined.Remove(new(this, instance));
                    abilityInstance.ability.SuspendInstance(abilityInstance.name);
                }
                joinedBy.Clear();
                return true;
            } 
            catch (KeyNotFoundException) {
                return false;
            }
        }

        /// <summary>
        /// Suspend the execution of all running instances of this ability
        /// </summary>
        internal protected void SuspendAll()
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
            if (AbilityManager.abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability)) {
                if (ability.configs.ContainsKey(instance)) {
                    ability.joinedBy.Add(new(this, Instance));
                    joined.Add(new(ability, instance));
                    return true;
                }
                return false;
            }
            return false;
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
            if (abilityType == null) {
                return false;
            }

            if (AbilityManager.abilities.TryGetValue(abilityType.AssemblyQualifiedName, out Ability ability))
            {
                if (ability.configs.ContainsKey(instance))
                {
                    ability.joinedBy.Add(new(this, Instance));
                    joined.Add(new(ability, instance));
                    return true;
                }
                return false;
            }
            return false;
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
                if (IsReady(instance)) // SetContext() called here
                {
                    Channel.IsRunning = true;
                    AbilityInstanceManagement.EnqueueAction(new AbilityInstance(this, instance));
                    OnAbilityEnqueue();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' Enqueue error:", AbilityManager);
                Debug.LogException(e);
                SuspendInstance(instance);
            }

            return false;
        }

        #region AbilityInstanceManagement
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
                if (Action())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' failed to execute with instance {instanceName}:\n", AbilityManager);
                Debug.LogException(ex);
            }
            SuspendInstance(instanceName);
            return false; 
        }

        internal void Suspend(string instanceName)
        {
            try
            {
                SetContext(instanceName);
                OnAbilityFinish();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ability '{GetType().FullName}' failed to execute with instance {instanceName}:\n");
                Debug.LogException(ex);
            }
            Channel.TimeWhenAvailable = Time.time + Config.CoolDown;
            Channel.IsSuspended = false;
            Channel.IsRunning = false;
            AbilityManager.RegisterSuspendedAbilityInstance(new(this, Instance));
        }
        #endregion

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
            if (channels.TryGetValue(configName, out var channel))
            {
                return channel;
            }
            return default;
        }
        #endregion

        #region Query
        /// <summary>
        /// Check if the ability instance is executing, this method will return false if the instance is not present
        /// </summary>
        /// <param name="instance"> The name of the instance to be examined </param>
        /// <returns> true if the specified instance is executing, otherwise false </returns>
        public bool IsRunning(string instance)
        {
            if (channels.TryGetValue(instance, out var channel)) {
                return channel.IsRunning;
            }
            return false;
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
                return !Channel.IsRunning && !Channel.OnCooldown && ConditionSatisfied();
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
        /// Called when the ability is added to the queue for execution
        /// </summary>
        protected virtual void OnAbilityEnqueue() { }

        /// <summary>
        /// Called when the ability is finished or halted.
        /// </summary>
        protected virtual void OnAbilityFinish() { }

        /// <summary>
        /// Called when the animation of the ability is interrupted by other abilities. Useful when abilities relies on animation events.
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
                if (Channel.IsRunning)
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
                if (Channel.IsRunning)
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

    #region Complementary classes (Must be defined for each Ability that can be instantiazed)
    /// <summary>
    ///  The runtime context of the ability. Not accessible from outside.
    ///  When creating a new ability, you must also declare a class named "AbilityName"Context in the same namespace where "AbilityName" is the name of the ability you're creating.
    /// </summary>
    public class AbilityContext { }

    /// <summary>
    /// Communication channel of the ability. Can be used to control ability behaviors at runtime.
    /// When creating a new ability, you must also declare a class named "AbilityName"Channel in the same namespace where "AbilityName" is the name of the ability you're creating.
    /// Can be accessed via <see cref="AbilityManager.GetAbilityChannel{T}(string)"/>
    /// </summary>
    public class AbilityChannel
    {
        internal protected AbilityContext context;
        internal protected AbilityConfig config;

        public bool IsRunning { get; internal set; }
        public bool IsSuspended { get; internal set; }
        public float TimeWhenAvailable { get; internal set; }
        public bool OnCooldown { get { return config.UseCooldown && Time.time < TimeWhenAvailable; } } 
        public float Cooldown { get { return config.CoolDown; } }
    }

    /// <summary>
    /// The configuration of the ability that will appear in the ability inspector.
    /// When creating a new ability, you must also declare a class named "AbilityName"Config in the same namespace where "AbilityName" is the name of the ability you're creating.
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

        private void OnValidate()
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
    /// Represents an instance of ability to be executed by <see cref="AbilityInstanceManagement"></see>
    /// </summary>
    internal readonly struct AbilityInstance
    {
        public readonly string name;
        public readonly Ability ability;
        public AbilityInstance(Ability ability, string name)
        {
            this.ability = ability;
            this.name = name;
        }

        public bool IsNullAbility { get { return ability == null; } }

        public bool Equals(AbilityInstance instance) { return ability == instance.ability && name == instance.name;  }
    }

    [Serializable]
    public class AbilityDictionary : SerializableDictionary<string, Ability> { }

    [Serializable]
    public class AbilityConfigDictionary : SerializableDictionary<string, AbilityConfig> { }
    #endregion
}
