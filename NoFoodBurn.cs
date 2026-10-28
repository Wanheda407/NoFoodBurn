using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NoFoodBurn
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NoFoodBurnPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.wanheda.nofoodburn";
        public const string PluginName = "No Food Burn";
        public const string PluginVersion = "1.1.1";

        public static ConfigEntry<bool> EnableWoodenStation;
        public static ConfigEntry<bool> EnableIronStation;
        public static ConfigEntry<bool> EnableStoneOven;
        public static ConfigEntry<bool> EnableModdedStations;

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            // Initialize Config Entries
            EnableWoodenStation = Config.Bind("General", "WoodenCookingStation", true, "Enable anti-burn for the standard wooden cooking station.");
            EnableIronStation = Config.Bind("General", "IronCookingStation", true, "Enable anti-burn for the upgraded iron hanging cooking station.");
            EnableStoneOven = Config.Bind("General", "StoneOven", true, "Enable anti-burn for the stone oven.");
            EnableModdedStations = Config.Bind("General", "ModdedStations", true, "Enable anti-burn for any custom cooking stations added by other mods.");

            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully!");
        }
    }

    [HarmonyPatch(typeof(CookingStation), "Awake")]
    public static class CookingStation_Awake_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CookingStation __instance)
        {
            if (__instance == null) return;

            string prefabName = __instance.gameObject.name.ToLower();

            // Check against configuration rules based on prefab name identifiers
            if (prefabName.Contains("cookingstation_iron") || prefabName.Contains("hanging"))
            {
                if (!NoFoodBurnPlugin.EnableIronStation.Value) return;
            }
            else if (prefabName.Contains("cookingstation"))
            {
                if (!NoFoodBurnPlugin.EnableWoodenStation.Value) return;
            }
            else if (prefabName.Contains("oven"))
            {
                if (!NoFoodBurnPlugin.EnableStoneOven.Value) return;
            }
            else
            {
                // Catch-all for custom or modded cooking stations
                if (!NoFoodBurnPlugin.EnableModdedStations.Value) return;
            }

            // Apply anti-burn protection if permitted by config
            __instance.m_canOvercookItems = false;
        }
    }
}
