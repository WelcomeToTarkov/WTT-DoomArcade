using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.DoomClone
{
    public class MenuNavigator : MonoBehaviour
    {
        [SerializeField] private AudioClip menuSelectSound;
        [SerializeField] private AudioClip menuBackSound;
        
        [System.Serializable]
        public class MenuConfig
        {
            public GameState state;
            public Button[] buttons;
            public int defaultIndex = 0;
        }


        [SerializeField] MenuConfig[] menus;

        private int currentSelection = 0;
        private Button currentButton;
        [SerializeField] Sprite[] skullSprites;
        private float blinkTimer = 0f;
        private int currentSkullIndex = 0;
        private Image activeSkull;
        void Start()
        {
            SelectCurrentMenu();
        }

        void Update()
        {
            var gsm = GameStateManager.instance;
            if (gsm == null) return;
            if (gsm.currentState == GameState.MainMenu && !gsm.MenuVisible)
                return;
            if (gsm.ignoreMenuInputThisFrame)
            {
                gsm.ignoreMenuInputThisFrame = false;
                return;
            }
            if (gsm.IsQuitConfirmActive)
                return;
            if (gsm.currentState != GameState.MainMenu &&
                gsm.currentState != GameState.DifficultySelect &&
                gsm.currentState != GameState.Paused)
                return;

            if (gsm.currentState == GameState.MainMenu && gsmIntroRunning())
                return;

            if (gsm.currentState == GameState.Victory) return;

            if (Input.GetKeyDown(DoomedInput.MenuUp) || Input.GetKeyDown(DoomedInput.MenuUpAlt))
            {
                Navigate(-1);
            }
            else if (Input.GetKeyDown(DoomedInput.MenuDown) || Input.GetKeyDown(DoomedInput.MenuDownAlt))
            {
                Navigate(1);
            }
            else if (Input.GetKeyDown(DoomedInput.MenuConfirm) || Input.GetKeyDown(DoomedInput.MenuConfirm2))
            {
                ActivateCurrentButton();
            }

            AnimateSkullBlink();
        }

        bool gsmIntroRunning()
        {
            return GameStateManager.instance != null &&
                   GameStateManager.instance.IsIntroRunning;
        }

        void AnimateSkullBlink() {
            if (activeSkull == null) return;
    
            blinkTimer += Time.unscaledDeltaTime;
            if (blinkTimer >= 2f) {
                blinkTimer = 0f;
                currentSkullIndex = 1 - currentSkullIndex;
                activeSkull.sprite = skullSprites[currentSkullIndex];
            }
        }

        void Navigate(int direction)
        {
            MenuConfig menu = GetCurrentMenu();
            if (menu == null || menu.buttons.Length == 0) return;

            currentSelection = Mathf.Clamp(currentSelection + direction, 0, menu.buttons.Length - 1);
            SelectButton(menu.buttons[currentSelection]);
        }

        void SelectButton(Button button) {
            HideAllSkulls();

            Image skull = button.transform.Find("Skull")?.GetComponent<Image>();
            if (skull) {
                skull.gameObject.SetActive(true);
                activeSkull = skull;
                skull.sprite = skullSprites[currentSkullIndex];
            }

            currentButton = button;
        }


        void HideAllSkulls()
        {
            foreach (var menu in menus)
            {
                foreach (var btn in menu.buttons)
                {
                    Image skull = btn.transform.Find("Skull")?.GetComponent<Image>();
                    if (skull) skull.gameObject.SetActive(false);
                }
            }
        }

        void ActivateCurrentButton()
        {
            if (currentButton != null)
            {
                if (menuSelectSound != null)
                    ArcadeAudioBus.Instance.PlayAtCabinet(menuSelectSound);
                currentButton.onClick.Invoke();
            }
        }

        public void SelectCurrentMenu()
        {
            MenuConfig menu = GetCurrentMenu();
            if (menu != null && menu.buttons.Length > 0)
            {
                int idx = Mathf.Clamp(menu.defaultIndex, 0, menu.buttons.Length - 1);
                currentSelection = idx;
                SelectButton(menu.buttons[idx]);
            }
        }


        MenuConfig GetCurrentMenu()
        {
            return menus.FirstOrDefault(m => m.state == GameStateManager.instance.currentState);
        }
    }
}