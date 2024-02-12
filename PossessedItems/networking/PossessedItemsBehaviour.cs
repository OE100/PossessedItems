using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PossessedItems.networking;

public class PossessedItemsBehaviour : NetworkBehaviour
{
    public static PossessedItemsBehaviour Instance { get; private set; }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        StartCoroutine(LoadConfigItemList());
    }

    private static IEnumerator LoadConfigItemList()
    {
        yield return new WaitUntil(() => ModConfig.Loaded);
        Utils.ItemNamesList = ModConfig.ItemList.Value.Split(",").Select(str => str.Trim()).ToList();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instance = null;
    }
    
    [ServerRpc]
    public void SetGrabbableStateServerRpc(NetworkObjectReference objRef, bool state)
    {
        SetGrabbableStateClientRpc(objRef, state);
    }

    [ClientRpc]
    private void SetGrabbableStateClientRpc(NetworkObjectReference objRef, bool state)
    {
        if (!objRef.TryGet(out var networkObject)) return;
        if (networkObject.gameObject.TryGetComponent<GrabbableObject>(out var obj))
            obj.enabled = state;
    }
}