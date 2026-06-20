using DoomArcadeClient.Components;
using DoomArcadeClient.Utils;
using EFT;
using EFT.Quests;
using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WTTClientCommonLib.Services;

namespace DoomArcadeClient.Patches;

public class HideoutGameSmethod6Patch : ModulePatch
{
    private const string DoomQuestChainEnd = "69dee8720d337cf6aa05b917";
    private static bool _spawnedCabinet;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutGame).GetMethod(
            "smethod_6",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
        );
    }

    [PatchPostfix]
    private static void Postfix(HideoutGame __instance, ref Profile profile)
    {
        if (profile == null)
            return;

        if (!HasDoomQuestChainComplete(profile))
            return;

        if (_spawnedCabinet)
            return;

        if (UnityEngine.Object.FindObjectsOfType<InteractableDoomArcade>().Any())
        {
            _spawnedCabinet = true;
            return;
        }

        SpawnArcade(
            new Vector3(13f, 0f, 2f),
            Quaternion.Euler(180f, -90f, 0f)
        );

        _spawnedCabinet = true;
    }

    private static bool HasDoomQuestChainComplete(Profile profile)
    {
        if (profile?.QuestsData == null)
            return false;

        var match = profile.QuestsData.FirstOrDefault(q => q.Id == DoomQuestChainEnd);
        if (match == null)
            return false;

        DoomArcadeClient.Log?.LogInfo(
            $"[HideoutGameSmethod6Patch] Found quest {DoomQuestChainEnd} with Status={match.Status}");

        return match.Status >= EQuestStatus.Success;
    }

    public static void SpawnArcade(Vector3 spawnPos, Quaternion spawnRot)
    {
        AssetLoader assetLoader = WTTClientCommonLib.WTTClientCommonLib.Instance.AssetLoader;

        if (assetLoader == null)
        {
            ConsoleScreen.LogError("AssetLoader not initialized.");
            return;
        }

        const string BUNDLE_NAME = "doomarcade.bundle";
        const string PREFAB_PATH = "Assets/DoomArcade/Cabinet Prefab/DoomCabinet.prefab";

        try
        {
            ConsoleScreen.Log("Spawning Cabinet...");

            var prefab = assetLoader.LoadPrefabFromBundle(BUNDLE_NAME, PREFAB_PATH);
            if (prefab == null)
            {
                ConsoleScreen.LogError($"Failed to load '{PREFAB_PATH}' from '{BUNDLE_NAME}'");
                return;
            }

            var arcadeObj = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRot);

            if (!arcadeObj.TryGetComponent(out InteractableDoomArcade doomArcade))
            {
                doomArcade = arcadeObj.AddComponent<InteractableDoomArcade>();
            }

            doomArcade.Init();
        }
        catch (Exception ex)
        {
            ConsoleScreen.LogError($"Spawn failed: {ex}");
        }
    }
}