using System;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.Utility
{
    public static class TypeCache
    {
        private static Dictionary<string, Type> cache = new();
        public static Type GetTypeByName(string typeName) {
            try
            {
                return cache[typeName];
            }
            catch (KeyNotFoundException)
            {
                Type result = Type.GetType(typeName);
                if (result != default) {
                    cache[typeName] = result;
                }
                return result;
            }
        }
    }
}
