using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<DataManager>();
                return _instance;
            }
        }
        private static DataManager _instance;
    
        [SerializeField] WeaponData[] weaponData;

        public AudioClip audioClipDoor;
        public AudioClip audioClipPickupGeneric;
        public AudioClip audioClipDoorLocked;
        public AudioClip audioClipDoorUnlock;

        public WeaponData GetWeaponById(int id)
        {
            foreach (WeaponData weapon in weaponData)
            {
                if (weapon.id == id)
                    return weapon;
            }

            return null;
        }
    }
}