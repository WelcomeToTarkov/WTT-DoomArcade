using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Items
{
    public abstract class Item : MonoBehaviour
    {
        public string itemName;
        public Texture2D itemSprite;
        public AudioClip audioClipPickup;
        public bool countsAsSecret; 
        public bool countsAsItem = true;
        protected virtual void Start()
        {
            GetComponent<Billboard>().SetTexture(itemSprite);
        }

        public abstract void OnPickup(Player player);

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, itemName + ".png", true);
        }
    }
}