using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LobsterFramework.Editors {
    public class AbilityEditorConfig : ScriptableObject
    {
        #region Setting Provider
        private static readonly string location = "Assets/LobsterFramework/Editor/Settings/AbilityEditorConfig.asset";
        private static AbilityEditorConfig instance;
        private static SerializedObject serializedObject;

        internal static AbilityEditorConfig Instance
        {
            get {
                return GetInstance();
            } 
        }

        private static AbilityEditorConfig GetInstance() {
            if (instance != null)
            {
                return instance;
            }
            instance = AssetDatabase.LoadAssetAtPath<AbilityEditorConfig>(location);

            if (instance == null)
            {
                instance = CreateInstance<AbilityEditorConfig>();
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
            var provider = new SettingsProvider("LobsterFramework/Ability Editor Setting", SettingsScope.User)
            {
                guiHandler = (searchContext) =>
                {
                    var settings = GetSerializedSettings();
                    var property = settings.GetIterator();
                    EditorUtils.DrawSubProperties(property);
                    settings.ApplyModifiedProperties();
                },
            };

            return provider;
        }
        #endregion

        [SerializeField] private List<FolderIcon> menuIcons;
        [SerializeField] internal Color menuPopupColor = Color.yellow;
        [SerializeField] internal Color abilityPopupColor = Color.white;
        [SerializeField] internal Color componentPopupColor = Color.white;
        [SerializeField] internal GUIStyle abilitySelectionStyle;
        [SerializeField] internal GUIStyle componentSelectionStyle;

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
        internal static Color AbilityPopupColor { get { return Instance.abilityPopupColor; } }

        internal static Color ComponentPopupColor { get { return Instance.componentPopupColor; } }

        internal static GUIStyle AbilitySelectionStyle { get { return Instance.abilitySelectionStyle;} }
        internal static GUIStyle ComponentSelectionStyle { get { return Instance.componentSelectionStyle;} }
    }
}
