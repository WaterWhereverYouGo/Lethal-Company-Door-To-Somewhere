using Unity.Netcode;
using DoorToSomewhereMod.Object;
using UnityEngine;
using DoorToSomewhereMod.Logger;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Runtime.CompilerServices;

namespace DoorToSomewhereMod.Networker
{
    [RequireComponent(typeof(InteractTrigger))]
    public class DoorToSomewhereNetworker : DoorLock
    {
        public static DoorToSomewhereNetworker Instance;

        public AudioSource teleporterAudio;
        public AudioClip teleporterBeamUpSFX;
        public AudioClip teleporterPrimeSFX;

        public float teleporterChargeUp = 2f;

        private List<PlayerControllerB> teleportingPlayers = new List<PlayerControllerB>();
        private List<EnemyAI> teleportingEnemies = new List<EnemyAI>();

        public static NetworkVariable<int>[] SpawnWeights = new NetworkVariable<int>[] {null};
        public static NetworkVariable<bool> SpawnRateDynamic = new NetworkVariable<bool>();

        public void OnTriggerEnter(Collider entity)
        {
            try
            {
                if (entity.CompareTag("Player"))
                {
                    OnTeleportPlayer(entity);
                }
                else if (entity.CompareTag("Enemy"))
                {
                    OnTeleportEnemy(entity);
                }
                else
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Unknown entity {entity}.");
                }
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
        private void OnTeleportPlayer(Collider entity)
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Attempting to teleport player.");

                PlayerControllerB playerControllerB = entity.gameObject.GetComponent<PlayerControllerB>();

                // Don't teleport player entity if the player controller is null.
                if (playerControllerB == null)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Player controller is null.");
                    return;
                }

                // Don't teleport player entity if the player controller does not match up.
                if (playerControllerB != GameNetworkManager.Instance.localPlayerController)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Player controller is not the local player controller.");
                    return;
                }

                // Don't teleport player entity if the player controller is already being teleported.
                if (teleportingPlayers.Contains(playerControllerB))
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Player controller is already being teleported.");
                    return;
                }

                // Don't teleport player entity if there are no locations to telport to.
                if (RoundManager.Instance.insideAINodes.Length == 0)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Nowhere to teleport to.");
                    return;
                }

                // Teleport player.
                teleportingPlayers.Add(playerControllerB);

                Vector3 position3 = FindNewPosition();

                if (position3 == Vector3.zero)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: New position is invalid.");
                    return;
                }

                teleporterAudio.PlayOneShot(teleporterPrimeSFX);
                playerControllerB.movementAudio.PlayOneShot(teleporterPrimeSFX);

                if (playerControllerB.deadBody != null)
                {
                    StartCoroutine(TeleportPlayerBodyCoroutine((int)playerControllerB.playerClientId, position3));
                    return;
                }
                StartCoroutine(TeleportPlayerCoroutine((int)playerControllerB.playerClientId, position3));
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }
        private void OnTeleportEnemy(Collider entity)
        {
            try
            {
                DoorToSomewhereBase.logger.LogInfo($"Attempting to teleport enemy.");

                EnemyAICollisionDetect enemyAICollision = entity.gameObject.GetComponent<EnemyAICollisionDetect>();

                // Don't teleport enemy entity if the enemy collision is null.
                if (enemyAICollision == null)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Enemy collision is null.");
                    return;
                }

                EnemyAI enemyAI = enemyAICollision.mainScript;

                // Don't teleport enemy entity if the enemy is already being teleported.
                if (teleportingEnemies.Contains(enemyAI))
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Enemy is already being teleported.");
                    return;
                }

                // Don't teleport enemy entity if there are no locations to telport to.
                if (RoundManager.Instance.insideAINodes.Length == 0)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Nowhere to teleport to.");
                    return;
                }

                // Teleport enemy.
                teleportingEnemies.Add(enemyAI);

                Vector3 position3 = FindNewPosition();

                if (position3 == Vector3.zero)
                {
                    DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: New position is invalid.");
                    return;
                }

                teleporterAudio.PlayOneShot(teleporterPrimeSFX);

                StartCoroutine(TeleportEnemyCoroutine(enemyAI, position3));
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
            }
        }

        private Vector3 FindNewPosition()
        {
            try
            {
                Vector3 newPosition = Vector3.zero;

                int maxAttemptsToFindPosition = 10;
                System.Random rand = new System.Random(StartOfRound.Instance.randomMapSeed + 42);
                for (int attempt = 0; attempt < maxAttemptsToFindPosition; attempt++)
                {
                    int randomNodeIndex = rand.Next(0, RoundManager.Instance.insideAINodes.Length);
                    Vector3 tempPosition = RoundManager.Instance.insideAINodes[randomNodeIndex].transform.position;
                    tempPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(tempPosition);

                    DoorToSomewhereBase.logger.LogInfo($"Possible teleport position {tempPosition}.");

                    // Is this new position certain death that will kill the player?
                    if (Physics.Raycast(tempPosition, Vector3.down, out var hitInfo, 80f, 268437760, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 targetFloorPosition = hitInfo.point;
                        DoorToSomewhereBase.logger.LogInfo($"Target floor position {targetFloorPosition}.");

                        float differenceInHeight = Math.Abs(targetFloorPosition.z - tempPosition.z);

                        if (differenceInHeight > 10)
                        {
                            DoorToSomewhereBase.logger.LogInfo($"Possible teleport position looks like certain death. Lets keep looking.");
                            continue;
                        }
                    }
                    else
                    {
                        DoorToSomewhereBase.logger.LogInfo($"Failed to get raycast. Lets keep looking.");
                        continue;
                    }

                    // Good position!
                    newPosition = tempPosition;
                    break;
                }

                return newPosition;
            }
            catch (Exception e)
            {
                LocalLogger.LogException(MethodBase.GetCurrentMethod(), e);
                return Vector3.zero;
            }
        }

        // Coroutines.
        public IEnumerator TeleportPlayerCoroutine(int playerId, Vector3 newPosition)
        {
            yield return new WaitForSeconds(teleporterChargeUp);
            TeleportPlayer(playerId, newPosition);

            teleportingPlayers.Remove(StartOfRound.Instance.allPlayerScripts[playerId]);
            TeleportPlayerServerRpc(playerId, newPosition);
        }
        public IEnumerator TeleportPlayerBodyCoroutine(int playerId, Vector3 newPosition)
        {
            yield return new WaitForSeconds(teleporterChargeUp);
            TeleportPlayerBodyServerRpc(playerId, newPosition);

            teleportingPlayers.Remove(StartOfRound.Instance.allPlayerScripts[playerId]);
            StartCoroutine(TeleportPlayerBody(playerId, newPosition));
        }

        public IEnumerator TeleportEnemyCoroutine(EnemyAI enemy, Vector3 newPosition)
        {
            yield return new WaitForSeconds(teleporterChargeUp);
            TeleportEnemy(enemy, newPosition);

            teleportingEnemies.Remove(enemy);
            TeleportEnemyServerRpc(enemy.NetworkObject, newPosition);
        }

        // Server RPCs.
        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerServerRpc(int playerId, Vector3 newPosition)
        {
            TeleportPlayerClientRpc(playerId, newPosition);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportEnemyServerRpc(NetworkObjectReference enemy, Vector3 newPosition)
        {
            TeleportEnemyClientRpc(enemy, newPosition);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerBodyServerRpc(int playerId, Vector3 newPosition)
        {
            TeleportPlayerBodyClientRpc(playerId, newPosition);
        }

        // Client RPCs.
        [ClientRpc]
        public void TeleportPlayerClientRpc(int playerId, Vector3 newPosition)
        {
            teleporterAudio.PlayOneShot(teleporterBeamUpSFX);
            StartOfRound.Instance.allPlayerScripts[playerId].movementAudio.PlayOneShot(teleporterBeamUpSFX);
            TeleportPlayer(playerId, newPosition);
        }

        [ClientRpc]
        public void TeleportEnemyClientRpc(NetworkObjectReference enemy, Vector3 newPosition)
        {
            if (!enemy.TryGet(out NetworkObject enemyObj))
            {
                DoorToSomewhereBase.logger.LogInfo($"Failed to teleport: Enemy network object reference was not resolved to a network object.");
                return;
            }

            teleporterAudio.PlayOneShot(teleporterBeamUpSFX);
            TeleportEnemy(enemyObj.GetComponent<EnemyAI>(), newPosition);
        }

        [ClientRpc]
        public void TeleportPlayerBodyClientRpc(int playerId, Vector3 newPosition)
        {
            teleporterAudio.PlayOneShot(teleporterBeamUpSFX);
            StartOfRound.Instance.allPlayerScripts[playerId].movementAudio.PlayOneShot(teleporterBeamUpSFX);
            StartCoroutine(TeleportPlayerBody(playerId, newPosition));
        }

        public static void TeleportPlayer(int playerId, Vector3 newPosition)
        {
            PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];

            // No item dropping woooo. lol.
            //playerControllerB.DropAllHeldItems();

            // If there are reverb presets, then change a preset for the teleporting player.
            if ((bool)FindObjectOfType<AudioReverbPresets>())
            {
                FindObjectOfType<AudioReverbPresets>().audioPresets[2].ChangeAudioReverbForPlayer(playerControllerB);
            }

            // May need to modify these... Thoughts -> Could retain velocity, teleports may go outside or to the ship.
            playerControllerB.isInElevator = false;
            playerControllerB.isInHangarShipRoom = false;
            playerControllerB.isInsideFactory = true;
            playerControllerB.averageVelocity = 0f;
            playerControllerB.velocityLastFrame = Vector3.zero;

            playerControllerB.TeleportPlayer(newPosition);
            playerControllerB.beamOutParticle.Play();

            // Shake the screen for the player that is being teleported.
            if (playerControllerB == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
        }

        public static IEnumerator TeleportPlayerBody(int playerId, Vector3 newPosition)
        {
            float startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => StartOfRound.Instance.allPlayerScripts[playerId].deadBody != null || Time.realtimeSinceStartup - startTime > 2f);
            if (StartOfRound.Instance.inShipPhase || SceneManager.sceneCount <= 1)
            {
                yield break;
            }
            DeadBodyInfo deadBody = StartOfRound.Instance.allPlayerScripts[playerId].deadBody;
            if (deadBody != null)
            {
                deadBody.attachedTo = null;
                deadBody.attachedLimb = null;
                deadBody.secondaryAttachedLimb = null;
                deadBody.secondaryAttachedTo = null;
                if (deadBody.grabBodyObject != null && deadBody.grabBodyObject.isHeld && deadBody.grabBodyObject.playerHeldBy != null)
                {
                    deadBody.grabBodyObject.playerHeldBy.DropAllHeldItems();
                }
                deadBody.isInShip = false;
                deadBody.parentedToShip = false;
                deadBody.transform.SetParent(null, worldPositionStays: true);
                deadBody.SetRagdollPositionSafely(newPosition, disableSpecialEffects: true);
            }
        }

        public static void TeleportEnemy(EnemyAI enemy, Vector3 newPosition)
        {
            enemy.serverPosition = newPosition;
            enemy.transform.position = newPosition;
            enemy.agent.Warp(newPosition);
            enemy.SyncPositionToClients();
        }
    }

}
