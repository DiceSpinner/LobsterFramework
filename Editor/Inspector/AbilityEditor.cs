using System.Linq;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(Ability), true)]
    public class AbilityEditor : Editor
    {
        private string newSelection;
        private bool removeSelection;
        private string selectedConfig;
        private string inputName;
        private string newInstance;
        private Editor configEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Ability ability = (Ability)target;
            bool isAsset = AssetDatabase.Contains(ability);

            if (Event.current.type == EventType.Layout) {
                if (newSelection != default) {
                    selectedConfig = newSelection;
                    DestroyImmediate(configEditor);
                    newSelection = default;
                }

                if (newInstance != default) {
                    ability.AddInstance(newInstance);
                    newInstance = default;
                }

                if (removeSelection) {
                    ability.RemoveInstance(selectedConfig);
                    if (ability.configs.Count > 0) {
                        selectedConfig = ability.configs.Last().Key;
                    }
                    removeSelection = false;
                    DestroyImmediate(configEditor);
                }

                if (ability.configs.Count > 0) {
                    if (selectedConfig == default)
                    {
                        selectedConfig = ability.configs.First().Key;
                    }

                    if (configEditor == null)
                    {
                        configEditor = CreateEditor(ability.configs[selectedConfig]);
                    }
                }
            }

            #region Add Instance
            if (isAsset) {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New Instance");
                GUILayout.FlexibleSpace();
                inputName = EditorGUILayout.TextField(inputName);

                if (GUILayout.Button("Create", GUILayout.Width(80)))
                {
                    if (inputName == null)
                    {
                        Debug.LogWarning("Field cannot be empty!");
                    }
                    else
                    {
                        newInstance = inputName;
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
                                newSelection = configName;
                            });
                    }
                    menu.ShowAsContext();
                }
                #endregion

                configEditor.OnInspectorGUI();

                #region Remove Instance
                if (isAsset) {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (selectedConfig == Ability.DefaultAbilityInstance) {
                        GUI.enabled = false;
                    }
                    if (EditorUtils.Button(Color.red, "Remove", EditorUtils.BoldButtonStyle, GUILayout.Width(80)))
                    {
                        removeSelection = true;
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
                #endregion
            }
            else
            {
                EditorGUILayout.LabelField("No ability instances available!");
            }
        }
        private void OnDestroy()
        {
            if (configEditor != null)
            {
                DestroyImmediate(configEditor);
            }
        }
    }
}
