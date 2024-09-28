using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.Init
{
    /// <summary>
    /// Apply on methods to inject them into player loop system. The method must be static, returns void, and parameterless.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class PlayerLoopEventAttribute : Attribute
    {
        internal Type EventType;

        public PlayerLoopEventAttribute(Type eventType)
        {
            this.EventType = eventType;
        }
    }
}
