using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.UI
{
    public class StatusBar : MonoBehaviour
    {
        [SerializeField] SpriteText textHealth;
        [SerializeField] SpriteText textArmor;
        [SerializeField] SpriteText textAmmo;
        [SerializeField] SpriteText textTime;

        Player player => Player.current;

        private bool isDead;                         // NEW
        public void ResetHUD()
        {
            isDead = false;
            UpdateUI();
            textTime.GenerateText("100");
        }

        private void OnEnable()
        {
            isDead = false;
            GlobalEventController.OnAnyEvent += UpdateUI;
            GlobalEventController.OnPlayerDeath += OnPlayerDeath;
            GlobalEventController.OnPlayerStatsChanged += UpdateUI; // NEW
        }

        private void OnDisable()
        {
            GlobalEventController.OnAnyEvent -= UpdateUI;
            GlobalEventController.OnPlayerDeath -= OnPlayerDeath;
            GlobalEventController.OnPlayerStatsChanged -= UpdateUI;
        }


        private void Start()
        {
            UpdateUI();
        }

        private void OnPlayerDeath()                // NEW
        {
            isDead = true;
            // Clamp to 0 immediately
            if (player != null)
                textHealth.GenerateText("0%");
        }

        private void UpdateUI()
        {
            if (player == null)
            {
                Clear();
                return;
            }

            if (!isDead)
            {
                textHealth.GenerateText(Mathf.Max(0, player.health) + "%");
                textArmor.GenerateText(Mathf.Max(0, player.armor) + "%");
            }

            if (player.currentWeapon == null || player.currentWeapon.ammoType == WeaponData.AmmoType.Melee)
                textAmmo.GenerateText("");
            else if (!isDead)
                textAmmo.GenerateText(player.ammo[player.currentWeapon.ammoType].ToString());
        }

        private void Update()
        {
            if (GameStateManager.instance.currentState != GameState.Playing) return;
            if (player == null) return;
            if (isDead) return;                     // NEW

            textTime.GenerateText(Mathf.CeilToInt(player.timer).ToString());
        }

        void Clear()
        {
            textHealth.GenerateText("");
            textArmor.GenerateText("");
            textAmmo.GenerateText("");
        }
    }
}
