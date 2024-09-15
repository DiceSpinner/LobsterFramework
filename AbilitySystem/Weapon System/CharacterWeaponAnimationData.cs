using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;
using System;
using System.Linq;
using LobsterFramework.Init;
using TypeCache = LobsterFramework.Utility.TypeCache;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Animations and other important data for weapon abilities.
    /// </summary>
    [CreateAssetMenu(menuName = "Ability/CharacterWeaponAnimationData")]
    public class CharacterWeaponAnimationData : ScriptableObject
    {
        [SerializeField] internal List<AbilityAnimationDictionary> abilityAnimations;
        [SerializeField] internal List<AnimationClip> movementAnimations;
        [SerializeField] internal List<WeaponAbilityAddOnDictionary> animationAddons;

#if UNITY_EDITOR
        private void OnValidate() {
            if (AttributeInitialization.Finished)
            {
                Verify();
            }
            else {
                AttributeInitialization.OnInitializationComplete -= Verify;
                AttributeInitialization.OnInitializationComplete += Verify;
            }
        }

        internal void Verify() {
            abilityAnimations ??= new();
            movementAnimations ??= new();
            animationAddons ??= new();

            #region Correct Ability Animation Entries
            // Remove null dictionaries
            for (int i = abilityAnimations.Count - 1; i >= 0; i--)
            {
                if (abilityAnimations[i] == null)
                {
                    abilityAnimations.RemoveAt(i);
                }
            }
            int enumSize = EnumCache.GetSize<WeaponType>();
            // Make sure the number of dictionaries is equal to the number of weapon types
            if (abilityAnimations.Count < enumSize)
            {
                for (int i = abilityAnimations.Count; i < enumSize; i++)
                {
                    abilityAnimations.Add(new());
                }
            }
            else if (abilityAnimations.Count > enumSize)
            {
                abilityAnimations.RemoveRange(enumSize, abilityAnimations.Count - enumSize);
            }

            // Ensure animation clip array for every weapon art compatible with the weapon type has the correct number of entries
            for (int i = 0;i < abilityAnimations.Count;i++) {
                AbilityAnimationDictionary abilityAnimationArrayMap = abilityAnimations[i];
                // Remove invalid ability entries
                foreach (string typeName in abilityAnimationArrayMap.Keys.ToList())
                {
                    Type abilityType = TypeCache.GetTypeByName(typeName);
                    if (abilityType == null) { // Types may be deleted after changes to codebase, remove those entries
                        abilityAnimationArrayMap.Remove(typeName);
                        continue;
                    }
                }
                // Add valid weapon art entries if not present and ensure each of the ability have correct clip array size allocated
                foreach (Type weaponArt in WeaponArtAttribute.weaponArtsByWeaponType[i]) {
                    int correctSize = 1;
                    if (WeaponAnimationAttribute.AbilityAnimationInfo.TryGetValue(weaponArt, out var enums))
                    {
                        correctSize = EnumCache.GetSize(enums);
                    }

                    if (!abilityAnimationArrayMap.TryGetValue(weaponArt.AssemblyQualifiedName, out var clipArray) || clipArray.Count != correctSize) {
                        abilityAnimationArrayMap[weaponArt.AssemblyQualifiedName] = new AnimationClip[correctSize];
                    }
                }
            }

            #endregion

            #region Correct Movement Animation Entries
            if (movementAnimations.Count < enumSize)
            {
                for (int i = movementAnimations.Count; i < enumSize; i++)
                {
                    movementAnimations.Add(null);
                }
            }
            else if (movementAnimations.Count > enumSize)
            {
                movementAnimations.RemoveRange(enumSize, movementAnimations.Count - enumSize);
            }

            #endregion

            #region Correct Ability Setting Entries
            for (int i = animationAddons.Count - 1; i >= 0; i--)
            {
                if (animationAddons[i] == null)
                {
                    animationAddons.RemoveAt(i);
                }
            }
            if (animationAddons.Count < enumSize)
            {
                for (int i = animationAddons.Count; i < enumSize; i++)
                {
                    animationAddons.Add(new());
                }
            }
            else if (animationAddons.Count > enumSize)
            {
                for (int i = animationAddons.Count - 1;i > enumSize;i--) {
                    var animationAddonMap = animationAddons[i];
                    foreach (var item in animationAddonMap.Values) {
                        DestroyImmediate(item, true);
                    }
                    animationAddons.RemoveAt(i);
                }
            }

            for (int i = 0; i < EnumCache.GetSize<WeaponType>(); i++)
            {
                VerifyAddonEntries(animationAddons[i], WeaponArtAttribute.weaponArtsByWeaponType[i]);
            }
            #endregion
        }

        private void VerifyAddonEntries(WeaponAbilityAddOnDictionary dataSet, List<Type> weaponArts) {
            // Validate existing entries, remove addon assets that are not valid after recompilation and replace with correct addon asset if necessary
            foreach (var name in dataSet.Keys.ToList()) {
                Type abilityType = TypeCache.GetTypeByName(name);
                if(abilityType == null || !WeaponAnimationAddonAttribute.RegisteredAddons.ContainsKey(abilityType)) {
                    if (dataSet[name] != null) {
                        DestroyImmediate(dataSet[name], true);
                    }
                    dataSet.Remove(name); 
                    continue; 
                }

                Type correctType = WeaponAnimationAddonAttribute.RegisteredAddons[abilityType];
                var obj = dataSet[name];
                if (obj != null) {
                    if (obj.GetType() == correctType) {
                        continue;
                    }
                    DestroyImmediate(dataSet[name], true);
                }     
                var asset = CreateInstance(correctType);
                dataSet[name] = asset;
                if (EditorUtility.IsPersistent(this))
                {
                    AssetDatabase.AddObjectToAsset(asset, this);
                }
            }

            // Add required entries if not already present
            foreach (Type weaponArt in weaponArts)
            {
                if (dataSet.ContainsKey(weaponArt.AssemblyQualifiedName)) {
                    continue;
                }
                if (WeaponAnimationAddonAttribute.RegisteredAddons.TryGetValue(weaponArt, out Type correctType)) 
                {
                    var asset = CreateInstance(correctType);
                    dataSet[weaponArt.AssemblyQualifiedName] = asset;
                    if (EditorUtility.IsPersistent(this) )
                    {
                        AssetDatabase.AddObjectToAsset(asset, this);
                    }
                }
            }
        }

        private void Reset()
        {
            abilityAnimations?.Clear();
            movementAnimations?.Clear();
            animationAddons?.Clear();
            OnValidate();
        }
#endif

        /// <summary>
        /// Find the animation clip of the weapon ability
        /// </summary>
        /// <typeparam name="T">The type of the ability</typeparam>
        /// <param name="weaponType">The type of the weapon</param>
        /// <param name="entry">The index to the array of animation clips</param>
        /// <returns>The animation clip of the specified weapon ability with regards to the specified weapon type and array index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="entry"/> is not a valid index that can be converted to the enum registered via  <see cref="WeaponAnimationAttribute"/></exception>
        public AnimationClip GetAbilityClip<T>(WeaponType weaponType, int entry=0) where T : Ability {
            AbilityAnimationDictionary st = abilityAnimations[(int)weaponType];
            string ability = typeof(T).AssemblyQualifiedName;
            if (st.TryGetValue(ability, out AnimationClipArray clips)) {
                return clips[entry];
            }
            return null;
        }

        /// <summary>
        /// Find the animation clip for movement of the weapon
        /// </summary>
        /// <param name="weaponType"></param>
        /// <returns></returns>
        public AnimationClip GetMoveClip(WeaponType weaponType) {
            return movementAnimations[(int)weaponType];
        }

        public T GetWeaponAbilitySetting<T, V>(WeaponType weaponType) where T : ScriptableObject where V : Ability 
        {
            return (T)animationAddons[(int)weaponType][typeof(V).AssemblyQualifiedName]; 
        } 
    }

    [Serializable]
    internal class WeaponAbilityAddOnDictionary : SerializableDictionary<string, ScriptableObject> { }

    [Serializable]
    internal class WeaponStatDictionary : SerializableDictionary<string, WeaponStat> { }
    [Serializable]
    internal class AbilityAnimationDictionary : SerializableDictionary<string, AnimationClipArray> { }
}
