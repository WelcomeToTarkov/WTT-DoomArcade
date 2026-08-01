using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class WorldEntity : MonoBehaviour
    {
        protected AudioSource audioSource
        {
            get
            {
                if (_audioSource == null)
                    _audioSource = GetComponent<AudioSource>();
                return _audioSource;
            }
        }
        AudioSource _audioSource;

        private static int GetDamage(WeaponData.AmmoType ammoType)
        {
            switch (ammoType)
            {
                case WeaponData.AmmoType.Light:
                    return 30;
                case WeaponData.AmmoType.Medium:
                    return 40;
                case WeaponData.AmmoType.Heavy:
                    return 150;
                case WeaponData.AmmoType.Shotgun:
                    return 20;
                case WeaponData.AmmoType.Melee:
                    return 30;
                default:
                    return 0;
            }
        }


        public void Shoot(WeaponData weaponData, float yOffset, bool aimAssist = false)
        {
            Vector3 sourcePos = transform.position;
            sourcePos.y += yOffset;

            int numberOfPellets = 1;
            float spreadAngle = 2f;
            int damage = GetDamage(weaponData.ammoType);
            var cfg = Game.CurrentDifficulty;
            damage = Mathf.RoundToInt(damage * cfg.enemyDamageMultiplier);
            float maxDistance = 100f;

            if (weaponData.ammoType == WeaponData.AmmoType.Melee)
                maxDistance = 2f;

            if (weaponData.ammoType == WeaponData.AmmoType.Shotgun)
            {
                numberOfPellets = 7;
                spreadAngle = 4f;
            }

            Vector3 baseDir = transform.forward;
            if (aimAssist)
            {
                Transform closestEnemy = GetEnemyForAimAssistCached(sourcePos);
                if (closestEnemy != null)
                {
                    Vector3 enemyPos = closestEnemy.position;
                    enemyPos.y += 1.5f;
                    baseDir = (enemyPos - sourcePos).normalized;
                }
            }

            for (int i = 0; i < numberOfPellets; i++)
            {
                float randomSpreadX = Random.Range(-spreadAngle, spreadAngle);
                float randomSpreadY = Random.Range(-spreadAngle, spreadAngle);

                Vector3 directionWithSpread =
                    Quaternion.Euler(randomSpreadX, randomSpreadY, 0f) * baseDir;

                if (Physics.Raycast(sourcePos, directionWithSpread, out RaycastHit hit, maxDistance))
                {
                    WorldEntity target = hit.collider.GetComponent<WorldEntity>();
                    if (target != null)
                    {
                        target.TakeDamage(damage, this);
                    }
                    else
                    {
                        ShootingEffects.CreateHitAnimation(hit.point);
                    }
                }
            }

            ArcadeAudioBus.Instance.PlayAtCabinet(weaponData.soundShoot);
            ArcadeAudioBus.Instance.PlayAtCabinet(weaponData.soundReload);
        }
        public Transform GetEnemyForAimAssistCached(Vector3 sourcePos)
        {
            if (this is Enemy)
                return null;

            var enemies = Game.ActiveEnemies;
            if (enemies == null || enemies.Count == 0)
                return null;

            Transform closestEnemy = null;
            float minDistance = float.MaxValue;

            Vector3 origin = sourcePos;
            Vector3 forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled)
                    continue;

                Transform enemyTransform = enemy.transform;
                Vector3 enemyPos = enemyTransform.position + Vector3.up * 1.5f;

                Vector3 toEnemy = enemyPos - origin;
                float dist = toEnemy.magnitude;
                if (dist >= minDistance)
                    continue;

                Vector3 toEnemyXZ = new Vector3(toEnemy.x, 0f, toEnemy.z).normalized;
                float angle = Vector3.Angle(forwardXZ, toEnemyXZ);
                if (angle > 5f)
                    continue;

                minDistance = dist;
                closestEnemy = enemyTransform;
            }

            return closestEnemy;
        }

        private void CreateShotLine(Vector3 start, Vector3 end)
        {
            return;
            GameObject lineObj = new GameObject("ShotLine");
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Custom/BoxProject"));
            lineRenderer.widthMultiplier = 0.1f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            Destroy(lineObj, 0.5f);
        }

        public abstract void TakeDamage(int dmg, WorldEntity source);
    }
}