using System;

namespace LobsterFramework.Init { 
    /// <summary>
    /// Marks an assembly such that any assemblies referencing the target assembly will be inspected for attribute initialization
    /// </summary>
    [AttributeUsage (AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class AttributeProviderAttribute : Attribute
    {

    }
}
