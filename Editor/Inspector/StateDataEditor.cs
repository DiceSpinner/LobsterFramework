using LobsterFramework.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(StateData))]
    public class StateDataEditor : Editor
    {
        private Dictionary<Type, Editor> stateEditors = new();

        private GUIStyle normalStateStyle = new();
        private GUIStyle initialStateStyle = new();

        #region Editor Status
        public State selectedState = null; // State to display
        public State nextState = null; // State to be switched to nex time editor is updated
        public Type removeState = null; // State to be removed next time editor is updated
        public State newInitialState = null;
        #endregion

        private Rect addStateRect;
        private Rect selectStateRect;

        public void OnEnable()
        {
            normalStateStyle.fontStyle = FontStyle.Bold;
            normalStateStyle.normal.textColor = Color.cyan;
            normalStateStyle.hover.background = Texture2D.grayTexture;

            initialStateStyle.fontStyle = FontStyle.Bold;
            initialStateStyle.normal.textColor = Color.green;
            initialStateStyle.hover.background = Texture2D.grayTexture;
        }

        public override void OnInspectorGUI()
        {
            StateData stateData = (StateData)target;
            EditorGUI.BeginChangeCheck();

            if (Event.current.type == EventType.Layout) {
                // Update editor status and editor data
                if (newInitialState != null) {
                    stateData.initialState = newInitialState;
                    newInitialState = null;
                }
                if (nextState != null)
                {
                    selectedState = nextState;
                    nextState = null;
                }
                if (removeState != null)
                {
                    stateData.RemoveState(removeState);
                    DestroyImmediate(stateEditors[removeState]);
                    stateEditors.Remove(removeState);
                    removeState = null;
                }
            }

            DrawStates(stateData);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawStates(StateData stateData)
        {
            SerializedProperty states = serializedObject.FindProperty("states");
            EditorGUILayout.BeginHorizontal();
            states.isExpanded = EditorGUILayout.Foldout(states.isExpanded, "States: " + stateData.states.Values.Count);
            GUILayout.FlexibleSpace();
            bool addBtnClicked = GUILayout.Button("Add State", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (addBtnClicked) // Add state button clicked
            {
                addStateRect.position = Event.current.mousePosition;
                AddStatePopup popup = new(stateData);
                PopupWindow.Show(addStateRect, popup);
            }

            if (states.isExpanded)
            {
                if (stateData.states.Count == 0)
                {
                    EditorGUILayout.LabelField("No states available for display!");
                }
                else
                {
                    EditorGUILayout.Space();
                    if (selectedState == null)
                    {
                        selectedState = stateData.states.First().Value;
                    }
                    DrawSelectedStateHeader(stateData);
                    DrawState(stateData, selectedState);
                    DrawTransition(stateData, selectedState);
                }
            }
        }

        private void DrawSelectedStateHeader(StateData stateData) {
            Type stateType = selectedState.GetType();

            EditorGUILayout.BeginHorizontal();
            GUIContent content = new();
            GUIStyle style;
            bool selected;
            if (stateData.initialState == selectedState)
            {
                content.text = $"{stateType.Name} (Initial State)";
                style = initialStateStyle;
            }
            else {
                content.text = stateType.Name;
                style = normalStateStyle;
            }
            content.tooltip = stateType.FullName;
            
            if (AddStateMenuAttribute.icons.TryGetValue(stateType, out Texture2D icon))
            {
                content.image = icon;
                selected = GUILayout.Button(content, style, GUILayout.Height(40));
            }
            else
            {
                selected = GUILayout.Button(content, style);
            }

            if (selected) // Open popup window to do state selection
            {
                selectStateRect.position = Event.current.mousePosition;
                SelectStatePopup popup = new SelectStatePopup(this, stateData);
                PopupWindow.Show(selectStateRect, popup);
            }

            GUILayout.FlexibleSpace();

            // Draw remove state button
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            bool rmvClicked = EditorUtils.Button(Color.red, "Remove State", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (rmvClicked) {
                removeState = stateType;
            }
        }

        private void DrawState(StateData stateData, State stateToDraw) {
            
            Editor editor;
            Type stateType = stateToDraw.GetType();

            // Create inspector for the state if there's none
            if (stateEditors.TryGetValue(stateType, out Editor stateEditor))
            {
                editor = stateEditor;
            }
            else
            {
                editor = CreateEditor(stateToDraw);
                stateEditors.Add(stateType, editor);
            }

            editor.OnInspectorGUI();
            if (stateData.initialState != stateToDraw)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (EditorUtils.Button(Color.green, "Set As Initial State", GUILayout.Width(120)))
                {
                    newInitialState = stateToDraw;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTransition(StateData stateData, State stateToDraw) {
            Type stateType = stateToDraw.GetType();

            // Draw options to select states that can be transitioned to
            if (StateTransitionAttribute.transitionTable.ContainsKey(stateType))
            { // Only draw if there's transitions declared for the state
                EditorGUILayout.Space(); 
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                HashSet<Type> transitions = StateTransitionAttribute.transitionTable[stateType];
                GUILayout.Label($"Transitions: {transitions.Count}");
                
                /* For each transition, draw out the editor for the state and a view button to switch the selected state to it.
                 * If the target state is not present, replace view button with add button to add it to the state data.
                 */
                foreach (Type transitionType in transitions)
                {
                    EditorGUILayout.Space();
                    GUIContent label = new GUIContent(transitionType.Name);
                    State state = null;

                    label.tooltip = transitionType.FullName;

                    if (AddStateMenuAttribute.icons.ContainsKey(transitionType))
                    {
                        label.image = AddStateMenuAttribute.icons[transitionType];
                    }

                    GUILayout.BeginHorizontal();
                   
                    if (stateData.states.ContainsKey(transitionType.AssemblyQualifiedName))
                    {
                        Color color = Color.yellow;
                        state = stateData.states[transitionType.AssemblyQualifiedName];
                        if (stateData.initialState == state) {
                            label.text += " (Initial State)";   
                            color = Color.green;
                        } 

                        Color before = GUI.color;
                        GUI.color = color;
                        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                        GUI.color = before;
                       

                        if (GUILayout.Button("View", GUILayout.Width(120)))
                        {
                            nextState = state;
                        }
                    }
                    else
                    {
                        Color before = GUI.color;
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                        GUI.color = before;

                        if (EditorUtils.Button(Color.green, "Add", GUILayout.Width(120)))
                        {
                            stateData.AddState(transitionType);
                        }
                    }

                    GUILayout.EndHorizontal();
                    if(state != null) {
                        DrawState(stateData, state);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Editor editor in stateEditors.Values)
            {
                DestroyImmediate(editor);
            }
        }
    }
}
