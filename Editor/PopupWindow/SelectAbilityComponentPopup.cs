using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;

namespace LobsterFramework.Editors
{
    public class SelectAbilityComponentPopup : PopupWindowContent
    {
        public AbilityDataEditor editor;
        public AbilityData data;
        private Vector2 scrollPosition;

        public override void OnGUI(Rect rect)
        {
            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find AbilityData or Editor!");
                return;
            }
            GUILayout.BeginVertical();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (Type type in AddAbilityComponentMenuAttribute.types)
            { 
                if (type == editor.removedAbilityComponent) {
                    continue;
                }
                // Display icon in options if there's one for the ability script
                string key = type.AssemblyQualifiedName;
                if (!data.components.ContainsKey(key))
                {
                    continue;
                }
                GUIContent content = new();
                content.text = type.Name;
                if (AddAbilityComponentMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
                {
                    content.image = icon;
                }
                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    editor.newSelectedAbilityComponent = data.components[key];
                    editorWindow.Close();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
