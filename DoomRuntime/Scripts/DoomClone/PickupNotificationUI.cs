using System.Collections;
using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Items;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.DoomClone
{
    public class PickupNotificationUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image messageImage;
        [SerializeField] private float displayTime = 2.0f;

        [Header("Messages")]
        [SerializeField] private List<PickupMessageEntry> messageEntries = new();

        private Dictionary<string, Sprite> messageLookup;
        private Coroutine displayRoutine;
        private float remainingTime = 0f;

        [System.Serializable]
        public class PickupMessageEntry
        {
            public string itemKey;
            public Sprite sprite; 
        }

        void Awake()
        {
            messageLookup = new Dictionary<string, Sprite>();
            foreach (var entry in messageEntries)
            {
                if (!string.IsNullOrEmpty(entry.itemKey) && entry.sprite != null)
                    messageLookup[entry.itemKey] = entry.sprite;
            }

            if (messageImage != null)
                messageImage.enabled = false;
        }

        void OnEnable()
        {
            GlobalEventController.OnPlayerPickUp += HandlePickup;
        }

        void OnDisable()
        {
            GlobalEventController.OnPlayerPickUp -= HandlePickup;
        }

        void HandlePickup(Item item)
        {
            if (item == null) return;

            var key = item.itemName;
            if (!messageLookup.TryGetValue(key, out var sprite) || sprite == null)
                return;

            if (messageImage != null)
            {
                messageImage.sprite = sprite;
                messageImage.enabled = true;
            }

            remainingTime = displayTime;

            if (displayRoutine == null)
                displayRoutine = StartCoroutine(DisplayTimer());
        }

        IEnumerator DisplayTimer()
        {
            while (remainingTime > 0f)
            {
                if (GameStateManager.instance != null &&
                    GameStateManager.instance.currentState == GameState.Playing)
                {
                    remainingTime -= Time.unscaledDeltaTime;
                }

                yield return null;
            }

            if (messageImage != null)
                messageImage.enabled = false;

            displayRoutine = null;
        }
    }
}
