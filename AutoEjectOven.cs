using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AutoEjectOven
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AutoEjectOvenPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.hunter.autoejectoven";
        public const string PluginName = "Auto Eject Oven";
        public const string PluginVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully!");
        }
    }

    [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
    public static class CookingStationUpdateCookingPatch
    {
        // Harmony automatically injects the private m_nview field via the '___' prefix
        static void Postfix(CookingStation __instance, ZNetView ___m_nview)
        {
            if (___m_nview == null || !___m_nview.IsOwner())
            {
                return;
            }

            // Use Traverse.Create to safely access private fields like m_slots
            var slots = Traverse.Create(__instance).Field("m_slots")?.GetValue<System.Collections.IList>();
            if (slots == null) return;

            for (int i = 0; i < slots.Count; i++)
            {
                object slot = slots[i];
                if (slot == null) continue;

                string itemName = Traverse.Create(slot).Field("m_name")?.GetValue<string>();
                float cookedTime = Traverse.Create(slot).Field("m_cookedTime")?.GetValue<float>() ?? 0f;

                if (string.IsNullOrEmpty(itemName)) continue;

                if (IsItemDoneCooking(__instance, itemName, cookedTime))
                {
                    // Call Valheim's internal RPC method to pop the item off the oven
                    ___m_nview.InvokeRPC("RPC_PopProduct", i);
                }
            }
        }

        private static bool IsItemDoneCooking(CookingStation station, string itemName, float cookedTime)
        {
            var conversionList = Traverse.Create(station).Field("m_conversion")?.GetValue<System.Collections.IList>();
            if (conversionList == null) return false;

            foreach (var conversion in conversionList)
            {
                var fromItem = Traverse.Create(conversion).Field("m_from")?.GetValue<ItemDrop>()?.m_itemData?.m_shared?.m_name;
                if (fromItem == itemName)
                {
                    float cookTime = Traverse.Create(conversion).Field("m_cookTime")?.GetValue<float>() ?? 0f;
                    return cookedTime >= cookTime;
                }
            }
            return false;
        }
    }
}