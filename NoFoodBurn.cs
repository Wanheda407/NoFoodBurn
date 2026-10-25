using BepInEx;
using HarmonyLib;

namespace NoFoodBurn
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NoFoodBurnPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.wanheda.NoFoodBurn";
        public const string PluginName = "Anti-Burn Cooking Station";
        public const string PluginVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private void Awake()
        {
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
            if (__instance != null)
            {
                // Globally disables overcooking for every cooking station and stone oven,
                // ensuring your food stops right at the 'Done' state and never turns to coal.
                __instance.m_canOvercookItems = false;
            }
        }
    }
}
