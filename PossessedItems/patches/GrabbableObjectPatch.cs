using HarmonyLib;

namespace PossessedItems.patches;

[HarmonyPatch(typeof(GrabbableObject))]
public class GrabbableObjectPatch
{
    [HarmonyPatch(nameof(GrabbableObject.Start)), HarmonyPostfix]
    private static void StartPostfix(GrabbableObject __instance)
    {
        if (!Utils.HostCheck) return;
        
        // todo: randomize item behaviour component
    }
}