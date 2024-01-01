using System;
using System.Reflection;
using BepInEx.Configuration;
using DoorToSomewhereMod.Logger;

namespace DoorToSomewhereMod.Configuration
{

    public class ConfigSettings
    {
        internal static ConfigEntry<int>[] SpawnRates;
        internal static ConfigEntry<bool> DynamicSpawnRate;

        public static ConfigSettings Instance;

        public ConfigSettings()
        {
            Instance = this;
        }

        public static void Bind()
        {
            try
            {

                DoorToSomewhereBase.logger.LogInfo($"Binding config settings.");

                // Create configuration file with defaults for spawn rate.
                SpawnRates = new ConfigEntry<int>[]
                {
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Zero Doors", 15, "Weight of zero doors spawning"),
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "One Door", 40, "Weight of one doors spawning"),
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Two Doors", 20, "Weight of two doors spawning"),
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Three Doors", 15, "Weight of three doors spawning"),
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Four Doors", 10, "Weight of four doors spawning"),
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Maximum Doors", 0, "Weight of maximum doors spawning")
                };

                DynamicSpawnRate = DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Dynamic Spawn Rate", true, "Increases door spawn rate based on dungeon size.");

                DoorToSomewhereBase.logger.LogInfo($"Saving config settings.");
                DoorToSomewhereBase.Instance.Config.Save();

                DoorToSomewhereBase.logger.LogInfo($"Assigning config settings in base.");

                int[] SpawnRatesValues = new int[SpawnRates.Length];

                for (int i = 0; i < SpawnRates.Length; i++)
                {
                    SpawnRatesValues[i] = SpawnRates[i].Value;
                }

                if (SpawnRatesValues.Length != SpawnRates.Length)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to pull values from the spawn rates array.");
                    throw new Exception("Failed to pull values from spawn rates array");
                }

                DoorToSomewhereBase.SpawnRates = new int[SpawnRatesValues.Length];

                for (int i = 0; i < SpawnRatesValues.Length; i++)
                {
                    DoorToSomewhereBase.SpawnRates[i] = SpawnRatesValues[i];
                }

                DoorToSomewhereBase.DynamicSpawnRate = DynamicSpawnRate.Value;

                DoorToSomewhereBase.logger.LogInfo($"Finished saving and loading config settings.\nSpawn Rates = {string.Join(", ", DoorToSomewhereBase.SpawnRates)}\nDynamic Spawn Rate = {DoorToSomewhereBase.DynamicSpawnRate}");
            }
            catch (Exception e)
            {
                // Fall back on some default values.
                DoorToSomewhereBase.logger.LogInfo($"Falling back on default values for config settings.");

                DoorToSomewhereBase.SpawnRates = new int[]
                {
                    15,
                    40,
                    20,
                    15,
                    10,
                    0
                };

                DoorToSomewhereBase.DynamicSpawnRate = true;

                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }
}
