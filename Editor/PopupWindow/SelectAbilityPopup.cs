using LobsterFramework.AbilitySystem;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System;

namespace LobsterFramework.Editors
{
    public class SelectAbilityPopup : PopupWindowContent
    {
        public AbilityData data;
        private Vector2 scrollPosition = Vector2.zero;
        public AbilityDataEditor editor;

        public override void OnGUI(Rect rect)
        {
            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find AbilityData or Editor!");
                return;
            }
            GUILayout.BeginVertical();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (Type type in AddAbilityMenuAttribute.abilities)
            {
                if (type == editor.removedAbility) { 
                    continue;
                }
                // Display icon in options if there's one for the ability script
                string key = type.ToString();
                if (!data.abilities.ContainsKey(key))
                {
                    continue;
                }
                GUIContent content = new();
                content.text = type.Name;
                if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(type, out Texture2D icon)) 
                {
                    content.image = icon;
                }
                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    editor.newSelectedAbility = data.abilities[key];
                    editorWindow.Close();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
