using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Items;
using DoomArcade.Scripts.DoomClone.UI;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    [System.Serializable]
    public class DifficultyConfig
    {
        public string name;
        [Range(0.25f, 4f)] public float enemyHealthMultiplier = 1f;
        [Range(0.25f, 4f)] public float enemyDamageMultiplier = 1f;
        [Range(0.25f, 4f)] public float enemyCountMultiplier = 1f;
        [Range(0.25f, 4f)] public float playerDamageTakenMultiplier = 1f;
        [Range(0.25f, 4f)] public float ammoPickupMultiplier = 1f;
        [Range(0.25f, 4f)] public float healthPickupMultiplier = 1f;

        public bool fastMonsters; 
        public bool respawnMonsters; 
        public bool doubleAmmo; 
    }

    public class Game : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private PlayerWeaponController _playerWeaponController;
        public static Game Instance { get; private set; }
        public static readonly List<Enemy> ActiveEnemies = new List<Enemy>();
        [Header("Billboard")]
        [SerializeField]
        public Shader boxProjectShader;
        [Header("Difficulty")] [SerializeField]
        public DifficultyConfig[] difficultyConfigs;
        public static DifficultyConfig CurrentDifficulty { get; private set; }
        
        public static EnemySpawner[] EnemySpawners { get; private set; }
        public static ItemSpawner[] ItemSpawners { get; private set; }
        public static SecretTrigger[] SecretTriggers { get; private set; }
        public static Item[] AllItems { get; private set; }
        public static Door[] Doors { get; private set; }
        
        public static void RegisterEnemy(Enemy e)
        {
            if (e == null) return;
            if (!ActiveEnemies.Contains(e))
                ActiveEnemies.Add(e);
        }

        public static void UnregisterEnemy(Enemy e)
        {
            if (e == null) return;
            ActiveEnemies.Remove(e);
        }
        public DifficultyConfig GetCurrentDifficulty()
        {
            if (difficultyConfigs == null || difficultyConfigs.Length == 0)
            {
                return new DifficultyConfig();
            }

            if (GameStateManager.instance == null)
            {
                return difficultyConfigs[0];
            }

            int diffIndex = Mathf.Clamp(GameStateManager.instance.difficulty - 1, 0, difficultyConfigs.Length - 1);
            CurrentDifficulty = difficultyConfigs[diffIndex];
            return difficultyConfigs[diffIndex];
        }


        void Awake()
        {
            Instance = this;
            if (boxProjectShader == null)
            {
                boxProjectShader = Shader.Find("Custom/BoxProject");
            }
        }

        public void RestartGame(int diff)
        {
            int diffIndex = Mathf.Clamp(diff - 1, 0, difficultyConfigs.Length - 1);
            var cfg = difficultyConfigs[diffIndex];
            CurrentDifficulty = cfg;
            if (Player.current?.body)
                Destroy(Player.current.body.gameObject);

            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (var enemy in enemies) Destroy(enemy.gameObject);
            Item[] droppedItems = FindObjectsOfType<Item>();
            foreach (var item in droppedItems) Destroy(item.gameObject);

            GameObject playerObject = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            DoomStyleController playerBody = playerObject.GetComponent<DoomStyleController>();

            var pistol = DataManager.instance.GetWeaponById(1);
            Player.current = new Player(playerBody, pistol);

            if (pistol != null)
            {
                Player.current.currentWeapon = pistol;
                Player.current.ammo[pistol.ammoType] = 50;
            }

            var status = FindObjectOfType<StatusBar>();
            if (status != null)
            {
                status.ResetHUD();
            }

            var face = FindObjectOfType<Face>();
            if (face != null)
                face.ResetFace();
            Player.current.kills = 0;
            Player.current.itemsPickedUp = 0;
            Player.current.secretsFound = 0;
            
            if (EnemySpawners == null)
                EnemySpawners = FindObjectsOfType<EnemySpawner>();
            if (ItemSpawners == null)
                ItemSpawners = FindObjectsOfType<ItemSpawner>();
            if (SecretTriggers == null)
                SecretTriggers = FindObjectsOfType<SecretTrigger>();
            
            if (ItemSpawners != null)
            {
                foreach (var spawner in ItemSpawners)
                    spawner.Restart(diff);
            }
            
            AllItems = FindObjectsOfType<Item>();
            if (Doors == null)
                Doors = FindObjectsOfType<Door>();
            int maxKills = 0;
            foreach (var spawner in EnemySpawners)
            {
                int count = Mathf.RoundToInt(spawner.baseCount * cfg.enemyCountMultiplier);
                maxKills += Mathf.Max(count, spawner.baseCount); 
            }
            Player.current.maxKills = maxKills;

            Player.current.maxItems = ItemSpawners.Length;
            
            int secretItemCount = 0;
            foreach (var it in AllItems)
            {
                if (it.countsAsSecret)
                    secretItemCount++;
            }
            Player.current.maxSecrets = (SecretTriggers?.Length ?? 0) + secretItemCount;

            Player.current.timer = 0f;

            if (Doors != null)
            {
                foreach (var door in Doors)
                    door.ResetState();
            }

            if (EnemySpawners != null)
            {
                foreach (var spawner in EnemySpawners)
                    spawner.Restart(diff);
            }

            var respawner = NightmareRespawner.instance;
            if (respawner != null)
                respawner.ClearAll();
            if (_playerWeaponController != null)
                _playerWeaponController.Reinit();

            if (Player.current.currentWeapon != null)
                GlobalEventController.PlayerWeaponSwitched(Player.current.currentWeapon);
            GlobalEventController.GameStarted(diff);
        }


        private void Update()
        {
            if (Player.current == null) return;
            Player.current.timer += Time.deltaTime;
        }
    }
}