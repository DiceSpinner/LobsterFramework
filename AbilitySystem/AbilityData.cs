using LobsterFramework.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability/AbilityData")]
    public class AbilityData : ScriptableObject
    {
        [SerializeField] internal AbilityComponentDictionary components = new();
        [SerializeField] internal AbilityDictionary abilities = new();
        internal Dictionary<string, Ability> availableAbilities = new();
        internal bool isRuntime = false;

        /// <summary>
        /// Initialize the ability runtime environments
        /// </summary>
        /// <param name="abilityRunner">The component that operates on this data</param>
        internal void Open(AbilityManager abilityRunner)
        {
            isRuntime = true;
            availableAbilities.Clear();
            GameObject topLevel = default;
            if (abilityRunner.TopLevelTransform != null) {
                topLevel = abilityRunner.TopLevelTransform.gameObject;
            }

            foreach (string key in abilities.Keys)
            {
                Ability ai = abilities[key];

                bool result;
                if (topLevel == null) {
                    result = ComponentRequiredAttribute.ComponentCheck(ai.GetType(), abilityRunner.gameObject);
                }
                else {
                    result = ComponentRequiredAttribute.ComponentCheck(ai.GetType(), abilityRunner.gameObject, topLevel);
                }
                if (result)
                {
                    ai.abilityManager = abilityRunner;
                    ai.OnOpen();
                    availableAbilities[ai.GetType().AssemblyQualifiedName] = ai;
                }
            }

            foreach (string key in components.Keys)
            {
                AbilityComponent component = components[key];
                component.Initialize();
            }
        }

        /// <summary>
        /// Clean up ability runtime environments. This has no effect if this is an asset.
        /// </summary>
        public void Close() {
            if (!isRuntime) {
                return;
            }
            foreach (Ability ability in availableAbilities.Values)
            {
                ability.OnClose();
            }

            foreach (AbilityComponent cmp in components.Values)
            {
                cmp.OnClose();
            }
        }

        /// <summary>
        /// Deep copy of action datas. Call this method after duplicate the AbilityData to ensure all of its contents are properly duplicated.
        /// </summary>
        private void CopyAbilityAsset()
        { 
            List<Ability> abilities = new();
            foreach (var kwp in this.abilities)
            {
                // Duplicate Abilities first
                Ability ability = Instantiate(kwp.Value);
                ability.name = kwp.Value.name;
                abilities.Add(ability);
            }

            // Duplicate AbilityConfig assiciated with each Ability
            foreach (var ability in abilities)
            {
                this.abilities[ability.GetType().AssemblyQualifiedName] = ability;

                // AbilityConfig
                List<(string, AbilityConfig)> configs = new();
                foreach (var kwp in ability.configs)
                {
                    if (kwp.Value != null)
                    {
                        AbilityConfig config = Instantiate(kwp.Value);
                        config.name = kwp.Value.name;
                        configs.Add((kwp.Key, config));
                    }
                }

                foreach ((string name, AbilityConfig config) in configs)
                {
                    ability.configs[name] = config; 
                }
            }

            List<AbilityComponent> abilityComponents = new();
            foreach (var kwp in components)
            {
                AbilityComponent abilityComponent = Instantiate(kwp.Value);
                abilityComponent.name = kwp.Value.name;
                abilityComponents.Add(abilityComponent);
            }
            foreach (var abilityComponent in abilityComponents)
            {
                components[abilityComponent.GetType().AssemblyQualifiedName] = abilityComponent;
            }
        }

        /// <summary>
        /// Clones the ability data
        /// </summary>
        /// <returns>A copy of the ability data</returns>
        public AbilityData Clone() { 
            AbilityData cloned = Instantiate(this);
            cloned.CopyAbilityAsset();
            cloned.name = this.name;
            cloned.isRuntime = false;
            return cloned;
        }

        private T GetAbility<T>() where T : Ability
        {
            string type = typeof(T).AssemblyQualifiedName;
            if (abilities.TryGetValue(type, out Ability ability))
            {
                return (T)ability;
            }
            return default;
        }

        private T GetAbilityComponent<T>() where T : AbilityComponent
        {
            string type = typeof(T).AssemblyQualifiedName;
            if (components.TryGetValue(type, out AbilityComponent stat))
            {
                return (T)stat;
            }
            return default;
        }

        /// <summary>
        /// Check if requirements for AbilityComponents by Ability T are satisfied
        /// </summary>
        /// <typeparam name="T"> Type of the Ability to be queried </typeparam>
        /// <returns>The result of the query</returns>
        internal bool AbilityComponentsCheck<T>() where T : Ability
        {
            Type type = typeof(T);
            bool f1 = true;

            if (RequireAbilityComponentsAttribute.requirement.TryGetValue(type, out var ts))
            {
                List<Type> lst1 = new List<Type>();
                foreach (Type t in ts)
                {
                    if (!components.ContainsKey(t.AssemblyQualifiedName))
                    {
                        lst1.Add(t);
                        f1 = false;
                    }
                }
                if (f1)
                {
                    return true;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("Missing AbilityComponents: ");
                foreach (Type t in lst1)
                {
                    sb.Append(t.Name);
                    sb.Append(", ");
                }
                sb.Remove(sb.Length - 2, 2);
                Debug.LogError(sb.ToString());
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Save data as assets by adding them to the AssetDataBase
        /// </summary>
        public void SaveContentsAsAsset()
        {
            if (AssetDatabase.Contains(this))
            {
                foreach (AbilityComponent cmp in components.Values)
                {
                    AssetDatabase.AddObjectToAsset(cmp, this);
                }
                foreach (Ability ai in abilities.Values)
                {
                    AssetDatabase.AddObjectToAsset(ai, this);
                    ai.SaveConfigsAsAsset();
                }
            }
        }


        /// <summary>
        /// Called by editor scritps, add AbilityComponent of type T to the set of available AbilityComponents if not already present, return the status of the operation. <br/>
        /// </summary>
        /// <typeparam name="T">Type of the AbilityComponent to be added</typeparam>
        /// <returns>true if successfully added AbilityComponent, false otherwise</returns>
        internal bool AddAbilityComponent<T>() where T : AbilityComponent
        {
            string str = typeof(T).AssemblyQualifiedName;
            if (components.ContainsKey(str))
            {
                return false;
            }
            T instance = CreateInstance<T>();
            components[str] = instance;
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.AddObjectToAsset(instance, this);
            }
            return true;
        }


        /// <summary>
        /// Called by editor scritps, add Ability of type T to the set of available Abilities if not already present, return the status of the operation. <br/>
        /// </summary>
        /// <typeparam name="T">Type of the Abilityto be added</typeparam>
        /// <returns>true if successfully added Ability, false otherwise</returns>
        internal bool AddAbility<T>() where T : Ability
        {
            if (GetAbility<T>() != default)
            {
                return false;
            }

            if (AbilityComponentsCheck<T>())
            {
                T ai = CreateInstance<T>();
                abilities.Add(typeof(T).AssemblyQualifiedName, ai);
                ai.configs = new();
                ai.name = typeof(T).FullName;
                if (AssetDatabase.Contains(this))
                {
                    AssetDatabase.AddObjectToAsset(ai, this);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called by editor script only! Remove the ability of specified type. If the ability is an asset it will be destroyed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal bool RemoveAbility<T>() where T : Ability
        {
            Ability ability = GetAbility<T>();
            if (ability != null)
            {
                abilities.Remove(typeof(T).AssemblyQualifiedName);
                AssetDatabase.RemoveObjectFromAsset(ability);
                DestroyImmediate(ability, true);
                return true;
            }
            return false;
        }

        internal bool RemoveAbilityComponent<T>() where T : AbilityComponent
        {
            string str = typeof(T).AssemblyQualifiedName;
            if (components.ContainsKey(str))
            {
                if (RequireAbilityComponentsAttribute.rev_requirement.ContainsKey(typeof(T)))
                {
                    StringBuilder sb = new();
                    sb.Append("Cannot remove AbilityComponent: " + typeof(T).FullName + ", since the following abilities requires it: ");
                    bool flag = false;
                    foreach (Type t in RequireAbilityComponentsAttribute.rev_requirement[typeof(T)])
                    {
                        if (abilities.ContainsKey(t.AssemblyQualifiedName))
                        {
                            flag = true;
                            sb.Append(t.Name);
                            sb.Append(", ");
                        }
                    }
                    if (flag)
                    {
                        sb.Remove(sb.Length - 2, 2);
                        Debug.LogError(sb.ToString());
                        return false;
                    }
                }
            }
            T cmp = GetAbilityComponent<T>();
            if (cmp != null)
            {
                components.Remove(str);
                AssetDatabase.RemoveObjectFromAsset(cmp);
                DestroyImmediate(cmp, true);
                return true;
            }
            return false;
        }
#endif
    }
}

