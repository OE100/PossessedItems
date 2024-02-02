using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PossessedItems.networking;
using UnityEngine;

namespace PossessedItems;

[BepInDependency(LethalLib.Plugin.ModGUID, LethalLib.Plugin.ModVersion)]
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony _harmony = new(Guid);

    public const string Guid = "oe.tweaks.possesseditems";
    internal const string Name = "Possessed Items";
    public const string Version = "0.0.0";

    internal static Plugin Instance;

    internal static readonly List<GameObject> NetworkPrefabs = [];

    internal static ManualLogSource Log;
    
    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"'{Name}' is loading...");

        if (!Instance)
            Instance = this;
        
        ModConfig.Init(Config);
        
        var crawlingBehaviour = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("PossessedItemsManager");
        crawlingBehaviour.AddComponent<PossessedItemsBehaviour>();
        NetworkPrefabs.Add(crawlingBehaviour);
        
        InitializeNetworkRoutine();
        
        _harmony.PatchAll();
        
        Log.LogInfo($"'{Name}' loaded!");
    }

    private void OnDestroy()
    {
        Instance = null;
    }


    private void InitializeNetworkRoutine()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    try
                    {
                        method.Invoke(null, null);
                    } 
                    catch (Exception e)
                    {
                        Log.LogError($"Failed to invoke method {method.Name}: {e}");
                    }
                }
            }
        }
    }
}