using UnityEngine;
using UnityEngine.AI;

namespace DoomArcade.Scripts.DoomClone
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] public GameObject enemyPrefab;
        [SerializeField] public int baseCount = 1;

        void OnEnable()
        {
            GlobalEventController.OnGameStarted += OnGameStarted;
        }

        void OnDisable()
        {
            GlobalEventController.OnGameStarted -= OnGameStarted;
        }

        private void OnGameStarted(int diff)
        {
            Restart(diff);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 1.0f);
        }

        public void Restart(int diff)
        {
            Enemy[] existing = GetComponentsInChildren<Enemy>();
            foreach (var e in existing)
                if (e) Destroy(e.gameObject);

            if (!enemyPrefab)
            {
                return;
            }

            var cfg = Game.CurrentDifficulty;
            if (cfg == null)
            {
                return;
            }

            int count = Mathf.RoundToInt(baseCount * cfg.enemyCountMultiplier);
            count = Mathf.Max(count, baseCount);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = transform.position;
                pos.x += Random.Range(-0.2f, 0.2f);
                pos.z += Random.Range(-0.2f, 0.2f);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(pos, out hit, 1.0f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }

                GameObject enemyObj = Instantiate(enemyPrefab, pos, transform.rotation, transform);
                Enemy e = enemyObj.GetComponent<Enemy>();
                if (e != null && e.data != null)
                {
                    e.InitForNewRun();
                }
            }
        }
    }
}
