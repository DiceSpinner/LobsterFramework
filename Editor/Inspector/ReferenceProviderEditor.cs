using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using TypeCache = LobsterFramework.Utility.TypeCache;

namespace LobsterFramework.Editors
{
    /// <summary>
    /// Custom inspector for <see cref="ReferenceProvider"/>. Custom editors of subclasses can inherit from this 
    /// to make use of the implementation of <see cref="OnInspectorGUI"/> to draw out fields to store component references.
    /// </summary>
    [CustomEditor(typeof(ReferenceProvider), true)] 
    public class ReferenceProviderEditor : Editor
    {
        private static readonly GUIContent label = new();
        private static readonly GUIContent quickfillLabel = new("Quick Fill", "Fill the fields using the current gameobject.");
        /// <summary>
        /// Flag to indicate whether the fields to store component references are visible in the inspector
        /// </summary>
        protected bool referenceFieldsExpanded = false;

        /// <summary>
        /// Draws out other fields and the reference assignment section underneath.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var refProvider = (ReferenceProvider)target;
            Color color = GUI.color;
            if (refProvider.referenceMapping.Count > 0) {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                referenceFieldsExpanded = EditorGUILayout.Foldout(referenceFieldsExpanded, "Required References", EditorStyles.foldout);
                if (GUILayout.Button(quickfillLabel, GUILayout.Width(100))) {
                    refProvider.QuickFillFields();
                }
                EditorGUILayout.EndHorizontal();
            }
            else {
                referenceFieldsExpanded = false;            
            }

            if (referenceFieldsExpanded) {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                foreach (var item in refProvider.referenceMapping)
                {
                    var requesterTypeName = item.Key;
                    Type typeRequesting = TypeCache.GetTypeByName(requesterTypeName);
                    var typrRequirementCollection = RequireComponentReferenceAttribute.Requirement[typeRequesting];

                    EditorGUILayout.BeginVertical();
                    label.text = typeRequesting.Name;
                    label.image = EditorUtils.GetScriptIcon(typeRequesting);
                    label.tooltip = typeRequesting.FullName;
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    foreach (var kwp in typrRequirementCollection)
                    {
                        var requiredType = kwp.Key;
                        var requiredTypeName = kwp.Key.AssemblyQualifiedName;
                        var referenceCollection = refProvider.referenceMapping[requesterTypeName][requiredTypeName];

                        for (int i = 0;i < kwp.Value.Count;i++) {
                            var requirementDescription = kwp.Value[i];
                            label.text = requirementDescription.Name != default ? requirementDescription.Name : requiredType.Name;
                            label.image = null;
                            if (requirementDescription.Description == default)
                            {
                                label.tooltip = requiredType.FullName;
                            }
                            else
                            {
                                label.tooltip = requiredType.FullName + "\n\n" + requirementDescription.Description;
                            }

                            if (referenceCollection[i] == null)
                            {
                                GUI.color = Color.red;
                            }
                            else
                            {
                                GUI.color = Color.green;
                            }
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(label);
                            var reference = (Component)EditorGUILayout.ObjectField(referenceCollection[i], requiredType, true);

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RegisterCompleteObjectUndo(target, "Set Component Reference");
                                referenceCollection[i] = reference;
                            }

                            EditorGUILayout.EndHorizontal();

                            GUI.color = color;
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
            }
        }
    }
}
