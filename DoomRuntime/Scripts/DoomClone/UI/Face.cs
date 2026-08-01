using DoomArcade.Scripts.DoomClone.Items;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.DoomClone.UI
{
    public class Face : MonoBehaviour
    {
        [SerializeField] private Sprite[] spriteIdle;
        [SerializeField] private Sprite[] spriteLookRight;
        [SerializeField] private Sprite[] spriteLookLeft;
        [SerializeField] private Sprite[] spriteHurt;
        [SerializeField] private Sprite[] spriteGrin;
        [SerializeField] private Sprite[] spriteDeath;   // NEW

        int health => Player.current.health;

        [SerializeField] private Image image;

        private float timeHeldFire;
        float overrideTime;
        float timeWait;
        private bool isDead;                           // NEW

        public void ResetFace()
        {
            isDead = false;
            overrideTime = 0f;
            timeWait = 0f;
            timeHeldFire = 0f;

            if (Player.current != null)
            {
                // Reset to idle matching current health
                SetSprite(spriteIdle, HealthToSpriteIndex(Player.current.health));
            }
            else
            {
                // Fallback idle
                SetSprite(spriteIdle, 0);
            }
        }

        private void OnEnable()
        {
            isDead = false; // also clear here in case the object is toggled
            GlobalEventController.OnPlayerTakeDamage += OnPlayerTakeDamage;
            GlobalEventController.OnPlayerPickUp += OnPlayerPickup;
            GlobalEventController.OnPlayerDeath += OnPlayerDeath;
        }

        private void OnDisable()
        {
            GlobalEventController.OnPlayerTakeDamage -= OnPlayerTakeDamage;
            GlobalEventController.OnPlayerPickUp -= OnPlayerPickup;
            GlobalEventController.OnPlayerDeath -= OnPlayerDeath;
        }

        private void Update()
        {
            if (isDead) return; // Freeze face on death
            var currentState = GameStateManager.instance.currentState;
            if (currentState != GameState.Playing) return;

            if (Input.GetKey(KeyCode.Mouse0))
            {
                timeHeldFire += Time.deltaTime;

                if (timeHeldFire > 2f)
                {
                    SetSprite(spriteGrin, HealthToSpriteIndex(health));
                    timeWait = 0f;
                    return;
                }
            }
            else
            {
                timeHeldFire = 0;
            }

            if (overrideTime > 0)
            {
                overrideTime -= Time.deltaTime;
                timeWait = 0;
                return;
            }

            if (timeWait > 0)
            {
                timeWait -= Time.deltaTime;
            }
            else
            {
                Sprite[] sprites = spriteIdle;
                switch (Random.Range(0, 3))
                {
                    case 0: sprites = spriteIdle; break;
                    case 1: sprites = spriteLookRight; break;
                    case 2: sprites = spriteLookLeft; break;
                }
                SetSprite(sprites, HealthToSpriteIndex(health));
                timeWait += Random.Range(0.5f, 1.5f);
            }
        }

        private int HealthToSpriteIndex(int health)
        {
            if (health >= 80) return 0;
            if (health >= 60) return 1;
            if (health >= 40) return 2;
            if (health >= 20) return 3;
            return 4;
        }

        private void SetSprite(Sprite[] spriteArray, int index)
        {
            if (spriteArray == null || spriteArray.Length == 0) return;
            index = Mathf.Clamp(index, 0, spriteArray.Length - 1);
            image.sprite = spriteArray[index];
        }

        private void OnPlayerTakeDamage(int dmg, WorldEntity source)
        {
            if (isDead) return;

            SetSprite(spriteHurt, HealthToSpriteIndex(health));
            overrideTime = 1f;
        }

        private void OnPlayerPickup(Item item)
        {
            if (isDead) return;

            if (item is WeaponItem)
            {
                SetSprite(spriteGrin, HealthToSpriteIndex(health));
                overrideTime = 1f;
            }
        }

        private void OnPlayerDeath()                   // NEW
        {
            isDead = true;
            // Use worst-health frame or index 0, your choice
            SetSprite(spriteDeath, HealthToSpriteIndex(0));
        }
    }
}
