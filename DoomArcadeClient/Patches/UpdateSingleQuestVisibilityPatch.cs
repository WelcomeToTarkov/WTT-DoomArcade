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
    private const int WallFinalLevel = 6;
    private const string DoomQuestChainStart = "69ac519839d15e3196551ec7";

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
            DoomArcadeClient.Log?.LogWarning("Profile is null");
            return;
        }

        if (!HasWallQuestUnlocked(profile))
        {
            var hideout = profile.Hideout;
            if (hideout?.Areas == null || hideout.Areas.Length == 0)
            {
                DoomArcadeClient.Log?.LogWarning("Hideout or Areas array is null/empty");
                return;
            }

            var wallArea = hideout.Areas.FirstOrDefault(a => a != null && a.Type == 22);
            if (wallArea != null && wallArea.Level == WallFinalLevel)
            {
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

        return match.Status >= EQuestStatus.AvailableForStart;
    }

    private static void RequestDoomQuestStart()
    {
        try
        {

            var response = WebRequestUtils.Post<string>("/WTT/WTTDoomQuestStart", DoomQuestChainStart);

        }
        catch (Exception ex)
        {
            DoomArcadeClient.Log?.LogError(
                $"Exception while calling /WTT/WTTDoomQuestStart: {ex}");
        }
    }
}

