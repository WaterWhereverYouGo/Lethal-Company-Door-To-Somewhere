using HarmonyLib;
using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using DoorToSomewhereMod.Logger;

namespace DoorToSomewhereMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(ref StartOfRound __instance)
        {
            try
            {
                // Ensure instance is server.
                if (!__instance.IsServer)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Will not patch StartOfRound when instance is not the server.");
                    return;
                }

                // Ensure that networker does not already have an instance.
                if (Networker.DoorToSomewhereNetworker.Instance != null)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Will not patch StartOfRound when networker instance exists.");
                    return;
                }

                DoorToSomewhereBase.logger.LogInfo($"Patching StartOfRound.");

                DoorToSomewhereBase.logger.LogInfo($"Spawn rates array length is {DoorToSomewhereBase.SpawnRates.Length}.");
                Networker.DoorToSomewhereNetworker.SpawnWeights = new NetworkVariable<int>[DoorToSomewhereBase.SpawnRates.Length];

                // Assign spawn rates.
                for (int i = 0; i < DoorToSomewhereBase.SpawnRates.Length; i++)
                {
                    Networker.DoorToSomewhereNetworker.SpawnWeights[i] = new NetworkVariable<int>(DoorToSomewhereBase.SpawnRates[i]);
                }

                //DoorToSomewhereBase.SpawnRates.CopyTo(Networker.DoorToSomewhereNetworker.SpawnWeights.Value, 0);
                Networker.DoorToSomewhereNetworker.SpawnRateDynamic = new NetworkVariable<bool>(DoorToSomewhereBase.DynamicSpawnRate);
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }
}
