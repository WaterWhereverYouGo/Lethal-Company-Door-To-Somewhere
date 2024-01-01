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
using System.Net.Sockets;

namespace DoorToSomewhereMod.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        static Dungeon dungeon;
        static Doorway doorwayToUse;
        static System.Random random = null;
        private static int numSpawnedDoorsToSomewhere = 0;

        static List<GameObjectWeight> ConnectorPrefabWeights = null;
        static List<Doorway> validDoorways = null;

        [HarmonyPatch("SetExitIDs")]
        [HarmonyPostfix]
        static void SetExitIDsPatch(ref RoundManager __instance)
        {
            try
            {
                DoorToSomewhere.allDoors = new List<DoorToSomewhere>();

                // Ensure dungeon is not custom.
                dungeon = __instance.dungeonGenerator.Generator.CurrentDungeon;
                if (!dungeon.DungeonFlow.name.StartsWith("Level1") && !dungeon.DungeonFlow.name.StartsWith("Level2"))
                {
                    DoorToSomewhereBase.logger.LogInfo($"Will not spawn doors to somewhere in custom dungeons.");
                    return;
                }

                // Setup Random object.
                random = new System.Random(StartOfRound.Instance.randomMapSeed + 42);

                DoorToSomewhereBase.logger.LogInfo($"Calling SetupDoorPrefab.");
                SetupDoorPrefab();

                DoorToSomewhereBase.logger.LogInfo($"Calling CalculateNumberOfDoorsToSpawn.");
                CalculateNumberOfDoorsToSpawn();

                DoorToSomewhereBase.logger.LogInfo($"Calling FindValidDoorways.");
                FindValidDoorways();

                DoorToSomewhereBase.logger.LogInfo($"Calling SetDoors.");
                SetDoors();

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }

        static void SetupDoorPrefab()
        {
            try
            {
                GameObject normalDoorPrefab = null;
                UnityEngine.Component[] components;

                // Lets get a door prefab to use for when we set the door.
                foreach (Tile tile in dungeon.AllTiles)
                {
                    foreach (Doorway doorway in tile.UsedDoorways)
                    {

                        // Doorway should have a list of connector prefab weights.
                        if (doorway.ConnectorPrefabWeights.Count > 0)
                        {
                            ConnectorPrefabWeights = doorway.ConnectorPrefabWeights;
                            DoorToSomewhereBase.logger.LogInfo($"Connector objects: {string.Join(", ", ConnectorPrefabWeights)}.");
                        }
                        else
                        {
                            continue;
                        }

                        if (!doorway.HasDoorPrefabInstance)
                        {
                            // Didn't find a door prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway did not have a prefab instance. {doorway}.");
                            continue;
                        }

                        if (doorway.DoorComponent == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway had null door component. {doorway}.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Doorway has a door. {doorway}.");

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
                    DoorToSomewhereBase.logger.LogInfo($"Failed to find a normal door prefab.");
                    throw new Exception("Failed to find a normal door prefab");
                }

                DoorToSomewhereBase.DoorToSomewherePrefab = normalDoorPrefab;

                DoorToSomewhereBase.logger.LogInfo($"Components in doorway to use as template:");
                components = doorwayToUse.GetComponents<UnityEngine.Component>();
                foreach (UnityEngine.Component component in components)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Normal door component {component}");
                }

                components = doorwayToUse.GetComponentsInChildren<UnityEngine.Component>();
                foreach (UnityEngine.Component component in components)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Normal door child component {component}");
                }

                DoorToSomewhereBase.logger.LogInfo($"Finished finding normal door prefab.");

                //GameObject doorToSomewhereNetworker = GameObject.Instantiate(DoorToSomewhereBase.DoorToSomewhereNetworkerPrefab);
                //doorToSomewhereNetworker.GetComponent<NetworkObject>().Spawn(true);
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
                throw;
            }
        }

        static void CalculateNumberOfDoorsToSpawn()
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Calculating doors to spawn.");

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

                //DoorToSomewhereBase.logger.LogInfo($"Total spawn rate weight is {totalWeight}.");

                // Get a random number from 1 to total weight, inclusive.
                int randomWeight = random.Next(1, totalWeight + 1);

                //DoorToSomewhereBase.logger.LogInfo($"Random spawn rate weight is {randomWeight}.");

                // Find the spawn rate where the random weight is less than or equal to the accumulated weight.
                int accumulatedWeight = 0;

                for (int i = 0; i < spawnRates.Length; i++)
                {
                    accumulatedWeight += spawnRates[i];
                    //DoorToSomewhereBase.logger.LogInfo($"Accumulated spawn rate weight is {accumulatedWeight}.");

                    if (randomWeight <= accumulatedWeight)
                    {
                        //DoorToSomewhereBase.logger.LogInfo($"Number of spawned doors to somewhere is {i}.");
                        numSpawnedDoorsToSomewhere = i;
                        break;
                    }
                }

                // There's more real estate, so let's put more doors, so long as we want it dynamic.
                if (DoorToSomewhereNetworker.SpawnRateDynamic.Value)
                {
                    //DoorToSomewhereBase.logger.LogInfo($"Spawn rate is dynamic.");

                    int dungeonSize = dungeon.AllTiles.Count;
                    //DoorToSomewhereBase.logger.LogInfo($"Number of tiles in dungeon is {dungeonSize}.");

                    // Just have a 50% chance of adding one more door or not. 
                    //numSpawnedDoorsToSomewhere += rand.Next(0, 2)

                }

                DoorToSomewhereBase.logger.LogInfo($"Finished calculating number of doors to spawn {numSpawnedDoorsToSomewhere}.");

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e); 
                throw;
            }

        }
        static void FindValidDoorways()
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Finding valid doorways.");
                validDoorways = new List<Doorway>();

                foreach (Tile tile in dungeon.AllTiles)
                {
                    foreach (Doorway doorway in tile.UnusedDoorways)
                    {
                        // Don't use doorway if a door is already present.
                        if (doorway.HasDoorPrefabInstance)
                        {
                            //DoorToSomewhereBase.logger.LogInfo($"Doorway had a prefab instance. Continue to search.");
                            continue;
                        }

                        // Don't use doorway if there is no component necessary for creating the game object.
                        if (doorway.GetComponentInChildren<SpawnSyncedObject>(true) == null)
                        {
                            //DoorToSomewhereBase.logger.LogInfo($"Doorway did not have a component for creating the game object. Continue to search.");
                            continue;
                        }

                        GameObject sillyDoorContainer = doorway.GetComponentInChildren<SpawnSyncedObject>(true).transform.parent.gameObject;

                        if ( sillyDoorContainer == null )
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has a null game object for its SpawnSynchedObject. Continue to search.");
                            continue;
                        }
                        
                        try
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Door container name: {sillyDoorContainer.name}");
                        }
                        catch (Exception e)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Door container does not have a name. {e.Message}");
                            continue;
                        }


                        /*
                        if (!sillyDoorContainer.name.StartsWith("SillyDoorContainer")) { continue; }
                        if (sillyDoorContainer.activeSelf) { continue; }   
                        */
                        /*
                        Matrix4x4 rotationMatrix = Matrix4x4.TRS(doorway.transform.position, doorway.transform.rotation, Vector3.one);
                        Bounds bounds = new Bounds(new Vector3(0f, 1.5f, 5.5f), new Vector3(2f, 6f, 8f));
                        bounds.center = rotationMatrix.MultiplyPoint3x4(bounds.center);
                        Collider[] badPositionCheck = Physics.OverlapBox(bounds.center, bounds.extents, doorway.transform.rotation, LayerMask.GetMask("Room", "Railing", "MapHazards"));
                        bool badPosition = false;

                        //DoorToSomewhereBase.logger.LogInfo($"Calculated. Before collider check.");

                        // Check for colliders and prevent spawning a door in a bad position.
                        foreach (Collider collider in badPositionCheck)
                        {
                            badPosition = true;
                            //DoorToSomewhereBase.logger.LogInfo($"Bad position, found collider: {collider}");
                            break;
                        }

                        if (badPosition)
                        {
                            //DoorToSomewhereBase.logger.LogInfo($"Bad position. Continue to search.");
                            continue;
                        }
                        */
                        DoorToSomewhereBase.logger.LogInfo($"Added doorway. {doorway}.");

                        // Good doorway!
                        validDoorways.Add(doorway);
                    }
                }

                DoorToSomewhereBase.logger.LogInfo($"Found {validDoorways.Count} valid doorways.");

            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
                throw;
            }
        }

        static void SetDoors()
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Setting doors.");

                DoorToSomewhereBase.logger.LogInfo($"Shuffling doorways.");

                // Shuffle valid doorways for random selection.
                Shuffle<Doorway>(validDoorways, StartOfRound.Instance.randomMapSeed + 42);

                List<Vector3> doorToSomewhereLocations = new List<Vector3>();
                int currentIndex = 0;

                DoorToSomewhereBase.logger.LogInfo($"Starting to set doors.");

                // Set doors to locations.
                foreach (Doorway doorway in validDoorways)
                {
                    try
                    {
                        if (currentIndex >= numSpawnedDoorsToSomewhere)
                        {
                            // Finished setting doors.
                            DoorToSomewhereBase.logger.LogInfo($"Finished spawning {numSpawnedDoorsToSomewhere} doors.");
                            return;
                        }

                        bool locationUsed = false;
                        Vector3 newLocation = doorway.transform.position;
                        foreach (Vector3 doorLocation in doorToSomewhereLocations)
                        {
                            if (Vector3.Distance(doorLocation, newLocation) < 4f)
                            {
                                DoorToSomewhereBase.logger.LogInfo($"Doorway already used {newLocation} and {doorLocation}.");
                                locationUsed = true;
                                break;
                            }
                        }

                        if (locationUsed)
                        {
                            // Doors too close to eachother.
                            DoorToSomewhereBase.logger.LogInfo($"Door too close to another door, moving along.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"New location {newLocation}.");
                        doorToSomewhereLocations.Add(newLocation);

                        Transform currentTransform = doorway.transform;
                        DoorToSomewhereBase.logger.LogInfo($"Current transform doorway {currentTransform.position} {currentTransform.rotation}.");

                        UnityEngine.Component[] components = doorway.GetComponentsInChildren<UnityEngine.Component>();
                        foreach (UnityEngine.Component component in components)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"New door child component {component}");
                        }

                        // Should remove the Blocker and Cube from the child components.
                        for (int i = 0; i < components.Length; i++)
                        {
                            UnityEngine.Component component = components[i];
                            if (component.name.Contains("Blocker"))
                            {
                                DoorToSomewhereBase.logger.LogInfo($"New door removing child component {component}");
                                component.gameObject.SetActive(false);
                            }
                        }

                        GameObject randomConnectorPrefab = GameObject.Instantiate(ConnectorPrefabWeights[random.Next(ConnectorPrefabWeights.Count)].GameObject);
                        DoorToSomewhereBase.logger.LogInfo($"Random connector prefab {randomConnectorPrefab}.");


                        Door door = randomConnectorPrefab.GetComponent<Door>();

                        if (door == null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"No door found, so add one.");
                            door = randomConnectorPrefab.AddComponent<Door>();
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Start traversing.");

                        var traverse = Traverse.Create(doorway);
                        traverse.Property("ConnectedDoorway").SetValue(doorway);

                        DoorToSomewhereBase.logger.LogInfo($"Done traversing.");

                        door.Dungeon = dungeon;
                        door.DoorwayA = doorway;
                        door.DoorwayB = doorway;
                        door.TileA = doorway.Tile;
                        door.TileB = doorway.Tile;

                        try
                        {
                            DungeonUtil.AddAndSetupDoorComponent(dungeon, randomConnectorPrefab, doorway);
                            DoorToSomewhereBase.logger.LogInfo($"Wow it worked.");
                        }
                        catch (Exception e)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"That failed... {e.Message}");
                        }

                        traverse.Method("SetUsedPrefab", new Type[] { typeof(GameObject) }, new object[] { randomConnectorPrefab });

                        var method = traverse.Method("SetUsedPrefab", new Type[] { typeof(GameObject) }, new object[] { randomConnectorPrefab });

                        if (method != null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Method not null.");
                            _ = method.GetValue();
                        }    
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Method null.");
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Check before silly Door.");

                        if (!doorway.HasDoorPrefabInstance)
                        {
                            // Didn't find a door prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway does not have a prefab instance. {doorway}.");
                        }

                        if (doorway.UsedDoorPrefabInstance == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has null door prefab instance. {doorway}.");
                        }
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has door prefab instance. {doorway.UsedDoorPrefabInstance}.");
                        }

                        if (doorway.DoorComponent == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has null door component. {doorway}.");
                        }
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has door component. {doorway.DoorComponent}.");
                        }

                        if (doorway.Socket == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has null socket. {doorway}.");
                        }
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has socket. {doorway.Socket}.");
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Start with silly Door.");
                        /*
                        GameObject sillyDoorContainer = doorway.GetComponentInChildren<SpawnSyncedObject>(true).transform.parent.gameObject;
                        GameObject sillyDoor = GameObject.Instantiate(randomConnectorPrefab, doorway.transform);
                        sillyDoor.transform.position = sillyDoorContainer.transform.position;
                        sillyDoor.transform.rotation = sillyDoorContainer.transform.rotation;

                        sillyDoor.AddComponent<DoorToSomewhere>();
                        DoorToSomewhere doorToSomewhere = sillyDoor.GetComponent<DoorToSomewhere>();

                        DoorToSomewhereBase.logger.LogInfo($"Current transform {sillyDoor.transform.position} {sillyDoor.transform.rotation}.");

                        if (doorToSomewhere == null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Door not instantiated. Failed to get component.");
                            continue;
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Door instantiated.");

                        if (DoorToSomewhereNetworker.SpawnWeights[5].Value == 4242)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Should debug door be added?");
                            // Spawn a door by the ship for testing.
                            if (currentIndex == 0)
                            {
                                sillyDoor.transform.position = new Vector3(-7f, 0f, -10f);
                                DoorToSomewhereBase.logger.LogInfo($"Debug door added.");
                            }
                        }

                        // Keep track of networking with indexs for each door.
                        DoorToSomewhere.allDoors.Add(doorToSomewhere);
                        doorToSomewhere.doorIndex = currentIndex;
                        */

                        components = doorwayToUse.GetComponentsInChildren<UnityEngine.Component>(true);
                        foreach (UnityEngine.Component component in components)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Modified checking child component {component}");
                            if (component.name.Contains("Spawn") || component.name.Contains("Frame"))
                            {
                                if (doorway.gameObject.GetComponent(component.GetType()) && doorway.gameObject.GetComponent(component.GetType()).name == component.name)
                                {
                                    DoorToSomewhereBase.logger.LogInfo($"Modified child component already present {component}");
                                    continue;
                                }
                                DoorToSomewhereBase.logger.LogInfo($"Modified adding child component {component}");
                                doorway.gameObject.AddComponent(component.GetType());
                            }
                        }


                        components = doorway.GetComponentsInChildren<UnityEngine.Component>(true);
                        foreach (UnityEngine.Component component in components)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Modified child component {component}");
                            if (component.gameObject.activeSelf)
                            {
                                DoorToSomewhereBase.logger.LogInfo($"Child component is active {component}");
                            }
                            else
                            {
                                DoorToSomewhereBase.logger.LogInfo($"Child component is inactive {component}");
                                component.gameObject.SetActive(true);
                            }
                        }


                        GameObject sillyDoorContainer = doorway.GetComponentInChildren<SpawnSyncedObject>(true).transform.parent.gameObject;
                        GameObject sillyDoor = GameObject.Instantiate(randomConnectorPrefab, doorway.transform);
                        //sillyDoor.transform.position = sillyDoorContainer.transform.position;
                        //sillyDoor.transform.rotation = sillyDoorContainer.transform.rotation;

                        sillyDoor.AddComponent<DoorToSomewhere>();
                        DoorToSomewhere doorToSomewhere = sillyDoor.GetComponent<DoorToSomewhere>();

                        DoorToSomewhereBase.logger.LogInfo($"Current transform {sillyDoor.transform.position} {sillyDoor.transform.rotation}.");

                        if (doorToSomewhere == null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Door not instantiated. Failed to get component.");
                            continue;
                        }
                        DoorToSomewhere.allDoors.Add(doorToSomewhere);
                        doorToSomewhere.doorIndex = currentIndex;


                        if (DoorToSomewhereNetworker.SpawnWeights[5].Value == 4242)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Should debug door be added?");
                            // Spawn a door by the ship for testing.
                            if (currentIndex == 0)
                            {

                                foreach (UnityEngine.Component component in components)
                                {
                                    component.gameObject.transform.position = new Vector3(-7f, 0f, -10f);
                                }
                                DoorToSomewhereBase.logger.LogInfo($"Debug door added.");
                            }
                        }
                        currentIndex++;

                        DoorToSomewhereBase.logger.LogInfo($"Length of all doors {DoorToSomewhere.allDoors.Count}.");

                        DoorToSomewhereBase.logger.LogInfo($"Messing with the wall behind the door.");

                        // Turn off the wall behind the door to somewhere.
                        GameObject wall = doorway.transform.GetChild(0).gameObject;
                        //wall.SetActive(false);

                        if (wall == null)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Wall is null.");
                        }

                        else if (wall.activeSelf != true)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Wall is not active.");
                        }

                        DoorToSomewhereBase.logger.LogInfo($"Collider checking.");

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

                        DoorToSomewhereBase.logger.LogInfo($"Mesh assignment.");
                        
                        MeshRenderer[] meshes = doorway.GetComponentsInChildren<MeshRenderer>();
                        foreach (MeshRenderer meshRender in meshes)
                        {
                            foreach (Material material in meshRender.materials)
                            {
                                // Make sure the wall shader is shared.
                                material.shader = wall.GetComponentInChildren<MeshRenderer>(true).material.shader;
                                material.renderQueue = wall.GetComponentInChildren<MeshRenderer>(true).material.renderQueue;
                            }
                        }
                        
                        
                        //Mesh mesh = wall.GetComponentInChildren<Mesh>(true);

                        //mesh = Mesh.Instantiate(DoorToSomewhereBase.portalMesh, wall.transform);

                        //wall.GetComponentInChildren<MeshRenderer>(true).material = Material.Instantiate(DoorToSomewhereBase.portalMaterial);

                        DoorToSomewhereBase.logger.LogInfo($"Finished setting door {currentIndex}.");

                        //doorToSomewhere.interactTrigger.onInteract = new InteractEvent();
                        //doorToSomewhere.interactTrigger.onInteract.AddListener(doorToSomewhere.TouchDoor);

                        // Could randomly assign different properties for door here.

                        if (!doorway.HasDoorPrefabInstance)
                        {
                            // Didn't find a door prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway does not have a prefab instance. {doorway}.");
                        }

                        if (doorway.UsedDoorPrefabInstance == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has null door prefab instance. {doorway}.");
                        }
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has door prefab instance. {doorway.UsedDoorPrefabInstance}.");
                        }

                        if (doorway.DoorComponent == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has null door component. {doorway}.");
                        }
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has door component. {doorway.DoorComponent}.");
                        }

                        if (doorway.Socket == null)
                        {
                            // Didn't find the prefab, continue to look.
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has null socket. {doorway}.");
                        }
                        else
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Doorway has socket. {doorway.Socket}.");
                        }

                    }
                    catch (Exception e)
                    {
                        LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
                        DoorToSomewhereBase.logger.LogInfo($"Failed to set door {currentIndex} with {doorway}.");
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

                DoorToSomewhereBase.logger.LogInfo($"Shuffling a count of: {n}");

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
