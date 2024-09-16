using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;
using System;

namespace LobsterFramework.AbilitySystem
{
    [Serializable]
    public class AbilitySelector : SerializableType<Ability>
    {
        [SerializeField] internal string instance = Ability.DefaultAbilityInstance;

        public string Instance { get { return instance; } }
    }
}
