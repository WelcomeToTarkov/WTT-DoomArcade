using System.Collections;
using DoomArcade.Scripts.Arcade;
using DoomArcade.Scripts.DoomClone.UI;
using UnityEngine;
using UnityEngine.UI;


namespace DoomArcade.Scripts.DoomClone
{
    public enum GameState
    {
        MainMenu,
        DifficultySelect,
        Playing,
        Paused,
        Victory
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager instance;

        [Header("Panels")] [SerializeField] GameObject mainMenuPanel;
        [SerializeField] private GameObject mainMenuBackgroundRoot;
        [SerializeField] GameObject difficultyPanel;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject victoryPanel;

        [Header("Victory SpriteTexts")] [SerializeField]
        SpriteText killsText;

        [SerializeField] SpriteText itemsText;
        [SerializeField] SpriteText secretsText;
        [SerializeField] SpriteText timeText;
        [SerializeField] SpriteText parText;

        [Header("Victory Audio")]
        private Coroutine victoryLoopRoutine;
        [SerializeField] private AudioClip victoryCountLoopClip;
        [SerializeField] private AudioClip victoryExplosionClip;
        [SerializeField] private float victoryCountLoopVolume = 1f;
        [SerializeField] private float victoryExplosionVolume = 1f;
        
        [Header("Groups")] [SerializeField] GameObject guiGroup;
        [SerializeField] GameObject renderGroup;
        [SerializeField] GameObject effectsGroup;

        [Header("Game Config")] [SerializeField]
        public string[] parLabels =
        {
            "PAR :30",
            "PAR :35",
            "PAR :55",
            "PAR 1:04",
            "PAR 1:30"
        };

        [Header("Quit Confirm")] [SerializeField]
        private Image quitConfirmImage;
        

        [SerializeField] private Sprite[] quitConfirmSprites;

        private bool quitConfirmActive = false;
        public bool IsQuitConfirmActive => quitConfirmActive;
        [SerializeField] private RectTransform panelRect;

        private enum MusicMode
        {
            None,
            Menu,
            Level,
            Victory
        }

        private MusicMode lastMusicMode = MusicMode.None;


        [Header("Refs")] [SerializeField] public MenuNavigator MenuNavigator;
        [SerializeField] MainMenuBackground mainMenuBackground;
        public bool hasStartedOnce = false;
        public int difficulty = 1;
        public GameState currentState = GameState.MainMenu;
        private bool difficultyFromMainMenu = false;
        private bool introRunning = false;
        private TarkovDoomExitHook _exitHook;
        public bool IsIntroRunning => introRunning;

        private int targetKillPercent;
        private int targetItemPercent;
        private int targetSecretPercent;
        private string targetTimeText;
        private string targetParText;
        private Coroutine victoryRoutine;
        private bool pausedFromVictory = false;
        private bool inPostVictoryFlow = false;
        private bool introSkipped = false;
        private bool isTransitioning = false;
        private bool menuVisible = false;
        public bool MenuVisible => menuVisible;
        public bool ignoreMenuInputThisFrame = false;
        void Awake()
        {
            instance = this;
            Debug.Log(" Awake on " + gameObject.name + " menus length=" + (MenuNavigator ? "OK" : "NULL"));
            Time.timeScale = 1f;
        }

        public void ResetToMainMenu()
        {
            StopRepeatingGunfire();
            hasStartedOnce = false;
            difficultyFromMainMenu = false;
            introRunning = false;
            introSkipped = false;
            pausedFromVictory = false;
            inPostVictoryFlow = false;
            quitConfirmActive = false;
            menuVisible = false;
            currentState = GameState.MainMenu;

            if (quitConfirmImage)
                quitConfirmImage.gameObject.SetActive(false);

            UpdateStateUI();
        }

        public void ReplayIntro()
        {
            StopAllCoroutines();

            introRunning = true;
            introSkipped = false;
            hasStartedOnce = false;
            pausedFromVictory = false;
            inPostVictoryFlow = false;
            quitConfirmActive = false;
            difficultyFromMainMenu = false;
            currentState = GameState.MainMenu;
            menuVisible = false; 
            if (quitConfirmImage)
                quitConfirmImage.gameObject.SetActive(false);

            if (mainMenuBackgroundRoot) mainMenuBackgroundRoot.SetActive(true);
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (difficultyPanel) difficultyPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            if (victoryPanel) victoryPanel.SetActive(false);

            if (MusicManager.instance != null)
                MusicManager.instance.PlayMenuMusic();

            StartCoroutine(IntroSequence());
        }


        public void SoftResetForReturnFromArcade()
        {
            quitConfirmActive = false;
            pausedFromVictory = false;
            inPostVictoryFlow = false;
            difficultyFromMainMenu = false;

            if (quitConfirmImage)
                quitConfirmImage.gameObject.SetActive(false);

            if (currentState != GameState.Playing)
            {
                currentState = GameState.MainMenu;
                menuVisible = false;
                UpdateStateUI();
            }
        }

        void Start()
        {
            introRunning = true;
            GlobalEventController.OnPlayerVictory += OnVictory;
            InvokeRepeating(nameof(CheckPlayerDeath), 0f, 0.1f);

            if (mainMenuBackgroundRoot) mainMenuBackgroundRoot.SetActive(true);

            if (MusicManager.instance == null)
                Debug.LogWarning("[GameStateManager] MusicManager.instance is NULL in Start");
            else
                MusicManager.instance.PlayMenuMusic();

            StartCoroutine(IntroSequence());
        }


        IEnumerator IntroSequence()
        {
            float delay = Random.Range(4f, 8f);
            float t = 0f;
            while (t < delay && !introSkipped)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (introRunning && currentState == GameState.MainMenu)
                SetState(GameState.MainMenu);

            introRunning = false;
        }

        void Update()
        {
            if (introRunning && !introSkipped)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Escape))
                {
                    introSkipped = true;
                }
            }

            if (quitConfirmActive)
            {
                if (Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(DoomedInput.MenuConfirmYes))
                {
                    DoQuitToArcade();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Escape) ||
                    Input.GetKeyDown(DoomedInput.MenuConfirmNo) ||
                    Input.GetKeyDown(DoomedInput.PauseOrBack))
                {
                    quitConfirmActive = false;
                    if (quitConfirmImage)
                        quitConfirmImage.gameObject.SetActive(false);
                    return;
                }

                return;
            }
            if (currentState == GameState.MainMenu && !introRunning && !isTransitioning)
            {
                if (!menuVisible)
                {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                        Input.GetKeyDown(KeyCode.Escape))
                    {
                        menuVisible = true;
                        ignoreMenuInputThisFrame = true;
                        UpdateStateUI();
                        MenuNavigator?.SelectCurrentMenu();
                        return;
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(DoomedInput.PauseOrBack))
                    {
                        menuVisible = false;
                        ignoreMenuInputThisFrame = true;
                        UpdateStateUI();
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(DoomedInput.PauseOrBack))
            {
                switch (currentState)
                {
                    case GameState.Playing:
                        pausedFromVictory = false;
                        SetState(GameState.Paused);
                        break;
                    case GameState.Paused:
                        if (pausedFromVictory)
                            SetState(GameState.Victory);
                        else
                            ResumeGame();
                        break;
                    case GameState.Victory:
                        pausedFromVictory = true;
                        SetState(GameState.Paused);
                        break;
                    case GameState.DifficultySelect:
                        if (difficultyFromMainMenu)
                        {
                            menuVisible = true;
                            SetState(GameState.MainMenu);
                        }
                        else
                            SetState(GameState.Paused);

                        break;
                }
            }
        }

        public void ResumeGame()
        {
            currentState = GameState.Playing;
            UpdateStateUI();
        }

        public void SetState(GameState newState)
        {
            var prevState = currentState;
            currentState = newState;

            UpdateStateUI();

            if (MusicManager.instance != null)
            {
                MusicMode mode;

                switch (currentState)
                {
                    case GameState.MainMenu:
                        mode = MusicMode.Menu;
                        break;

                    case GameState.DifficultySelect:
                        if (difficultyFromMainMenu)
                            mode = MusicMode.Menu;
                        else if (inPostVictoryFlow)
                            mode = MusicMode.Victory;
                        else
                            mode = MusicMode.Level;
                        break;

                    case GameState.Playing:
                        mode = MusicMode.Level;
                        break;

                    case GameState.Paused:
                        mode = pausedFromVictory ? MusicMode.Victory : MusicMode.Level;
                        break;

                    case GameState.Victory:
                        mode = MusicMode.Victory;
                        break;

                    default:
                        mode = MusicMode.Level;
                        break;
                }

                if (mode != lastMusicMode)
                {
                    lastMusicMode = mode;

                    switch (mode)
                    {
                        case MusicMode.Menu:
                            break;
                        case MusicMode.Level:
                            MusicManager.instance.PlayLevelMusic();
                            break;
                        case MusicMode.Victory:
                            MusicManager.instance.PlayVictorySequence();
                            break;
                    }
                }
            }

            if (newState == GameState.Playing && prevState != GameState.Playing)
            {
                var game = Game.Instance;
                if (game)
                {
                    game.RestartGame(difficulty);
                    GlobalEventController.GameStarted(difficulty);
                }
            }
        }

        void UpdateStateUI()
        {
            bool showBG = currentState == GameState.MainMenu
                          || (currentState == GameState.DifficultySelect && difficultyFromMainMenu);
            mainMenuBackgroundRoot.SetActive(showBG);

            if (showBG && currentState == GameState.MainMenu && !hasStartedOnce)
            {
                hasStartedOnce = true;
                mainMenuBackground.PlayIntro();
            }

            bool showVictoryPanel =
                currentState == GameState.Victory ||
                (currentState == GameState.Paused && pausedFromVictory) ||
                (currentState == GameState.DifficultySelect && inPostVictoryFlow && !difficultyFromMainMenu);

            victoryPanel.SetActive(showVictoryPanel);

            bool showMenu = currentState == GameState.MainMenu && menuVisible;
            mainMenuPanel.SetActive(showMenu);
            difficultyPanel.SetActive(currentState == GameState.DifficultySelect);
            pausePanel.SetActive(currentState == GameState.Paused);

            bool gameplayVisible = currentState == GameState.Playing || currentState == GameState.Paused;
            guiGroup.SetActive(true);
            renderGroup.SetActive(true);
            effectsGroup.SetActive(gameplayVisible || currentState == GameState.Victory);

            if (!introRunning &&
                (currentState == GameState.MainMenu ||
                 currentState == GameState.DifficultySelect ||
                 currentState == GameState.Paused))
            {
                MenuNavigator?.SelectCurrentMenu();
            }

            if (Player.current?.body)
                Player.current.body.enabled = currentState == GameState.Playing;
        }


        public void OnNewGame()
        {
            difficultyFromMainMenu = true;
            SetState(GameState.DifficultySelect);
        }

        public void OnPauseNewGame()
        {
            difficultyFromMainMenu = false;
            SetState(GameState.DifficultySelect);
        }

        public void OnQuitGame()
        {
            if (!quitConfirmActive)
            {
                ShowQuitConfirm();
                return;
            }

            DoQuitToArcade();
        }

        private void ShowQuitConfirm()
        {
            quitConfirmActive = true;

            if (quitConfirmImage)
            {
                if (quitConfirmSprites != null && quitConfirmSprites.Length > 0)
                {
                    int idx = Random.Range(0, quitConfirmSprites.Length);
                    quitConfirmImage.sprite = quitConfirmSprites[idx];
                }

                quitConfirmImage.gameObject.SetActive(true);
            }
        }

        private void DoQuitToArcade()
        {
            StopRepeatingGunfire();

            inPostVictoryFlow = false;
            pausedFromVictory = false;

            if (_exitHook == null)
                _exitHook = TarkovDoomExitHook.Instance ?? FindObjectOfType<TarkovDoomExitHook>();

            if (_exitHook != null)
            {
                _exitHook.ExitToArcade();
            }
            else
            {
                Debug.LogWarning("[GameStateManager] No TarkovDoomExitHook found, cannot exit to arcade.");
            }
        }


        public void StartDifficulty(int diffIndex)
        {
            StartCoroutine(StartDifficultyWithTransition(diffIndex));
        }

        private IEnumerator StartDifficultyWithTransition(int diffIndex)
        {
            isTransitioning = true;

            RectTransform target = panelRect;
            if (target == null)
            {
                Debug.LogError("panelRect is null!");
                isTransitioning = false;
                SetState(GameState.Playing);
                yield break;
            }

            Texture capturedUITexture = mainMenuBackground.CaptureUITexture(target);
            if (capturedUITexture == null)
            {
                Debug.LogError("Failed to capture UI texture!");
                isTransitioning = false;
                SetState(GameState.Playing);
                yield break;
            }

            difficulty = diffIndex + 1;
            Debug.Log($" Starting difficulty index={diffIndex}, difficulty={difficulty}");
            inPostVictoryFlow = false;
            pausedFromVictory = false;
            SetState(GameState.Playing);

            yield return StartCoroutine(mainMenuBackground.MeltStrips(target, false, () =>
            {
                isTransitioning = false;
            }, capturedUITexture));
        }


        void OnVictory()
        {
            if (currentState == GameState.Playing)
            {
                inPostVictoryFlow = true;
                pausedFromVictory = false;

                UpdateVictoryStats();
                SetState(GameState.Victory);

                if (victoryRoutine != null)
                    StopCoroutine(victoryRoutine);
                victoryRoutine = StartCoroutine(VictorySequenceRoutine());
            }
        }

        IEnumerator VictorySequenceRoutine()
        {
            yield return new WaitForSeconds(2f);

            StartRepeatingGunfire();
            yield return StartCoroutine(CountUpPercent(killsText, targetKillPercent, 1.0f));
            StopRepeatingGunfire();
            PlayVictoryExplosion();

            yield return new WaitForSeconds(1f);

            StartRepeatingGunfire();
            yield return StartCoroutine(CountUpPercent(itemsText, targetItemPercent, 1.0f));
            StopRepeatingGunfire();
            PlayVictoryExplosion();

            yield return new WaitForSeconds(1f);

            StartRepeatingGunfire();
            yield return StartCoroutine(CountUpPercent(secretsText, targetSecretPercent, 1.0f));
            StopRepeatingGunfire();
            PlayVictoryExplosion();

            yield return new WaitForSeconds(1f);

            StartRepeatingGunfire();
            yield return StartCoroutine(CountUpTimeAndPar(targetTimeText, targetParText, 1.0f));
            StopRepeatingGunfire();
            PlayVictoryExplosion();

            victoryRoutine = null;
        }
        
        IEnumerator CountUpTimeAndPar(string finalTime, string finalParLabel, float duration)
        {
            int finalMin = 0, finalSec = 0;
            if (!string.IsNullOrEmpty(finalTime))
            {
                var parts = finalTime.Split(':');
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0], out finalMin);
                    int.TryParse(parts[1], out finalSec);
                }
            }

            int parMin = 0, parSec = 0;
            if (!string.IsNullOrEmpty(finalParLabel))
            {
                var label = finalParLabel.Replace("PAR", "").Trim();
                if (label.StartsWith(":"))
                {
                    int.TryParse(label.Substring(1), out parSec);
                }
                else
                {
                    var p = label.Split(':');
                    if (p.Length == 2)
                    {
                        int.TryParse(p[0], out parMin);
                        int.TryParse(p[1], out parSec);
                    }
                }
            }

            int totalTimeSecs = finalMin * 60 + finalSec;
            int totalParSecs = parMin * 60 + parSec;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float r = Mathf.Clamp01(t / duration);

                int curTime = Mathf.RoundToInt(Mathf.Lerp(0, totalTimeSecs, r));
                int curPar = Mathf.RoundToInt(Mathf.Lerp(0, totalParSecs, r));

                int tm = curTime / 60;
                int ts = curTime % 60;
                timeText.GenerateText($"{tm:00}:{ts:00}");

                int pm = curPar / 60;
                int ps = curPar % 60;

                if (pm == 0)
                    parText.GenerateText($"PAR :{ps:00}");
                else
                    parText.GenerateText($"PAR {pm}:{ps:00}");

                yield return null;
            }

            timeText.GenerateText(finalTime);
            parText.GenerateText(finalParLabel);
        }

        IEnumerator CountUpPercent(SpriteText text, int target, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float r = Mathf.Clamp01(t / duration);
                int val = Mathf.RoundToInt(Mathf.Lerp(0, target, r));
                text.GenerateText($"{val}%");
                yield return null;
            }

            text.GenerateText($"{target}%");
        }

        void CheckPlayerDeath()
        {
            if (currentState != GameState.Playing) return;
            if (Player.current == null) return;

            if (Player.current.health <= 0)
            {
                GlobalEventController.PlayerDeath();
            }
        }

        void UpdateVictoryStats()
        {
            var p = Player.current;
            if (p == null) return;

            targetKillPercent = (p.maxKills > 0) ? Mathf.RoundToInt(p.kills / (float)p.maxKills * 100f) : 0;
            targetItemPercent = (p.maxItems > 0) ? Mathf.RoundToInt(p.itemsPickedUp / (float)p.maxItems * 100f) : 0;
            targetSecretPercent =
                (p.maxSecrets > 0) ? Mathf.RoundToInt(p.secretsFound / (float)p.maxSecrets * 100f) : 0;

            float elapsed = p.timer;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            targetTimeText = $"{minutes:00}:{seconds:00}";

            int idx = Mathf.Clamp(difficulty - 1, 0, parLabels.Length - 1);
            targetParText = parLabels[idx];

            killsText.GenerateText("");
            itemsText.GenerateText("");
            secretsText.GenerateText("");
            timeText.GenerateText("");
            parText.GenerateText("");
        }
        
        private void PlayVictoryExplosion()
        {
            if (ArcadeAudioBus.Instance != null && victoryExplosionClip != null)
                ArcadeAudioBus.Instance.PlayAtCabinet(victoryExplosionClip, victoryExplosionVolume);
        }
        
        
        private IEnumerator PlayRepeatingGunfire(AudioClip clip, float interval, float volumeMul)
        {
            while (true)
            {
                if (ArcadeAudioBus.Instance != null && clip != null)
                    ArcadeAudioBus.Instance.PlayAtCabinet(clip, volumeMul);

                yield return new WaitForSeconds(interval);
            }
        }

        private void StartRepeatingGunfire()
        {
            StopRepeatingGunfire();

            if (victoryCountLoopClip != null)
                victoryLoopRoutine = StartCoroutine(PlayRepeatingGunfire(victoryCountLoopClip, 0.08f, victoryCountLoopVolume));
        }

        private void StopRepeatingGunfire()
        {
            if (victoryLoopRoutine != null)
            {
                StopCoroutine(victoryLoopRoutine);
                victoryLoopRoutine = null;
            }
        }
    }
}