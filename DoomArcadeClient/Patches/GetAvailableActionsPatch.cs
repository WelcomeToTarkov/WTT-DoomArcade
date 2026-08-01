using DoomArcadeClient;
using DoomArcadeClient.Components;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace DoomArcadeClient.Patches;

internal class GetAvailableActionsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(typeof(InteractionContextHelper),
            x => x.Name == nameof(InteractionContextHelper.GetAvailableActions) && x.GetParameters()[0].Name == "owner");
    }

    [PatchPrefix]
    public static bool PatchPrefix(object[] __args, ref AvailableInteractionState __result)
    {
        var interactive = __args[1];
        if (interactive is InteractableDoomArcade arcade)
        {
            if (arcade.IsPoweredOn)
            {
                __result = new AvailableInteractionState { Actions = new List<InteractionAction>() };
                return false;
            }

            var actions = new List<InteractionAction>
            {
                new()
                {
                    Name = "TURN_ON_ARCADE".Localized(null),
                    Action = () =>
                    {
                        arcade.PowerOnFromInteraction();
                    }
                }
            };

            __result = new AvailableInteractionState { Actions = actions };
            return false;
        }
        return true;
    }
}
