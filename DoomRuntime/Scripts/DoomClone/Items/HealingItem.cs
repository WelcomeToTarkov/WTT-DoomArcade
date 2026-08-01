using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Items
{
    public class HealingItem : Item
    {
        public int healAmountHealth;
        public int healAmountArmor;

        public bool overheal;

        public override void OnPickup(Player player)
        {
            int deltaHealth = player.health;
            int deltaArmor = player.armor;
            var cfg = Game.CurrentDifficulty;

            int amount = Mathf.RoundToInt(healAmountHealth * cfg.healthPickupMultiplier);
            player.Heal(amount, healAmountArmor, overheal);

            deltaHealth = player.health - deltaHealth;
            deltaArmor = player.armor - deltaArmor;

            if (deltaHealth > 0 || deltaArmor > 0)
            {
                if (countsAsItem)
                    Player.current.itemsPickedUp++;
                GlobalEventController.PlayerPickUp(this);
                Destroy(gameObject);
            }
        }
    }
}