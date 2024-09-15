using LobsterFramework.AbilitySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.Init
{
    /// <summary>
    /// Provides access to singleton components defined by LobsterFramework
    /// </summary>
    public static class Singleton
    {
        private static GameObject obj;
        public static GameObject SingletonObject { 
            get {
                if (obj == null) { obj = new(); }
                return obj;
            }
        }
    }
}
