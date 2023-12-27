using HarmonyLib;
using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using DoorToSomewhereMod.Logger;

namespace DoorToSomewhereMod.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(ref GameObject __DoorToSomewhereNetworkerPrefab)
        {
            try
            {
                // Register the networker prefab.
                GameNetworkManager.Instance.GetComponent<NetworkManager>().AddNetworkPrefab(__DoorToSomewhereNetworkerPrefab);
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }
}
