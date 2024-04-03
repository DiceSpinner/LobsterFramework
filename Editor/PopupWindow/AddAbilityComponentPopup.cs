using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;
using System.Reflection;

namespace LobsterFramework.Editors
{
    public class AddAbilityComponentPopup : PopupWindowContent
    {
        public AbilityData data;
        private Vector2 scrollPosition;

        public override void OnGUI(Rect rect)
        {
            if (data == null)
            {
                EditorGUILayout.LabelField("Cannot Find AbilityData!");
                return;
            }
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            bool hasAbilityStat = false;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (Type type in AddAbilityComponentMenuAttribute.types)
            {
                // Display icon in options if there's one for the ability script
                if (data.components.ContainsKey(type.AssemblyQualifiedName))
                {
                    continue;
                }
                hasAbilityStat = true;
                GUIContent content = new();
                content.text = type.Name;
                if (AddAbilityComponentMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
                {
                    content.image = icon;
                }
                if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                {
                    var m = typeof(AbilityData).GetMethod("AddAbilityComponent", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo mRef = m.MakeGenericMethod(type);
                    mRef.Invoke(data, null);
                }
            }
            GUILayout.EndScrollView();
            if (!hasAbilityStat) {
                GUIStyle color = new();
                color.normal.textColor = Color.yellow;
                EditorGUILayout.LabelField("No AbilityComponent to add!", color);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}
