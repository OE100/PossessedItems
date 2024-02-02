using System.Collections;
using PossessedItems.networking;
using UnityEngine;

namespace PossessedItems;

public static class SharedConfig
{
    internal static IEnumerator DelayedRequestConfig()
    {
        if (Utils.HostCheck)
        {
            yield return new WaitUntil(() => ModConfig.Loaded);
            // todo: load config values
        }
        else
        {
            // todo: request config values from host   
        }
    }
}