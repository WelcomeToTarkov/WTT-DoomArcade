#if !UNITY_EDITOR
using System.IO;
using BepInEx.Logging;
using DoomArcadeClient.Components;
using EFT.Console.Core;
using EFT.UI;
using UnityEngine;
using WTTClientCommonLib.Services;

namespace DoomArcadeClient.Utils;

public class CommandProcessor
{
    public static AssetLoader AssetLoader { get; private set; }
    
    public void RegisterCommandProcessor()
    {
        AssetLoader assetLoader = WTTClientCommonLib.WTTClientCommonLib.Instance.AssetLoader;
        AssetLoader = assetLoader;
        InitializeBundles();
        ConsoleScreen.Processor.RegisterCommand("clear", delegate
        {
            MonoBehaviourSingleton<PreloaderUI>.Instance.Console.Clear();
        });
        
        ConsoleScreen.Processor.RegisterCommandGroup<AdvancedConsoleCommands>();
    }

    public void InitializeBundles()
    {
        if (AssetLoader != null)
            AssetLoader.InitializeBundlesAsync().ContinueWith(_ => { });
    }
}

public class AdvancedConsoleCommands
{
    [ConsoleCommand("spawnarcade", "Spawn arcade cabinet in front of player")]
    public static void SpawnArcade()
    {
        if (DoomArcadeClient.Player == null)
        {
            ConsoleScreen.LogError("No player found. Must be in raid.");
            return;
        }

        var playerPos = DoomArcadeClient.Player.Position;
        var forward = DoomArcadeClient.Player.LookDirection;
        var spawnPos = playerPos + forward * 5f;
        var spawnRot = Quaternion.Euler(180f, -90f, 0f);

        SpawnArcade(spawnPos, spawnRot);
    }

    public static void SpawnArcade(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (DoomArcadeClient.Player == null)
        {
            ConsoleScreen.LogError("No player found. Must be in raid.");
            return;
        }

        if (CommandProcessor.AssetLoader == null)
        {
            ConsoleScreen.LogError("AssetLoader not initialized.");
            return;
        }

        const string BUNDLE_NAME = "doomarcade.bundle";
        const string PREFAB_PATH = "Assets/DoomArcade/Cabinet Prefab/DoomCabinet.prefab";

        try
        {
            ConsoleScreen.Log("Spawning Cabinet...");

            var prefab = CommandProcessor.AssetLoader.LoadPrefabFromBundle(BUNDLE_NAME, PREFAB_PATH);
            if (prefab == null)
            {
                ConsoleScreen.LogError($"Failed to load '{PREFAB_PATH}' from '{BUNDLE_NAME}'");
                return;
            }

            var arcadeObj = Object.Instantiate(prefab, spawnPos, spawnRot);

            if (!arcadeObj.TryGetComponent(out InteractableDoomArcade doomArcade))
            {
                doomArcade = arcadeObj.AddComponent<InteractableDoomArcade>();
            }

            doomArcade.Init();
        }
        catch (System.Exception ex)
        {
            ConsoleScreen.LogError($"Spawn failed: {ex.Message}");
        }
    }
}
#endif
