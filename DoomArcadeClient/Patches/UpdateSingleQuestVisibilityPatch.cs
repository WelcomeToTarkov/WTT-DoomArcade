using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DoomArcadeClient.Utils;
using EFT;
using EFT.Quests;
using EFT.UI;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace DoomArcadeClient.Patches;

public class UpdateSingleQuestVisibilityPatch : ModulePatch
{
    private const EAreaType WallAreaType = EAreaType.EmergencyWall;
    private const int WallFinalLevel = 6;
    private const string DoomQuestChainStart = "69ac519839d15e3196551ec7";
    private static bool requestedQuestStart = false;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(QuestsListView).GetMethod(
            "UpdateSingleQuestVisibility",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
    }
    [PatchPostfix]
    private static void Prefix(QuestsListView __instance, QuestListItem questView)
    {
        if (__instance == null || questView == null)
            return;

        if (questView.Quest == null || questView.Quest.Id != DoomQuestChainStart)
            return;

        if (questView.Quest.QuestStatus != EQuestStatus.Locked)
            return;

        var clientApp = ClientAppUtils.GetClientApp();
        var backendSession = clientApp?.GetClientBackEndSession();
        var profile = backendSession?.Profile;
        if (profile == null)
        {
            DoomArcadeClient.Log?.LogWarning("[DefectiveWallQuestUnlockPatch] Profile is null");
            return;
        }

        DoomArcadeClient.Log?.LogInfo(
            $"[DefectiveWallQuestUnlockPatch] Profile: {profile.Nickname} ({profile.Id})");

        if (!HasWallQuestUnlocked(profile))
        {
            var hideout = profile.Hideout;
            if (hideout?.Areas == null || hideout.Areas.Length == 0)
            {
                DoomArcadeClient.Log?.LogWarning("[DefectiveWallQuestUnlockPatch] Hideout or Areas array is null/empty");
                return;
            }

            var wallArea = hideout.Areas.FirstOrDefault(a => a != null && a.Type == 22);
            if (wallArea != null && wallArea.Level == WallFinalLevel)
            {
                DoomArcadeClient.Log?.LogInfo(
                    "[DefectiveWallQuestUnlockPatch] Wall is fully deconstructed, proceeding to unlock quest");

                RequestDoomQuestStart();
                questView.Quest.QuestStatus = EQuestStatus.AvailableForStart;
                questView.gameObject.SetActive(true);
                questView.enabled = true;
                questView.OnQuestStatusChanged(questView.Quest, false);
            }
        }
    }

    private static bool HasWallQuestUnlocked(Profile profile)
    {
        if (profile?.QuestsData == null)
            return false;

        var match = profile.QuestsData.FirstOrDefault(q => q.Id == DoomQuestChainStart);
        if (match == null)
            return false;

        DoomArcadeClient.Log?.LogInfo(
            $"[DefectiveWallQuestUnlockPatch] Found quest {DoomQuestChainStart} with Status={match.Status}");

        return match.Status >= EQuestStatus.AvailableForStart;
    }

    private static void RequestDoomQuestStart()
    {
        try
        {
            DoomArcadeClient.Log?.LogInfo(
                $"[DefectiveWallQuestUnlockPatch] Requesting server to add quest '{DoomQuestChainStart}'");

            var response = WebRequestUtils.Post<string>("/WTT/WTTDoomQuestStart", DoomQuestChainStart);

            if (response != null)
            {
                DoomArcadeClient.Log?.LogInfo(
                    $"[DefectiveWallQuestUnlockPatch] Server response from /WTT/WTTDoomQuestStart: {response}");
            }
            else
            {
                DoomArcadeClient.Log?.LogWarning(
                    "[DefectiveWallQuestUnlockPatch] /WTT/WTTDoomQuestStart returned null response");
            }
        }
        catch (Exception ex)
        {
            DoomArcadeClient.Log?.LogError(
                $"[DefectiveWallQuestUnlockPatch] Exception while calling /WTT/WTTDoomQuestStart: {ex}");
        }
    }
}

