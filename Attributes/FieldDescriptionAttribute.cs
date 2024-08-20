using System;

namespace LobsterFramework
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class FieldDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name;

        /// <summary>
        /// Description of this field
        /// </summary>
        public string Description;
    }
}
