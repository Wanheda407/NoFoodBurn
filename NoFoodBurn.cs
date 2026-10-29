using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NoFoodBurn
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NoFoodBurnPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.wanheda.nofoodburn";
        public const string PluginName = "No Food Burn";
        public const string PluginVersion = "1.2.0";

        // Station Anti-Burn Toggles
        public static ConfigEntry<bool> EnableWoodenStation;
        public static ConfigEntry<bool> EnableIronStation;
        public static ConfigEntry<bool> EnableStoneOven;
        public static ConfigEntry<bool> EnableModdedStations;

        // Sound Notification Settings
        public static ConfigEntry<bool> EnableSoundNotification;
        public static ConfigEntry<int> SoundMaxPlayCount;
        public static ConfigEntry<float> SoundRepeatInterval;
        public static ConfigEntry<string> SoundPrefabName;

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            // --- Anti-Burn Config Section ---
            EnableWoodenStation = Config.Bind("Stations", "WoodenCookingStation", true, "Enable anti-burn for the standard wooden cooking station.");
            EnableIronStation = Config.Bind("Stations", "IronCookingStation", true, "Enable anti-burn for the upgraded iron hanging cooking station.");
            EnableStoneOven = Config.Bind("Stations", "StoneOven", true, "Enable anti-burn for the stone oven.");
            EnableModdedStations = Config.Bind("Stations", "ModdedStations", true, "Enable anti-burn for any custom cooking stations added by other mods.");

            // --- Sound Notification Config Section ---
            EnableSoundNotification = Config.Bind("AudioNotification", "EnableNotification", true, "Play a sound when food finishes cooking.");
            SoundMaxPlayCount = Config.Bind("AudioNotification", "MaxPlayCount", 3, "How many times the sound will play per finished item.");
            SoundRepeatInterval = Config.Bind("AudioNotification", "RepeatInterval", 5.0f, "Delay in seconds between repeated sound notifications.");
            SoundPrefabName = Config.Bind("AudioNotification", "SoundPrefab", "sfx_ping", "Name of the built-in Valheim sound effect to play (e.g., sfx_ping, sfx_coins, sfx_build_hammer).");

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
            bool enableAntiBurn = true;

            if (prefabName.Contains("cookingstation_iron") || prefabName.Contains("hanging"))
            {
                enableAntiBurn = NoFoodBurnPlugin.EnableIronStation.Value;
            }
            else if (prefabName.Contains("cookingstation"))
            {
                enableAntiBurn = NoFoodBurnPlugin.EnableWoodenStation.Value;
            }
            else if (prefabName.Contains("oven"))
            {
                enableAntiBurn = NoFoodBurnPlugin.EnableStoneOven.Value;
            }
            else
            {
                enableAntiBurn = NoFoodBurnPlugin.EnableModdedStations.Value;
            }

            if (enableAntiBurn)
            {
                __instance.m_canOvercookItems = false;
            }

            // Attach notification listener if enabled
            if (NoFoodBurnPlugin.EnableSoundNotification.Value)
            {
                if (__instance.gameObject.GetComponent<CookingNotifier>() == null)
                {
                    __instance.gameObject.AddComponent<CookingNotifier>();
                }
            }
        }
    }

    public class CookingNotifier : MonoBehaviour
    {
        private CookingStation _station;
        private Dictionary<int, Coroutine> _activeRoutines = new Dictionary<int, Coroutine>();
        private Dictionary<int, string> _slotLastItem = new Dictionary<int, string>();

        private void Awake()
        {
            _station = GetComponent<CookingStation>();
        }

        private void Update()
        {
            if (_station == null || !NoFoodBurnPlugin.EnableSoundNotification.Value) return;

            var slotsList = Traverse.Create(_station).Field("m_slots")?.GetValue<System.Collections.IList>();
            if (slotsList == null) return;

            for (int i = 0; i < slotsList.Count; i++)
            {
                object slot = slotsList[i];
                if (slot == null) continue;

                string itemName = Traverse.Create(slot).Field("m_name")?.GetValue<string>();
                float cookedTime = Traverse.Create(slot).Field("m_cookedTime")?.GetValue<float>() ?? 0f;

                // Check recipe cook time threshold
                bool isDone = IsItemDone(_station, itemName, cookedTime);

                // Slot emptied or reset -> stop sound routine
                if (string.IsNullOrEmpty(itemName))
                {
                    StopNotifierForSlot(i);
                    _slotLastItem[i] = "";
                    continue;
                }

                // Newly finished item detected
                if (isDone && (!_slotLastItem.ContainsKey(i) || _slotLastItem[i] != itemName))
                {
                    _slotLastItem[i] = itemName;
                    StartNotifierForSlot(i);
                }
            }
        }

        private bool IsItemDone(CookingStation station, string itemName, float cookedTime)
        {
            if (string.IsNullOrEmpty(itemName)) return false;

            var conversionList = Traverse.Create(station).Field("m_conversion")?.GetValue<System.Collections.IList>();
            if (conversionList == null) return false;

            foreach (var conversion in conversionList)
            {
                var fromItem = Traverse.Create(conversion).Field("m_from")?.GetValue<ItemDrop>();
                string fromSharedName = fromItem?.m_itemData?.m_shared?.m_name;

                if (fromSharedName == itemName)
                {
                    float cookTime = Traverse.Create(conversion).Field("m_cookTime")?.GetValue<float>() ?? 0f;
                    return cookedTime >= cookTime;
                }
            }
            return false;
        }

        private void StartNotifierForSlot(int slotIndex)
        {
            StopNotifierForSlot(slotIndex);
            _activeRoutines[slotIndex] = StartCoroutine(PlayNotificationRoutine(slotIndex));
        }

        private void StopNotifierForSlot(int slotIndex)
        {
            if (_activeRoutines.ContainsKey(slotIndex) && _activeRoutines[slotIndex] != null)
            {
                StopCoroutine(_activeRoutines[slotIndex]);
                _activeRoutines.Remove(slotIndex);
            }
        }

        private IEnumerator PlayNotificationRoutine(int slotIndex)
        {
            int maxPlays = Mathf.Max(1, NoFoodBurnPlugin.SoundMaxPlayCount.Value);
            float interval = Mathf.Max(0.2f, NoFoodBurnPlugin.SoundRepeatInterval.Value);
            string soundName = NoFoodBurnPlugin.SoundPrefabName.Value;

            for (int play = 0; play < maxPlays; play++)
            {
                // Instantiate Valheim built-in sound effect at station location
                GameObject sfxPrefab = ZNetScene.instance?.GetPrefab(soundName);
                if (sfxPrefab != null)
                {
                    Instantiate(sfxPrefab, transform.position, Quaternion.identity);
                }

                yield return new WaitForSeconds(interval);
            }

            _activeRoutines.Remove(slotIndex);
        }
    }
}
