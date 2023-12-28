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
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} will not patch StartOfRound when instance is not the server.");
                    return;
                }

                // Ensure that networker does not already have an instance.
                if (Networker.DoorToSomewhereNetworker.Instance != null)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} will not patch StartOfRound when networker instance exists.");
                    return;
                }

                // Create networker with networker prefab.
                GameObject doorToSomewhereNetworker = GameObject.Instantiate(DoorToSomewhereBase.DoorToSomewhereNetworkerPrefab);
                doorToSomewhereNetworker.GetComponent<NetworkObject>().Spawn(true);

                // Assign spawn rates.
                Networker.DoorToSomewhereNetworker.SpawnWeights.Value = DoorToSomewhereBase.SpawnRates;
                /*
                Networker.DoorToSomewhereNetworker.SpawnWeight0.Value = SpawnRates[0];
                Networker.DoorToSomewhereNetworker.SpawnWeight1.Value = SpawnRates[1];
                Networker.DoorToSomewhereNetworker.SpawnWeight2.Value = SpawnRates[2];
                Networker.DoorToSomewhereNetworker.SpawnWeight3.Value = SpawnRates[3];
                Networker.DoorToSomewhereNetworker.SpawnWeight4.Value = SpawnRates[4];
                Networker.DoorToSomewhereNetworker.SpawnWeightMax.Value = SpawnRates[5];
                */
                Networker.DoorToSomewhereNetworker.SpawnRateDynamic.Value = DoorToSomewhereBase.DynamicSpawnRate;
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }
}
