using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Scriptables
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        public int id;

        public Sprite idleSprite;
        public Sprite[] spritesShooting;
        public Sprite[] spritesReloading;

        public float[] timingShooting;
        public float[] timingReloading;

        public float timeReload
        {
            get
            {
                float time = 0;
                foreach (float t in timingReloading)
                {
                    time += t;
                }
                return time;
            }
        }

        public int recoil;

        public enum AmmoType
        {
            Light,
            Medium,
            Heavy,
            Shotgun,
            Melee
        }


        public AudioClip soundShoot;
        public AudioClip soundReload;
        [Header("Ammo")]
        public int defaultPickupAmmo = 4;  
        public AmmoType ammoType;
    
    }
}