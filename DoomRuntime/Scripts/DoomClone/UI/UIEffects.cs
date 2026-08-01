using DoomArcade.Scripts.DoomClone.Items;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.DoomClone.UI
{
    public class UIEffects : MonoBehaviour
    {
        [SerializeField] private Image imageOverlayHurt;
        [SerializeField] private Image imageOverlayPickup;

        private float timeHurt;
        private float timePickup;

        private void OnEnable()
        {
            GlobalEventController.OnPlayerTakeDamage += OnPlayerTakeDamage;
            GlobalEventController.OnPlayerPickUp += OnPlayerPickupItem;
        }

        private void OnDisable()
        {
            GlobalEventController.OnPlayerTakeDamage -= OnPlayerTakeDamage;
            GlobalEventController.OnPlayerPickUp -= OnPlayerPickupItem;
        }


        private void Update()
        {
            if (timeHurt > 0)
            {
                timeHurt -= Time.deltaTime;
                imageOverlayHurt.enabled = true;
            }
            else
            {
                imageOverlayHurt.enabled = false;
            }

            if (timePickup > 0)
            {
                timePickup -= Time.deltaTime;
                imageOverlayPickup.enabled = true;
            }
            else
            {
                imageOverlayPickup.enabled = false;
            }
        }

        private void OnPlayerTakeDamage(int dmg, WorldEntity source)
        {
            timeHurt = 0.1f;
        }

        public void OnPlayerPickupItem(Item item)
        {
            timePickup = 0.1f;
        }
    }
}
