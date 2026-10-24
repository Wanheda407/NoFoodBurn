using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AutoEjectOven
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AutoEjectOvenPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.wanheda.autoejectoven";
        public const string PluginName = "Auto Eject Oven";
        public const string PluginVersion = "1.0.1";

        private void Awake()
        {
            // Attach our auto-eject manager directly to BepInEx's persistent game object
            gameObject.AddComponent<AutoEjectController>();
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully!");
        }
    }

    public class AutoEjectController : MonoBehaviour
    {
        private float _timer = 0f;

        private void Update()
        {
            // Run check every 0.2 seconds to keep performance smooth and response near-instant
            _timer += Time.deltaTime;
            if (_timer < 0.2f) return;
            _timer = 0f;

            if (Player.m_localPlayer == null) return;

            // Find all cooking stations and stone ovens currently loaded in the world
            CookingStation[] stations = FindObjectsOfType<CookingStation>();
            foreach (var station in stations)
            {
                if (station == null) continue;

                ZNetView nview = Traverse.Create(station).Field("m_nview")?.GetValue<ZNetView>();
                if (nview == null || !nview.IsValid() || !nview.IsOwner()) continue;

                var slots = Traverse.Create(station).Field("m_slots")?.GetValue<System.Collections.IList>();
                if (slots == null || slots.Count == 0) continue;

                var conversionList = Traverse.Create(station).Field("m_conversion")?.GetValue<System.Collections.IList>();
                if (conversionList == null) continue;

                bool hasDoneItem = false;

                for (int i = 0; i < slots.Count; i++)
                {
                    object slot = slots[i];
                    if (slot == null) continue;

                    string itemName = Traverse.Create(slot).Field("m_name")?.GetValue<string>();
                    float cookedTime = Traverse.Create(slot).Field("m_cookedTime")?.GetValue<float>() ?? 0f;

                    if (string.IsNullOrEmpty(itemName)) continue;

                    foreach (var conversion in conversionList)
                    {
                        var fromItem = Traverse.Create(conversion).Field("m_from")?.GetValue<ItemDrop>();
                        string fromSharedName = fromItem?.m_itemData?.m_shared?.m_name;

                        if (fromSharedName == itemName)
                        {
                            float cookTime = Traverse.Create(conversion).Field("m_cookTime")?.GetValue<float>() ?? 0f;

                            // If the food has finished cooking, flag it for interaction
                            if (cookedTime >= cookTime)
                            {
                                hasDoneItem = true;
                                break;
                            }
                        }
                    }
                    if (hasDoneItem) break;
                }

                // If any slot is finished, invoke Valheim's native Interact method as if the player pressed 'E'
                if (hasDoneItem)
                {
                    // Parameters: Humanoid character, bool hold, bool alt
                    station.Interact(Player.m_localPlayer, false, false);
                }
            }
        }
    }
}