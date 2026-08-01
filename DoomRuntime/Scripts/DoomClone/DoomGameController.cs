using System;
using System.IO;
using DoomArcade.Scripts.Arcade;
using ManagedDoom;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class DoomGameController : MonoBehaviour
    {
        [Header("Runtime Root")]
        [Tooltip("Root object that contains TarkovDoom (map + systems + canvas).")]
        [SerializeField] private GameObject tarkovDoomRoot;

        public event Action OnSessionExit;

        public bool IsRunning => tarkovDoomRoot != null && tarkovDoomRoot.activeSelf;

        [Header("Debug Controls")]
        [SerializeField] private KeyCode debugBootKey = KeyCode.F1;
        [SerializeField] private KeyCode debugExitKey = KeyCode.F2;

        public TarkovDoomExitHook _exitHook;

        void Awake()
        {
            if (tarkovDoomRoot != null)
                tarkovDoomRoot.SetActive(false);
        }

        public void Boot()
        {
            if (tarkovDoomRoot == null)
            {
                Debug.LogError("[DoomGameController] No tarkovDoomRoot assigned!");
                return;
            }

            if (IsRunning)
            {
                Debug.LogWarning("[DoomGameController] Boot called while session is already running.");
                return;
            }

            var cfgPath = ConfigUtilities.GetConfigPath();
            var config  = File.Exists(cfgPath) ? new Config(cfgPath) : new Config();
            DoomedInputInitializer.ApplyConfigToDoomedInput(config);

            tarkovDoomRoot.SetActive(true);

            var gsm = GameStateManager.instance;
            if (gsm != null)
                gsm.ReplayIntro();

            if (_exitHook != null)
                _exitHook.Init(this);

        }


        public void ExitToArcade()
        {
            if (tarkovDoomRoot != null && tarkovDoomRoot.activeSelf)
                tarkovDoomRoot.SetActive(false);

            OnSessionExit?.Invoke();
        }
    }
}
