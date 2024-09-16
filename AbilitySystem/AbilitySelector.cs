using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;
using System;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// A serializable ability instance. Can be restricted type via <see cref="RestrictAbilityTypeAttribute"/>.
    /// </summary>
    [Serializable]
    public class AbilitySelector : SerializableType<Ability>
    {
        [SerializeField] internal string instance = Ability.DefaultAbilityInstance;
        public Type AbilityType { get { return Type;  } }
        public string Instance { get { return instance; } }
    }
}
