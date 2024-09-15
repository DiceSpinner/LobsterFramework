using UnityEngine;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LobsterFrameworkEditor")] 
namespace LobsterFramework
{
    public class Setting : MonoBehaviour
    {
        private static Setting instance;
        
        public static Setting Instance { get { return instance; } }

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
    }
}
