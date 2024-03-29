using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.Utility;
using Animancer;

namespace LobsterFramework.AbilitySystem {
    /// <summary>
    /// Component acts as a container and platform for different kinds of abilities and moves.
    /// Requires ability data that defines what abilities and configurations are available to be supplied. 
    /// </summary>
    [AddComponentMenu("AbilityRunner")]
    public class AbilityRunner : SubLevelComponent
    {
        // Callbacks
        public Action<Type> onAbilityEnqueue;
        public Action<Type> onAbilityFinished;

        // Execution Info
        internal HashSet<AbilityInstance> executing = new();
        private readonly Dictionary<AbilityInstance, AbilityInstance> jointlyRunning = new();

        // Core Data
        [SerializeField] private AbilityData inputData;
        [HideInInspector]
        [SerializeField] private AbilityData abilityData;
        private Dictionary<string, Ability> availableAbilities;
        private TypeAbilityComponentDictionary components;

        //Animation
        /// <summary>
        /// Send true if starting ability animation, false if ending ability animation
        /// </summary>
        public Action<bool> onAbilityAnimation;
        private AnimancerState currentState;
        private (Ability, string) animating;
        private AnimancerComponent animancer;

        // Status
        public readonly Or actionLock = new(false);
        public bool ActionBlocked => actionLock.Value;

        // Entity
        private Entity entity;
        private CombinedValueEffector<bool> postureBrokenActionBlock;

        #region EnqueueAbility
        /// <summary>
        /// Add the ability with specified type (T) and config (name) to the executing queue, return the status of this operation. <br/>
        /// For this operation to be successful, the following must be satisfied: <br/>
        /// 1. The entity must not be action blocked. (i.e Not affected by stun effect) <br/>
        /// 2. The specified ability of Type (T) as well as the config (name) must be present in the ability data.<br/>
        /// 3. The precondition of the specified ability must be satisfied as well as the cooldown of the config if the ability uses cooldowns. <br/>
        /// 4. The specified ability must not be currently running or enqueued. <br/>
        /// <br/>
        /// Note that this method should only be called inside Update(), calling it elsewhere will have unpredictable results.
        /// </summary>
        /// <typeparam name="T">Type of the Action to be enqueued</typeparam>
        /// <param name="configName">Name of the configuration to be enqueued</param>
        /// <returns></returns>
        public bool EnqueueAbility<T>(string configName="default") where T : Ability
        {
            if (ActionBlocked)
            {
                return false;
            }
            T action = GetAbility<T>();
            if (action == default)
            {
                return false;
            }
            if (action.EnqueueAbility(configName))
            {
                executing.Add(new AbilityInstance(action, configName));
                onAbilityEnqueue?.Invoke(typeof(T));
                return true;
            }
            return false;
        }

        public bool EnqueueAbility(Type abilityType, string configName="default") {
            if (ActionBlocked)
            {
                return false;
            }
            Ability action = GetAbility(abilityType);
            if (action == default)
            {
                return false;
            }
            if (action.EnqueueAbility(configName))
            {
                executing.Add(new AbilityInstance(action, configName));
                onAbilityEnqueue?.Invoke(abilityType);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enqueue two abilities of different types together with the second one being guaranteed to <br/>
        /// terminate no later than the first one.
        /// </summary>
        /// <typeparam name="T"> The type of the first ability </typeparam>
        /// <typeparam name="V"> The type of the second ability </typeparam>
        /// <param name="configName1"> Name of the configuration for the first ability </param>
        /// <param name="configName2"> Name of the configuration for the second ability </param>
        /// <returns></returns>
        public bool EnqueueAbilitiesInJoint<T, V>(string configName1="default", string configName2="default") 
            where T : Ability 
            where V : Ability
        {
            T a1 = GetAbility<T>();
            V a2 = GetAbility<V>();

            // The two abilities must both be present and not the same
            if (ActionBlocked || a1 == default || a2 == default || a1 == a2) {
                return false; 
            }

            if (a1.IsReady(configName1) && a2.IsReady(configName2)) {
                EnqueueAbility<T>(configName1);
                EnqueueAbility<V>(configName2);
                AbilityInstance p1 = new(a1, configName1);
                AbilityInstance p2 = new(a2, configName2);
                jointlyRunning[p1] = p2;
                return true;
            }
            return false;
        }
        #endregion

        /// <summary>
        /// Get the ability stat of type T attached to this Component if it is present.
        /// </summary>
        /// <typeparam name="T">Type of the AbilityStat being requested</typeparam>
        /// <returns>Return the ability stat if it is present, otherwise null</returns>
        public T GetAbilityStat<T>() where T : AbilityComponent
        {
            string type = typeof(T).ToString();
            if (components.TryGetValue(type, out AbilityComponent stat))
            {
                return (T)stat;
            }
            return default; 
        }

        /// <summary>
        /// Get the ability pipe of specified ability and configuration
        /// </summary>
        /// <typeparam name="T">The type of Ability to pipe to.</typeparam>
        /// <param name="configName">The name of the ability configuration to pipe to</param>
        /// <returns> The pipe that connects to the specified ability and configuration if it exists, otherwise return null. </returns>
        public AbilityPipe GetAbilityPipe<T>(string configName="default") where T : Ability {
            T ability = GetAbility<T>();
            if (ability != null) {
                return ability.GetAbilityPipe(configName);
            }
            return default;
        }

        /// <summary>
        /// Stops the execution of the action and returns the status of this operation
        /// </summary>
        /// <typeparam name="T">Type of the action to be halted</typeparam>
        /// <param name="name">Name of the config of T to be halted</param>
        /// <returns>Return the status of this operation</returns>
        public bool HaltAbility<T>(string name="default") where T : Ability
        {
            Ability ac = GetAbility<T>();
            AbilityInstance pair = new(ac, name);
            if (executing.Contains(pair))
            {
                return ac.HaltAbilityExecution(name);
            }
            return false;
        }

        /// <summary>
        /// Stop the execution of the specified ability on all of its configs
        /// </summary>
        /// <typeparam name="T">The type of the ability to be stopped</typeparam>
        /// <returns>true if the ability exists, otherwise false</returns>
        public bool HaltAbilityComplete<T>() where T:Ability { 
            Ability ability = GetAbility<T>();
            if (ability == null) {
                return false;
            }
            ability.HaltOnAllConfigs();
            return true;
        }

        /// <summary>
        /// Stops execution of all active abilities
        /// </summary>
        public void HaltAbilities() {
            try
            {
                foreach (Ability ability in availableAbilities.Values)
                {
                    ability.HaltOnAllConfigs();
                }
            }catch (Exception) { }
        }

        /// <summary>
        /// Check if the action instance of type T with config is ready. <br/>
        /// If T is not present or config is not available, return false.
        /// </summary>
        /// <typeparam name="T">Type of the Ability to be queried</typeparam>
        /// <param name="config">Name of the config to be queried</param>
        /// <returns>The result of the query</returns>
        public bool IsAbilityReady<T>(string config="default") where T : Ability
        {
            try
            {
                string type = typeof(T).ToString();
                if (availableAbilities.ContainsKey(type))
                {
                    return availableAbilities[type].IsReady(config);
                }
            }catch(NullReferenceException) { }
            return false;
        }

        public bool IsAbilityReady(Type abilityType, string config = "default")
        {
            try
            {
                if (abilityType == null)
                {
                    return false;
                }
                string type = abilityType.ToString();
                if (availableAbilities.ContainsKey(type))
                {
                    return availableAbilities[type].IsReady(config);
                }
            }
            catch (NullReferenceException) { }
            return false;
        }

        /// <summary>
        /// Check if the ability with specified config is running
        /// </summary>
        /// <typeparam name="T">The type of the ability being queried</typeparam>
        /// <param name="configName">The name of the config being queried</param>
        /// <returns> true if the ability exists and is running, otherwise false </returns>
        public bool IsAbilityRunning<T>(string configName="default") where T : Ability {
            T ability = GetAbility<T>();
            if (ability == null) {
                return false;
            }
            return ability.IsExecuting(configName);
        }

        /// <summary>
        /// Check if the ability with specified config is running
        /// </summary>
        /// <typeparam name="T">The type of the ability being queried</typeparam>
        /// <param name="configName">The name of the config being queried</param>
        /// <returns> true if the ability exists and is running, otherwise false </returns>
        public bool IsAbilityRunning(Type abilityType, string configName = "default") 
        {
            Ability ability = GetAbility(abilityType);
            if (ability == null)
            {
                return false;
            }
            return ability.IsExecuting(configName);
        }

        public bool JoinAbilities(Type primaryAbility, Type secondaryAbility, string config1, string config2) {
            if (IsAbilityRunning(primaryAbility, config1) && IsAbilityRunning(secondaryAbility, config2)) {
                AbilityInstance p1 = new() { ability = GetAbility(primaryAbility), configName = config1};
                AbilityInstance p2 = new() { ability = GetAbility(secondaryAbility), configName = config2};
                jointlyRunning[p1] = p2;
                return true;
            }
            return false;
        }

        private void OnActionStateChanged(bool isBlocked) {
            if (isBlocked)
            {
                HaltAbilities();
            }
        }

        /// <summary>
        /// Reset the status of all abilities and their configs to their initial state
        /// </summary>

        public void ResetStatus()
        {
            if (components == null || availableAbilities == null) {
                return;
            }
            foreach (AbilityComponent component in components.Values)
            {
                component.Reset();
            }
            foreach (Ability ac in availableAbilities.Values)
            {
                ac.ResetStatus();
            }
        }

        /// <summary>
        /// Only to be called inside play mode! Save the current ability data as an asset with specified assetName to the default path.
        /// </summary>
        /// <param name="assetName">Name of the asset to be saved</param>
        public void SaveAbilityData(string assetName, string path)
        {
            if (assetName == "") {
                assetName = inputData.name;
            }
            if (path.StartsWith(Application.dataPath)) {
                path = "Assets/" + path[Application.dataPath.Length..];
            } 
            if (abilityData != null)
            {
                AbilityData cloned = abilityData.Clone(); 
                AssetDatabase.CreateAsset(cloned, path + "/" + assetName + ".asset");
                cloned.SaveContentsAsAsset();
                inputData = cloned;
            }
        }
         
        private T GetAbility<T>() where T : Ability
        {
            try
            {
                string type = typeof(T).ToString();
                if (availableAbilities.TryGetValue(type, out Ability ability))
                {
                    return (T)ability;
                }
            }catch (NullReferenceException) { }
            return default;
        }

        private Ability GetAbility(Type abilityType) {
            try
            {
                string type = abilityType.ToString();
                if (availableAbilities.TryGetValue(type, out Ability ability))
                {
                    return ability;
                }
            }catch(NullReferenceException) { }
            return default;
        }

        private void OnDisable()
        {
            foreach (AbilityInstance pair in executing) {
                pair.HaltAbility();
            }
            if (abilityData != null) {
                Destroy(abilityData);
            }
            
            if (entity != null)
            {
                entity.onPostureStatusChange -= OnPostureStatusChange;
            }
        }

        private void OnEnable()
        {
            if (abilityData != null) {
                Destroy(abilityData);
            }
            if (inputData == null)
            {
                Debug.LogWarning("Ability Data is not set!", gameObject);
                return;
            }
            abilityData = inputData.Clone();

            // stats need to be set before Initializing abilities since Abilities that requires fetching AbilityStats can only get it through AbilityRunner
            components = abilityData.components;
            abilityData.Open(this);
            actionLock.ClearEffectors();
            
            // availableAbilities is only determined after running through the initialization check of AbilityData
            availableAbilities = abilityData.availableAbilities;

            if (entity != null) {
                entity.onPostureStatusChange += OnPostureStatusChange;
            }
        }

        /// <summary>
        /// The Start Method of AbilityRunner needs to be runned at high priority as it needs to duplicate the ability data asset for other script refer to in their Start() method
        /// </summary>
        private void Awake()
        {
            entity = GetComponentInBoth<Entity>();
            animancer = GetComponent<AnimancerComponent>();
            postureBrokenActionBlock = actionLock.MakeEffector();
            actionLock.onValueChanged += OnActionStateChanged;
        }

        private List<AbilityInstance> removed = new();
        private void Update()
        {
            try
            {
                components.Values.ToList().ForEach(value => value.Update());
            }catch(NullReferenceException) { }

            removed.Clear();
            foreach (AbilityInstance ap in executing)
            {
                Ability ac = ap.ability;
                if (ActionBlocked)
                {
                    ac.HaltOnAllConfigs();
                }
                if (!ac.IsExecuting(ap.configName))
                {
                    removed.Add(ap);
                    if (onAbilityFinished != null) {
                        onAbilityFinished.Invoke(ac.GetType());
                    }
                    if (jointlyRunning.ContainsKey(ap)) { 
                        jointlyRunning[ap].HaltAbility();
                        jointlyRunning.Remove(ap);
                    }
                }
            }
            foreach (AbilityInstance ap in removed)
            {
                executing.Remove(ap);
                if (IsAnimating(ap.ability, ap.configName)) { FinishAnimation(); }
            }
        }

        /// <summary>
        /// Block Action if posture broken, unblock if posture recovered
        /// </summary>
        /// <param name="postureBroken"></param>
        private void OnPostureStatusChange(bool postureBroken) {
            if (postureBroken)
            {
                postureBrokenActionBlock.Apply(true);
            }
            else {
                postureBrokenActionBlock.Release();
            }
        }


        /// <summary>
        /// Check if the specified ability config pair is playing animation. 
        /// </summary>
        /// <param name="ability">The ability to be queried</param>
        /// <param name="configName">The config of the ability to be queried</param>
        /// <returns>Return true if the ability and the config are both valid and present and are playing animation, otherwise false</returns>
        private bool IsAnimating(Ability ability, string configName) { 
            return animating == (ability, configName);
        }

        public bool IsAnimating()
        {
            return animating != default;
        }

        /// <summary>
        /// Used by abilities to initiate animations, will override any currently running animations by other abilities
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configName"></param>
        /// <param name="animation"></param>
        public AnimancerState StartAnimation(Ability ability, string configName, AnimationClip animation, float speed=1)
        {
            if (ability == null) {
                return null;
            }
            if (!ability.HasConfig(configName)) {
                return null;
            }
            if (animation == null) {
                Debug.Log("Cannot play null animation!");
                return null;
            }
            if (speed <= 0) {
                speed = 1;
            }
            currentState = animancer.Play(animation, 0.3f / speed, FadeMode.FromStart);
            if (animating != default) {
                animating.Item1.AnimationInterrupt(animating.Item2, currentState);
            }
            animating = (ability, configName);
            
            currentState.Speed = speed;
            onAbilityAnimation?.Invoke(true);
            return currentState;
        }

        /// <summary>
        /// End ability animations
        /// </summary>

        private void FinishAnimation()
        {
            if(animating == default) { return; }
            animating = default;
            onAbilityAnimation?.Invoke(false);
        }

        public void InterruptAbilityAnimation() {
            if (animating == default) {
                return;
            }
            animating.Item1.AnimationInterrupt(animating.Item2, currentState);
            animating = default;
            onAbilityAnimation?.Invoke(false);
        }

        /// <summary>
        /// Send a signal to the specified ability. 
        /// </summary>
        public void Signal<T>(string configName="default") where T : Ability
        {
            Ability ability = GetAbility<T>();
            if (ability == null) {
                return;
            }
            ability.Signal(configName, null);
        }

        /// <summary>
        /// Used by animation events to send signals
        /// </summary>
        public void AnimationSignal(AnimationEvent animationEvent) {
            if(animating == default) { return; }
            if (animationEvent.animatorClipInfo.clip == currentState.Clip) {
                animating.Item1.Signal(animating.Item2, animationEvent);
            }
        }

        public void AnimationEnd(AnimationEvent animationEvent) {
            if (animating == default) { return; }
            if (animationEvent.animatorClipInfo.clip == currentState.Clip)
            {
                animating.Item1.HaltAbilityExecution(animating.Item2);
            }
        }
    }
}

