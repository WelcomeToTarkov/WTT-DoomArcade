using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Items
{
    public class WeaponItem : Item
    {
        public WeaponData weaponData;

        public override void OnPickup(Player player)
        {
            // Was this weapon already unlocked before this pickup?
            bool wasUnlocked = player.weaponUnlocked.TryGetValue(weaponData, out bool flag) && flag;

            // Unlock it (if new).
            player.weaponUnlocked[weaponData] = true;

            int amount = weaponData.defaultPickupAmmo;
            if (weaponData.ammoType != WeaponData.AmmoType.Melee && amount > 0)
            {
                player.AddAmmo(weaponData.ammoType, amount);
            }

            
            if (countsAsItem)
                Player.current.itemsPickedUp++;
            if (countsAsSecret)
                Player.current.secretsFound++;

            // If it was NOT unlocked, this is a new weapon → force switch.
            if (!wasUnlocked && PlayerWeaponControllerExists())
            {
                var controller = Object.FindObjectOfType<PlayerWeaponController>();
                if (controller != null)
                    controller.EquipWeapon(weaponData);
            }

            GlobalEventController.PlayerPickUp(this);
            Destroy(gameObject);
        }

        private bool PlayerWeaponControllerExists() => true; // placeholder if you want extra safety
    }
}