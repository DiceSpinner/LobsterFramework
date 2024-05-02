using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LobsterFramework.Editors {
    public class StateEditorConfig : ScriptableObject
    {
        #region Setting Provider
        private static readonly string location = "Assets/LobsterFramework/Editor/Settings/StateEditorConfig.asset";
        private static StateEditorConfig instance;
        private static SerializedObject serializedObject;

        internal static StateEditorConfig Instance
        {
            get {
                return GetInstance();
            } 
        }

        private static StateEditorConfig GetInstance() {
            if (instance != null)
            {
                return instance;
            }
            instance = AssetDatabase.LoadAssetAtPath<StateEditorConfig>(location);

            if (instance == null)
            {
                instance = CreateInstance<StateEditorConfig>();
                AssetDatabase.CreateAsset(instance, location);
                AssetDatabase.SaveAssets();
            }
            serializedObject?.Dispose();
            serializedObject = new(instance);

            return instance;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            GetInstance();
            return serializedObject;
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("LobsterFramework/State Editor Setting", SettingsScope.User)
            {
                // By default the last token of the path is used as display name if no label is provided.
                // label = "State Editor Setting",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings = GetSerializedSettings();
                    var property = settings.GetIterator();
                    EditorUtils.DrawSubProperties(property);
                    
                    settings.ApplyModifiedProperties();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                // keywords = new HashSet<string>(new[] { "Number", "Some String" })
            };

            return provider;
        }
        #endregion

        [SerializeField] private List<FolderIcon> menuIcons;
        [SerializeField] internal Color menuPopupColor;
        [SerializeField] internal Color statePopupColor;
        [SerializeField] internal GUIStyle stateStyle;
        [SerializeField] internal GUIStyle initialStateStyle;

        internal static Texture2D GetFolderIcon(string path)
        {
            foreach (FolderIcon folderIcon in Instance.menuIcons)
            {
                if (folderIcon.path == path)
                {
                    return folderIcon.icon;
                }
            }
            return null;
        }

        internal static Color MenuPopupColor { get { return Instance.menuPopupColor; } }
        internal static Color StatePopupColor { get { return Instance.statePopupColor; } }

        internal static GUIStyle StateStyle { get { return Instance.stateStyle; } }
        internal static GUIStyle InitialStateStyle { get { return Instance.initialStateStyle; }  }
    }

    [Serializable]
    public class FolderIcon
    {
        [FilePath]
        public string path;
        public Texture2D icon;
    }
}
