using BepInEx.Configuration;

namespace PossessedItems;

public static class ModConfig
{
    internal static bool Loaded { get; private set; }
    
    internal static void Init(ConfigFile config)
    {
        Loaded = true;
    }
}