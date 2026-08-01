using System;
using DoomArcade.Scripts.DoomClone.Items;
using DoomArcade.Scripts.DoomClone.Scriptables;

namespace DoomArcade.Scripts.DoomClone
{
    public static class GlobalEventController
    {
        public static event Action<int, WorldEntity> OnPlayerTakeDamage;
        public static event Action<WeaponData> OnPlayerShoot;
        public static event Action<Item> OnPlayerPickUp;
        public static event Action OnPlayerVictory;
        public static event Action OnPlayerDeath;  
        public static event Action OnAnyEvent;
        public static event Action OnPlayerStatsChanged;
        public static event Action<int> OnGameStarted;

        public static void PlayerStatsChanged()
        {
            OnPlayerStatsChanged?.Invoke();
            OnAnyEvent?.Invoke();
        }

        public static void PlayerTakeDamage(int dmg, WorldEntity source)
        {
            OnPlayerTakeDamage?.Invoke(dmg, source);
            OnAnyEvent?.Invoke();
        }
        public static void PlayerDeath()
        {
            OnPlayerDeath?.Invoke();
            OnAnyEvent?.Invoke();
        }
        public static void PlayerShoot(WeaponData weaponData)
        {
            OnPlayerShoot?.Invoke(weaponData);
            OnAnyEvent?.Invoke();
        }

        public static void PlayerPickUp(Item item)
        {
            OnPlayerPickUp?.Invoke(item);
            OnAnyEvent?.Invoke();
        }

        public static void PlayerVictory()
        {
            OnPlayerVictory?.Invoke();
            OnAnyEvent?.Invoke();
        }

        public static void PlayerWeaponSwitched(WeaponData weaponData)
        {
            OnAnyEvent?.Invoke();
        }
    
        public static void GameStarted(int difficulty)
        {
            OnGameStarted?.Invoke(difficulty);
            OnAnyEvent?.Invoke();
        }
    }
}
