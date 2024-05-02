using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.Utility
{
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
}
