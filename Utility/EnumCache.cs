using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections.ObjectModel;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Helper class for quicker enum type queries. The result obtained from reflection is cached locally making subsequent queries faster.
    /// </summary>
    public static class EnumCache
    {
        private static readonly Dictionary<Type, ReadOnlyCollection<string>> cache = new();
        /// <summary>
        /// Finds the number of entries for the given enum type
        /// </summary>
        /// <typeparam name="T">The enum type to be inspected</typeparam>
        public static int GetSize<T>() where T : Enum
        {
            try {
                return cache[typeof(T)].Count;
            }catch(KeyNotFoundException) {
                Type t = typeof(T);
                cache[t] = new(Enum.GetNames(t));
                return cache[t].Count;
            }
        }

        /// <summary>
        /// Finds the number of entries for the given enum type
        /// </summary>
        /// <returns>The number of entries of the enum type, 0 if the type is not an enum type</returns>
        public static int GetSize(Type type)
        {
            try
            {
                return cache[type].Count;
            }
            catch (KeyNotFoundException)
            {
                if (!type.IsEnum) {
                    Debug.LogWarning($"Type {type.FullName} is not a valid enum type!");
                    return 0;
                }
                cache[type] = new(Enum.GetNames(type));
                return cache[type].Count;
            }
        }

        /// <summary>
        /// Finds the names of an enum type
        /// </summary>
        /// <typeparam name="T">The enum type to be inspected</typeparam>
        /// <returns>A collection of names of the enum entries</returns>
        public static ReadOnlyCollection<string> GetNames<T>() where T : Enum {
            try
            {
                return cache[typeof(T)];
            }
            catch (KeyNotFoundException)
            {
                Type t = typeof(T);
                cache[t] = new(Enum.GetNames(t));
                return cache[t];
            }
        }

        /// <summary>
        /// Finds the names of an enum type
        /// </summary>
        /// <returns>A collection of names of the enum entries, null if the argument is not an enum type</returns>
        public static ReadOnlyCollection<string> GetNames(Type type)
        {
            try
            {
                return cache[type];
            }
            catch (KeyNotFoundException)
            {
                if (!type.IsEnum)
                {
                    Debug.LogWarning($"Type {type.FullName} is not a valid enum type!");
                    return null;
                }
                cache[type] = new(Enum.GetNames(type));
                return cache[type];
            }
        }
    }
}
