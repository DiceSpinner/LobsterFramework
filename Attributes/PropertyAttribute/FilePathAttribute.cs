using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework
{
    public class FilePathAttribute : PropertyAttribute
    {
        public string extension;
        public string defaultName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultName">The default name of the lookup file, empty string means the current value of the field will be used.</param>
        /// <param name="extension">The file extension to look up for, empty string means to display all files</param>
        public FilePathAttribute(string defaultName="", string extension = "") {
            this.extension = extension;
            this.defaultName = defaultName;
        }
    }
}
