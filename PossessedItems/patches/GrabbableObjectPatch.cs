using HarmonyLib;
using PossessedItems.machines.impl.general;

namespace PossessedItems.patches;

[HarmonyPatch(typeof(GrabbableObject))]
public class GrabbableObjectPatch
{
    [HarmonyPatch(nameof(GrabbableObject.Start)), HarmonyPostfix]
    private static void StartPostfix(GrabbableObject __instance)
    {
        if (!Utils.HostCheck) return;
        
        // todo: randomize item behaviour component
        if (Utils.InLevel && __instance.transform.position.y < 100f)
            __instance.gameObject.AddComponent<StingAndRun>();
    }
}