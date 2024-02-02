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
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instance = null;
    }

    [ServerRpc]
    public void SyncLocationServerRpc(NetworkObjectReference maskRef, Vector3 position, Quaternion rotation)
    {
        SyncLocationClientRpc(maskRef, position, rotation);
    }

    [ClientRpc]
    private void SyncLocationClientRpc(NetworkObjectReference maskRef, Vector3 position, Quaternion rotation)
    {
        if (Utils.HostCheck) return;
        if (!maskRef.TryGet(out var networkObject)) return;
        var obj = networkObject.gameObject;
        obj.transform.position = position;
        obj.transform.rotation = rotation;
    }
    
    [ServerRpc]
    public void SetObjStateServerRpc(NetworkObjectReference objRef, bool state)
    {
        SetObjStateClientRpc(objRef, state);
    }

    [ClientRpc]
    private void SetObjStateClientRpc(NetworkObjectReference objRef, bool state)
    {
        if (!objRef.TryGet(out var networkObject)) return;
        var obj = networkObject.gameObject.GetComponent<GrabbableObject>();
        obj.enabled = state;
    }

    [ServerRpc]
    public void SetEyesFilledServerRpc(NetworkObjectReference maskRef, bool state)
    {
        SetEyesFilledClientRpc(maskRef, state);
    }

    [ClientRpc]
    private void SetEyesFilledClientRpc(NetworkObjectReference maskRef, bool state)
    {
        if (!maskRef.TryGet(out var networkObject)) return;
        var obj = networkObject.gameObject.GetComponent<GrabbableObject>();
        if (obj is HauntedMaskItem mask)
        {
            mask.maskEyesFilled.enabled = state;
        }
    }

    [ServerRpc]
    public void AttachServerRpc(NetworkObjectReference maskRef)
    {
        AttachClientRpc(maskRef);
    }

    [ClientRpc]
    private void AttachClientRpc(NetworkObjectReference maskRef)
    {
        if (!maskRef.TryGet(out var networkObject)) return;
        var mask = networkObject.gameObject.GetComponent<HauntedMaskItem>();
        StartCoroutine(DelayedAttach(mask));
    }

    private static IEnumerator DelayedAttach(HauntedMaskItem mask)
    {
        yield return new WaitForEndOfFrame();
        mask.BeginAttachment();
    }
}