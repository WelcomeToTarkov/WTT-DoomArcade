using EFT;
using EFT.Quests;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Patches;

internal class OnGameStarted : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPostfix]
    private static void PatchPostfix(GameWorld __instance)
    {
        try
        {
            var questBook = DoomArcadeClient.DoomArcadeClient.Player.AbstractQuestControllerClass.QuestBookClass;

            foreach (var quest in questBook)
            {
                if (quest.Id != "69ac519839d15e3196551ec7")
                    continue;

                LogHelper.LogInfo($"[Salvage] Quest {quest.Id} status={quest.QuestStatus}");

                var conds = quest.GetConditions<ConditionZone>(EQuestStatus.AvailableForFinish).ToList();
                LogHelper.LogInfo($"[Salvage] GetConditions<ConditionZone> count={conds.Count}");

                foreach (var cz in conds)
                {
                    LogHelper.LogInfo(
                        $"[Salvage] cond {cz.id} type={cz.GetType().Name} zoneId={cz.zoneId} visible={quest.CheckVisibilityStatus(cz)} completed={quest.CompletedConditions.Contains(cz.id)}");
                }
            }

        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }
}