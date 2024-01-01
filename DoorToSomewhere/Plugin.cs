using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using DoorToSomewhereMod.Logger;
using DoorToSomewhereMod.Configuration;
using System.IO;
using UnityEngine.Assertions;

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
        public static Material portalMaterial;
        public static Mesh portalMesh;

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

                AssetBundle doorToSomewhereBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "portal"));

                if (doorToSomewhereBundle == null)
                {
                    throw new Exception("Failed to load asset bundle");
                }

                portalMaterial = doorToSomewhereBundle.LoadAsset<Material>("Assets/Materials/normal_portal_mat.mat");
                portalMesh = doorToSomewhereBundle.LoadAsset<Mesh>("Assets/Mesh/Portal_mesh.asset");

                if (portalMaterial == null)
                { 
                    throw new Exception("Failed to load material");
                }

                if (portalMesh == null)
                { 
                    throw new Exception("Failed to load mesh");
                }

                DoorToSomewherePrefab = null;
                DoorToSomewhereNetworkerPrefab = null;
                DoorToSomewhereFile = null;

                // Handle configuration.
                ConfigSettings.Bind();

                // Patch changes.
                harmony.PatchAll(typeof(DoorToSomewhereBase));
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

        // Just something for testing, thanks Angel-Madeline!
        [HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
        [HarmonyPrefix]
        private static bool NoMonstersSpawn(ref SelectableLevel newLevel)
        {
            foreach (SpawnableEnemyWithRarity Enemy in newLevel.Enemies)
            {
                Enemy.rarity = 0;
            }
            foreach (SpawnableEnemyWithRarity Enemy in newLevel.OutsideEnemies)
            {
                Enemy.rarity = 0;
            }
            logger.LogInfo($"Removed All Enemies.");
            return true;
        }
    }

}
