using System.Collections;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    [RequireComponent(typeof(MeshFilter))]
    public class Door : MonoBehaviour
    {
        public static Door currentDoor; 
    
        [Header("Key / Access")]
        [SerializeField] private int requiredKeyId = 0;

        bool playerNearby = false;
        bool doorOpen = false;
        Vector3 closedPosition;
        Vector3 openPosition;
        Coroutine slideCoroutine;

        BoxCollider physicalCollider;
        AudioSource audioSource;
        public void ResetState()
        {
            doorOpen = false;
            transform.position = closedPosition;
            if (physicalCollider) physicalCollider.enabled = true;
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        }
        void Awake()
        {
            Debug.Log($"[Door] Awake on {name}, enabled={enabled}, activeInHierarchy={gameObject.activeInHierarchy}");
        }

        void OnEnable()
        {
            Debug.Log($"[Door] OnEnable on {name}, enabled={enabled}, activeInHierarchy={gameObject.activeInHierarchy}");
            EnsureSetup();
        }

        void Start()
        {
            Debug.Log($"[Door] Start on {name}, Player.current={Player.current}, body={Player.current?.body}");

        }
        public void EnsureSetup()
        {
            if (physicalCollider != null)
                return;

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null)
            {
                Debug.LogError($"[Door] {name} has no MeshFilter or mesh, cannot setup door.");
                return;
            }

            Vector3 meshSize = meshFilter.mesh.bounds.size;
            Vector3 meshCenter = meshFilter.mesh.bounds.center;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            physicalCollider = GetComponent<BoxCollider>();
            if (physicalCollider == null)
                physicalCollider = gameObject.AddComponent<BoxCollider>();

            physicalCollider.size   = meshSize;
            physicalCollider.center = meshCenter;
            physicalCollider.isTrigger = false;

            var triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size   = meshSize + new Vector3(3f, 10f, 3f);
            triggerCollider.center = meshCenter;

            closedPosition = transform.position;
            openPosition = new Vector3(
                transform.position.x,
                transform.position.y + meshSize.y,
                transform.position.z
            );

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            Debug.Log($"[Door] EnsureSetup complete on {name}, physCollider={physicalCollider}, trigger={triggerCollider}");
        }

        void Update()
        {
            if (Door.currentDoor != this) return;
            if (!playerNearby) return;
            if (!Input.GetKeyDown(DoomedInput.Use)) return;

            if (requiredKeyId > 0)
            {
                if (!Player.current.HasKey(requiredKeyId))
                {
                    ArcadeAudioBus.Instance.PlayAtCabinet(DataManager.instance.audioClipDoorLocked);
                    return;
                }

                ArcadeAudioBus.Instance.PlayAtCabinet(DataManager.instance.audioClipDoorUnlock);
                ToggleDoor(true);
                return;
            }

            ToggleDoor();
        }


        void OnTriggerEnter(Collider other)
        {

            if (!IsPlayerBody(other)) return;

            playerNearby = true;
            UpdateAsClosestDoor(other.transform);
        }
        
        public void ManualUseFromPlayer()
        {
            if (requiredKeyId > 0)
            {
                if (!Player.current.HasKey(requiredKeyId))
                {
                    ArcadeAudioBus.Instance.PlayAtCabinet(DataManager.instance.audioClipDoorLocked);
                    return;
                }

                ArcadeAudioBus.Instance.PlayAtCabinet(DataManager.instance.audioClipDoorUnlock);
                ToggleDoor(true);
                return;
            }

            ToggleDoor();
        }




        void OnTriggerStay(Collider other)
        {
            if (IsPlayerBody(other))
            {
                UpdateAsClosestDoor(other.transform);
                playerNearby = true;
            }
        }


        void OnTriggerExit(Collider other)
        {
            if (!IsPlayerBody(other)) return;

            playerNearby = false;
            if (Door.currentDoor == this)
                Door.currentDoor = null;
        }

        bool IsPlayerBody(Collider other)
        {
            if (Player.current == null || Player.current.body == null)
                return false;

            return other.transform == Player.current.body.transform
                   || other.transform.IsChildOf(Player.current.body.transform);
        }


        void UpdateAsClosestDoor(Transform player)
        {
            if (Door.currentDoor == null)
            {
                Door.currentDoor = this;
                return;
            }

            float myDist = Vector3.SqrMagnitude(player.position - transform.position);
            float otherDist = Vector3.SqrMagnitude(player.position - Door.currentDoor.transform.position);

            if (myDist < otherDist)
                Door.currentDoor = this;
        }


        void ToggleDoor(bool playClipDelayed = false)
        {
            if (slideCoroutine != null)
                StopCoroutine(slideCoroutine);

            slideCoroutine = StartCoroutine(SlideDoor(doorOpen ? closedPosition : openPosition));
            doorOpen = !doorOpen;

            if (playClipDelayed)
                StartCoroutine(PlayDoorSoundDelayed(0.3f));
            else
                ArcadeAudioBus.Instance.PlayAtCabinet(DataManager.instance.audioClipDoor);
        }

        IEnumerator PlayDoorSoundDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            ArcadeAudioBus.Instance.PlayAtCabinet(DataManager.instance.audioClipDoor);
        }


        IEnumerator SlideDoor(Vector3 targetPosition)
        {
            float journeyLength = Vector3.Distance(transform.position, targetPosition);
            float startTime = Time.time;

            float speed = 1.0f;
            float distanceCovered, fractionOfJourney;

            while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
            {
                distanceCovered = (Time.time - startTime) * speed;
                fractionOfJourney = distanceCovered / journeyLength;

                transform.position = Vector3.Lerp(transform.position, targetPosition, fractionOfJourney);

                yield return null;
            }
        
            physicalCollider.enabled = !doorOpen;
            transform.position = targetPosition;
        }
    }
}
