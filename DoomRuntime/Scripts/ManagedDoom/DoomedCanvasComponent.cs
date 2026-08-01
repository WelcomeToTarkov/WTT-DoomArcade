using DoomArcade.Scripts.Arcade;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.ManagedDoom
{
    [RequireComponent(typeof(Image))] 
    [DisallowMultipleComponent]
    public class DoomCanvasComponent : MonoBehaviour
    {
        [Header("Doom Settings")]
        public bool LockKeyboard = true;
        public float TickTime = 0.028571f;
        [HideInInspector] public string WadPath;
        [HideInInspector] public string[] WadPaths; 
        [HideInInspector] public string[] DehPaths;
        [HideInInspector] public string SfPath;
        public DoomArcadeUI arcadeUI;

        public Object Wad;
        [SerializeField]public Object SoundFont;

        public Image DoomScreen;
        private RuntimeDoom runtimeDoom;
        private bool isRunning;

        void Start()
        {
            if (DoomScreen == null)
                DoomScreen = GetComponent<Image>();

            runtimeDoom = gameObject.AddComponent<RuntimeDoom>();
            runtimeDoom._image = DoomScreen;
            runtimeDoom.arcadeUI = arcadeUI;
            runtimeDoom.doomedComponent = this;
        
            runtimeDoom.arcadeUI = FindObjectOfType<DoomArcadeUI>();

            StartDoom();
        }

        void OnDestroy()
        {
            if (runtimeDoom != null)
            {
                Destroy(runtimeDoom);
                runtimeDoom = null;
            }
        }

        public void StartDoom()
        {
            if (isRunning) return;
            isRunning = true;
        }

        public void StopDoom()
        {
            isRunning = false;
        }
    }
}