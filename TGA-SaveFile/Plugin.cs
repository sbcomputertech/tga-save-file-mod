using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace SaveFile
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
		private static List<SaveEntry> Entries = new();
		private static ManualLogSource LogSource;
		private void Awake()
		{
			Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded!"); // print a message to the BepInEx console
			var harmony = new Harmony(PluginInfo.PLUGIN_GUID); // create a harmony patcher
			harmony.PatchAll(); // run all patches in the mod dll
			LogSource = Logger;
		}

		private static void WriteToJson<T>(string key, T value)
		{
			var obj = (object)value;
			var entry = new SaveEntry(key, obj);
			Entries.RemoveAll(e => e.key == key);
			Entries.Add(entry);
			var json = JsonConvert.SerializeObject(Entries);
			File.WriteAllText("savefile.json", json);
		}

		private static object LoadFromJson(string key)
		{
			var text = File.ReadAllText("savefile.json");
			Entries = JsonConvert.DeserializeObject<List<SaveEntry>>(text);
			var saveEntries = Entries.Where(e => e.key == key).ToArray();
			return saveEntries.Any() ? saveEntries.First().value : null;
		}

		[HarmonyPatch(typeof(PlayerPrefs))]
		public class PlayerPrefsPatch
		{
			[HarmonyPatch("SetFloat")]
			[HarmonyPrefix]
			public static bool PrefixSetFloat(string key, float value)
			{
				WriteToJson(key, value);
				return false;
			}

			[HarmonyPatch("GetFloat", typeof(string))]
			[HarmonyPrefix]
			public static bool PrefixGetFloat(string key, ref float __result)
			{
				var res = LoadFromJson(key);
				try
				{
					__result = (float)(double)res; // don't ask me why but this somehow makes it work
				}
				catch (InvalidCastException)
				{
					LogSource.LogError("InvalidCastException on key " + key + ". Val type: " + res.GetType());
					__result = 0f;
				}
				return false;
			}
		}
	}
}