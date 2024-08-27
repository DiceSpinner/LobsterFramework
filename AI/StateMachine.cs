using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LobsterFramework.AI
{
    /// <summary>
    /// Manages and runs <see cref="State"/>. Takes in <see cref="StateData"/> as input.
    /// </summary>
    public class StateMachine : ReferenceProvider
    {
        [SerializeField, DisableEditInPlayMode] internal StateData inputData;
        internal StateData runtimeData;

        [ReadOnly]
        [SerializeField] private State currentState;
        [HideInInspector]
        [SerializeField] internal string statePath;    

        #region Coroutine
        private readonly CoroutineRunner coroutineRunner = new();
        private Type switchingTo = null;

        public Utility.Coroutine RunCoroutine(IEnumerable<CoroutineOption> coroutine) {
            return coroutineRunner.AddCoroutine(coroutine);
        }
        #endregion

        public State CurrentState
        {
            get { return currentState; }
            set { currentState = value; }
        }

        private void OnEnable()
        {
            if (runtimeData == null) {
                if (inputData == null || !inputData.Validate()) {
                    Debug.Log("Input state data is missing initial state or missing transitions.");
                    return;
                }
                runtimeData = inputData.Clone();
            }
            Bind(runtimeData);
            currentState = runtimeData.initialState;
            runtimeData.Activate(this);
        }

        private void OnDisable()
        {
            runtimeData.Deactivate();
            Bind(inputData);
        }

        private new void OnValidate()
        {
            if (AttributeInitializer.Finished)
            {
                Bind(inputData);
            }
            else
            {
                void lambda() { Bind(inputData); }
                AttributeInitializer.OnInitializationComplete -= lambda;
                AttributeInitializer.OnInitializationComplete += lambda;
            }
        }

        public void Update()
        {
            if (currentState == null) {
                return;
            }

            // Execute Coroutine
            if (coroutineRunner.Size > 0) {
                coroutineRunner.Run();
                if (switchingTo != null && coroutineRunner.Size == 0) {
                    currentState.OnExit();
                    currentState = runtimeData.states[switchingTo.AssemblyQualifiedName];
                    currentState.OnEnter();
                    switchingTo = null;
                }
                return;
            }

            // Normal state ticking behavior
            Type target = currentState.Tick();

            if (target != null)
            {
                if (!runtimeData.states.ContainsKey(target.AssemblyQualifiedName)) {
                    Debug.LogError($"Cannot transition to {target.FullName}. This state is not defined in the StateData.");
                    enabled = false;
                    return;
                }

                if (coroutineRunner.Size == 0)
                {
                    currentState.OnExit();
                    currentState = runtimeData.states[target.AssemblyQualifiedName];
                    currentState.OnEnter();
                }
                else { // Coroutine is called by a state, postpone state switch until coroutine is finished
                    switchingTo = target;
                }
            }
        }

#if UNITY_EDITOR
        public void SaveRuntimeData(string path) {
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets/" + path[Application.dataPath.Length..];
            }
            else if (path == "")
            {
                path = AssetDatabase.GetAssetPath(inputData);
            }
            else
            {
                Debug.LogError($"Invalid path {path}, can't save state data!");
                return;
            }
            if (runtimeData != null)
            {
                StateData cloned = runtimeData.Clone();
                cloned.SaveAsAsset(path);
                inputData = cloned;
            }
        }
#endif
    }
}
