using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// A type object that can be serialized and deserialized by Unity.
    /// </summary>
    /// <typeparam name="T">Constraint to the value of this type object, only types that equals this type parameter or inherits from it can be stored in this object.</typeparam>
    [Serializable]
    public class SerializableType<T> : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Returns Type.AssemblyQualifiedName of the serialized type value
        /// </summary>
        [SerializeField] internal string typeName;
        private Type type;

        public Type Type
        {
            get
            {
                return type;
            }
            set {
                if (value == null || value == typeof(T) || value.IsSubclassOf(typeof(T))) {
                    type = value;
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (typeName != default) {
                Type = Type.GetType(typeName);
            }
        }

        public void OnBeforeSerialize()
        {
            if (type != null) {
                typeName = type.AssemblyQualifiedName;
            }
        }
    }

    /// <summary>
    /// Unconstrainted version of <see cref="SerializableType{T}"/>
    /// </summary>
    [Serializable]
    public class SerializableType {
        /// <summary>
        /// Returns Type.AssemblyQualifiedName of the serialized type value
        /// </summary>
        [SerializeField] internal string typeName;
        public Type Type;

        public SerializableType() { }
        public SerializableType(Type type) { Type = type; }

        public void OnAfterDeserialize()
        {
            if (typeName != default)
            {
                Type = Type.GetType(typeName);
            }
        }

        public void OnBeforeSerialize()
        {
            if (Type != null)
            {
                typeName = Type.AssemblyQualifiedName;
            }
        }
    }
}
