using System.Collections;
using System.Collections.Generic;
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
    }
}
