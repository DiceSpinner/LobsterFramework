using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AI;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(StateMachine))]
    public class StateMachineEditor : ReferenceProviderEditor
    {
        private Editor editor;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            StateMachine stateMachine = (StateMachine)target;
            StateData stateData = stateMachine.runtimeData;

            if (editor == null)
            {
                if (stateData != null)
                {
                    editor = CreateEditor(stateData);
                }
                else if (Application.isPlaying)
                {
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Rebind & Reset"))
                        {
                            stateMachine.OnEnable();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            if (editor != null)
            {
                editor.OnInspectorGUI();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset States"))
                {
                    stateMachine.ResetStates();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OnDestroy()
        {
            if (editor != null)
            {
                DestroyImmediate(editor);
            }
        }
    }
}
