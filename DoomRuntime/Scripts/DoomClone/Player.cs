using System;
using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class Player
    {
        public static Player current;

        public DoomStyleController body;

        public int health;
        public int armor;

        public WeaponData currentWeapon;
        public Dictionary<WeaponData.AmmoType, int> ammo;

        public Dictionary<WeaponData, bool> weaponUnlocked;
        private Dictionary<int, bool> keys;

        public int kills = 0, itemsPickedUp = 0, secretsFound = 0;

        public int maxKills = 0;
        public int maxItems = 0;
        public int maxSecrets = 0;

        public float timer;

        public bool HasKey(int keyID) => keys.ContainsKey(keyID) && keys[keyID];
        public void AddKey(int keyID)
        {
            if (!keys.ContainsKey(keyID))
                keys.Add(keyID, true);
            else
                keys[keyID] = true;
        }

        public void AddAmmo(WeaponData.AmmoType ammoType, int amount)
        {
            if (!ammo.ContainsKey(ammoType))
                ammo.Add(ammoType, amount);
            else
                ammo[ammoType] += amount;
        }

        public void Heal(int healAmountHealth, int healAmountArmor, bool overheal)
        {
            int maxHeal = overheal ? 200 : 100;
            health = Math.Min(health + healAmountHealth, maxHeal);
            armor = Math.Max(armor, healAmountArmor);
            GlobalEventController.PlayerStatsChanged();
        }


        public Player(DoomStyleController body, WeaponData firstWeapon)
        {
            keys = new Dictionary<int, bool>();
            weaponUnlocked = new Dictionary<WeaponData, bool>();
            for (int i = 0; i < 10; i++)
            {
                WeaponData weaponData = DataManager.instance.GetWeaponById(i);
                if (weaponData == null)
                    continue;
                weaponUnlocked.Add(weaponData, false);
            }

            weaponUnlocked[DataManager.instance.GetWeaponById(0)] = true;
            weaponUnlocked[firstWeapon] = true;

            health = 100;
            armor = 0;

            ammo = new Dictionary<WeaponData.AmmoType, int>();
            foreach (WeaponData.AmmoType ammoType in Enum.GetValues(typeof(WeaponData.AmmoType)))
            {
                ammo.Add(ammoType, 0);
            }

            timer = 0f;

            this.body = body;
        }
    }
}