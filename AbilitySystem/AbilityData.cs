using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Defines the set of abilities and ability components an actor can take on. Used as input to <see cref="AbilityManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Ability/AbilityData")]
    public class AbilityData : ReferenceRequester
    {
        [SerializeField] internal AbilityComponentDictionary components = new();
        [SerializeField] internal AbilityDictionary abilities = new();
        internal Dictionary<string, Ability> runnables = new();

        public override IEnumerator<Type> GetRequestingTypes()
        {
            foreach (Ability ability in abilities.Values) {
                Type type = ability.GetType();
                if (RequireComponentReferenceAttribute.Requirement.ContainsKey(type)) {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Initialize the ability context environments
        /// </summary>
        /// <param name="abilityManager">The component that operates on this data</param>
        internal void Activate(AbilityManager abilityManager)
        {
            runnables.Clear();

            foreach (string key in components.Keys)
            {
                AbilityComponent component = components[key];
                try { component.Initialize(); }catch (Exception e)
                {
                    Debug.LogWarning($"Error occured when initializing ability component {component.GetType().FullName} !");
                    Debug.LogException(e);
                }
            }

            abilityManager.components = components;

            foreach (string key in abilities.Keys)
            {
                Ability ability = abilities[key];

                if (VerifyComponentRequirements(ability.GetType()) && abilityManager.IsRequirementSatisfied(ability.GetType()))
                {
                    ability.AbilityManager = abilityManager;
                    try {
                        ability.Activate();
                        runnables[ability.GetType().AssemblyQualifiedName] = ability;
                    }catch (Exception ex)
                    {
                        Debug.LogWarning($"Error occured when initializing ability {ability.GetType().FullName} !");
                        Debug.LogException(ex);
                    }
                }
            }
            
            abilityManager.abilities = runnables;
        }

        /// <summary>
        /// Clean up ability context environments.
        /// </summary>
        internal void FinalizeContext() {
            foreach (Ability ability in runnables.Values)
            {
                try{ ability.OnBecomeInactive(); }catch(Exception e)
                {
                    Debug.LogWarning($"Error occured when finalizing context for ability {ability.GetType().FullName} !");
                    Debug.LogException(e);
                }
            }

            foreach (AbilityComponent cmp in components.Values)
            {
                try { cmp.OnBecomeInactive(); }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error occured when finalizing context for ability component {cmp.GetType().FullName} !");
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Deep copy of action datas. Call this method after duplicate the AbilityData to ensure all of its contents are properly duplicated.
        /// </summary>
        private void DeepCopySubAssets()
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
            cloned.DeepCopySubAssets();
            cloned.name = this.name;
            return cloned;
        }

        /// <summary>
        /// Check if requirements for AbilityComponents by the spefified ability type are satisfied
        /// </summary>
        /// <returns>The result of the query</returns>
        internal bool VerifyComponentRequirements(Type typeToVerify)
        {
            bool f1 = true;

            if (RequireAbilityComponentsAttribute.Requirement.TryGetValue(typeToVerify, out var ts))
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
                sb.Append($"Ability {typeToVerify.FullName} missing required AbilityComponents: ");
                foreach (Type t in lst1)
                {
                    sb.Append(t.Name);
                    sb.Append(", ");
                }
                sb.Remove(sb.Length - 2, 2);
                Debug.LogError(sb.ToString(), this);
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Add required <see cref="AbilityComponent"/> if missing after recompilation
        /// </summary>
        private void OnValidate()
        {
            foreach (var ability in abilities.Values)
            {
                if (RequireAbilityComponentsAttribute.Requirement.TryGetValue(ability.GetType(), out var set))
                {
                    foreach (Type componentType in set)
                    {
                        if (!components.ContainsKey(componentType.AssemblyQualifiedName))
                        {
                            Debug.Log($"Added missing required component of type {componentType.FullName}.", this);
                            AddAbilityComponent(componentType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save data as assets by adding them to the AssetDataBase
        /// </summary>
        public void SaveAsAsset()
        {
            if (AssetDatabase.Contains(this))
            {
                foreach (AbilityComponent component in components.Values)
                {
                    AssetDatabase.AddObjectToAsset(component, this);
                }
                foreach (Ability ability in abilities.Values)
                {
                    AssetDatabase.AddObjectToAsset(ability, this);
                    ability.SaveConfigsAsAsset();
                }
            }
        }


        /// <summary>
        /// Called by editor scritps, add <see cref="AbilityComponent"/> of specified type to the set of available <see cref="AbilityComponent"/> if not already present
        /// </summary>
        /// <returns>true if successfully added AbilityComponent, false otherwise</returns>
        internal void AddAbilityComponent(Type typeToAdd)
        {
            string str = typeToAdd.AssemblyQualifiedName;
            if (components.ContainsKey(str))
            {
                return;
            }
            var instance = CreateInstance(typeToAdd);
            components[str] = (AbilityComponent)instance;
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.AddObjectToAsset(instance, this);
            }
        }


        /// <summary>
        /// Called by editor scritps, add Ability of specified type to the set of available <see cref="Ability"/> if not already present
        /// </summary>
        internal void AddAbility(Type typeToAdd)
        {
            if (typeToAdd == null || abilities.ContainsKey(typeToAdd.AssemblyQualifiedName))
            {
                return;
            }

            if (VerifyComponentRequirements(typeToAdd))
            {
                Ability ability = (Ability)CreateInstance(typeToAdd);
                abilities.Add(typeToAdd.AssemblyQualifiedName, ability);
                ability.configs = new();
                ability.name = typeToAdd.FullName;

                if (AssetDatabase.Contains(this))
                {
                    AssetDatabase.AddObjectToAsset(ability, this);
                }

                ability.AddInstance(Ability.DefaultAbilityInstance);
                RaiseRequirementAddedEvent(typeToAdd);
            }
        }

        /// <summary>
        /// Called by editor script only! Remove the <see cref="Ability"/> of specified type. If the ability is an asset it will be destroyed.
        /// </summary>
        /// <returns>true if successfully removed, otherwise false</returns>
        internal void RemoveAbility(Type typeToRemove)
        {
            if (typeToRemove == null) {
                return;
            }
            if (abilities.TryGetValue(typeToRemove.AssemblyQualifiedName, out var ability))
            {
                abilities.Remove(typeToRemove.AssemblyQualifiedName);
                AssetDatabase.RemoveObjectFromAsset(ability);
                ability.RemoveAllInstances();
                DestroyImmediate(ability, true);
                RaiseRequirementRemovedEvent(typeToRemove);
            }
        }

        /// <summary>
        /// Remove <see cref="AbilityComponent"/> of specified type, this operation will stop if there's at least 1 <see cref="Ability"/> instance relying on the specified <see cref="AbilityComponent"/>
        /// </summary>
        internal void RemoveAbilityComponent(Type typeToRemove)
        {
            string str = typeToRemove.AssemblyQualifiedName;
            if (components.ContainsKey(str))
            {
                if (RequireAbilityComponentsAttribute.ReverseRequirement.ContainsKey(typeToRemove))
                {
                    StringBuilder sb = new();
                    sb.Append($"Cannot remove AbilityComponent: {typeToRemove.FullName}, since the following abilities requires it: ");
                    bool flag = false;
                    foreach (Type t in RequireAbilityComponentsAttribute.ReverseRequirement[typeToRemove])
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
                        return;
                    }
                }
            }
            if (components.TryGetValue(typeToRemove.AssemblyQualifiedName, out var cmp))
            {
                components.Remove(str);
                AssetDatabase.RemoveObjectFromAsset(cmp);
                DestroyImmediate(cmp, true);
            }
        }
#endif
    }
}

