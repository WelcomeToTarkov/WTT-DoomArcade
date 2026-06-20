using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace WTTExampleMod.Routers;

public record DoomQuestStartData : IRequestData
{
    [JsonPropertyName("Data")]
    public string Data { get; set; } = string.Empty;
}


[Injectable]
public class DoomQuestRouter(
    DoomQuestCallbacks callbacks,
    JsonUtil jsonUtil
) : StaticRouter(jsonUtil, [
    new RouteAction<DoomQuestStartData>("/WTT/WTTDoomQuestStart",
        async (_, info, sessionId, _) =>
        {
            return await callbacks.StartQuest(sessionId, info?.Data);
        })
])
{ }
[Injectable]
public class DoomQuestCallbacks(
    SaveServer saveServer
)
{
    public async ValueTask<string> StartQuest(MongoId sessionId, string questId)
    {

        var profile = saveServer.GetProfile(sessionId);
        if (profile is null)
        {
            return "FAILED_NO_PROFILE";
        }

        var pmc = profile.CharacterData?.PmcData;
        if (pmc is null)
        {
            return "FAILED_NO_PMC";
        }

        var quests = pmc.Quests;
        if (quests == null)
        {
            quests = new List<QuestStatus>();
            pmc.Quests = quests;
        }


        if (quests.Any(q => q.QId == questId))
        {
            return "ALREADY_EXISTS";
        }

        double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var newQuest = new QuestStatus
        {
            QId = questId,
            StartTime = 0,
            Status = QuestStatusEnum.AvailableForStart,
            StatusTimers = new Dictionary<QuestStatusEnum, double>
            {
                { QuestStatusEnum.AvailableForStart, now }
            },
            CompletedConditions = new List<string>(),
            AvailableAfter = 0
        };

        quests.Add(newQuest);

        await saveServer.SaveProfileAsync(sessionId);

        return "OK";
    }
}
