using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using LobsterFramework.AbilitySystem;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(AbilityRunner))]
    public class AbilityRunnerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor editor;
        private bool editData = false;
        private string assetName = "";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Note: The ability data may not work properly before the first run of the game. " +
                "Enter play mode for the first time to verify its integrity!", MessageType.Info);
            base.OnInspectorGUI();
            AbilityRunner abilityRunner = (AbilityRunner)target;
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
            EditorGUILayout.LabelField("Is Blocked", abilityRunner.ActionBlocked + "");
            if (abilityRunner.executing.Count > 0) {
                foreach (AbilityInstance pair in abilityRunner.executing) {
                    EditorGUILayout.LabelField(pair.ability.GetType().Name, pair.configName);
                }
            }

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
                assetName = EditorGUILayout.TextField(assetName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save Ability Data", GUILayout.Width(150)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Saving Path", Application.dataPath, "");
                    abilityRunner.SaveAbilityData(assetName, path); 
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
