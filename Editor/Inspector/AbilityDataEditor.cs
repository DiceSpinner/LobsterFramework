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

        internal Ability selectedAbility = null;
        private AbilityComponent selectedAbilityComponent = null;
        public Ability newSelectedAbility = null;
        public AbilityComponent newSelectedAbilityComponent = null;

        private Rect addAbilityRect;
        private Rect selectAbilityRect;
        private Rect addComponentRect;
        private Rect selectAbilityComponentRect;

        public Type removedAbility = null;
        public Type removedAbilityComponent = null;

        public void OnEnable()
        {
            style1.normal.textColor = Color.cyan;
            style1.fontStyle = FontStyle.Bold;
            style2.normal.textColor = Color.yellow;
            style2.fontStyle = FontStyle.Bold;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AbilityData abilityData = (AbilityData)target;
            bool isAsset = EditorUtility.IsPersistent(abilityData);

            #region Update AbilityData
            if (Event.current.type == EventType.Layout) {
                if (newSelectedAbility != null)
                {
                    selectedAbility = newSelectedAbility;
                    newSelectedAbility = null;
                }
                else if (selectedAbility == null && abilityData.abilities.Count > 0)
                {
                    selectedAbility = abilityData.abilities.First().Value;
                }


                if (newSelectedAbilityComponent != null) {
                    selectedAbilityComponent = newSelectedAbilityComponent;
                    newSelectedAbilityComponent = null;
                }
                else if (selectedAbilityComponent == null && abilityData.components.Count > 0)
                {
                    selectedAbilityComponent = abilityData.components.First().Value;
                }

                if (removedAbility != null) {
                    var m = typeof(AbilityData).GetMethod(nameof(AbilityData.RemoveAbility), BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo removed = m.MakeGenericMethod(removedAbility);
                    DestroyImmediate(abilityEditors[removedAbility]);
                    abilityEditors.Remove(removedAbility);
                    removed.Invoke(abilityData, null);
                    removedAbility = null;
                }

                if (removedAbilityComponent != null) {
                    var m = typeof(AbilityData).GetMethod(nameof(AbilityData.RemoveAbilityComponent), BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo removed = m.MakeGenericMethod(selectedAbilityComponent.GetType());
                    DestroyImmediate(abilityComponentEditors[removedAbilityComponent]);
                    abilityComponentEditors.Remove(removedAbilityComponent);
                    removed.Invoke(abilityData, null);
                    removedAbilityComponent = null;
                }
            }
            #endregion

            EditorGUI.BeginChangeCheck();

            DrawAbilityComponents(abilityData, isAsset);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
            DrawAbilities(abilityData, isAsset);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawAbilityComponents(AbilityData abilityData, bool isAsset) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ability Components: " + abilityData.components.Values.Count, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            bool addComponentClicked = false;

            // Add/Remove components at runtime is not allowed
            if (isAsset) {
                addComponentClicked = GUILayout.Button("Add", GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            #region Add Ability Component
            if (addComponentClicked) // Add ability button clicked
            {
                addComponentRect.position = Event.current.mousePosition;
                AddAbilityComponentPopup popup = new(abilityData);
                PopupWindow.Show(addComponentRect, popup);
            }
            #endregion

            if (abilityData.components.Count == 0)
            {
                EditorGUILayout.LabelField("No ability components available for display!");
                return;
            }
            if (selectedAbilityComponent == null) {
                return;
            }

            #region Create editor for selected component
            EditorGUILayout.Space();
            Editor editor;
            Type type = selectedAbilityComponent.GetType();
            if (abilityComponentEditors.TryGetValue(type, out Editor componentEditor))
            {
                editor = componentEditor;
            }
            else
            {
                editor = CreateEditor(selectedAbilityComponent);
                abilityComponentEditors.Add(type, editor);
            }
            #endregion

            #region Draw Selected Ability Component Buttons
            EditorGUILayout.BeginHorizontal();
            GUIContent content = new();
            bool selectAbilityComponentClicked;
            content.text = selectedAbilityComponent.GetType().Name;
            if (AddAbilityComponentMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
            {
                content.image = icon;
                selectAbilityComponentClicked = GUILayout.Button(content, AbilityEditorConfig.ComponentSelectionStyle, GUILayout.Height(40));
            }
            else
            {
                selectAbilityComponentClicked = GUILayout.Button(content, AbilityEditorConfig.ComponentSelectionStyle);
            } 
            GUILayout.FlexibleSpace();

            if (isAsset) {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (EditorUtils.Button(Color.red, "Remove", EditorUtils.BoldButtonStyle, GUILayout.Width(80))) {
                    removedAbilityComponent = type;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
                    
            EditorGUILayout.EndHorizontal();
            #endregion

            editor.OnInspectorGUI();

            #region SelectAbilityComponent Button Clicked
            if (selectAbilityComponentClicked)
            {
                selectAbilityComponentRect.position = Event.current.mousePosition;
                SelectAbilityComponentPopup popup = new SelectAbilityComponentPopup(this, abilityData);
                PopupWindow.Show(selectAbilityComponentRect, popup);
            }
            #endregion
        }

        private void DrawAbilities(AbilityData abilityData, bool isAsset) {
            // Draw Ability Section
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Abilities: " + abilityData.abilities.Count, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            bool addAbilityClicked = false;
            if (isAsset) {
                addAbilityClicked = GUILayout.Button("Add Ability", GUILayout.Width(110));
            }
            EditorGUILayout.EndHorizontal();

            if (addAbilityClicked)
            {
                addAbilityRect.position = Event.current.mousePosition;
                AddAbilityPopup popup = new AddAbilityPopup(abilityData);
                PopupWindow.Show(addAbilityRect, popup);
            }

            EditorGUILayout.Space();
            Editor editor;
            if (abilityData.abilities.Count == 0)
            {
                EditorGUILayout.LabelField("No abilities available for display!");
                return; 
            }
            if (selectedAbility == null) {
                return;
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
            content.text = selectedAbility.GetType().Name;
            bool selectClicked;
            if (AddAbilityMenuAttribute.abilityIcons.TryGetValue(abilityType, out Texture2D icon))
            {
                content.image = icon;
                selectClicked = GUILayout.Button(content, AbilityEditorConfig.AbilitySelectionStyle, GUILayout.Height(40));
            }
            else
            {
                selectClicked = GUILayout.Button(content, AbilityEditorConfig.AbilitySelectionStyle);
            }

            if (isAsset)
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (EditorUtils.Button(Color.red, "Remove Ability", EditorUtils.BoldButtonStyle, GUILayout.Width(110)))
                {
                    removedAbility = abilityType;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            editor.OnInspectorGUI();

            if (selectClicked)
            {
                selectAbilityRect.position = Event.current.mousePosition;
                SelectAbilityPopup popup = new SelectAbilityPopup(abilityData, this);
                PopupWindow.Show(selectAbilityRect, popup);
            }
            #endregion
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
