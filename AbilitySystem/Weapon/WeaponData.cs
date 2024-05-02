using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    [CreateAssetMenu(menuName = "Ability/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] internal WeaponStatDictionary weaponStats;

#if UNITY_EDITOR
        /// <summary>
        /// Called by editor scritps, add <see cref="WeaponStat"/> of given type to the set of available WeaponStats if not already present
        /// </summary>
        /// <param name="type">The type of the <see cref="WeaponStat"/> to be created</param>
        internal void AddWeaponStat(Type type)
        {
            if (!type.IsSubclassOf(typeof(WeaponStat))) {
                Debug.LogError($"Type {type.FullName} is not a valid WeaponStat");
                return;
            }

            string str = type.AssemblyQualifiedName;
            if (weaponStats.ContainsKey(str))
            {
                return;
            }
            WeaponStat instance = (WeaponStat)CreateInstance(type);
            weaponStats[str] = instance;
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.AddObjectToAsset(instance, this);
                AssetDatabase.SaveAssets();
            }
        }

        private T GetWeaponStat<T>() where T : WeaponStat
        {
            string key = typeof(T).AssemblyQualifiedName;
            if (weaponStats.TryGetValue(key, out WeaponStat stat)) {
                return (T)stat;
            }
            return default;
        }

        internal bool RemoveWeaponStat<T>() where T : WeaponStat
        {
            string str = typeof(T).AssemblyQualifiedName;
            T cmp = GetWeaponStat<T>();
            if (cmp != null)
            {
                weaponStats.Remove(str);
                AssetDatabase.RemoveObjectFromAsset(cmp);
                DestroyImmediate(cmp, true);
                AssetDatabase.SaveAssets();
                return true;
            }
            return false;
        }
#endif
    }
}
