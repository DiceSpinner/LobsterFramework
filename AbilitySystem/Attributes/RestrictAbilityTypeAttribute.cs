using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.AbilitySystem
{
    /// <summary>
    /// Applies to <see cref="AbilitySelector"/> to restrict the set of items available in the menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class RestrictAbilityTypeAttribute : Attribute
    {
        public Type ParentType;
        public bool IncludeParent;

        public RestrictAbilityTypeAttribute(Type parentType) { this.ParentType = parentType; }
    }
}
