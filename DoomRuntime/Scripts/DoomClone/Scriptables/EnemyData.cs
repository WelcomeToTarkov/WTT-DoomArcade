using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Scriptables
{
    public class EnemyData : ScriptableObject
    {
        public int id;
        public int baseHealth = 100;
        public GameObject prefab;

        public string enemyName;
        [HideInInspector] public int health;

        public WeaponData weaponData;

        public GameObject[] itemDropPool;

        public Texture2D[] spritesWalkingA;
        public Texture2D[] spritesWalkingB;
        public Texture2D[] spritesAiming;
        public Texture2D[] spritesHurt;
        public Texture2D[] spritesDying;
        public Texture2D spriteDead;

        public Texture2D spriteAttack;

        public AudioClip[] audioHit;
        public AudioClip[] audioDie;
        public AudioClip[] audioFight;

        [Header("Easter Eggs")]
        public AudioClip wilhelmScream;      // assign in inspector
        [Range(0f, 1f)]
        public float wilhelmChance = 0.02f;  // 2% default
    }
}