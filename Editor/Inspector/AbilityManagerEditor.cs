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
    public class AbilityManagerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor editor;
        private bool editData = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AbilityManager abilityManager = (AbilityManager)target;
            EditorGUI.BeginChangeCheck();
            SerializedProperty abilityData = serializedObject.FindProperty("abilityData");
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
            EditorGUILayout.LabelField("Is Blocked", abilityManager.ActionBlocked + "");
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
                EditorGUILayout.LabelField("Asset Name");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save", GUILayout.Width(80)))
                {
                    abilityManager.SaveRuntimeData(""); 
                }

                if (GUILayout.Button("Save As", GUILayout.Width(80)))
                {
                    string path = EditorUtility.SaveFilePanel("Select Saving Path", Application.dataPath, abilityManager.abilityData.name, "asset");
                    if (path != null) { abilityManager.SaveRuntimeData(path); } 
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
