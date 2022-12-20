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
			Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} ver {PluginInfo.PLUGIN_VERSION} loaded!"); // print a message to the BepInEx console
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
			File.WriteAllText("tga.save.json", json);
		}

		private static object LoadFromJson(string key)
		{
			var text = File.ReadAllText("tga.save.json");
			Entries = JsonConvert.DeserializeObject<List<SaveEntry>>(text);
			var e = Entries.Where(e => e.key == key);
			var saveEntries = e as SaveEntry[] ?? e.ToArray();
			return saveEntries.Any() ? saveEntries.First().value : null;
		}

		[HarmonyPatch(typeof(PlayerPrefs))]
		public class PlayerPrefsPatch
		{
			[HarmonyPatch("SetFloat")]
			[HarmonyPrefix]
			public static void PrefixSetFloat(string key, float value)
			{
				WriteToJson(key, value);
			}

			[HarmonyPatch("GetFloat", typeof(string))]
			[HarmonyPrefix]
			public static bool PrefixGetFloat(string key, ref float __result)
			{
				var res = LoadFromJson(key);
				if (res == null)
				{
					return true;
				}

				try
				{
					__result = (float)(double)res; // don't ask me why but this somehow makes it work
				}
				catch (InvalidCastException e)
				{
					LogSource.LogError("InvalidCastException on key " + key + ". Val type: " + res.GetType());
				}
				return false;
			}
		}
	}
}