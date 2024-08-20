using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using LobsterFramework.AbilitySystem;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(AbilityManager))]
    public class AbilityManagerEditor : ReferenceProviderEditor
    {
        private Editor editor;
        private bool editData = false;

        private static GUIContent label = new("Action Blocked", "Flag to indicate whether the character is able to perform actions.");
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AbilityManager abilityManager = (AbilityManager)target;
            EditorGUI.BeginChangeCheck();
            SerializedProperty abilityData = serializedObject.FindProperty(nameof(abilityManager.abilityData));
            if (!editData)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (Application.isPlaying && GUILayout.Button("Edit Ability Data", GUILayout.Width(150)))
                {
                    editData = true;
                    editor = null;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            if (EditorApplication.isPlaying) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label);
                EditorGUILayout.LabelField(abilityManager.ActionBlocked + "");
                EditorGUILayout.EndHorizontal();
            }

            abilityManager.DisplayCurrentExecutingAbilitiesInEditor();

            if (editData)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                abilityData.isExpanded = EditorGUILayout.Foldout(abilityData.isExpanded, "Ability Data");
                if (abilityData.isExpanded && abilityData.objectReferenceValue != null)
                {
                    EditorGUI.indentLevel++;
                    if (editor == null)
                    {
                        editor = CreateEditor(abilityData.objectReferenceValue);
                    }
                    editor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save", GUILayout.Width(80)))
                {
                    abilityManager.Save(""); 
                }

                if (GUILayout.Button("Save As", GUILayout.Width(80)))
                {
                    string path = EditorUtility.SaveFilePanel("Select Saving Path", Application.dataPath, abilityManager.abilityData.name, "asset");
                    if (path != null) { abilityManager.Save(path); } 
                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
