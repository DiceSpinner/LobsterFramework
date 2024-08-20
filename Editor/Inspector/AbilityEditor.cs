using System.Linq;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(Ability), true)]
    public class AbilityEditor : Editor
    {
        private string selectedConfig;
        private string addInstanceName;
        private Editor configEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();
            Ability ability = (Ability)target;
            bool isAsset = AssetDatabase.Contains(ability);

            #region Add Instance
            if (isAsset) {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New Instance");
                GUILayout.FlexibleSpace();
                addInstanceName = EditorGUILayout.TextField(addInstanceName);

                if (GUILayout.Button("Create", GUILayout.Width(80)))
                {
                    if (addInstanceName == null)
                    {
                        Debug.LogWarning("Field cannot be empty!");
                    }
                    else
                    {
                        ability.AddInstance(addInstanceName);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            if (ability.configs.Count > 0)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUIStyle style = new();
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                style.hover.background = Texture2D.grayTexture;

                #region Select Instance
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
                #endregion

                configEditor.OnInspectorGUI();

                #region Remove Instance
                if (isAsset &&  selectedConfig != Ability.DefaultAbilityInstance) {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (EditorUtils.Button(Color.red, "Remove", EditorUtils.BoldButtonStyle, GUILayout.Width(80)))
                    {
                        ability.RemoveInstance(selectedConfig);
                        if (ability.configs.Count > 0)
                        {
                            selectedConfig = ability.configs.Last().Key;
                        }

                        DestroyImmediate(configEditor);
                    }
                    GUILayout.EndHorizontal();
                }
                #endregion
            }
            else
            {
                EditorGUILayout.LabelField("No ability instances available!");
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
