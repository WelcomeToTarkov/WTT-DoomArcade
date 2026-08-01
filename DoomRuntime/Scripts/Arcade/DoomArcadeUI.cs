using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DoomArcade.Scripts.DoomClone;
using DoomArcade.Scripts.ManagedDoom;
using DoomArcade.Scripts.Tarkov;
using ManagedDoom;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DoomArcade.Scripts.Arcade
{
    public class DoomArcadeUI : MonoBehaviour
    {
        [Header("Refs")]
        public WadLibrary wadLibrary;
        public DoomCanvasComponent doomCanvasPrefab;
        public Transform doomParent;
        public RectTransform menuRoot;
        public GameObject menuItemPrefab;
        public RawImage backgroundImage;
        public RawImage logoImage;
        public RawImage blackScreen;
        public RawImage previewImage;
        public TMP_Text descriptionText;
        [SerializeField] private InteractableArcade arcade;
        [SerializeField]
        public Canvas  arcadeUICanvas;
        [Header("Special Entries")]
        public string tarkovEntryName = "TARKOV";
        public string tarkovKey = "__TARKOV__";

        [Serializable]
        public class GameMeta
        {
            public string wadFileName;
            public string displayName;
            public string previewImage;
            [TextArea(2,4)]
            public string description;

            public string iwadFileName;
            public string[] pwadFileNames;
            public string[] dehFileNames;

            [NonSerialized]
            public Texture2D PreviewTexture;
        }



        [Header("External Metadata")]
        [SerializeField] private bool autoLoadMetadataAtStart = true;

        private List<GameMeta> _gameMeta = new List<GameMeta>();
        private Dictionary<string, GameMeta> _metaByFile;

        [Header("Timings")]
        public float blackDuration = 1.0f;
        public float logoDuration  = 4.0f;

        private DoomCanvasComponent _currentDoom;
        private bool _arcadeVisible;
        private const int ItemsPerPage = 6;
        private int _currentPage;
        private int _selectedIndex;
        private Coroutine _bootRoutine;

        private readonly List<string> _wadPaths = new();
        private readonly List<string> _keys = new(); 
        private readonly List<MenuItem> _menuItems = new();
        [SerializeField]public Object soundFont;
        [Header("Tarkov Game")]
        public DoomGameController tarkovGameController;
        private class MenuItem
        {
            public TMP_Text Label;
            public GameObject Arrow;
        }

        void Start()
        {
            ShowArcade(false);

            if (previewImage)
            {
                previewImage.enabled = false;
                previewImage.texture = null;
            }

            if (descriptionText)
                descriptionText.text = "";

            if (autoLoadMetadataAtStart)
                LoadMetadataFromJson();
        }

        public void LoadMetadataFromJson()
        {
            var baseDir = GetBaseDir();
            var metadataPath = Path.Combine(baseDir, "GameMetadata");
            string jsonPath = Path.Combine(metadataPath, "GameMetadata.json");

            Debug.Log($"[ArcadeUI] BaseDir={baseDir}");
            Debug.Log($"[ArcadeUI] MetadataPath={metadataPath}");
            Debug.Log($"[ArcadeUI] JsonPath={jsonPath}");

            if (!Directory.Exists(metadataPath))
            {
                Debug.LogWarning($"[ArcadeUI] Metadata directory not found: {metadataPath}");
                return;
            }

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[ArcadeUI] Metadata JSON not found: {jsonPath}");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                List<GameMeta> loadedMeta = JsonUtility.FromJson<GameMetaWrapper>(jsonContent)?.gameMetas ?? new List<GameMeta>();

                string imagesDir = Path.Combine(metadataPath, "Images");
                Debug.Log($"[ArcadeUI] Images dir: {imagesDir}");

                foreach (var meta in loadedMeta)
                {
                    if (string.IsNullOrEmpty(meta.previewImage))
                    {
                        Debug.Log($"[ArcadeUI] {meta.wadFileName} has no previewImage set.");
                        continue;
                    }

                    var fullPath = Path.Combine(imagesDir, meta.previewImage);
                    Debug.Log($"[ArcadeUI] {meta.wadFileName} -> preview '{meta.previewImage}', fullPath '{fullPath}'");

                    if (File.Exists(fullPath))
                    {
                        meta.PreviewTexture = LoadTextureFromFile(fullPath);
                        Debug.Log($"[ArcadeUI] LoadTextureFromFile result for {meta.wadFileName}: " +
                                  $"{(meta.PreviewTexture == null ? "NULL" : "OK " + meta.PreviewTexture.width + "x" + meta.PreviewTexture.height)}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ArcadeUI] Image file missing for {meta.wadFileName}: {fullPath}");
                    }
                }

                _gameMeta = loadedMeta;
                BuildMetaDictionary();

                Debug.Log($"[ArcadeUI] Loaded {_gameMeta.Count} metadata entries from {jsonPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ArcadeUI] Failed to load metadata: {e.Message}");
            }
        }

        private Texture2D LoadTextureFromFile(string imagePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(imagePath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return tex;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ArcadeUI] Failed to load preview image {imagePath}: {e.Message}");
            }
            return null;
        }

        [Serializable]
        private class GameMetaWrapper
        {
            public List<GameMeta> gameMetas;
        }

        private void BuildMetaDictionary()
        {
            _metaByFile = new Dictionary<string, GameMeta>(StringComparer.OrdinalIgnoreCase);
            foreach (var meta in _gameMeta)
            {
                if (meta != null && !string.IsNullOrEmpty(meta.wadFileName))
                {
                    _metaByFile[meta.wadFileName.ToUpperInvariant()] = meta;
                }
            }
        }

        public void PowerOnFromGame()
        {
            Debug.Log("[ArcadeUI] PowerOnFromGame called");
            PowerOn();
        }

        public void PowerOffFromGame()
        {
            Debug.Log("[ArcadeUI] PowerOffFromGame called");
            PowerOff();
        }
        
        private static string GetBaseDir()
        {
            if (!string.IsNullOrEmpty(ConfigUtilities.OverrideBaseDir))
                return ConfigUtilities.OverrideBaseDir;

            var asmLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(asmLocation) ?? "";
        }


        void Update()
        {
            if (!_arcadeVisible || _currentDoom)
                return;

            if (Input.GetKeyDown(DoomedInput.MenuUp) || Input.GetKeyDown(DoomedInput.MenuUpAlt))
                MoveSelection(-1);

            if (Input.GetKeyDown(DoomedInput.MenuDown) || Input.GetKeyDown(DoomedInput.MenuDownAlt))
                MoveSelection(1);

            if (Input.GetKeyDown(DoomedInput.MenuConfirm) || Input.GetKeyDown(DoomedInput.MenuConfirm2))
                LaunchSelected();

            if (Input.GetKeyDown(DoomedInput.PauseOrBack))
                PowerOff();
        }

        void PowerOn()
        {
            if (_arcadeVisible) return;
            _arcadeVisible = true;

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas)
                canvas.gameObject.SetActive(true);

            ArcadeMenuMusic.Instance?.PlayMenuMusic();

            if (_bootRoutine != null)
                StopCoroutine(_bootRoutine);

            _bootRoutine = StartCoroutine(BootSequence());
        }


        void PowerOff()
        {
            if (_bootRoutine != null)
                StopCoroutine(_bootRoutine);

            ArcadeMenuMusic.Instance?.StopMenuMusic();
            ClearMenu();
            ShowArcade(false);
            if (arcade != null)
                arcade.OnPowerOff.Invoke();
        }

        IEnumerator BootSequence()
        {
            SetScreenState(black: true, logo: false, background: false, menu: false);
            yield return new WaitForSeconds(blackDuration);

            SetScreenState(black: false, logo: true, background: false, menu: false);
            yield return new WaitForSeconds(logoDuration);

            SetScreenState(black: false, logo: false, background: true, menu: true);
            wadLibrary.Scan();
            BuildMenu();
        }


        void SetScreenState(bool black, bool logo, bool background, bool menu)
        {
            if (blackScreen) blackScreen.enabled = black;
            if (logoImage)   logoImage.enabled   = logo;
            if (backgroundImage) backgroundImage.enabled = background;
            if (menuRoot) menuRoot.gameObject.SetActive(menu);
        }

        public void ShowArcade(bool show)
        {
            _arcadeVisible = show;
        
            if (arcadeUICanvas)
                arcadeUICanvas.gameObject.SetActive(show);

            if (!show)
            {
                SetScreenState(false, false, false, false);

                if (previewImage)
                {
                    previewImage.enabled = false;
                    previewImage.texture = null;
                }

                if (descriptionText)
                    descriptionText.text = "";
            }
        }

        void BuildMenu()
        {
            ClearMenu();

            if (_metaByFile == null)
                BuildMetaDictionary();

            if (!menuRoot || !menuItemPrefab)
            {
                return;
            }

            _wadPaths.Clear();
            _keys.Clear();

            _wadPaths.Add(tarkovEntryName);
            _keys.Add(tarkovKey);

            foreach (var w in wadLibrary.WadPaths)
            {
                _wadPaths.Add(w);
                _keys.Add(Path.GetFileName(w).ToUpperInvariant());
            }

            int rowCount = Mathf.Min(ItemsPerPage, _wadPaths.Count);
            for (int i = 0; i < rowCount; i++)
            {
                var go = Instantiate(menuItemPrefab, menuRoot);
                go.SetActive(true);

                TMP_Text label = go.transform.Find("Label")?.GetComponent<TMP_Text>();
                var arrow = go.transform.Find("Arrow")?.gameObject;

                if (!label)
                {
                    Destroy(go);
                    continue;
                }

                if (arrow != null)
                    arrow.SetActive(false);

                _menuItems.Add(new MenuItem { Label = label, Arrow = arrow });
            }

            _currentPage = 0;
            _selectedIndex = 0;
            RefreshPage();
        }

        void RefreshPage()
        {
            if (_menuItems.Count == 0)
                return;

            int total = _wadPaths.Count;
            int start = _currentPage * ItemsPerPage;

            for (int i = 0; i < _menuItems.Count; i++)
            {
                int globalIndex = start + i;
                var item = _menuItems[i];

                bool hasItem = globalIndex < total;
                if (!hasItem)
                {
                    item.Label.gameObject.SetActive(false);
                    if (item.Arrow)
                        item.Arrow.SetActive(false);
                    continue;
                }

                item.Label.gameObject.SetActive(true);

                string key = _keys[globalIndex];
                string labelText;

                GameMeta meta = null;

                if (key == tarkovKey)
                {
                    _metaByFile?.TryGetValue("TARKOV.WAD", out meta);
                }
                else
                {
                    _metaByFile?.TryGetValue(key, out meta);
                }

                if (meta != null && !string.IsNullOrEmpty(meta.displayName))
                {
                    labelText = meta.displayName;
                }
                else if (key == tarkovKey)
                {
                    labelText = tarkovEntryName;
                }
                else
                {
                    string path = _wadPaths[globalIndex];
                    labelText = Path.GetFileNameWithoutExtension(path);
                }

                item.Label.text = labelText;

                bool selected = (globalIndex == _selectedIndex);
                if (item.Arrow)
                    item.Arrow.SetActive(selected);

                item.Label.color = selected ? Color.yellow : Color.white;
            }
            UpdatePreviewAndDescription();
        }

        void ClearMenu()
        {
            if (menuRoot)
            {
                foreach (Transform child in menuRoot)
                    Destroy(child.gameObject);
            }
            _menuItems.Clear();
            _wadPaths.Clear();
        }

        void MoveSelection(int delta)
        {
            if (_wadPaths.Count == 0)
                return;

            int total = _wadPaths.Count;
            _selectedIndex = (_selectedIndex + delta + total) % total;
            _currentPage = _selectedIndex / ItemsPerPage;
            RefreshPage();
        }
    
        void UpdatePreviewAndDescription()
        {
            if (!previewImage && descriptionText == null)
                return;

            if (_selectedIndex < 0 || _selectedIndex >= _wadPaths.Count)
                return;

            string key = _keys[_selectedIndex];
            
            
            GameMeta meta = null;

            if (key == tarkovKey)
            {
                _metaByFile?.TryGetValue("TARKOV.WAD", out meta);
            }
            else
            {
                _metaByFile?.TryGetValue(key, out meta);
            }

            if (meta != null)
            {
                if (previewImage)
                {
                    previewImage.texture = meta.PreviewTexture;
                    previewImage.enabled = (meta.PreviewTexture != null);
                }

                if (descriptionText)
                    descriptionText.text = meta.description ?? "";
            }
            else
            {
                if (previewImage)
                {
                    previewImage.texture = null;
                    previewImage.enabled = false;
                }

                if (descriptionText)
                    descriptionText.text = "";
            }
        }

        void LaunchSelected()
        {
            if (_wadPaths.Count == 0)
                return;

            string key = _keys[_selectedIndex];

            if (key == tarkovKey)
            {
                LaunchTarkov();
            }
            else
            {
                var wadPath = _wadPaths[_selectedIndex];
                LaunchDoom(wadPath);
            }
        }

        void LaunchTarkov()
        {
            if (!tarkovGameController)
            {
                return;
            }

            ShowArcade(false);
            ArcadeMenuMusic.Instance?.StopMenuMusic();

            tarkovGameController.OnSessionExit -= HandleTarkovExit;
            tarkovGameController.OnSessionExit += HandleTarkovExit;

            tarkovGameController.Boot();
        }

        void HandleTarkovExit()
        {
            ShowArcade(true);
            wadLibrary.Scan();
            BuildMenu();
            SetScreenState(black: false, logo: false, background: true, menu: true);
            ArcadeMenuMusic.Instance?.PlayMenuMusic();
        }
        void LaunchDoom(string selectedWadPath)
        {
            ShowArcade(false);
            ArcadeMenuMusic.Instance?.StopMenuMusic();

            if (_currentDoom)
                Destroy(_currentDoom.gameObject);

            var doomGo = Instantiate(doomCanvasPrefab, doomParent);
            _currentDoom = doomGo;

            string key = Path.GetFileName(selectedWadPath).ToUpperInvariant();
            GameMeta meta = null;
            _metaByFile?.TryGetValue(key, out meta);

            var wadList = new List<string>();
            var dehList = new List<string>();

            var baseDir = Path.GetDirectoryName(selectedWadPath) ?? "";

            if (meta != null && !string.IsNullOrEmpty(meta.iwadFileName))
            {
                var iwadPath = Path.Combine(baseDir, meta.iwadFileName);
                wadList.Add(iwadPath);

                if (meta.pwadFileNames != null)
                {
                    foreach (var pwadName in meta.pwadFileNames)
                    {
                        var p = Path.Combine(baseDir, pwadName);
                        if (File.Exists(p))
                            wadList.Add(p);
                    }
                }

                if (meta.dehFileNames != null)
                {
                    foreach (var dehName in meta.dehFileNames)
                    {
                        var d = Path.Combine(baseDir, dehName);
                        if (File.Exists(d))
                            dehList.Add(d);
                    }
                }
            }
            else
            {
                wadList.Add(selectedWadPath);
            }

            doomGo.WadPaths = wadList.ToArray();
            doomGo.DehPaths = dehList.ToArray();
            doomGo.SoundFont = soundFont;

            var runtime = doomGo.GetComponent<RuntimeDoom>();
            if (runtime)
                runtime.arcadeUI = this;

            doomGo.StartDoom();
        }


        public void OnDoomQuit()
        {
            if (_currentDoom)
            {
                Destroy(_currentDoom.gameObject);
                _currentDoom = null;
            }

            ShowArcade(true);
            wadLibrary.Scan();
            BuildMenu();
            SetScreenState(black: false, logo: false, background: true, menu: true);
            ArcadeMenuMusic.Instance?.PlayMenuMusic();
        }
    }
}
