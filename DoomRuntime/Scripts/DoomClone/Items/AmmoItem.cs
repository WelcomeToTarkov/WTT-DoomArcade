using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Items
{
    public class AmmoItem : Item
    {
        public WeaponData.AmmoType type;
        public int ammoAmount;
        public override void OnPickup(Player player)
        {
            var cfg = Game.CurrentDifficulty;
            int amount = ammoAmount;

            if (cfg.doubleAmmo)
                amount *= 2;

            amount = Mathf.RoundToInt(amount * cfg.ammoPickupMultiplier);
            player.AddAmmo(type, amount);
            if (countsAsItem)
                Player.current.itemsPickedUp++;
            if (countsAsSecret)
                Player.current.secretsFound++;
            GlobalEventController.PlayerPickUp(this);
            Destroy(gameObject);
        }
    }
}
