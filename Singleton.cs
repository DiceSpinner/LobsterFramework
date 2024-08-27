using LobsterFramework.AbilitySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework
{
    public static class Singleton
    {
        public static GameObject SingletonObject { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeSingletons() { 
            SingletonObject = new GameObject();
            SingletonObject.AddComponent<AbilityExecutor>();
        }
    }
}