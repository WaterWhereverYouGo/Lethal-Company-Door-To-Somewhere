using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using DoorToSomewhereMod.Logger;
using DoorToSomewhereMod.Configuration;

namespace DoorToSomewhereMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class DoorToSomewhereBase : BaseUnityPlugin
    {
        private const string modGUID = "WaterWhereverYouGo.DoorToSomewhere";
        public const string modName = "Door To Somewhere";
        private const string modVersion = "0.1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        public static DoorToSomewhereBase Instance { get; private set; }

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

                // Ensure singleton.
                if (Instance == null)
                {
                    Instance = this;
                }

                AssetBundle doorToSomewhereBundle = AssetBundle.LoadFromMemory(LethalCompanyDoorToSomewhere.Properties.Resources.doorToSomewhere);
                DoorToSomewherePrefab = doorToSomewhereBundle.LoadAsset<GameObject>("Assets/DoorToSomewhere.prefab");
                DoorToSomewhereNetworkerPrefab = doorToSomewhereBundle.LoadAsset<GameObject>("Assets/DoorToSomewhereNetworker.prefab");
                DoorToSomewhereFile = doorToSomewhereBundle.LoadAsset<TerminalNode>("Assets/DoorToSomewhereFile.asset");

                // Handle configuration.
                ConfigSettings.Bind();

                // Patch changes.
                harmony.PatchAll();
                logger.LogInfo($"Plugin {modName} loaded.");

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
