using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AI;
using System;
using NUnit.Framework;
using LobsterFramework.Utility;


namespace LobsterFramework.Editors
{
    public class SelectStatePopup : PopupWindowContent
    {
        private StateDataEditor editor;
        private StateData data;
        private Vector2 scrollPosition;
        private Dictionary<MenuGroup<Type>, List<State>> groups = new();

        public SelectStatePopup(StateDataEditor editor, StateData data) {
            this.editor = editor;
            this.data = data;
            PopulateMapping();
        }

        public override void OnGUI(Rect rect)
        {
            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find WeaponData or Editor!");
                return;
            }
            
            GUILayout.BeginVertical();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            Color color = GUI.color;

            foreach (var kwp in groups) {
                GUI.color = StateEditorConfig.MenuColor;
                GUILayout.Label(kwp.Key.pathName);
                GUI.color = StateEditorConfig.StateColor;
                foreach (State state in kwp.Value) {
                    Type type = state.GetType();

                    // Display icon in options if there's one for the script
                    GUIContent content = new();
                    string key = type.AssemblyQualifiedName;
                    
                    if (state == data.initialState)
                    {
                        content.text = $"{type.Name} (Initial State)";
                    }
                    else {
                        content.text = type.Name;
                    }
                   
                    if (AddStateMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
                    {
                        content.image = icon;
                    }

                    if (state == data.initialState) {
                        GUI.color = Color.green;
                    }
                    if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
                    {
                        editor.nextState = data.states[key];
                        editorWindow.Close();
                    }
                    GUI.color = StateEditorConfig.StateColor;
                }
            }

            GUI.color = color;
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void PopulateMapping() {
            foreach (State state in data.states.Values)
            {
                Type type = state.GetType();
                MenuGroup<Type> group;
                if (AddStateMenuAttribute.stateMenus.ContainsKey(type))
                {
                    group = AddStateMenuAttribute.stateMenus[type];
                }
                else {
                    Debug.LogWarning($"{state.name} is not visible to the editor script. Make sure it is added to the menu via {nameof(AddStateMenuAttribute)}.");
                    continue;
                }
                if (!groups.ContainsKey(group))
                {
                    groups[group] = new List<State>();
                }
                groups[group].Add(state);
            }
        }
    }
}
