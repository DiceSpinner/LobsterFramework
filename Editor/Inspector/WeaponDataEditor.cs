using LobsterFramework.AbilitySystem.WeaponSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(WeaponData))]
    public class WeaponDataEditor : Editor
    {
        private Dictionary<Type, Editor> weaponStatsEditors = new();

        public WeaponStat selectedWeaponStat = null;

        private Rect addWeaponStatRect;
        private Rect selectWeaponStatRect;

        public override void OnInspectorGUI()
        {
            WeaponData weaponData = (WeaponData)target;
            EditorGUI.BeginChangeCheck();

            try
            {
                DrawWeaponStats(weaponData);
            }
            catch (ArgumentException) { }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawWeaponStats(WeaponData weaponData)
        {
            SerializedProperty weaponStats = serializedObject.FindProperty("weaponStats");
            EditorGUILayout.BeginHorizontal();
            weaponStats.isExpanded = EditorGUILayout.Foldout(weaponStats.isExpanded, "Weapon Stats: " + weaponData.weaponStats.Values.Count);
            GUILayout.FlexibleSpace();
            bool aButton = GUILayout.Button("Add Stat", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (aButton) // Add action component button clicked
            {
                AddWeaponStatPopup popup = new(weaponData);
                PopupWindow.Show(addWeaponStatRect, popup);
            }

            if (weaponStats.isExpanded)
            {
                if (weaponData.weaponStats.Count == 0)
                {
                    EditorGUILayout.LabelField("No ability stats available for display!");
                }
                else
                {
                    EditorGUILayout.Space();
                    if (selectedWeaponStat == null)
                    {
                        selectedWeaponStat = weaponData.weaponStats.First().Value;
                    }
                    Editor editor;
                    Type type = selectedWeaponStat.GetType();
                    if (weaponStatsEditors.TryGetValue(type, out Editor statsEditor))
                    {
                        editor = statsEditor;
                    }
                    else
                    {
                        editor = CreateEditor(selectedWeaponStat);
                        weaponStatsEditors.Add(type, editor);
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUIContent content = new();
                    bool selected;
                    content.text = selectedWeaponStat.GetType().Name;
                    if (AddWeaponStatMenuAttribute.icons.TryGetValue(type, out Texture2D icon))
                    {
                        content.image = icon;
                        selected = GUILayout.Button(content, AbilityEditorConfig.ComponentSelectionStyle, GUILayout.Height(40));
                    }
                    else
                    {
                        selected = GUILayout.Button(content, AbilityEditorConfig.ComponentSelectionStyle);
                    }

                    if (selected)
                    {
                        SelectWeaponStatPopup popup = new SelectWeaponStatPopup(this, weaponData);
                        PopupWindow.Show(selectWeaponStatRect, popup);
                    }

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    bool clicked = EditorUtils.Button(Color.red, "Remove Stat", GUILayout.Width(100));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    if (!clicked)
                    {
                        editor.OnInspectorGUI();
                    }
                    else
                    {
                        var m = typeof(WeaponData).GetMethod("RemoveWeaponStat", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo removed = m.MakeGenericMethod(selectedWeaponStat.GetType());
                        DestroyImmediate(weaponStatsEditors[type]);
                        weaponStatsEditors.Remove(type);
                        removed.Invoke(weaponData, null);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Editor editor in weaponStatsEditors.Values) {
                DestroyImmediate(editor);
            }
        }
    }
}
