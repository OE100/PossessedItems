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
        
        if (UnityEngine.Random.Range(0, 100) >= ModConfig.PossessionProbability.Value) return;
        if (Utils.InLevel && 
            !__instance.isInShipRoom && 
            __instance.transform.position.y < 100f &&
            !Utils.ItemNamesList.Any(name => __instance.itemProperties.itemName.Contains(name)))
        {
            __instance.gameObject.AddComponent<StingAndRun>();
        }
    }
}