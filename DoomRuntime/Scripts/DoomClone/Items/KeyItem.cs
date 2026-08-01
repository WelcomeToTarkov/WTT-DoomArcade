namespace DoomArcade.Scripts.DoomClone.Items
{
    public class KeyItem : Item
    {
        public int keyID;

        public override void OnPickup(Player player)
        {
            player.AddKey(keyID);

            if (countsAsItem)
                Player.current.itemsPickedUp++;
            if (countsAsSecret)
                Player.current.secretsFound++;
            GlobalEventController.PlayerPickUp(this);
            Destroy(gameObject);
        }
    }
}
