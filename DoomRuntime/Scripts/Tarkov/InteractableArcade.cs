using DoomArcade.Scripts.Arcade;
using UnityEngine;
using UnityEngine.Events;

namespace DoomArcade.Scripts.Tarkov
{
    public class InteractableArcade : MonoBehaviour
    {
        [SerializeField] private DoomArcadeUI arcadeUI;
    
        [Header("Actions")]
        public UnityEvent OnPowerOn;
        public UnityEvent OnPowerOff;
    
        [Header("State Events")]
        public UnityEvent<bool> OnInputCutoff;

        public void Init()
        {
            if (arcadeUI == null)
            {
                arcadeUI = GetComponentInChildren<DoomArcadeUI>(true);
            }
        }

        public void PowerOnArcade()
        {

            if (arcadeUI == null)
            {
                arcadeUI = GetComponentInChildren<DoomArcadeUI>(true);
            }

            arcadeUI.PowerOnFromGame();
        
            OnPowerOn?.Invoke();
            OnInputCutoff?.Invoke(true);
        }

        public void PowerOffArcade()
        {
            if (arcadeUI == null)
            {
                arcadeUI = GetComponentInChildren<DoomArcadeUI>(true);
            }

            if (arcadeUI != null)
            {
                arcadeUI.PowerOffFromGame();
            }

            OnPowerOff?.Invoke();
            OnInputCutoff?.Invoke(false);
        }
    }
}