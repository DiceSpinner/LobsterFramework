using System.Linq;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(Ability), true)]
    public class AbilityEditor : UnityEditor.Editor
    {
        private string selectedConfig;
        private string addConfigName;
        private Editor configEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();
            Ability ability = (Ability)target;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Config Name");
            GUILayout.FlexibleSpace();
            addConfigName = EditorGUILayout.TextField(addConfigName);

            if (GUILayout.Button("Add", GUILayout.Width(100)))
            {
                if (addConfigName == null)
                {
                    Debug.LogError("Field is empty!");
                }
                else
                {
                    ability.AddConfig(addConfigName);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (ability.configs.Count > 0)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUIStyle style = new();
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                style.hover.background = Texture2D.grayTexture;

                if (GUILayout.Button(selectedConfig, style, GUILayout.Width(100)))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (string configName in ability.configs.Keys)
                    {
                        menu.AddItem(new GUIContent(configName), false,
                            () =>
                            {
                                selectedConfig = configName;
                                if (configEditor != null) { DestroyImmediate(configEditor); }
                            });
                    }
                    menu.ShowAsContext();
                }

                if (selectedConfig == default)
                {
                    selectedConfig = ability.configs.First().Key;
                }
                if (configEditor == null) 
                {
                    configEditor = CreateEditor(ability.configs[selectedConfig]);
                }

                configEditor.OnInspectorGUI();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (EditorUtils.Button(Color.red, "Remove Config", EditorUtils.BoldButtonStyle(), GUILayout.Width(110)))
                {
                    ability.RemoveConfig(selectedConfig);
                    if (ability.configs.Count > 0)
                    {
                        selectedConfig = ability.configs.Last().Key;
                    }

                    DestroyImmediate(configEditor);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("No configs available");
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
