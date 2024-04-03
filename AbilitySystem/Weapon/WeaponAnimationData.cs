using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;
using System;

namespace LobsterFramework.AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability/WeaponAnimationData")]
    public class WeaponAnimationData : ScriptableObject
    {
        [SerializeField] internal List<AbilityAnimationConfig> setting;
        [SerializeField] internal List<AnimationClip> moveSetting;

        public AnimationClip GetAbilityClip(WeaponType weaponType, Type abilityType, int entry=0) {
            AbilityAnimationConfig st = setting[(int)weaponType];
            string ability = abilityType.AssemblyQualifiedName;
            if (st.TryGetValue(ability, out AnimationClipArray clips)) {
                return clips[entry];
            }
            return null;
        }

        public AnimationClip GetMoveClip(WeaponType weaponType) {
            return moveSetting[(int)weaponType];
        }
    }

    [Serializable]
    public class WeaponStatDictionary : SerializableDictionary<string, WeaponStat> { }
    [Serializable]
    public class AbilityAnimationConfig : SerializableDictionary<string, AnimationClipArray> { }
}
