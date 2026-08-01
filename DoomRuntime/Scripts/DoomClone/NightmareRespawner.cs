using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class NightmareRespawner : MonoBehaviour
    {
        [SerializeField] public Game game;
        class DeadInfo
        {
            public EnemyData data;
            public Vector3 position;
            public float respawnAt;
        }

        List<DeadInfo> dead = new List<DeadInfo>();

        public static NightmareRespawner instance;

        void Awake() => instance = this;
        public void ClearAll()
        {
            dead.Clear();
        }

        public void RegisterDeath(Enemy enemy)
        {
            if (!game) return;

            var cfg = game.GetCurrentDifficulty();
            if (!cfg.respawnMonsters) return;

            float delay = Random.Range(10f, 20f); 
            dead.Add(new DeadInfo
            {
                data = enemy.data,
                position = enemy.transform.position,
                respawnAt = Time.time + delay
            });
        }

        void Update()
        {
            if (!game) return;

            var cfg = game.GetCurrentDifficulty();
            if (cfg == null) return;
            if (!cfg.respawnMonsters) return;

            for (int i = dead.Count - 1; i >= 0; i--)
            {
                if (Time.time >= dead[i].respawnAt)
                {
                    Instantiate(dead[i].data.prefab, dead[i].position, Quaternion.identity);
                    dead.RemoveAt(i);
                }
            }
        }
    }

}