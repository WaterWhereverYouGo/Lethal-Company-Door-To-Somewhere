using System;
using System.Reflection;
using DoorToSomewhereMod.Logger;

namespace DoorToSomewhereMod.Configuration
{

    public class ConfigSettings
    {
        public static int[] SpawnRates;
        public static bool DynamicSpawnRate;

        public static ConfigSettings Instance;

        public ConfigSettings()
        {
            Instance = this;
        }

        public static void Bind()
        {
            try
            {
                // Create configuration file with defaults for spawn rate.
                SpawnRates = new int[] {
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Zero Doors", 15, "Weight of zero doors spawning").Value,
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "One Door", 40, "Weight of one doors spawning").Value,
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Two Doors", 20, "Weight of two doors spawning").Value,
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Three Doors", 15, "Weight of three doors spawning").Value,
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Four Doors", 10, "Weight of four doors spawning").Value,
                    DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Maximum Doors", 0, "Weight of maximum doors spawning").Value
                };

                DynamicSpawnRate = DoorToSomewhereBase.Instance.Config.Bind("Spawn Rate", "Dynamic Spawn Rate", true, "Increases door spawn rate based on dungeon size.").Value;

                // Save config.
                DoorToSomewhereBase.Instance.Config.Save();
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }
}
