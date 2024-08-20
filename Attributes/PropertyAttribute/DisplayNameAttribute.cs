using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework
{
    /// <summary>
    /// Overrides the displayed name of the field in inspector. Need to be used before other Unity property attributes to avoid being overriden.
    /// </summary>
    public class DisplayNameAttribute : PropertyAttribute
    {
        public string name; 

        public DisplayNameAttribute(string name)
        {
            this.name = name;
        }
    }
}
