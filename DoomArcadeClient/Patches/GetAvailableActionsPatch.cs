using System.Collections.Generic;
using System.Reflection;
using DoomArcadeClient;
using DoomArcadeClient.Components;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoomArcadeClient.Patches;

internal class GetAvailableActionsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(typeof(GetActionsClass),
            x => x.Name == nameof(GetActionsClass.GetAvailableActions) && x.GetParameters()[0].Name == "owner");
    }

    [PatchPrefix]
    public static bool PatchPrefix(object[] __args, ref ActionsReturnClass __result)
    {
        var interactive = __args[1];
        if (interactive is InteractableDoomArcade arcade)
        {
            if (arcade.IsPoweredOn)
            {
                __result = new ActionsReturnClass { Actions = new List<ActionsTypesClass>() };
                return false;
            }

            var actions = new List<ActionsTypesClass>
            {
                new()
                {
                    Name = "Turn On Arcade",
                    Action = () =>
                    {
                        DoomArcadeClient.Log?.LogInfo("[ArcadeClient] 'Turn On Arcade' invoked → unityInteractable.PowerOnArcade()");
                        arcade.PowerOnFromInteraction();
                    }
                }
            };

            __result = new ActionsReturnClass { Actions = actions };
            return false;
        }
        return true;
    }
}
