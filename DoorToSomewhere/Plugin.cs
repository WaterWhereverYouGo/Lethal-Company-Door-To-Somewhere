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


                //AssetBundle doorToSomewhereBundle = AssetBundle.LoadFromFile (Path.Combine(Application.streamingAssetsPath, "portal"));

                //AssetBundle doorToSomewhereBundle = AssetBundle.LoadFromFile($"{Application.streamingAssetsPath}/Assets/portal");

                AssetBundle doorToSomewhereBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "portal"));

                if (doorToSomewhereBundle == null)
                {
                    logger.LogInfo($"Plugin {modName} failed to load asset bundle.");
                    throw new Exception("Failed to load asset bundle.");
                }

                logger.LogInfo($"Plugin {modName} loaded asset bundle successfully.");

                //DoorToSomewherePrefab = doorToSomewhereBundle.LoadAsset<GameObject>("Assets/DoorToSomewhere.prefab");
                //DoorToSomewhereNetworkerPrefab = doorToSomewhereBundle.LoadAsset<GameObject>("Assets/DoorToSomewhereNetworker.prefab");
                //DoorToSomewhereFile = doorToSomewhereBundle.LoadAsset<TerminalNode>("Assets/DoorToSomewhereFile.asset");

                portalMaterial = doorToSomewhereBundle.LoadAsset<Material>("Assets/Shaders/normal_portal_mat");
                
                if (portalMaterial == null)
                {
                    logger.LogInfo($"Plugin {modName} failed to load material.");

                    portalMaterial = doorToSomewhereBundle.LoadAsset<Material>("Assets/Shaders/normal_portal_mat.mat");

                    if (portalMaterial == null)
                    {
                        logger.LogInfo($"Plugin {modName} still failed to load material.");
                        throw new Exception("Failed to load material.");
                    }
                }    

                DoorToSomewherePrefab = null;
                DoorToSomewhereNetworkerPrefab = null;
                DoorToSomewhereFile = null;

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
