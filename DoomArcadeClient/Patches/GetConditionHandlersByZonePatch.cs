using DoomArcadeClient;
using DoomArcadeClient.Components;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DoomArcadeClient.Patches;

internal class GetConditionHandlersByZonePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(typeof(QuestBookClass),
            x => x.Name == nameof(QuestBookClass.GetConditionHandlersByZone));
    }

    [PatchPostfix]
    public static void PatchPostfix<T>(
        string zoneId,
        QuestBookClass __instance,
        ref IEnumerable<ConditionProgressChecker> __result)
        where T : ConditionZone
    {
        if (__result != null && __result.Any())
            return;

        var list = new List<ConditionProgressChecker>();

        foreach (var quest in __instance)
        {
            if (quest.QuestStatus != EQuestStatus.Started &&
                quest.QuestStatus != EQuestStatus.AvailableForFinish)
                continue;

            foreach (var kvp in quest.ProgressCheckers)
            {
                if (kvp.Key is not T zoneCond)
                    continue;

                if (zoneCond.zoneId != zoneId)
                    continue;

                if (quest.CompletedConditions.Contains(zoneCond.id))
                    continue;

                if (!quest.CheckVisibilityStatus(zoneCond))
                    continue;

                list.Add(kvp.Value);
            }
        }

        __result = list;
    }
}
