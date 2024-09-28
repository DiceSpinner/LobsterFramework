using System;

namespace LobsterFramework.Init
{
    /// <summary>
    /// Attributes that will be feteched for each public class at the start of the game or after compilation. For more info, check out <see cref="RegisterInitializationAttribute"/>.
    /// </summary>
    public abstract class InitializationAttribute : Attribute
    {
        public const string CompatabilityCheckerMethodName = "IsCompatible";
        /// <summary>
        /// Perform initialization tasks
        /// </summary>
        /// <param name="type">The class type this attribute is applied on</param>
        internal protected abstract void Init(Type type);
    }
}
