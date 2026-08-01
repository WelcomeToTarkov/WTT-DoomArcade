using System.Collections;
using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Items;
using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.DoomClone
{
    public class PlayerWeaponController : MonoBehaviour
    {
        private Transform doomCam;
        public WeaponData weaponData {
            get {
                return Player.current?.currentWeapon;
            }
        }
        public Image weaponImage;
        public Image weaponImageMuzzle;

        private RectTransform rectWeaponSpriteRoot;

        private bool isShooting = false;
        private bool isReloading = false;

        Vector3 playerPosPrev;
        private Queue<float> deltaMagnitudes;
        private int frameWindow = 10;
        private float recoilOffset;

        Vector2 weaponSway;

        private WeaponData nextWeaponData;
        private float weaponSwitchProgress = 0f;
        private bool isSwitchingWeapon = false;

        private void Start()
        {
            StartCoroutine(WaitForPlayerAndInit());
        }
        private IEnumerator WaitForPlayerAndInit()
        {
            while (Player.current == null || Player.current.body == null || Player.current.body.playerCamera == null)
                yield return null;

            doomCam = Player.current.body.playerCamera;

            if (!weaponImage || !weaponImageMuzzle)
            {
                yield break;
            }

            rectWeaponSpriteRoot = weaponImage.transform.parent as RectTransform;

            if (deltaMagnitudes == null)
                deltaMagnitudes = new Queue<float>(frameWindow);
            else
                deltaMagnitudes.Clear();

            playerPosPrev = doomCam.position;
        }

        public void Reinit()
        {
            doomCam = null;
            playerPosPrev = Vector3.zero;

            if (deltaMagnitudes == null)
                deltaMagnitudes = new Queue<float>(frameWindow);
            else
                deltaMagnitudes.Clear();

            recoilOffset = 0f;
            weaponSway = Vector2.zero;
            isSwitchingWeapon = false;
            isShooting = false;
            isReloading = false;

            StopAllCoroutines();
            StartCoroutine(WaitForPlayerAndInit());
        }


        private void OnEnable()
        {
            GlobalEventController.OnPlayerPickUp += PickupWeapon;
            GlobalEventController.OnGameStarted += OnGameStarted;
        }

        private void OnDisable()
        {
            GlobalEventController.OnPlayerPickUp -= PickupWeapon;
            GlobalEventController.OnGameStarted -= OnGameStarted;
        }

        private void OnGameStarted(int diff)
        {
            if (GameStateManager.instance.currentState != GameState.Playing)
                return;

            if (!weaponImage || !weaponImageMuzzle)
                return;

            weaponImage.enabled = true;
            weaponImageMuzzle.enabled = false;

            var pistol = DataManager.instance.GetWeaponById(1);
            if (pistol != null)
            {
                EquipWeapon(pistol, skipAnimation: true);
            }
        }

        private void Update()
        {
            if (GameStateManager.instance.currentState != GameState.Playing) return;
            if (Player.current == null) return;
            if (Player.current.health <= 0) {
                if (weaponImage) weaponImage.enabled = false;
                return;
            }
            if (weaponData == null) return;

            if (isSwitchingWeapon) return;

            if (Input.GetMouseButton(DoomedInput.FireMouseButton) && !isShooting && !isReloading)
            {
                if (CheckAmmo(weaponData.ammoType))
                {
                    StartCoroutine(AnimShoot());
                    StartCoroutine(AnimReload());
                    Shoot();
                }
            }

            if (!isShooting && !isReloading)
            {
                for (int i = 0; i < DoomedInput.WeaponKeys.Length; i++)
                {
                    if (Input.GetKeyDown(DoomedInput.WeaponKeys[i]))
                    {
                        WeaponData wd = DataManager.instance.GetWeaponById(i);
                        if (wd && Player.current.currentWeapon != wd && Player.current.weaponUnlocked[wd])
                            EquipWeapon(wd);
                    }
                }
            }
        }
        private void PickupWeapon(Item item)
        {
            if (item is not WeaponItem weaponItem)
                return;

            var wd = weaponItem.weaponData;
            if (wd == null || Player.current == null)
                return;

            if (Player.current.weaponUnlocked.TryGetValue(wd, out bool alreadyUnlocked) && alreadyUnlocked)
            {
                return;
            }

            EquipWeapon(wd);
        }


        public void EquipWeapon(WeaponData weaponData, bool skipAnimation = false)
        {
            if (!weaponImage.enabled)
                weaponImage.enabled = true;
            if (!isSwitchingWeapon)
            {
                nextWeaponData = weaponData;
                StartCoroutine(SmoothWeaponSwitch(skipAnimation));
            }
        
        }
        private IEnumerator SmoothWeaponSwitch(bool skipAnimation)
        {

            if (!nextWeaponData || !weaponImage)
            {
                isSwitchingWeapon = false;
                yield break;
            }
        
            if (!weaponImage.enabled)
                weaponImage.enabled = true;

            isSwitchingWeapon = true;
            weaponSwitchProgress = skipAnimation ? 1f : 0f;
            bool hasSwitchedSprite = false;

            if (skipAnimation)
            {
                Player.current.currentWeapon = nextWeaponData;
                weaponImage.sprite = nextWeaponData.idleSprite;
                GlobalEventController.PlayerWeaponSwitched(nextWeaponData);
                isSwitchingWeapon = false;
                yield break;
            }

            while (weaponSwitchProgress < 1f)
            {
                weaponSwitchProgress += Time.deltaTime;
                if (weaponSwitchProgress > 1f) weaponSwitchProgress = 1f;

                if (weaponSwitchProgress >= 0.5f && !hasSwitchedSprite)
                {
                    Player.current.currentWeapon = nextWeaponData;
                    weaponImage.sprite = nextWeaponData.idleSprite;
                    hasSwitchedSprite = true;
                }

                yield return null;
            }

            isSwitchingWeapon = false;
            GlobalEventController.PlayerWeaponSwitched(nextWeaponData);
        }


        private bool CheckAmmo(WeaponData.AmmoType ammoType)
        {
            if (Player.current == null) { 
                return false; 
            }
    
            if (ammoType == WeaponData.AmmoType.Melee) return true;
    
            bool hasAmmo = Player.current.ammo.TryGetValue(ammoType, out int ammo) && ammo > 0;
            return hasAmmo;
        }


        private void Shoot()
        {
            if (weaponData.ammoType != WeaponData.AmmoType.Melee)
                Player.current.ammo[weaponData.ammoType]--;

            recoilOffset += weaponData.recoil;
            weaponImageMuzzle.rectTransform.anchoredPosition = rectWeaponSpriteRoot.anchoredPosition;

            Player.current.body.Shoot(weaponData, 0.1f, true);

            GlobalEventController.PlayerShoot(weaponData);
        }

        private void LateUpdate()
        {
            if (GameStateManager.instance.currentState != GameState.Playing) return;
            if (Player.current == null || Player.current.body == null) return;

            if (doomCam == null || doomCam != Player.current.body.playerCamera)
            {
                doomCam = Player.current.body.playerCamera;
                if (doomCam == null) return;

                playerPosPrev = doomCam.position;
                if (deltaMagnitudes == null)
                    deltaMagnitudes = new Queue<float>(frameWindow);
                else
                    deltaMagnitudes.Clear();
            }

            if (deltaMagnitudes == null || rectWeaponSpriteRoot == null) return;

            Sway();
        }

        private void Sway()
        {
            if (doomCam == null) return;
            if (deltaMagnitudes == null) return;
            if (rectWeaponSpriteRoot == null) return;

            Vector3 posDelta = doomCam.position - playerPosPrev;
            float currentDeltaMagnitude = posDelta.magnitude / Time.deltaTime;
            playerPosPrev = doomCam.position;

            deltaMagnitudes.Enqueue(currentDeltaMagnitude);
            if (deltaMagnitudes.Count > frameWindow)
                deltaMagnitudes.Dequeue();

            float averageDeltaMagnitude = 0f;
            foreach (float delta in deltaMagnitudes)
                averageDeltaMagnitude += delta;

            if (deltaMagnitudes.Count > 0)
                averageDeltaMagnitude /= deltaMagnitudes.Count;

            float amplitude = averageDeltaMagnitude * 2f;
            float frequency = 0.6f;

            float angle = 2f * Mathf.PI * frequency * Time.time;
            float x = Mathf.Cos(angle) * amplitude;
            float y = Mathf.Abs(Mathf.Sin(angle)) * amplitude;

            y += recoilOffset;
            recoilOffset = Mathf.Lerp(recoilOffset, 0, Time.deltaTime * 10f);

            weaponSway = new Vector2(x, -y);

            if (!isSwitchingWeapon)
                rectWeaponSpriteRoot.anchoredPosition = weaponSway;
        }

        private IEnumerator AnimShoot()
        {
            if (weaponData == null || weaponImageMuzzle == null) yield break;
    
            isShooting = true;
            weaponImageMuzzle.enabled = true;
            for (int i = 0; i < weaponData.spritesShooting.Length; i++)
            {
                if (weaponImageMuzzle == null) yield break;
                weaponImageMuzzle.sprite = weaponData.spritesShooting[i];
                yield return new WaitForSeconds(weaponData.timingShooting[i]);
            }
            if (weaponImageMuzzle) weaponImageMuzzle.enabled = false;
            isShooting = false;
        }



        private IEnumerator AnimReload()
        {
            if (weaponData == null || weaponImageMuzzle == null) yield break;
            isReloading = true;
            for (int i = 0; i < weaponData.spritesReloading.Length; i++)
            {
                weaponImage.sprite = weaponData.spritesReloading[i];
                yield return new WaitForSeconds(weaponData.timingReloading[i]);
            }
            weaponImage.sprite = weaponData.idleSprite;
            isShooting = false;
            isReloading = false;
        }
    }
}
