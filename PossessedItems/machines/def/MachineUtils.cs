using UnityEngine;
using UnityEngine.AI;

namespace PossessedItems.machines.def;

public static class MachineUtils
{
    public static bool AveragePlayerPosition(bool inside, out Vector3 average)
    {
        var players = Utils.GetActivePlayers(inside);
        if (players.Count == 0)
        {
            average = default;
            return false;
        }

        var sum = players.Select(player => player.transform.position).Aggregate((a, b) => a + b);
        average = sum / players.Count;
        return true;
    }
    
    
    
    public static List<GameObject> InsideAINodes;
    public static List<GameObject> OutsideAINodes;
}