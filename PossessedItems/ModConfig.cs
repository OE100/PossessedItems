using BepInEx.Configuration;

namespace PossessedItems;

public static class ModConfig
{
    internal static bool Loaded { get; private set; }
    
    // probability
    internal static ConfigEntry<int> PossessionProbability; // The probability of an item being possessed
    
    // permissions
    internal static ConfigEntry<bool> ListMode; // true -> whitelist, false -> blacklist
    internal static ConfigEntry<string> ItemList; // The list of item names that are allowed or disallowed from possession
    
    // damage section
    internal static ConfigEntry<int> BaseDamageOfItems; // The base damage of the items
    internal static ConfigEntry<int> UnitsOfWeightPerDamage; // The amount of weight units per 1 extra damage
    internal static ConfigEntry<int> MaxDamagePerHit; // The max damage an item can do per hit
    internal static ConfigEntry<int> MaxDamageOverall; // The max damage an item can do before reverting
    
    internal static void Init(ConfigFile config)
    {
        PossessionProbability = config.Bind("Probability", "PossessionProbability", 5, "The probability of an item being possessed (0-100)");

        ListMode = config.Bind("Permissions", "ListMode", false, "true -> whitelist, false -> blacklist");
        ItemList = config.Bind("Permissions", "ItemList", "", "The list of item names (, as separator) that are allowed or disallowed from possession");

        BaseDamageOfItems = config.Bind("Damage", "BaseDamageOfItems", 3, "The base damage of the items");
        UnitsOfWeightPerDamage = config.Bind("Damage", "UnitsOfWeightPerDamage", 5, "The amount of weight units per 1 extra damage");
        MaxDamagePerHit = config.Bind("Damage", "MaxDamagePerHit", 10, "The max damage an item can do per hit");
        MaxDamageOverall = config.Bind("Damage", "MaxDamageOverall", 50, "The max damage an item can do before reverting");
        
        Loaded = true;
    }
}