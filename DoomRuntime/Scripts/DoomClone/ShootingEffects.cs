using System.Collections;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class ShootingEffects : MonoBehaviour
    {
        public static ShootingEffects instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ShootingEffects>();
                return _instance;
            }
        }
        private static ShootingEffects _instance;

        [SerializeField]
        private Texture2D[] spritesBulletImpact;

        [SerializeField]
        private int poolSize = 16;

        private GameObject[] pool;
        private Billboard[] poolBillboards;
        private int nextIndex = 0;

        private WaitForSeconds cachedWait;

        void Awake()
        {
            cachedWait = new WaitForSeconds(0.05f);

            pool = new GameObject[poolSize];
            poolBillboards = new Billboard[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"HitEffect_{i}");
                go.SetActive(false);
                var bb = go.AddComponent<Billboard>();

                pool[i] = go;
                poolBillboards[i] = bb;
            }
        }

        public static void CreateHitAnimation(Vector3 point)
        {
            var inst = instance;
            if (inst == null) return;

            inst.StartCoroutine(inst.AnimateHitEffectPooled(point));
        }

        private IEnumerator AnimateHitEffectPooled(Vector3 point)
        {
            if (pool == null || pool.Length == 0)
                yield break;

            int idx = nextIndex;
            nextIndex = (nextIndex + 1) % pool.Length;

            var hitEffect = pool[idx];
            var billboard = poolBillboards[idx];
            if (hitEffect == null || billboard == null)
                yield break;

            point.y -= 0.2f;
            hitEffect.transform.position = point;
            hitEffect.SetActive(true);

            for (int i = 0; i < spritesBulletImpact.Length; i++)
            {
                billboard.SetTexture(spritesBulletImpact[i], false);
                yield return cachedWait;
            }

            hitEffect.SetActive(false);
        }
    }
}
