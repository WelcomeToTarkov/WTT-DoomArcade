// DoomArcade_Client/Scripts/InteractableDoomArcade.cs
using System.Collections.Generic;
using BepInEx.Logging;
using DoomArcade.Scripts.Tarkov;
using DoomArcadeClient.Utils;
using EFT;
using EFT.UI;
using EFT.Interactive;
using UnityEngine;

namespace DoomArcadeClient.Components
{
  public class InteractableDoomArcade : InteractableObject
    {
        public bool IsPoweredOn { get; private set; }

        [SerializeField] private InteractableArcade unityInteractable;

        private bool _wasActive;

        private static ManualLogSource Log => DoomArcadeClient.Log;

        public void Init()
        {

            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (unityInteractable == null)
            {
                unityInteractable = GetComponent<InteractableArcade>();
            }

            if (unityInteractable == null)
            {
                return;
            }

            unityInteractable.Init();

            unityInteractable.OnPowerOn.AddListener(() =>
            {
                IsPoweredOn = true;
                SetInputCutoff(true);
                RefreshInteractionCast();
            });

            unityInteractable.OnPowerOff.AddListener(() =>
            {
                IsPoweredOn = false;
                SetInputCutoff(false);
                RefreshInteractionCast();
            });

            unityInteractable.OnInputCutoff.AddListener(OnUnityInputCutoff);
        }

        public void PowerOnFromInteraction()
        {
            if (unityInteractable == null)
            {
                return;
            }

            unityInteractable.PowerOnArcade();
        }

        private void RefreshInteractionCast()
        {
            if (DoomArcadeClient.Player != null)
            {
                DoomArcadeClient.Player.UpdateInteractionCast();
            }
        }

        private void OnUnityInputCutoff(bool cutoff)
        {
            SetInputCutoff(cutoff);
        }

        private void SetInputCutoff(bool active)
        {
            if (active == _wasActive)
            {
                return;
            }

            _wasActive = active;

            if (active)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                CursorSettings.SetCursor(ECursorType.Idle);
                UIEventSystem.Instance.SetTemporaryStatus(true);
                GamePlayerOwner.IgnoreInputWithKeepResetLook = true;
                GamePlayerOwner.IgnoreInputInNPCDialog = true;
            }
            else
            {
                UIEventSystem.Instance.SetTemporaryStatus(false);
                GamePlayerOwner.IgnoreInputWithKeepResetLook = false;
                GamePlayerOwner.IgnoreInputInNPCDialog = false;
                RefreshInteractionCast();
            }
        }
    }
}
