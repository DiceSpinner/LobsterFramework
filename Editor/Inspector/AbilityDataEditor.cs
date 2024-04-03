using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using LobsterFramework.AbilitySystem;
using System.Linq;
using NUnit.Framework;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(AbilityData))]
    public class AbilityDataEditor : Editor
    {
        private readonly Dictionary<Type, Editor> abilityEditors = new();
        private readonly Dictionary<Type, Editor> abilityComponentEditors = new();
        private readonly GUIStyle style1 = new();
        private readonly GUIStyle style2 = new();

        private readonly GUIStyle selectAbilityStyle = new();
        private readonly GUIStyle selectAbilityComponentStyle = new();

        private Ability selectedAbility = null;
        private AbilityComponent selectedAbilityComponent = null;
        public Ability newSelectedAbility = null;
        public AbilityComponent newSelectedAbilityComponent = null;

        private Rect addAbilityRect;
        private Rect selectAbilityRect;
        private Rect addAbilityComponentRect;
        private Rect selectAbilityComponentRect;

        public Type removedAbility = null;
        public Type removedAbilityComponent = null;

        public void OnEnable()
        {
            style1.normal.textColor = Color.cyan;
            style1.fontStyle = FontStyle.Bold;
            style2.normal.textColor = Color.yellow;
            style2.fontStyle = FontStyle.Bold;

            selectAbilityStyle.fontStyle = FontStyle.Bold;
            selectAbilityStyle.alignment = TextAnchor.MiddleLeft;
            selectAbilityStyle.normal.textColor = Color.yellow;
            selectAbilityStyle.hover.background = Texture2D.grayTexture;

            selectAbilityComponentStyle.fontStyle = FontStyle.Bold;
            selectAbilityComponentStyle.normal.textColor = Color.cyan;
            selectAbilityComponentStyle.alignment = TextAnchor.MiddleLeft;
            selectAbilityComponentStyle.hover.background = Texture2D.grayTexture;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AbilityData abilityData = (AbilityData)target;

            if (Event.current.type == EventType.Layout) {
                if (newSelectedAbility != null) {
                    selectedAbility = newSelectedAbility;
                    newSelectedAbility = null;
                }
                if (newSelectedAbilityComponent != null) {
                    selectedAbilityComponent = newSelectedAbilityComponent;
                    newSelectedAbilityComponent = null;
                }

                if (removedAbility != null) {
                    var m = typeof(AbilityData).GetMethod("RemoveAbility", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo removed = m.MakeGenericMethod(removedAbility);
                    DestroyImmediate(abilityEditors[removedAbility]);
                    abilityEditors.Remove(removedAbility);
                    removed.Invoke(abilityData, null);
                    removedAbility = null;
                }

                if (removedAbilityComponent != null) {
                    var m = typeof(AbilityData).GetMethod("RemoveAbilityComponent", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo removed = m.MakeGenericMethod(selectedAbilityComponent.GetType());
                    DestroyImmediate(abilityComponentEditors[removedAbilityComponent]);
                    abilityComponentEditors.Remove(removedAbilityComponent);
                    removed.Invoke(abilityData, null);
                    removedAbilityComponent = null;
                }
            }

            EditorGUILayout.HelpBox("Note: When editing list properties of abilities, drag reference directly to the list itself instead of its element fields, " +
            "otherwise the reference may not be saved.", MessageType.Info, true);

            EditorGUI.BeginChangeCheck();

            DrawAbilityComponents(abilityData);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
            DrawAbilities(abilityData);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawAbilityComponents(AbilityData abilityData) {
            SerializedProperty abilityComponents = serializedObject.FindProperty("components");
            EditorGUILayout.BeginHorizontal();
            abilityComponents.isExpanded = EditorGUILayout.Foldout(abilityComponents.isExpanded, "Ability Components: " + abilityData.components.Values.Count);
            GUILayout.FlexibleSpace();
            bool aButton = GUILayout.Button("Add", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (aButton) // Add action component button clicked
            {
                addAbilityComponentRect.position = Event.current.mousePosition;
                AddAbilityComponentPopup popup = new();
                popup.data = abilityData;
                PopupWindow.Show(addAbilityComponentRect, popup);
            }

            if (abilityComponents.isExpanded)
            {
                if (abilityData.components.Count == 0)
                {
                    EditorGUILayout.LabelField("No ability components available for display!");
                }
                else
                {
                    EditorGUILayout.Space();
                    if (selectedAbilityComponent == null)
                    {
                        selectedAbilityComponent = abilityData.components.First().Value;
                    }
                    Editor editor;
                    Type type = selectedAbilityComponent.GetType();
                    if (abilityComponentEditors.TryGetValue(type, out Editor componentsEditor))
                    {
                        editor = componentsEditor;
                    }
                    else
                    {
                        editor = CreateEditor(selectedAbilityComponent);
                        abilityComponentEditors.Add(type, editor);
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUIContent content = new();
                    bool selected;
                    content.text = selectedAbilityComponent.GetType().Name;
                    if (AddAbilityComponentMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
                    {
                        content.image = icon;
                        selected = GUILayout.Button(content, selectAbilityComponentStyle, GUILayout.Height(40));
                    }
                    else
                    {
                        selected = GUILayout.Button(content, selectAbilityComponentStyle);
                    } 
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginVertical();
                    bool clicked = EditorUtils.Button(Color.red, "Remove", EditorUtils.BoldButtonStyle(), GUILayout.Width(80));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();

                    editor.OnInspectorGUI();

                    if (selected)
                    {
                        selectAbilityComponentRect.position = Event.current.mousePosition;
                        SelectAbilityComponentPopup popup = new SelectAbilityComponentPopup();
                        popup.editor = this;
                        popup.data = abilityData;
                        PopupWindow.Show(selectAbilityComponentRect, popup);
                    }
                    
                    if (clicked) {
                        removedAbilityComponent = type;
                    }
                }
            }
        }

        private void DrawAbilities(AbilityData abilityData) {
            // Draw Ability Section
            SerializedProperty abilities = serializedObject.FindProperty("abilities");

            EditorGUILayout.BeginHorizontal();
            abilities.isExpanded = EditorGUILayout.Foldout(abilities.isExpanded, "Abilities: " + abilityData.abilities.Count);
            GUILayout.FlexibleSpace();
            bool aiButton = GUILayout.Button("Add Ability", GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            if (aiButton) // Add ability button clicked 
            {
                addAbilityRect.position = Event.current.mousePosition;
                AddAbilityPopup.data = abilityData;
                AddAbilityPopup popup = new AddAbilityPopup();
                PopupWindow.Show(addAbilityRect, popup);
            }

            if (abilities.isExpanded)
            {
                EditorGUILayout.Space();
                UnityEditor.Editor editor;
                if (abilityData.abilities.Count == 0)
                {
                    EditorGUILayout.LabelField("No abilities available for display!");
                }
                else
                {
                    if (selectedAbility == null)
                    {
                        selectedAbility = abilityData.abilities.First().Value;
                    }

                    Type abilityType = selectedAbility.GetType();
                    if (!abilityEditors.ContainsKey(abilityType))
                    {
                        editor = CreateEditor(selectedAbility);
                        abilityEditors.Add(abilityType, editor);
                    }
                    else
                    {
                        editor = abilityEditors[abilityType];
                    }

                    #region DrawAbilityEditorSection
                    EditorGUILayout.BeginHorizontal();

                    GUIContent content = new();
                    bool selected;
                    content.text = selectedAbility.GetType().Name;
                    if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(abilityType, out Texture2D icon))
                    {
                        content.image = icon;
                        selected = GUILayout.Button(content, selectAbilityStyle, GUILayout.Height(40));
                    }
                    else
                    {
                        selected = GUILayout.Button(content, selectAbilityStyle);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginVertical();
                    bool clicked = EditorUtils.Button(Color.red, "Remove Ability", EditorUtils.BoldButtonStyle(), GUILayout.Width(110));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();

                    editor.OnInspectorGUI();

                    if (selected)
                    {
                        selectAbilityRect.position = Event.current.mousePosition;
                        SelectAbilityPopup popup = new SelectAbilityPopup();
                        popup.editor = this;
                        popup.data = abilityData;
                        PopupWindow.Show(selectAbilityRect, popup);
                    }

                    if (clicked)
                    {
                        removedAbility = abilityType;
                    }
                    GUILayout.FlexibleSpace();
                }
                #endregion
            }
        }

        private void OnDestroy()
        {
            foreach (Editor editor in abilityEditors.Values) {
                DestroyImmediate(editor);
            }
            foreach (Editor editor in abilityComponentEditors.Values) {
                DestroyImmediate(editor);
            }
        }
    }
}
