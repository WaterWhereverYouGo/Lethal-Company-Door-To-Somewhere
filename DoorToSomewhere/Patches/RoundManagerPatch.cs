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
using HarmonyLib.Tools;
using DoorToSomewhereMod;
using DoorToSomewhereMod.Networker;
using DoorToSomewhereMod.Object;
using System.Runtime.InteropServices;
using DoorToSomewhereMod.Logger;

namespace DoorToSomewhereMod.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        private static int numSpawnedDoorsToSomewhere = 0;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(ref RoundManager __instance, Vector3 mainEntrancePosition)
        {
            try
            {
                // Patch SetExitIDs.

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }

        [HarmonyPatch("SetExitIDs")]
        [HarmonyPostfix]
        static void SetExitIDsPatch(ref RoundManager __instance, Vector3 mainEntrancePosition)
        {
            try
            {
                DoorToSomewhere.allDoors = new List<DoorToSomewhere>();

                // Ensure dungeon is not custom.
                Dungeon dungeon = __instance.dungeonGenerator.Generator.CurrentDungeon;
                if (!dungeon.DungeonFlow.name.StartsWith("Level1") && !dungeon.DungeonFlow.name.StartsWith("Level2"))
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} will not spawn doors to somewhere in custom dungeons.");
                    return;
                }

                // Spawn doors with weighted spawn rates.
                SpawnDoors(ref __instance);

                // Set doors with valid positions.
                SetDoors(ref __instance, mainEntrancePosition);

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }

        static void SpawnDoors(ref RoundManager __instance)
        {
            try
            {
                // Spawn doors with weighted spawn rates.
                int[] spawnRates = DoorToSomewhereNetworker.SpawnWeights.Value;

                // Calculate total weight.
                int totalWeight = 0;
                foreach (int rate in spawnRates)
                {
                    totalWeight += rate;
                }

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} total spawn rate weight is {totalWeight}.");

                // Get a random number from 1 to total weight, inclusive.
                System.Random rand = new System.Random(StartOfRound.Instance.randomMapSeed + 42);
                int randomWeight = rand.Next(1, totalWeight + 1);

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} random spawn rate weight is {randomWeight}.");

                // Find the spawn rate where the random weight is less than or equal to the accumulated weight.
                int accumulatedWeight = 0;

                for (int i = 0; i < spawnRates.Length; i++)
                {
                    accumulatedWeight += spawnRates[i];
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} accumulated spawn rate weight is {accumulatedWeight}.");

                    if (randomWeight <= accumulatedWeight)
                    {
                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} number of spawned doors to somewhere is {i}.");
                        numSpawnedDoorsToSomewhere = i;
                        break;
                    }
                }

                // There's more real estate, so let's put more doors, so long as we want it dynamic.
                if (DoorToSomewhereNetworker.SpawnRateDynamic.Value)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} spawn rate is dynamic.");

                    Dungeon dungeon = __instance.dungeonGenerator.Generator.CurrentDungeon;
                    int dungeonSize = dungeon.AllTiles.Count;
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} number of tiles in dungeon is {dungeonSize}.");

                    // Just have a 50% chance of adding one more door or not. 
                    //numSpawnedDoorsToSomewhere += rand.Next(0, 2)

                }

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e); 
                throw e;
            }

        }

        static void SetDoors(ref RoundManager __instance, Vector3 mainEntrancePosition)
        {
            try
            {
                // Find valid positions for doors.
                List<Doorway> validDoorways = new List<Doorway>();

                Dungeon dungeon = __instance.dungeonGenerator.Generator.CurrentDungeon;
                foreach (Tile tile in dungeon.AllTiles)
                {
                    foreach (Doorway doorway in tile.UnusedDoorways)
                    {
                        // Don't use doorway if a door is already present.
                        if (doorway.HasDoorPrefabInstance)
                        {
                            continue;
                        }

                        // Don't use doorway if there is no component necessary for creating the game object.
                        if (doorway.GetComponentInChildren<SpawnSyncedObject>(true) == null)
                        {
                            continue;
                        }

                        GameObject sillyDoorContainer = doorway.GetComponentInChildren<SpawnSyncedObject>(true).transform.parent.gameObject;

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} door container name: {sillyDoorContainer.name}");

                        /*
                        if (!sillyDoorContainer.name.StartsWith("SillyDoorContainer")) { continue; }
                        if (sillyDoorContainer.activeSelf) { continue; }   
                        */


                        Matrix4x4 rotationMatrix = Matrix4x4.TRS(doorway.transform.position, doorway.transform.rotation, Vector3.one);
                        Bounds bounds = new Bounds(new Vector3(0f, 1.5f, 5.5f), new Vector3(2f, 6f, 8f));
                        bounds.center = rotationMatrix.MultiplyPoint3x4(bounds.center);
                        Collider[] badPositionCheck = Physics.OverlapBox(bounds.center, bounds.extents, doorway.transform.rotation, LayerMask.GetMask("Room", "Railing", "MapHazards"));
                        bool badPosition = false;

                        // Check for colliders and prevent spawning a door in a bad position.
                        foreach (Collider collider in badPositionCheck)
                        {
                            badPosition = true;
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} bad position, found collider: {collider}");
                            break;
                        }

                        if (badPosition)
                        {
                            continue;
                        }

                        // Good doorway!
                        validDoorways.Add(doorway);
                    }
                }
                        
                // Shuffle valid doorways for random selection.
                Shuffle<Doorway>(validDoorways, StartOfRound.Instance.randomMapSeed + 42);

                List<Vector3> doorToSomewhereLocations = new List<Vector3>();
                int currentIndex = 0;

                // Set doors to locations.
                foreach (Doorway doorway in validDoorways)
                {
                    if (currentIndex >= numSpawnedDoorsToSomewhere)
                    {
                        // Finished setting doors.
                        return;
                    }

                    bool locationUsed = false;
                    Vector3 newLocation = doorway.transform.position + 5 * doorway.transform.forward;
                    foreach (Vector3 doorLocation in doorToSomewhereLocations)
                    {
                        if (Vector3.Distance(doorLocation, newLocation) < 4f)
                        {
                            locationUsed = true;
                            break;
                        }
                    }

                    if (locationUsed)
                    {
                        // Doors too close to eachother.
                        continue;
                    }

                    doorToSomewhereLocations.Add(newLocation);

                    GameObject sillyDoorContainer = doorway.GetComponentInChildren<SpawnSyncedObject>(true).transform.parent.gameObject;
                    GameObject sillyDoor = GameObject.Instantiate(DoorToSomewhereBase.DoorToSomewherePrefab, doorway.transform);
                    sillyDoor.transform.position = sillyDoorContainer.transform.position;
                    DoorToSomewhere doorToSomewhere = sillyDoor.GetComponent<DoorToSomewhere>();

                    if (DoorToSomewhereNetworker.SpawnWeights.Value[5] == 4242)
                    {
                        // Spawn a door by the ship for testing.
                        if (currentIndex == 0) { sillyDoor.transform.position = new Vector3(-7f, 0f, -10f); }
                    }

                    // Keep track of networking with indexs for each door.
                    DoorToSomewhere.allDoors.Add(doorToSomewhere);
                    doorToSomewhere.mimicIndex = currentIndex;
                    currentIndex++;

                    // Turn off the wall behind the door to somewhere.
                    GameObject wall = doorway.transform.GetChild(0).gameObject;
                    wall.SetActive(false);

                    // This might not be necessary!!
                    foreach (Collider collider in Physics.OverlapBox(doorToSomewhere.frameBox.bounds.center, doorToSomewhere.frameBox.bounds.extents, Quaternion.identity))
                    {
                        if (collider.gameObject.name.Contains("Shelf"))
                        {
                            collider.gameObject.SetActive(false);
                        }
                    }

                    MeshRenderer[] meshes = sillyDoor.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mesh in meshes)
                    {
                        foreach (Material material in mesh.materials)
                        {
                            // Make sure the wall shader is shared.
                            material.shader = wall.GetComponentInChildren<MeshRenderer>(true).material.shader;
                            material.renderQueue = wall.GetComponentInChildren<MeshRenderer>(true).material.renderQueue;
                        }
                    }

                    doorToSomewhere.interactTrigger.onInteract = new InteractEvent();
                    doorToSomewhere.interactTrigger.onInteract.AddListener(doorToSomewhere.TouchDoor);

                    // Could randomly assign different properties for door here.

                }

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e); 
                throw e;
            }
        }

        public static void Shuffle<T>(IList<T> list, int seed)
        {
            var rng = new System.Random(seed);
            int n = list.Count;

            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} shuffling a count of: {n}");

            while (n > 1)
            {
                int k = (rng.Next(0, n));
                n--;
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}
