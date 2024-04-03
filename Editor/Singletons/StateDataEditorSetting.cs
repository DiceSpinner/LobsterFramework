using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace LobsterFramework.Editors
{
    [UnityEditor.FilePath("Assets/LobsterFramework/Editor/Singletons/StateData Editor Setting.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
    public class StateDataEditorSetting : ScriptableSingleton<StateDataEditorSetting>
    {
        [SerializeField] private List<FolderIcon> menuIcons;
        [SerializeField] internal Color menuColor;
        [SerializeField] internal Color stateColor;

        internal Texture2D GetFolderIcon(string path) { 
            foreach (FolderIcon folderIcon in menuIcons) {
                if (folderIcon.path == path) { 
                    return folderIcon.icon;
                }
            } 
            return null;
        }

        private void OnEnable() 
        {
            if (!AssetDatabase.Contains(this)) {
                Save(true);
            }
        }
    }

    [Serializable]
    public class FolderIcon
    {
        public string name;
        [FilePath]
        public string path;
        public Texture2D icon;
    }
}
