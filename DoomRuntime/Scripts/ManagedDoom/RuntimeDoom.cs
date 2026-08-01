using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DoomArcade.Scripts.Arcade;
using DoomArcade.Scripts.DoomClone;
using DoomArcade.Scripts.ManagedDoom;
using ManagedDoom;
using ManagedDoom.Unity;
using UnityEngine;
using UnityEngine.UI;
using Sprite = UnityEngine.Sprite;

public class RuntimeDoom : MonoBehaviour
{
    [SerializeField] private int fps = 30;

    [SerializeField] public Image _image;

    [SerializeField] public DoomCanvasComponent doomedComponent;

    private readonly List<KeyCode> keysPressed = new(8);
    private UnityDoom doom;
    private Texture2D screen;

    private KeyCode[] _allKeycodes;
    private Sprite _sprite;

    private Coroutine _loop;
    public DoomArcadeUI arcadeUI;

    private void Start()
    {
        _allKeycodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();

        try
        {
            CreateDoom();
        }
        catch (Exception e)
        {
            return;
        }

        _sprite = Sprite.Create(screen, new Rect(0, 0, screen.width, screen.height), new Vector2(0.5f, 0.5f));
        _image.sprite = _sprite;

        var rt = _image.rectTransform;
        rt.sizeDelta = new Vector2(200, 320);

        _loop = StartCoroutine(UpdateFrame());
    }


    private void OnDestroy()
    {
        if (_loop != null)
            StopCoroutine(_loop);

        if (doom != null)
        {
            doom.OnClose();
            doom.Dispose();
            doom = null;
        }

        _sprite = null;
        screen = null;
        keysPressed.Clear();
    }


    private void Update()
    {
        if (doom == null)
            return;

        FetchInput();
    }


    private IEnumerator UpdateFrame()
    {
        var wait = new WaitForSeconds(1f / fps);
        while (true)
        {
            yield return wait;

            doom.UpdateKeys(keysPressed);
            var result = doom.OnUpdate();
            doom.OnRender();

            if (result == UpdateResult.Completed)
            {

                if (arcadeUI != null)
                    arcadeUI.OnDoomQuit();

                Destroy(gameObject);
                yield break;
            }
        }
    }

    private string ResolveSoundFontPath()
    {
        if (!string.IsNullOrEmpty(doomedComponent.SfPath))
            return doomedComponent.SfPath;

        string baseDir;

        if (!string.IsNullOrEmpty(ConfigUtilities.OverrideBaseDir))
        {
            baseDir = ConfigUtilities.OverrideBaseDir;
        }
        else
        {
            var asmLocation = Assembly.GetExecutingAssembly().Location;
            baseDir = Path.GetDirectoryName(asmLocation) ?? "";
        }

        var sfPath = Path.Combine(baseDir, "SoundFonts", "RLNDGM.SF2");
        return sfPath;
    }
    private void CreateDoom()
    {
        var wadPaths = doomedComponent.WadPaths;
        if (wadPaths == null || wadPaths.Length == 0)
            wadPaths = new[] { doomedComponent.WadPath };

        var dehPaths = doomedComponent.DehPaths;
        var sfPath = ResolveSoundFontPath();

        DoomStaticReset.RestoreVanilla();

        var cli = new List<string>();

        cli.Add("-iwad");
        cli.Add(wadPaths[0]);

        if (wadPaths.Length > 1)
        {
            cli.Add("-file");
            for (int i = 1; i < wadPaths.Length; i++)
                cli.Add(wadPaths[i]);
        }

        if (dehPaths != null && dehPaths.Length > 0)
        {
            cli.Add("-deh");
            foreach (var d in dehPaths)
                cli.Add(d);
        }
        else
        {
            cli.Add("-nodeh");
        }

        var args = new CommandLineArgs(cli.ToArray());

        var cfgPath = ManagedDoom.ConfigUtilities.GetConfigPath();
        var config = File.Exists(cfgPath) ? new Config(cfgPath) : new Config();
        config.audio_soundfont = sfPath;
        DoomedInputInitializer.ApplyConfigToDoomedInput(config);

        doom = new UnityDoom(args, sfPath);
        doom.OnLoad();
        screen = doom.GetVideoTexture();
    }

    private void FrameTick()
    {
        doom.UpdateKeys(keysPressed);
        doom.OnUpdate();
        doom.OnRender();
    }

    private void FetchInput()
    {
        for (int i = 0; i < _allKeycodes.Length; i++)
        {
            var keyCode = _allKeycodes[i];

            if (Input.GetMouseButton(0))
            {
                if (!keysPressed.Contains(KeyCode.F))
                    keysPressed.Add(KeyCode.F);
            }
            else
            {
                keysPressed.Remove(KeyCode.F);
            }


            if (Input.GetKeyDown(keyCode))
            {
                if (!keysPressed.Contains(keyCode))
                {
                    keysPressed.Add(keyCode);
                }

                doom.KeyDown(keyCode);
            }

            if (Input.GetKeyUp(keyCode))
            {
                if (keysPressed.Contains(keyCode))
                {
                    keysPressed.Remove(keyCode);
                }

                doom.KeyUp(keyCode);
            }
        }
    }
}