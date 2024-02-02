using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace PossessedItems;

public static class Utils
{
    public static readonly Dictionary<Type, GameObject> EnemyPrefabRegistry = new();

    public static Terminal Terminal;
    private static bool _registered;

    public static bool HostCheck => NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
    public static bool InLevel =>
        StartOfRound.Instance && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevelID != 3;

    private static void RegisterAllItems()
    {
        StartOfRound.Instance.allItemsList.itemsList.ForEach(item =>
        {
            var prefab = item.spawnPrefab;
            if (!prefab.TryGetComponent(out NetworkTransform networkTransform))
            {
                // add network transform to prefab
                prefab.AddComponent<NetworkTransform>();
            }
        });
    }
    
    private static void RegisterEnemyPrefab(Type enemyAI, GameObject prefab)
    {
        if (!typeof(EnemyAI).IsAssignableFrom(enemyAI)) return;
        EnemyPrefabRegistry.TryAdd(enemyAI, prefab);
    }

    private static void RegisterOnAllLevels()
    {
        foreach (var level in Terminal.moonsCatalogueList)
        {
            // add network transform to all level items
            foreach (var prefab in level.spawnableScrap.Select(spawnable => spawnable.spawnableItem.spawnPrefab))
                if (!prefab.TryGetComponent(out NetworkTransform networkTransform)) prefab.AddComponent<NetworkTransform>();

            // register enemy prefabs to dictionary
            level.Enemies.ForEach(spawnable =>
            {
                var prefab = spawnable.enemyType.enemyPrefab;
                RegisterEnemyPrefab(prefab.GetComponent<EnemyAI>().GetType(), prefab);
            });
        }
    }
    
    public static void RegisterAll()
    {
        if (_registered || !Terminal || !StartOfRound.Instance) return;
        _registered = true;
        
        Plugin.Log.LogMessage("Registering all!");
        
        RegisterOnAllLevels();
        RegisterAllItems();
    }

    public static void PlayRandomAudioClipFromList(AudioSource source, List<AudioClip> clips, float volumeScale = 1f)
    {
        source.PlayOneShot(clips[Random.Range(0, clips.Count)], volumeScale);
    }
    
    public static int MathMod(int muduli, int modulus) => ((muduli % modulus) + modulus) % modulus;
    
    public static bool IsActivePlayer(PlayerControllerB player) => player && player.isPlayerControlled && !player.isPlayerDead;

    public static int ItemCount(PlayerControllerB player) => 
        player.ItemSlots.Count(item => item);

    public static List<PlayerControllerB> GetActivePlayers(bool inside)
    {
        if (!StartOfRound.Instance) return [];
        var query = from player in StartOfRound.Instance.allPlayerScripts
            where player.isInsideFactory == inside
            where IsActivePlayer(player)
            select player;
        return query.ToList();
    }

    public static bool PathNotVisibleByPlayer(NavMeshPath path)
    {
        var corners = path.corners;
        for (var i = 1; i < corners.Length; i++)
            if (Physics.Linecast(corners[i - 1], corners[i], 262144))
                return false;

        return true;
    }
}