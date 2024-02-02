using HarmonyLib;
using PossessedItems.networking;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PossessedItems.patches;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    [HarmonyPatch(nameof(StartOfRound.Awake)), HarmonyPostfix]
    private static void AwakePostfix()
    {
        // host only instructions
        if (Utils.HostCheck)
        {
            // spawn network prefabs
            Plugin.Log.LogMessage("Spawning network prefabs (host only)");
            Plugin.NetworkPrefabs.ForEach(prefab =>
            {
                Object.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                    .GetComponent<NetworkObject>()
                    .Spawn(destroyWithScene: false);
            });
        }
    }
    
    [HarmonyPatch(nameof(StartOfRound.Start)), HarmonyPostfix]
    private static void StartPostfix(StartOfRound __instance)
    {
        // client only instructions
        if (Utils.HostCheck)
        {
            // register all if not already done
            Utils.RegisterAll();
        }
    }

    [HarmonyPatch(nameof(StartOfRound.ShipHasLeft)), HarmonyPostfix]
    private static void ShipHasLeftPostfix(StartOfRound __instance)
    {
        if (PossessedItemsBehaviour.Instance)
            PossessedItemsBehaviour.Instance.StopAllCoroutines();
    }
}