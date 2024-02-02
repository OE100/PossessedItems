using HarmonyLib;
using PossessedItems.machines.def;

namespace PossessedItems.patches;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManagerPatch
{
    [HarmonyPatch(nameof(RoundManager.FinishGeneratingLevel)), HarmonyPostfix]
    private static void FinishGeneratingLevelPostfix(RoundManager __instance)
    {
        MachineUtils.InsideAINodes = __instance.insideAINodes.ToList();
        MachineUtils.OutsideAINodes = __instance.outsideAINodes.ToList();
    }
}