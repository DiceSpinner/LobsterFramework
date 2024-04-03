using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

        public static GUIStyle BoldButtonStyle()
        {
            GUIStyle style = new(GUI.skin.button);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            return style;
        }

        public static bool SetPropertyPointer(SerializedProperty property, string name) {
            property.Reset();
            while (property.name != name) {
                if (!property.Next(true)) {
                    return false;
                }
            }
            return true;
        }
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
