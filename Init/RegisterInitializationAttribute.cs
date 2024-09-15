using System;

namespace LobsterFramework.Init
{
    /// <summary>
    /// Attributes used to mark a custom attribute as an initialization attribute. Meaning it'll be initialized in editor mode after code recompiles or at runtime.
    /// </summary>
    /// <remarks>
    /// The attribute inherit from <see cref="InitializationAttribute"/>, be sealed, and implement a static method returns bool with name as <see cref="InitializationAttribute.CompatabilityCheckerMethodName"/>/>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RegisterInitializationAttribute : Attribute
    {
        internal int priority;
        public InitializationAttributeType AttributeType = InitializationAttributeType.Runtime;

        /// <param name="priority"> Determines the order this attribute will be initialized compared to other attribtues. Higher priority means earlier initialization. </param>
        public RegisterInitializationAttribute(int priority=0)
        {
            this.priority = priority;
        }   
    }

    public enum InitializationAttributeType { 
        Editor = 3,
        Runtime = 1,
        Dual = 2
    }
}
