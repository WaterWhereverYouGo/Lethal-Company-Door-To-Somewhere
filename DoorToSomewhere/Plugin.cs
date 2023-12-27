using BepInEx;
using DunGen;
using GameNetcodeStuff;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Audio;
using BepInEx.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using DoorToSomewhereMod.Logger;

namespace DoorToSomewhereMod
{

    [BepInPlugin(modGUID, modName, modVersion)]
    public class DoorToSomewhereBase : BaseUnityPlugin
    {
        private const string modGUID = "WaterWhereverYouGo.DoorToSomewhere";
        public const string modName = "Door To Somewhere";
        private const string modVersion = "1.0.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        private static DoorToSomewhereBase Instance;

        public static GameObject DoorToSomewherePrefab;
        public static GameObject DoorToSomewhereNetworkerPrefab;
        public static TerminalNode DoorToSomewhereFile;

        public static int[] SpawnRates;
        public static bool DynamicSpawnRate;

        internal static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

        private void Awake()
        {
            try
            {
                AssetBundle doorToSomewhereBundle = AssetBundle.LoadFromMemory(LethalCompanyDoorToSomewhere.Properties.Resources.doorToSomewhere);
                DoorToSomewherePrefab = doorToSomewhereBundle.LoadAsset<GameObject>("Assets/DoorToSomewhere.prefab");
                DoorToSomewhereNetworkerPrefab = doorToSomewhereBundle.LoadAsset<GameObject>("Assets/DoorToSomewhereNetworker.prefab");
                DoorToSomewhereFile = doorToSomewhereBundle.LoadAsset<TerminalNode>("Assets/DoorToSomewhereFile.asset");


                if (Instance == null)
                {
                    Instance = this;
                }

                harmony.PatchAll();
                logger.LogInfo($"Plugin {modName} is loaded.");

                // Handle configs.
                {
                    // Create configuration file with defaults for spawn rate.
                    SpawnRates = new int[] {
                    Config.Bind("Spawn Rate", "Zero Doors", 15, "Weight of zero doors spawning").Value,
                    Config.Bind("Spawn Rate", "One Door", 40, "Weight of one doors spawning").Value,
                    Config.Bind("Spawn Rate", "Two Doors", 20, "Weight of two doors spawning").Value,
                    Config.Bind("Spawn Rate", "Three Doors", 15, "Weight of three doors spawning").Value,
                    Config.Bind("Spawn Rate", "Four Doors", 10, "Weight of four doors spawning").Value,
                    Config.Bind("Spawn Rate", "Maximum Doors", 0, "Weight of maximum doors spawning").Value
                };

                    DynamicSpawnRate = Config.Bind("Spawn Rate", "Dynamic Spawn Rate", true, "Increases door spawn rate based on dungeon size.").Value;

                    // Save config.
                    Config.Save();

                }

                // UnityNetcodeWeaver patch requires this.

                var types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                        if (attributes.Length > 0)
                        {
                            method.Invoke(null, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }

}
