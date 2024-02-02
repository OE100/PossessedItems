using HarmonyLib;
using Unity.Netcode;

namespace PossessedItems.patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManagerPatch
{
    [HarmonyPatch(nameof(GameNetworkManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void StartPostfix(GameNetworkManager __instance)
    {
        // register network prefab
        Plugin.NetworkPrefabs.ForEach(prefab => NetworkManager.Singleton.AddNetworkPrefab(prefab));
    }
}