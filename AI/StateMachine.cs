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
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private AIController controller;
        [SerializeField] internal StateData inputData;
        internal StateData runtimeData;

        [ReadOnly]
        [SerializeField] private State currentState;
        [HideInInspector]
        [SerializeField] internal string statePath;    

        #region Coroutine
        public readonly CoroutineRunner coroutineRunner = new();
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
            if (!inputData.Validate()) {
                Debug.Log("Input state data is missing initial state or missing transitions.");
                return;
            }
            runtimeData = inputData.Clone();
            currentState = runtimeData.initialState;
            runtimeData.Initialize(this, controller);
        }

        private void OnDisable()
        {
            runtimeData.Close();
            Destroy(runtimeData);
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
