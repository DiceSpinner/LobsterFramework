using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.Utility;

namespace LobsterFramework.Editors
{
    public static class EditorUtils
    {
        #region Button
        public static bool Button(Color color, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            Color before = GUI.color;
            GUI.color = color;
            bool result = GUILayout.Button(content, style, options);
            GUI.color = before;
            return result;
        }

        public static bool Button(Color color, GUIContent content, params GUILayoutOption[] options)
        {
            Color before = GUI.color;
            GUI.color = color;
            bool result = GUILayout.Button(content, options);
            GUI.color = before;
            return result;
        }

        public static bool Button(Color color, string content, GUIStyle style, params GUILayoutOption[] options)
        {
            Color before = GUI.color;
            GUI.color = color;
            bool result = GUILayout.Button(content, style, options);
            GUI.color = before;
            return result;
        }

        public static bool Button(Color color, string content, params GUILayoutOption[] options)
        {
            Color before = GUI.color;
            GUI.color = color;
            bool result = GUILayout.Button(content, options);
            GUI.color = before;
            return result;
        }
        #endregion

        public static GUIStyle BoldButtonStyle = new(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };

        public static GUIStyle CentredTitleLabelStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14 };

        #region SerializedProperty
        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool SetPropertyPointer(SerializedProperty property, string name) {
            property.Reset();
            while (property.name != name) {
                if (!property.Next(true)) {
                    return false;
                }
            }
            return true;
        }

        public static void DrawSubProperties(SerializedProperty property) {
            property.NextVisible(true);
            do
            {
                if (property.displayName != "Script")
                {
                    EditorGUILayout.PropertyField(property);
                }
            }
            while (property.NextVisible(false));
        }

        #endregion

        public static Texture2D MakeBackgroundTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D backgroundTexture = new Texture2D(width, height);

            backgroundTexture.SetPixels(pixels);
            backgroundTexture.Apply();

            return backgroundTexture;
        }

        private static Dictionary<Type, Texture2D> scriptIcons = new();
        public static Texture2D GetScriptIcon(Type type) {
            if (!type.IsSubclassOf(typeof(ScriptableObject)))
            {
                Debug.LogWarning($"Cannot read script icon for {type.FullName}. It is not a ScriptableObject.");
                return null;
            }
            if (scriptIcons.TryGetValue(type, out Texture2D value)) {
                return value;
            }

            MonoScript script = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(type));
            SerializedObject scriptObj = new(script);
            SerializedProperty iconProperty = scriptObj.FindProperty("m_Icon");
            Texture2D texture = (Texture2D)iconProperty.objectReferenceValue;
            if (texture != null)
            {
                scriptIcons[type] = texture;
            }
            return texture;
        }
    }
}
