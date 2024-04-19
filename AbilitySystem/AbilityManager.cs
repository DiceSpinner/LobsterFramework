using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.Utility;
using Animancer;

namespace LobsterFramework.AbilitySystem {
    /// <summary>
    /// Manages running and stopping abilities.
    /// </summary>
    [AddComponentMenu("AbilityManager")]
    public class AbilityManager : SubLevelComponent
    {
        #region Setting
        /// <summary>
        /// The input data that defines the set of abilities the parent entity can execute
        /// </summary>
        [SerializeField, DisableEditInPlayMode] private AbilityData inputData;
        /// <summary>
        /// The context ability data that can be inspected and edited
        /// </summary>
        [SerializeField, HideInInspector] internal AbilityData abilityData;
        internal Dictionary<string, Ability> abilities;
        internal AbilityComponentDictionary components;
        #endregion

        #region Events
        /// <summary>
        /// Invoked when an ability is enqueued, the type of the ability is passed in as parameter.
        /// </summary>
        public Action<Type> onAbilityEnqueue;
        /// <summary>
        /// Invoked when an ability is terminated, the type of the ability is passed in as parameter.
        /// </summary>
        public Action<Type> onAbilityFinished;

        /// <summary>
        /// Invoked when an animation of an ability is initiated.
        /// </summary>
        public Action<Type> onAnimationBegin;

        /// <summary>
        /// Invoked when an animation of an ability is terminated.
        /// </summary>
        public Action<Type> onAnimationEnd;
        #endregion

        // Abilities running
        private readonly Dictionary<AbilityInstance, AbilityInstance> jointlyRunning = new();

        //Animation
        private AnimancerState currentState;
        private AbilityInstance animating;
        private AnimancerComponent animancer;

        // Status
        /// <summary>
        /// If this evaluates to true, all abilities are halted and no abilities can be enqueued.
        /// </summary>
        public readonly OrValue actionLock = new(false);

        /// <summary>
        /// A short cut for examining the value of actionLock
        /// </summary>
        public bool ActionBlocked => actionLock.Value;

        // Entity
        private Entity entity;
        private CombinedValueEffector<bool> postureBrokenActionBlock;

        #region Initialization & Update
        private void Awake()
        {
            entity = GetComponentInBoth<Entity>();
            animancer = GetComponent<AnimancerComponent>();
            postureBrokenActionBlock = actionLock.MakeEffector();
            actionLock.onValueChanged += OnActionStateChanged;
        }

        private void OnDisable()
        {
            actionLock.ClearEffectors();
            SuspendAbilities();
            if (abilityData != null)
            {
                abilityData.FinalizeContext();
            }

            if (entity != null)
            {
                entity.onPostureStatusChange -= OnPostureStatusChange;
            }
        }

        private void OnEnable()
        {
            if (abilityData == null)
            {
                if (inputData == null) {
                    Debug.LogWarning("Ability Data is not set!", gameObject);
                    return;
                }
                abilityData = inputData.Clone();
            }

            abilityData.Begin(this);

            if (entity != null)
            {
                entity.onPostureStatusChange += OnPostureStatusChange;
            }
        }

        private void Update()
        {
            foreach (AbilityComponent component in components.Values)
            {
                component.Update();
            }
        }

        internal void OnAbilityFinish(AbilityInstance instance) { 
            if (onAbilityFinished != null)
            {
                onAbilityFinished.Invoke(instance.ability.GetType());
            }

            if (animating.ability == instance.ability && animating.configName == instance.configName)
            {
                animating = default;
                onAnimationEnd?.Invoke(instance.ability.GetType());
            }

            if (jointlyRunning.ContainsKey(instance))
            {
                AbilityInstance joined = jointlyRunning[instance];
                jointlyRunning.Remove(instance);
                joined.StopAbility();
            }
        }

        /// <summary>
        /// Reset the status of all abilities and their configs to their initial state
        /// </summary>
        public void Reset()
        {
            if (components == null || abilities == null)
            {
                return;
            }
            SuspendAbilities();

            foreach (AbilityComponent component in components.Values)
            {
                component.Reset();
            }
            foreach (Ability ability in abilities.Values)
            {
                ability.ResetStatus();
            }
        }

        /// <summary>
        /// Block Action if posture broken, unblock if posture recovered
        /// </summary>
        /// <param name="postureBroken"></param>
        private void OnPostureStatusChange(bool postureBroken)
        {
            if (postureBroken)
            {
                postureBrokenActionBlock.Apply(true);
            }
            else
            {
                postureBrokenActionBlock.Release();
            }
        }
        private void OnActionStateChanged(bool isBlocked)
        {
            if (isBlocked)
            {
                SuspendAbilities();
            }
        }
        #endregion

        #region EnqueueAbility
        /// <summary>
        /// Add an ability instance to the executing queue, return the status of this operation. <br/>
        /// For this operation to be successful, the following must be satisfied: <br/>
        /// 1. The entity must not be action blocked. <br/>
        /// 2. The specified ability instance must be present <br/>
        /// 3. The precondition of the specified ability instance must be satisfied. <br/>
        /// 4. The ability instance must not be currently running or enqueued. <br/>
        /// <br/>
        /// Note that this method should only be called inside Update(), calling it elsewhere will result in undefined behavior.
        /// </summary>
        /// <typeparam name="T">Type of the Ability to be enqueued</typeparam>
        /// <param name="instance">Name of the instance to be enqueued</param>
        /// <returns>true if successfully enqueued the ability instance, false otherwise</returns>
        public bool EnqueueAbility<T>(string instance=AbilityData.defaultAbilityInstance) where T : Ability
        {
            if (ActionBlocked)
            {
                return false;
            }
            T ability = GetAbility<T>();
            if (ability == default)
            {
                return false;
            }
            if (ability.EnqueueAbility(instance))
            {
                onAbilityEnqueue?.Invoke(typeof(T));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add an ability instance to the executing queue, return the status of this operation. <br/>
        /// For this operation to be successful, the following must be satisfied: <br/>
        /// 1. The entity must not be action blocked. <br/>
        /// 2. The specified ability instance must be present <br/>
        /// 3. The precondition of the specified ability instance must be satisfied. <br/>
        /// 4. The ability instance must not be currently running or enqueued. <br/>
        /// <br/>
        /// Note that this method should only be called inside Update(), calling it elsewhere will result in undefined behavior.
        /// </summary>
        /// <param name="abilityType">Type of the ability to be enqueued</param>
        /// <param name="instance">Name of the instance to be enqueued</param>
        /// <returns>true if successfully enqueued the ability instance, false otherwise</returns>
        public bool EnqueueAbility(Type abilityType, string instance=AbilityData.defaultAbilityInstance) {
            if (ActionBlocked)
            {
                return false;
            }
            Ability action = GetAbility(abilityType);
            if (action == default)
            {
                return false;
            }
            if (action.EnqueueAbility(instance))
            {
                onAbilityEnqueue?.Invoke(abilityType);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enqueue two abilities of different types together with the second one being guaranteed to terminate no later than the first one. 
        /// </summary>
        /// <typeparam name="T"> The type of the first ability </typeparam>
        /// <typeparam name="V"> The type of the second ability </typeparam>
        /// <param name="instance1"> Name of the instance for the first ability </param>
        /// <param name="instance2"> Name of the instance for the second ability </param>
        /// <returns></returns>
        public bool EnqueueAbilitiesInJoint<T, V>(string instance1=AbilityData.defaultAbilityInstance, string instance2=AbilityData.defaultAbilityInstance) 
            where T : Ability 
            where V : Ability
        {
            T a1 = GetAbility<T>();
            V a2 = GetAbility<V>();

            // The two abilities must both be present and not the same
            if (ActionBlocked || a1 == default || a2 == default || a1 == a2) {
                return false; 
            }

            if (a1.IsReady(instance1) && a2.IsReady(instance2)) {
                EnqueueAbility<T>(instance1);
                EnqueueAbility<V>(instance2);
                AbilityInstance p1 = new(a1, instance1);
                AbilityInstance p2 = new(a2, instance2);
                jointlyRunning[p1] = p2;
                return true;
            }
            return false;
        }
        #endregion

        #region Suspend Abilities
        /// <summary>
        /// Stops the execution of the ability and returns the status of this operation
        /// </summary>
        /// <typeparam name="T">Type of the ability to be stopped</typeparam>
        /// <param name="instance">Name of the ability instance to be stopped</param>
        /// <returns>true if the ability instance exists and is stopped, otherwise return false</returns>
        public bool SuspendAbilityInstance<T>(string instance = AbilityData.defaultAbilityInstance) where T : Ability
        {
            Ability ability = GetAbility<T>();
            if (ability != null) {
                return ability.SuspendInstance(instance);
            }
            return false;
        }

        /// <summary>
        /// Stop the execution of all instances of the specified ability
        /// </summary>
        /// <typeparam name="T">The type of the ability to be stopped</typeparam>
        /// <returns>true if the ability exists, otherwise false</returns>
        public bool SuspendAbility<T>() where T : Ability
        {
            Ability ability = GetAbility<T>();
            if (ability == null)
            {
                return false;
            }
            ability.SuspendAll();
            return true;
        }

        /// <summary>
        /// Stops execution of all abilities
        /// </summary>
        public void SuspendAbilities()
        {
            foreach (Ability ability in abilities.Values)
            {
                ability.SuspendAll();
            }
        }
        #endregion

        #region Getters
        /// <summary>
        /// Get the specified ability component if it is present.
        /// </summary>
        /// <typeparam name="T">Type of the AbilityComponent being requested</typeparam>
        /// <returns>Return the ability component if it is present, otherwise null</returns>
        public T GetAbilityComponent<T>() where T : AbilityComponent
        {
            string type = typeof(T).AssemblyQualifiedName;
            if (components.TryGetValue(type, out AbilityComponent stat))
            {
                return (T)stat;
            }
            return default; 
        }

        /// <summary>
        /// Get the ability channel of specified ability and configuration
        /// </summary>
        /// <typeparam name="T">The type of Ability this channel is associated with.</typeparam>
        /// <param name="instance">The name of the ability instance</param>
        /// <returns> The channel that connects to the specified ability and configuration if it exists, otherwise return null. </returns>
        public AbilityChannel GetAbilityChannel<T>(string instance=AbilityData.defaultAbilityInstance) where T : Ability {
            T ability = GetAbility<T>();
            if (ability != null) {
                return ability.GetAbilityChannel(instance);
            }
            return default;
        }

        private T GetAbility<T>() where T : Ability
        {
            try
            {
                string type = typeof(T).AssemblyQualifiedName;
                if (abilities.TryGetValue(type, out Ability ability))
                {
                    return (T)ability;
                }
            }
            catch (NullReferenceException) { }
            return default;
        }

        private Ability GetAbility(Type abilityType)
        {
            try
            {
                string type = abilityType.AssemblyQualifiedName;
                if (abilities.TryGetValue(type, out Ability ability))
                {
                    return ability;
                }
            }
            catch (NullReferenceException) { }
            return default;
        }
        #endregion

        #region Query
        /// <summary>
        /// Check if the specified ability instance is ready
        /// </summary>
        /// <typeparam name="T">Type of the Ability to be queried</typeparam>
        /// <param name="instance">Name of the ability instance to be queried</param>
        /// <returns>true if the ability instance exists and is ready, false otherwise</returns>
        public bool IsAbilityReady<T>(string instance=AbilityData.defaultAbilityInstance) where T : Ability
        {
            string type = typeof(T).AssemblyQualifiedName;
            if (abilities.ContainsKey(type))
            {
                return abilities[type].IsReady(instance);
            }
            return false;
        }

        /// <summary>
        /// Check if the specified ability instance is ready
        /// </summary>
        /// <param name="abilityType">Type of the Ability to be queried</param>
        /// <param name="instance">Name of the ability instance to be queried</param>
        /// <returns>true if the ability instance exists and is ready, false otherwise</returns>
        public bool IsAbilityReady(Type abilityType, string instance = AbilityData.defaultAbilityInstance)
        {
            if (abilityType == null)
            {
                return false;
            }
            string type = abilityType.AssemblyQualifiedName;
            if (abilities.ContainsKey(type))
            {
                return abilities[type].IsReady(instance);
            }
            return false;
        }

        /// <summary>
        /// Check if the ability with specified config is running
        /// </summary>
        /// <typeparam name="T">The type of the ability being queried</typeparam>
        /// <param name="instance">The name of the ability instance being queried</param>
        /// <returns> true if the ability instance exists and is running, otherwise false </returns>
        public bool IsAbilityRunning<T>(string instance=AbilityData.defaultAbilityInstance) where T : Ability {
            T ability = GetAbility<T>();
            if (ability == null) {
                return false;
            }
            return ability.IsRunning(instance);
        }

        /// <summary>
        /// Check if the specified ability instance is running
        /// </summary>
        /// <param name="abilityType">The type of the ability being queried</param>
        /// <param name="configName">The name of the config being queried</param>
        /// <returns> true if the ability instance exists and is running, otherwise false </returns>
        public bool IsAbilityRunning(Type abilityType, string configName = AbilityData.defaultAbilityInstance) 
        {
            Ability ability = GetAbility(abilityType);
            if (ability == null)
            {
                return false;
            }
            return ability.IsRunning(configName);
        }

        /// <summary>
        /// True if a animation of an ability is currently being played, false otherwise
        /// </summary>
        public bool IsAnimating
        {
            get  { return !animating.IsEmpty; }
        }
        #endregion

        #region Other Ability Operations
        /// <summary>
        /// Join two running abilities such that the second ability terminates no later than the primary ability.
        /// </summary>
        /// <param name="primaryAbility">The type of the primary ability</param>
        /// <param name="secondaryAbility">The type of the secondary ability</param>
        /// <param name="instance1">The name of the primary ability instance</param>
        /// <param name="instance2">The name of the secondary ability instance</param>
        /// <returns>true on success, false otherwise</returns>
        public bool JoinAbilities(Type primaryAbility, Type secondaryAbility, string instance1=AbilityData.defaultAbilityInstance, string instance2=AbilityData.defaultAbilityInstance) {
            if (IsAbilityRunning(primaryAbility, instance1) && IsAbilityRunning(secondaryAbility, instance2)) {
                AbilityInstance p1 = new() { ability = GetAbility(primaryAbility), configName = instance1};
                AbilityInstance p2 = new() { ability = GetAbility(secondaryAbility), configName = instance2};
                jointlyRunning[p1] = p2;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Join two running abilities such that the second ability terminates no later than the primary ability.
        /// </summary>
        /// <typeparam name="T">The type of the primary ability</typeparam>
        /// <typeparam name="V">The type of the secondary ability</typeparam>
        /// <param name="instance1">The name of the primary ability instance</param>
        /// <param name="instance2">The name of the secondary ability instance</param>
        /// <returns>true on success, false otherwise</returns>
        public bool JoinAbilities<T, V>(string instance1=AbilityData.defaultAbilityInstance, string instance2=AbilityData.defaultAbilityInstance) where T : Ability where V : Ability
        {
            if (IsAbilityRunning<T>(instance1) && IsAbilityRunning<V>(instance2))
            {
                AbilityInstance p1 = new() { ability = GetAbility<T>(), configName = instance1 };
                AbilityInstance p2 = new() { ability = GetAbility<V>(), configName = instance2 };
                jointlyRunning[p1] = p2;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Used by abilities to initiate animations, will interrupt any currently running animations by other abilities
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="animation"></param>
        internal AnimancerState StartAnimation(Ability ability, string instance, AnimationClip animation, float speed)
        {
            if (animation == null)
            {
                Debug.LogWarning("Cannot play null animation!");
                return null;
            }
            if (speed <= 0)
            {
                speed = 1;
            }
            if (!animating.IsEmpty)
            {
                animating.ability.AnimationInterrupt(animating.configName, currentState);
            }
            animating = new(ability, instance);

            onAnimationBegin?.Invoke(ability.GetType());
            currentState = animancer.Play(animation, 0.3f / speed, FadeMode.FromStart);
            currentState.Speed = speed;
            return currentState;
        }

        /// <summary>
        /// Interrupt the currently playing ability animation, do nothing if no abilities is currently playing animation.
        /// </summary>
        public void InterruptAbilityAnimation()
        {
            if (animating.IsEmpty)
            {
                return;
            }
            Ability ability = animating.ability;
            ability.AnimationInterrupt(animating.configName, currentState);
            animating.ability = null;
            onAnimationEnd?.Invoke(ability.GetType());
        }

        /// <summary>
        /// Send a signal to the specified ability. 
        /// </summary>
        public void Signal<T>(string instance = AbilityData.defaultAbilityInstance) where T : Ability
        {
            Ability ability = GetAbility<T>();
            if (ability == null)
            {
                return;
            }
            ability.Signal(instance);
        }

        /// <summary>
        /// Used by animation events to send signals
        /// </summary>
        public void AnimationSignal(AnimationEvent animationEvent)
        {
            if (animating.IsEmpty) { return; }
            if (animationEvent.animatorClipInfo.clip == currentState.Clip)
            {
                animating.ability.Signal(animating.configName, animationEvent);
            }
        }

        /// <summary>
        /// Used by animation events to signal the end of the ability. The ability will immediately terminate after this call. Do nothing if the event does not belong to the current ability animation.
        /// </summary>
        /// <param name="animationEvent">The animation event instance to be queried</param>
        public void AnimationEnd(AnimationEvent animationEvent)
        {
            if (animating.IsEmpty) { return; }
            if (animationEvent.animatorClipInfo.clip == currentState.Clip)
            {
                animating.ability.SuspendInstance(animating.configName);
            }
        }
        #endregion

#if UNITY_EDITOR
        internal void DisplayCurrentExecutingAbilitiesInEditor() {
            if (Application.isPlaying) {
                foreach (Ability ability in abilities.Values)
                {
                    ability.DisplayCurrentExecutingInstances();
                }
            }
        }

        /// <summary>
        /// Only to be called inside play mode in the editor! Save the current ability data as an asset with specified assetName to the default path.
        /// </summary>
        /// <param name="assetName">Name of the asset to be saved</param>
        public void SaveRuntimeData(string path)
        {
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets/" + path[Application.dataPath.Length..];
            } else if (path == "") {
                path = AssetDatabase.GetAssetPath(inputData);
            }
            else {
                Debug.LogError($"Invalid path {path}, can't save ability data!");
                return;
            } 
            if (abilityData != null)
            {
                AbilityData cloned = abilityData.Clone(); 
                AssetDatabase.CreateAsset(cloned, path);
                cloned.SaveContentsAsAsset();
                inputData = cloned;
            }
        }
#endif
    }

    [Serializable]
    public class AbilityComponentDictionary : SerializableDictionary<string, AbilityComponent> { }
}

