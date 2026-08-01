using DoomArcade.Scripts.DoomClone.Items;
using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;
using UnityEngine.AI;

namespace DoomArcade.Scripts.DoomClone
{
    [RequireComponent(typeof(Billboard))]
    public class Enemy : WorldEntity
    {
        [SerializeField] public EnemyData data;

        private bool wasPlaying = true;
        private Billboard billboard;
        private float chaseRepathTimer = 0f;
        private const float ChaseRepathInterval = 0.25f;
        private NavMeshAgent agent;
        private Collider collider;
        private AudioSource audioSource;
        private int health;
        private bool alive => health > 0;

        private float timeSinceLastStep = 0;
        private bool useArrayA;

        private int animDyingIndex;
        private float animDyingTime;
        private float animHurtTime;
        private float animAttackTime;
        private Vector3 _lastChaseTarget;
        private bool _hasChaseTarget = false;
        private float tickTime;

        private float weaponCooldown;

        public enum State
        {
            Patrol,
            Aiming,
            Chase
        }
        private State state;
        private State previousState;
        private Transform _playerBody;

        void Start()
        {
            state = State.Patrol;
            previousState = state;
            billboard = GetComponent<Billboard>();
            agent = GetComponent<NavMeshAgent>();
            collider = GetComponent<Collider>();
            audioSource = GetComponent<AudioSource>();
            animDyingIndex = -1;

            var cfg = Game.CurrentDifficulty;

            health = Mathf.RoundToInt(data.baseHealth * cfg.enemyHealthMultiplier);
        
            weaponCooldown = data.weaponData.timeReload;
            if (cfg.fastMonsters)
            {
                weaponCooldown *= 0.5f;
            }
        }
        void OnEnable()
        {
            Game.RegisterEnemy(this);
        }

        void OnDisable()
        {
            Game.UnregisterEnemy(this);
        }
        public void InitForNewRun()
        {
            state = State.Patrol;
            previousState = state;
            weaponCooldown = data.weaponData.timeReload;
            animAttackTime = 0f;
            animHurtTime = 0f;
        }

        public override void TakeDamage(int dmg, WorldEntity source)
        {
            if (!alive)
                return;

            data.health -= dmg;
            health -= dmg;

            if (!alive)
            {
                OnDeath();
                return;
            }

            if (animHurtTime <= 0)
            {
                AudioClip clip = data.audioHit[Random.Range(0, data.audioHit.Length)];
                ArcadeAudioBus.Instance.PlayAtCabinet(clip);
            }

            animHurtTime = 0.1f;

            if (data.weaponData.ammoType != WeaponData.AmmoType.Melee)
            {
                State old = state;
                state = State.Aiming;
                weaponCooldown += 0.7f;
                HandleStateChange(old, state);
            }
        }

        void Update()
        {
            bool playing = GameStateManager.instance.currentState == GameState.Playing;
            _playerBody = Player.current?.body?.transform;
            if (agent != null && agent.isOnNavMesh && playing != wasPlaying)
            {
                agent.isStopped = !playing;
            }
            wasPlaying = playing;

            if (!playing) return;
            State oldState = state;

            if (alive)
            {
                if (weaponCooldown > 0)
                {
                    weaponCooldown -= Time.deltaTime;
                }

                if (state == State.Patrol)
                {
                    tickTime += Time.deltaTime;
                    if (tickTime > 0.3f)
                    {
                        tickTime = 0;
                        if (IsPlayerInFOV())
                        {
                            if (data.weaponData.ammoType == WeaponData.AmmoType.Melee)
                            {
                                state = State.Chase;
                            }
                            else
                            {
                                state = State.Aiming;
                            }
                        }
                        else
                        {
                            Patrol();
                        }
                    }
                }

                if (_playerBody == null)
                    state = State.Patrol;

                if (state == State.Aiming)
                {
                    Aim(_playerBody);
                    if (data.weaponData.ammoType == WeaponData.AmmoType.Melee)
                        state = State.Chase;
                    if (weaponCooldown <= 0)
                    {
                        Shoot(data.weaponData, 1.4f);
                        weaponCooldown += data.weaponData.timeReload;
                        weaponCooldown += 0.4f;

                        if (!IsPlayerInFOV())
                            state = State.Patrol;
                    }
                }
                else if (state == State.Chase)
                {
                    var body = _playerBody;
                    if (body == null) { state = State.Patrol; return; }

                    float distanceToPlayer = Vector3.Distance(body.position, transform.position);
                    Aim(body);

                    if (distanceToPlayer < 2f)
                    {
                        agent.ResetPath();
                        agent.velocity = Vector3.zero;

                        if (weaponCooldown <= 0f)
                        {
                            animAttackTime = 0.2f;

                            Shoot(data.weaponData, 1.4f);
                            Shoot(data.weaponData, 1.4f);
                            Shoot(data.weaponData, 1.4f);

                            weaponCooldown = data.weaponData.timeReload;
                        }
                    }
                    else if (distanceToPlayer < 5f)
                    {
                        agent.ResetPath();
                        Vector3 dir = (body.position - transform.position).normalized;
                        agent.Move(dir * agent.speed * Time.deltaTime);
                    }
                    else
                    {
                        chaseRepathTimer -= Time.deltaTime;
                        if (chaseRepathTimer <= 0f)
                        {
                            Chase(body);
                            chaseRepathTimer = ChaseRepathInterval;
                        }
                    }
                }
            }
        
            HandleStateChange(oldState, state);
            UpdateVisuals();
        }
        private void HandleStateChange(State from, State to)
        {
            if (from == to) return;

            bool wasPassive = (from == State.Patrol);
            bool nowAggro = (to == State.Aiming || to == State.Chase);

            if (wasPassive && nowAggro)
            {
                PlayFightClip();
            }

            previousState = to;
        }

        private void PlayFightClip()
        {
            if (data.audioFight == null || data.audioFight.Length == 0) return;
            if (audioSource == null) return;

            AudioClip clip = data.audioFight[Random.Range(0, data.audioFight.Length)];
            ArcadeAudioBus.Instance.PlayAtCabinet(clip);
        }

        private void Patrol()
        {
            var cfg = Game.CurrentDifficulty;

            agent.speed = 3.5f;
            if (cfg.fastMonsters)
            {
                agent.speed *= 1.5f;
            }

            float patrolRadius = 5f;

            if (agent.remainingDistance < 0.1f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                randomDirection += transform.position;
                NavMeshHit hit;
                NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1);
                Vector3 finalPosition = hit.position;
                agent.SetDestination(finalPosition);
            }
        }

        private void Aim(Transform target)
        {
            float turnSpeed = 10f;

            Vector3 a = transform.position;
            Vector3 b = target.position;
            b.y = a.y;

            Quaternion lookRotation = Quaternion.LookRotation(b - a);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }

        private void Chase(Transform body)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            Vector3 target = body.position;

            if (_hasChaseTarget)
            {
                float distChange = Vector3.Distance(target, _lastChaseTarget);
                if (distChange < 0.5f)
                    return;
            }

            var cfg = Game.CurrentDifficulty;
            agent.speed = 8f;
            if (cfg.fastMonsters) agent.speed *= 1.5f;

            agent.autoBraking = false;
            agent.stoppingDistance = 0.01f;

            agent.SetDestination(target);

            float distToPlayer = Vector3.Distance(transform.position, body.position);

            if (agent.remainingDistance <= agent.stoppingDistance && distToPlayer > 1.2f)
            {
                Vector3 dir = (body.position - transform.position).normalized;
                agent.Move(dir * agent.speed * Time.deltaTime);
            }

            _lastChaseTarget = target;
            _hasChaseTarget = true;
        }

        void OnDeath()
        {
            agent.enabled = false;
            collider.enabled = false;
            animDyingIndex = 0;

            if (audioSource != null)
            {
                AudioClip deathSound = null;

                bool canWilhelm = data.wilhelmScream != null && data.wilhelmChance > 0f;
                if (canWilhelm && Random.value < data.wilhelmChance)
                {
                    deathSound = data.wilhelmScream;
                }
                else if (data.audioDie != null && data.audioDie.Length > 0)
                {
                    deathSound = data.audioDie[Random.Range(0, data.audioDie.Length)];
                }

                if (deathSound != null)
                    ArcadeAudioBus.Instance.PlayAtCabinet(deathSound);
            }

            Player.current.kills++;

            if (data.itemDropPool.Length > 0)
                DropItem();
            NightmareRespawner.instance?.RegisterDeath(this);
        }

        private void DropItem()
        {
            Vector3 pos = transform.position;
            pos.x += Random.Range(-0.5f, 0.5f);
            pos.z += Random.Range(-0.5f, 0.5f);

            int randomIndex = Random.Range(0, data.itemDropPool.Length);
            GameObject itemPrefab = data.itemDropPool[randomIndex];
            GameObject item = Instantiate(itemPrefab, pos, Quaternion.identity);
            item.transform.parent = null;
            var itemComp = item.GetComponent<Item>();
            if (itemComp != null)
            {
                itemComp.countsAsItem = false;
                itemComp.countsAsSecret = false;
            }
        }

        private void UpdateVisuals()
        {
            if (_playerBody == null)
                return;

            Texture2D selectedTexture;

            if (alive)
            {
                Vector3 toViewer = _playerBody.position - transform.position;
                toViewer.y = 0f;

                if (toViewer.sqrMagnitude < 0.0001f)
                    return;

                float angleToViewer = Vector3.SignedAngle(transform.forward, toViewer, Vector3.up);

                float normalizedAngle = (angleToViewer + 360f) % 360f;
                float degreesPerSprite = 360f / 8f;
                float offsetAngle = normalizedAngle + (degreesPerSprite / 2f);
                int spriteIndex = Mathf.FloorToInt(offsetAngle / degreesPerSprite) % 8;

                if (animAttackTime > 0)
                {
                    animAttackTime -= Time.deltaTime;
                    selectedTexture = data.spriteAttack;
                }
                else if (animHurtTime > 0)
                {
                    animHurtTime -= Time.deltaTime;
                    selectedTexture = data.spritesHurt[spriteIndex];
                }
                else if (state == State.Aiming)
                {
                    selectedTexture = data.spritesAiming[spriteIndex];
                }
                else
                {
                    timeSinceLastStep += Time.deltaTime;
                    if (timeSinceLastStep > 0.5f)
                    {
                        timeSinceLastStep = 0;
                        useArrayA = !useArrayA;
                    }

                    selectedTexture = useArrayA
                        ? data.spritesWalkingA[spriteIndex]
                        : data.spritesWalkingB[spriteIndex];
                }
            }
            else
            {
                if (animDyingIndex >= 0)
                {
                    selectedTexture = data.spritesDying[animDyingIndex];
                    animDyingTime += Time.deltaTime;
                    if (animDyingTime > 0.1)
                    {
                        animDyingTime = 0;
                        animDyingIndex++;
                        if (animDyingIndex >= data.spritesDying.Length)
                        {
                            animDyingIndex = -1;
                        }
                    }
                }
                else
                {
                    selectedTexture = data.spriteDead;
                }
            }

            billboard.SetTexture(selectedTexture, false);
        }

        bool IsPlayerInFOV()
        {
            if (_playerBody == null)
                return false;

            float detectionRange = 20f;
            float detectionAngle = 150f;
            Vector3 toPlayer = _playerBody.position - transform.position;

            if (toPlayer.magnitude <= detectionRange)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
                if (angle <= detectionAngle / 2f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, toPlayer.normalized, out hit, detectionRange))
                    {
                        var body = Player.current?.body;
                        if (body != null && hit.collider.GetComponent<DoomStyleController>() == body)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, data.enemyName + ".png", true);
        }
    }
}
