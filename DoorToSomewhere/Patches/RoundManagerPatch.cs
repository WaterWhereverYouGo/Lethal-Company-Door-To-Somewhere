using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DoorToSomewhereMod.Networker;
using DoorToSomewhereMod.Object;
using DoorToSomewhereMod.Logger;
using Unity.Netcode;
using System.ComponentModel;

namespace DoorToSomewhereMod.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        static Doorway doorwayToUse;
        private static int numSpawnedDoorsToSomewhere = 0;

        [HarmonyPatch("SetExitIDs")]
        [HarmonyPostfix]
        static void SetExitIDsPatch(ref RoundManager __instance)
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

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} calling SetupDoorPrefab.");

                // Obtain a normal door prefab in the dungeon to use.
                SetupDoorPrefab(ref __instance);

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} calling CalculateNumberOfDoorsToSpawn.");

                // Calculate number of doors to spawn with weighted spawn rates.
                CalculateNumberOfDoorsToSpawn(ref __instance);

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} calling SetDoors.");

                // Set doors with valid positions.
                SetDoors(ref __instance);

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }

        static void SetupDoorPrefab(ref RoundManager __instance)
        {
            try
            {

                Dungeon dungeon = __instance.dungeonGenerator.Generator.CurrentDungeon;
                GameObject normalDoorPrefab = null;

                // Lets get a door prefab to use for when we set the door.
                foreach (Tile tile in dungeon.AllTiles)
                {
                    foreach (Doorway doorway in tile.AllDoorways)
                    {
                        if (!doorway.HasDoorPrefabInstance)
                        {
                            // Didn't find a door prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway did not have a prefab instance. {doorway}.");
                            continue;
                        }

                        if (doorway.UsedDoorPrefabInstance == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway had null door prefab instance. {doorway}.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway has GameObject. {doorway}.");

                        if (doorway.DoorComponent == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway had null door component. {doorway}.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway has a door. {doorway}.");

                        normalDoorPrefab = doorway.UsedDoorPrefabInstance;
                        doorwayToUse = doorway;
                        break;
                    }

                    if (normalDoorPrefab != null)
                    {
                        break;
                    }    
                }

                if (normalDoorPrefab == null)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} failed to find a normal door prefab.");
                    throw new Exception("Failed to find a normal door prefab.");
                }

                DoorToSomewhereBase.DoorToSomewherePrefab = normalDoorPrefab;

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} components in doorway to use as template:");
                UnityEngine.Component[] components = doorwayToUse.GetComponents<UnityEngine.Component>();
                foreach (UnityEngine.Component component in components)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} component {component}");
                }

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} finished finding normal door prefab.");

                // Create networker with networker prefab.
                foreach (var component in normalDoorPrefab.GetComponents<NetworkObject>())
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} found NetworkObject {component}.");
                }

                foreach (var component in normalDoorPrefab.GetComponents<NetworkBehaviour>())
                {
                    DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} found NetworkBehaviour {component}.");
                }
                //GameObject doorToSomewhereNetworker = GameObject.Instantiate(DoorToSomewhereBase.DoorToSomewhereNetworkerPrefab);
                //doorToSomewhereNetworker.GetComponent<NetworkObject>().Spawn(true);
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
                throw;
            }
        }

        static void CalculateNumberOfDoorsToSpawn(ref RoundManager __instance)
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} calculating doors to spawn.");

                // Spawn doors with weighted spawn rates.
                int[] spawnRates = new int[DoorToSomewhereNetworker.SpawnWeights.Length];
                
                for (int i = 0; i < DoorToSomewhereNetworker.SpawnWeights.Length; i++)
                {
                    spawnRates[i] = DoorToSomewhereNetworker.SpawnWeights[i].Value;
                }

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

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} finished calculating number of doors to spawn {numSpawnedDoorsToSomewhere}.");

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e); 
                throw;
            }

        }

        static void SetDoors(ref RoundManager __instance)
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} setting doors.");

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
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway had a prefab instance. Continue to search.");
                            continue;
                        }

                        // Don't use doorway if there is no component necessary for creating the game object.
                        if (doorway.GetComponentInChildren<SpawnSyncedObject>(true) == null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway did not have a component for creating the game object. Continue to search.");
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

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} calculated. Before collider check.");

                        // Check for colliders and prevent spawning a door in a bad position.
                        foreach (Collider collider in badPositionCheck)
                        {
                            badPosition = true;
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} bad position, found collider: {collider}");
                            break;
                        }

                        if (badPosition)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} bad position. Continue to search.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} added doorway. {doorway}.");

                        // Good doorway!
                        validDoorways.Add(doorway);
                    }
                }

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} shuffling doorways.");

                // Shuffle valid doorways for random selection.
                Shuffle<Doorway>(validDoorways, StartOfRound.Instance.randomMapSeed + 42);

                List<Vector3> doorToSomewhereLocations = new List<Vector3>();
                int currentIndex = 0;

                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} starting to set doors.");

                // Set doors to locations.
                foreach (Doorway doorway in validDoorways)
                {
                    try
                    {
                        if (currentIndex >= numSpawnedDoorsToSomewhere)
                        {
                            // Finished setting doors.
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} finished spawning {numSpawnedDoorsToSomewhere} doors.");
                            return;
                        }

                        bool locationUsed = false;
                        Vector3 newLocation = doorway.transform.position + 5 * doorway.transform.forward;
                        foreach (Vector3 doorLocation in doorToSomewhereLocations)
                        {
                            if (Vector3.Distance(doorLocation, newLocation) < 4f)
                            {
                                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} doorway already used {newLocation} and {doorLocation}.");
                                locationUsed = true;
                                break;
                            }
                        }

                        if (locationUsed)
                        {
                            // Doors too close to eachother.
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} door too close to another door, moving along.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} components in doorway to modify:");
                        UnityEngine.Component[] components = doorway.GetComponents<UnityEngine.Component>();
                        foreach (UnityEngine.Component component in components)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} component {component}");
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} adding location {newLocation}.");
                        doorToSomewhereLocations.Add(newLocation);

                        GameObject sillyDoorContainer = doorway.GetComponentInChildren<SpawnSyncedObject>(true).transform.parent.gameObject;
                        GameObject sillyDoor = GameObject.Instantiate(DoorToSomewhereBase.DoorToSomewherePrefab, doorway.transform);
                        sillyDoor.transform.position = sillyDoorContainer.transform.position;
                        sillyDoor.AddComponent<DoorToSomewhere>();
                        DoorToSomewhere doorToSomewhere = sillyDoor.GetComponent<DoorToSomewhere>();

                        if (doorToSomewhere == null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} door not instantiated. Failed to get component.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} door instantiated.");

                        if (DoorToSomewhereNetworker.SpawnWeights[5].Value == 4242)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} should debug door be added?");
                            // Spawn a door by the ship for testing.
                            if (currentIndex == 0)
                            {
                                sillyDoor.transform.position = new Vector3(-7f, 0f, -10f);
                                DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} debug door added.");
                            }
                        }

                        // Keep track of networking with indexs for each door.
                        DoorToSomewhere.allDoors.Add(doorToSomewhere);
                        doorToSomewhere.doorIndex = currentIndex;
                        currentIndex++;

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} length of all doors {DoorToSomewhere.allDoors.Count}.");

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} messing with the wall behind the door.");

                        // Turn off the wall behind the door to somewhere.
                        GameObject wall = doorway.transform.GetChild(0).gameObject;
                        //wall.SetActive(false);

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} collider checking.");

                        // This might not be necessary!!
                        /*
                        foreach (Collider collider in Physics.OverlapBox(doorToSomewhere.frameBox.bounds.center, doorToSomewhere.frameBox.bounds.extents, Quaternion.identity))
                        {
                            if (collider.gameObject.name.Contains("Shelf"))
                            {
                                collider.gameObject.SetActive(false);
                            }
                        }
                        */

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} mesh assignment.");

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

                        wall.GetComponentInChildren<MeshRenderer>(true).material = Material.Instantiate(DoorToSomewhereBase.portalMaterial);

                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} finished setting door {currentIndex}.");

                        //doorToSomewhere.interactTrigger.onInteract = new InteractEvent();
                        //doorToSomewhere.interactTrigger.onInteract.AddListener(doorToSomewhere.TouchDoor);

                        // Could randomly assign different properties for door here.
                    }
                    catch (Exception e)
                    {
                        LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
                        DoorToSomewhereBase.logger.LogInfo($"Plugin {DoorToSomewhereBase.modName} failed to set door {currentIndex} with {doorway}.");
                        continue;
                    }
                }

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e); 
                throw;
            }
        }

        public static void Shuffle<T>(IList<T> list, int seed)
        {
            try
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
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
    }
}
