using DoomArcade.Scripts.DoomClone.Items;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class DoomStyleController : WorldEntity
    {
        public float moveSpeed = 6.0f;
        public float rotationSpeed = 700.0f;
        private CharacterController characterController;

        public Transform playerCamera;
        public float bobbingSpeed = 0.18f;
        public float bobbingAmount = 0.2f;
        private float defaultPosY = 0.0f;
        private float timer = 0.0f;

        public float gravity = 9.81f;
        private float verticalSpeed = 0.0f;

        public AudioClip[] audioClipHurt;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>().transform;
            defaultPosY = playerCamera.localPosition.y;
        }

        private void OnEnable()
        {
            GlobalEventController.OnPlayerPickUp += PlayPickUpSound;
        }

        private void OnDisable()
        {
            GlobalEventController.OnPlayerPickUp -= PlayPickUpSound;
        }

        public void PlayPickUpSound(Item item)
        {
            AudioClip clip;

            if (item.audioClipPickup == null)
                clip = DataManager.instance.audioClipPickupGeneric;
            else
                clip = item.audioClipPickup;
            Player.current.itemsPickedUp++;
            ArcadeAudioBus.Instance.PlayAtCabinet(clip);
        }

        public override void TakeDamage(int dmg, WorldEntity source)
        {
            if (Player.current == null) return;
            if (Player.current.health <= 0) return;

            var cfg = Game.CurrentDifficulty;
            int effective = Mathf.RoundToInt(dmg * cfg.playerDamageTakenMultiplier);

            if (Player.current.armor <= 0)
                Player.current.health -= effective;
            else
                Player.current.armor = Mathf.Max(Player.current.armor - effective, 0);

            ArcadeAudioBus.Instance.PlayAtCabinet(audioClipHurt[Random.Range(0, audioClipHurt.Length)]);
            GlobalEventController.PlayerTakeDamage(effective, source);
        }

        void Awake()
        {
            var cc = GetComponent<CharacterController>();
            var col = GetComponent<CapsuleCollider>();
            if (col == null)
                col = gameObject.AddComponent<CapsuleCollider>();

            col.center = cc.center;
            col.radius = cc.radius;
            col.height = cc.height;
            col.isTrigger = false;

            gameObject.layer = LayerMask.NameToLayer("Default");
        }


        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[DoomTriggerTest] Player collider hit {other.name} (layer={other.gameObject.layer})");
            Item item = other.GetComponent<Item>();
            if (item != null)
                item.OnPickup(Player.current);

            if (other.GetComponent<FinishTrigger>() != null)
                GlobalEventController.PlayerVictory();
        }

        void Update()
        {
            if (GameStateManager.instance == null ||
                GameStateManager.instance.currentState != GameState.Playing)
                return;
            if (Player.current.health <= 0)
            {
                Vector3 target = new Vector3(playerCamera.localPosition.x, -0.6f, playerCamera.localPosition.z);
                playerCamera.transform.localPosition =
                    Vector3.Lerp(playerCamera.transform.localPosition, target, Time.deltaTime);
                return;
            }

            float moveDirectionX = Input.GetAxis(DoomedInput.MoveHorizontal);
            float moveDirectionZ = Input.GetAxis(DoomedInput.MoveVertical);

            bool sprint = Input.GetKey(DoomedInput.Sprint);

            Vector3 move = transform.right * moveDirectionX + transform.forward * moveDirectionZ;

            if (characterController.isGrounded)
            {
                verticalSpeed = 0;
            }
            else
            {
                verticalSpeed -= gravity * Time.deltaTime * 2;
            }

            Vector3 finalMove = move * moveSpeed * Time.deltaTime;
            if (sprint)
                finalMove *= 2;
            finalMove.y = verticalSpeed * Time.deltaTime;

            characterController.Move(finalMove);

            float turn = Input.GetAxis(DoomedInput.LookHorizontal);
            transform.rotation *= Quaternion.Euler(0, turn * rotationSpeed * Time.deltaTime, 0);

            if (Mathf.Abs(moveDirectionX) > 0.1f || Mathf.Abs(moveDirectionZ) > 0.1f)
            {
                timer += Time.deltaTime * bobbingSpeed;
                playerCamera.localPosition = new Vector3(playerCamera.localPosition.x,
                    defaultPosY + Mathf.Sin(timer) * bobbingAmount, playerCamera.localPosition.z);
            }
            else
            {
                timer = 0.0f;
                playerCamera.localPosition = new Vector3(playerCamera.localPosition.x,
                    Mathf.Lerp(playerCamera.localPosition.y, defaultPosY, Time.deltaTime * bobbingSpeed),
                    playerCamera.localPosition.z);
            }

            HandleDoorInteraction();
            HandleItemAndFinishOverlap();
        }

        void HandleDoorInteraction()
        {
            if (Player.current == null) return;
            if (!Input.GetKeyDown(DoomedInput.Use)) return;

            float radius = 0.4f;
            Vector3 origin = transform.position + transform.forward * 0.6f;

            Collider[] hits = Physics.OverlapSphere(origin, radius);
            Door bestDoor = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var door = hit.GetComponentInParent<Door>();
                if (door == null) continue;

                float d = Vector3.SqrMagnitude(hit.transform.position - origin);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestDoor = door;
                }
            }

            if (bestDoor != null)
            {
                bestDoor.ManualUseFromPlayer();
            }
        }

        void HandleItemAndFinishOverlap()
        {
            if (Player.current == null) return;

            Vector3 feet = transform.position;
            Vector3 knees = feet + Vector3.up * 0.5f;
            float radius = 0.3f;

            Collider[] hits = Physics.OverlapCapsule(feet, knees, radius);

            foreach (var hit in hits)
            {
                var item = hit.GetComponent<Item>();
                if (item != null)
                {
                    item.OnPickup(Player.current);
                    continue;
                }

                var finish = hit.GetComponent<FinishTrigger>();
                if (finish != null)
                {
                    GlobalEventController.PlayerVictory();
                }

                var secret = hit.GetComponent<SecretTrigger>();
                if (secret != null)
                {
                    secret.TryDiscoverFromPlayer();
                }
            }
        }
    }
}