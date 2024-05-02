using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.Events;
using LobsterFramework.Utility;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LobsterFrameworkEditor")] 
namespace LobsterFramework
{
    public enum Scene
    {
        IntroMenu,
        Gameplay,
    }

    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        public static GameManager Instance { get { return instance; } }
        [field: SerializeField] public bool UseAlternativeInput { get; private set; }
        [field: SerializeField] public bool DefaultInteractionRadius { get; private set; }

        [SerializeField] private VoidEventChannel exitChannel;
        // Game Settings
        [field: SerializeField] public int TARGET_FRAME_RATE { get; private set; }

        // Attack info duration (seconds)
        [field: SerializeField] public float EXPIRE_ATTACK_TIME { get; private set; }

        //
        [field: SerializeField] public float POSTURE_BROKEN_DAMAGE_MODIFIER { get; private set; }
        [field: SerializeField] public float POSTURE_BROKEN_DURATION { get; private set; }
        [field: SerializeField] public float SUPPRESS_REGEN_DURATION { get; private set; }

        #region Inquiries
        private static bool gamePaused;
        public static bool GamePaused { get { return gamePaused; }
            set {
                bool temp = gamePaused;
                gamePaused = value;
                if (temp != value && onGamePaused != null) {
                    onGamePaused.Invoke(value);
                }
         } }
        public static Action<bool> onGamePaused;
        #endregion

        private void Awake()
        {

            if (instance != null && instance != this)
            {
                Destroy(this);
            }
            else
            {
                instance = this;
            }
        }

        void Start()
        {
            Application.targetFrameRate = TARGET_FRAME_RATE;
            QualitySettings.vSyncCount = 0;
            exitChannel.OnEventRaised += ExitGame;
            GamePaused = false;
        }

        private void ExitGame()
        { 
            Application.Quit();
            Debug.Log("Exit!");
        }

        
#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void OnInitScript()
        {
            Assembly frameworkAssembly = typeof(GameManager).Assembly;
            AttributeInitializer.Initialize(frameworkAssembly);

            AssemblyName assemblyName = frameworkAssembly.GetName();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName[] references = assembly.GetReferencedAssemblies();
                foreach (AssemblyName reference in references) {
                    if (reference.Name == assemblyName.Name) {
                        AttributeInitializer.Initialize(assembly);
                        // Debug.Log($"Assembly {assembly.GetName().Name} use of LobsterFramework detected!"); 
                        break;
                    }
                }
            }
        }
    }
}
