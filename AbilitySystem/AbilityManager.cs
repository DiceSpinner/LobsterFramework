using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.Init;
using LobsterFramework.Utility;
using Animancer;

namespace LobsterFramework.AbilitySystem {
    /// <summary>
    /// Manages running, querying, stopping and interaction with abilities. Takes in an <see cref="AbilityData"/> as input that defines the set of abilities this actor have access to.
    /// </summary>
    /// 
    /// <remarks>
    /// All inqueries and ability manipulations should be done before the LateUpdate event. Doing it during the LateUpdate event could result in race conditions and other undefined behaviors.
    /// At runtime, the input <see cref="AbilityData"/> is duplicated so any modifications done to the original asset will not be reflected. However, the data used at runtime can be edited via the custom inspector of this component and saved as asset.
    /// When a new asset derived from the runtime data is saved to the disk, the input data reference will be redirected to that instead.
    /// The custom inspector does not allow adding or removing ability configurations to avoid breaking the running abilities.
    /// </remarks>
    [AddComponentMenu("AbilityManager")]
    public class AbilityManager : ReferenceProvider
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
        public event Action<Type> OnAbilityEnqueued;
        /// <summary>
        /// Invoked when an ability is terminated, the type of the ability is passed in as parameter.
        /// </summary>
        public event Action<Type> OnAbilityFinished;

        /// <summary>
        /// Invoked when an animation of an ability is initiated.
        /// </summary>
        public event Action<Type> OnAnimationBegin;

        /// <summary>
        /// Invoked when an animation of an ability is terminated.
        /// </summary>
        public event Action<Type> OnAnimationEnd;
        #endregion

        //Animation
        private AnimancerState currentState;

        internal AbilityInstance Animating {
            get; private set;
        }
        private AnimancerComponent animancer;

        // Status
        /// <summary>
        /// If this evaluates to true, all abilities are halted and no abilities can be enqueued.
        /// </summary>
        public readonly OrValue ActionBlocked = new(false);

        #region Initialization & Update
        private void Awake()
        {
            animancer = GetComponent<AnimancerComponent>();
            ActionBlocked.OnValueChanged += OnActionBlockStatusChanged;
        }

        private new void OnValidate()
        {
            if (AttributeInitialization.Finished)
            {                
                Bind(inputData); 
            }
            else {
                void lambda() { Bind(inputData); }
                AttributeInitialization.OnInitializationComplete -= lambda;
                AttributeInitialization.OnInitializationComplete += lambda;
            }
        }

        private void OnActionBlockStatusChanged(bool status) {
            if (status) {
                SuspendAbilities();
            }
        }

        internal void RegisterSuspendedAbilityInstance(AbilityInstance abilityInstance) {
            Type abilityType = abilityInstance.ability.GetType();
            if (Animating.ability == abilityInstance.ability && Animating.name == abilityInstance.name) {
                OnAnimationEnd?.Invoke(abilityType);
                Animating = default;
            }
            OnAbilityFinished?.Invoke(abilityType);
        }
         
        private void OnDisable()
        {
            ActionBlocked.ClearEffectors();
            SuspendAbilities();
            if (abilityData != null)
            {
                abilityData.FinalizeContext();
            }
            Bind(inputData);
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
           Bind(abilityData);
            abilityData.Activate(this);
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
        /// Note that this method should only be called before LateUpdate(), otherwise the ability instance execution will be deferred to the next frame.
        /// <see cref="Ability.OnAbilityEnqueue"/> will be immediately called if enqueued successfully.
        /// </summary>
        /// <typeparam name="T">Type of the Ability to be enqueued</typeparam>
        /// <param name="instance">Name of the instance to be enqueued</param>
        /// <returns>true if successfully enqueued the ability instance, false otherwise</returns>
        public bool EnqueueAbility<T>(string instance=Ability.DefaultAbilityInstance) where T : Ability
        {
            if (ActionBlocked)
            {
                return false;
            }
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability))
            {
                if (ability.EnqueueAbility(instance))
                {
                    OnAbilityEnqueued?.Invoke(typeof(T));
                    return true;
                }
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
        /// Note that this method should only be called before LateUpdate(), otherwise the ability instance execution will be deferred to the next frame.
        /// <see cref="Ability.OnAbilityEnqueue"/> will be immediately called if enqueued successfully.
        /// </summary>
        /// <param name="abilityType">Type of the ability to be enqueued</param>
        /// <param name="instance">Name of the instance to be enqueued</param>
        /// <returns>true if successfully enqueued the ability instance, false otherwise</returns>
        public bool EnqueueAbility(Type abilityType, string instance=Ability.DefaultAbilityInstance) {
            if (ActionBlocked || abilityType == null)
            {
                return false;
            }
            if (abilities.TryGetValue(abilityType.AssemblyQualifiedName, out Ability ability)) {
                if (ability.EnqueueAbility(instance))
                {
                    OnAbilityEnqueued?.Invoke(abilityType);
                    return true;
                }
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
        public bool EnqueueAbilitiesInJoint<T, V>(string instance1=Ability.DefaultAbilityInstance, string instance2=Ability.DefaultAbilityInstance) 
            where T : Ability 
            where V : Ability
        {
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability a1) && abilities.TryGetValue(typeof(V).AssemblyQualifiedName, out Ability a2)) {
                // The two abilities must both be present and not the same
                if (ActionBlocked || a1 == a2)
                {
                    return false;
                }

                if (a1.IsReady(instance1) && a2.IsReady(instance2))
                {
                    EnqueueAbility<T>(instance1);
                    EnqueueAbility<V>(instance2);
                    AbilityInstance p1 = new(a1, instance1);
                    AbilityInstance p2 = new(a2, instance2);
                    a2.joined.Add(p1);
                    a1.joinedBy.Add(p2);
                    return true;
                }
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
        public bool SuspendAbilityInstance<T>(string instance = Ability.DefaultAbilityInstance) where T : Ability
        {
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability))
            {
                return ability.SuspendInstance(instance);
            }
            return false;
        }

        /// <summary>
        /// Stops the execution of the ability and returns the status of this operation
        /// </summary>
        /// <param name="abilityType">Type of the ability to be stopped</param>
        /// <param name="instance">Name of the ability instance to be stopped</param>
        /// <returns>true if the ability instance exists and is stopped, otherwise return false</returns>
        public bool SuspendAbilityInstance(Type abilityType, string instance = Ability.DefaultAbilityInstance)
        {
            if (abilityType == null) {
                return false;
            }
            if (abilities.TryGetValue(abilityType.AssemblyQualifiedName, out Ability ability))
            {
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
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability)) {
                ability.SuspendAll();
                return true;
            }
            return false;
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
        public AbilityChannel GetAbilityChannel<T>(string instance=Ability.DefaultAbilityInstance) where T : Ability {
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability)) {
                return ability.GetAbilityChannel(instance);
            }
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
        public bool IsAbilityReady<T>(string instance=Ability.DefaultAbilityInstance) where T : Ability
        {
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability))
            {
                return ability.IsReady(instance);
            }
            return false;
        }

        /// <summary>
        /// Check if the specified ability instance is ready
        /// </summary>
        /// <param name="abilityType">Type of the Ability to be queried</param>
        /// <param name="instance">Name of the ability instance to be queried</param>
        /// <returns>true if the ability instance exists and is ready, false otherwise</returns>
        public bool IsAbilityReady(Type abilityType, string instance = Ability.DefaultAbilityInstance)
        {
            if (abilityType == null)
            {
                return false;
            }
            if (abilities.TryGetValue(abilityType.AssemblyQualifiedName, out Ability ability))
            {
                return ability.IsReady(instance);
            }
            return false;
        }

        /// <summary>
        /// Check if the ability with specified config is running
        /// </summary>
        /// <typeparam name="T">The type of the ability being queried</typeparam>
        /// <param name="instance">The name of the ability instance being queried</param>
        /// <returns> true if the ability instance exists and is running, otherwise false </returns>
        public bool IsAbilityRunning<T>(string instance=Ability.DefaultAbilityInstance) where T : Ability {
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability)) {
                return ability.IsRunning(instance);
            }
            return false;
        }

        /// <summary>
        /// Check if the specified ability instance is running
        /// </summary>
        /// <param name="abilityType">The type of the ability being queried</param>
        /// <param name="instance">The name of the ability instance being queried</param>
        /// <returns> true if the ability instance exists and is running, otherwise false </returns>
        public bool IsAbilityRunning(Type abilityType, string instance = Ability.DefaultAbilityInstance) 
        {
            if (abilityType == null) {
                return false;
            }
            if (abilities.TryGetValue(abilityType.AssemblyQualifiedName, out Ability ability))
            {
                return ability.IsRunning(instance);
            }
            return false;
        }

        /// <summary>
        /// True if a animation of an ability is currently being played, false otherwise
        /// </summary>
        public bool IsAnimating
        {
            get  { return !Animating.IsNullAbility; }
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
        public bool JoinAbilities(Type primaryAbility, Type secondaryAbility, string instance1=Ability.DefaultAbilityInstance, string instance2=Ability.DefaultAbilityInstance) {
            if (IsAbilityRunning(primaryAbility, instance1) && IsAbilityRunning(secondaryAbility, instance2)) {
                var a1 = abilities[primaryAbility.AssemblyQualifiedName];
                var a2 = abilities[secondaryAbility.AssemblyQualifiedName];
                AbilityInstance p1 = new(a1, instance1);
                AbilityInstance p2 = new(a2, instance2);
                a1.joinedBy.Add(p2);
                a2.joined.Add(p1);
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
        public bool JoinAbilities<T, V>(string instance1=Ability.DefaultAbilityInstance, string instance2=Ability.DefaultAbilityInstance) where T : Ability where V : Ability
        {
            if (IsAbilityRunning<T>(instance1) && IsAbilityRunning<V>(instance2))
            {
                var a1 = abilities[typeof(T).AssemblyQualifiedName];
                var a2 = abilities[typeof(V).AssemblyQualifiedName];
                AbilityInstance p1 = new(a1, instance1);
                AbilityInstance p2 = new(a2, instance2);
                a1.joinedBy.Add(p2);
                a2.joined.Add(p1);
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
            if (!Animating.IsNullAbility && !Animating.Equals(new AbilityInstance(ability, instance)))
            {
                Animating.ability.AnimationInterrupt(Animating.name, currentState);
            }
            Animating = new(ability, instance);

            OnAnimationBegin?.Invoke(ability.GetType());
            currentState = animancer.Play(animation, 0.3f / speed, FadeMode.FromStart);
            currentState.Speed = speed;
            return currentState;
        }

        /// <summary>
        /// Interrupt the currently playing ability animation, do nothing if no abilities is currently playing animation.
        /// </summary>
        public void InterruptAbilityAnimation()
        {
            if (Animating.IsNullAbility)
            {
                return;
            }
            Ability ability = Animating.ability;
            ability.AnimationInterrupt(Animating.name, currentState);
            Animating = default;
            OnAnimationEnd?.Invoke(ability.GetType());
        }

        /// <summary>
        /// Send a signal to the specified ability. 
        /// </summary>
        public void Signal<T>(string instance = Ability.DefaultAbilityInstance) where T : Ability
        {
            if (abilities.TryGetValue(typeof(T).AssemblyQualifiedName, out Ability ability)){
                ability.Signal(instance);
            }
        }

        /// <summary>
        /// Used by animation events to send signals
        /// </summary>
        public void AnimationSignal(AnimationEvent animationEvent)
        {
            if (Animating.IsNullAbility) { return; }
            if (animationEvent.animatorClipInfo.clip == currentState.Clip)
            {
                Animating.ability.Signal(Animating.name, animationEvent);
            }
        }

        /// <summary>
        /// Used by animation events to signal the end of the ability. The ability will immediately terminate after this call. Do nothing if the event does not belong to the current ability animation.
        /// </summary>
        /// <param name="animationEvent">The animation event instance to be queried</param>
        public void AnimationEnd(AnimationEvent animationEvent)
        {
            if (Animating.IsNullAbility) { return; }
            if (animationEvent.animatorClipInfo.clip == currentState.Clip)
            {
                Animating.ability.SuspendInstance(Animating.name);
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
        public void Save(string path)
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
                cloned.SaveAsAsset();
                inputData = cloned;
            }
        }
#endif
    }

    [Serializable]
    public class AbilityComponentDictionary : SerializableDictionary<string, AbilityComponent> { }
}

