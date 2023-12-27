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

namespace DoorToSomewhereMod.Object
{
    public class DoorToSomewhere : MonoBehaviour
    {
        public GameObject playerTarget;

        public BoxCollider frameBox;

        public Sprite LostFingersIcon;

        public Animator mimicAnimator;

        public GameObject grabPoint;

        public InteractTrigger interactTrigger;

        public ScanNodeProperties scanNode;

        public int anger = 0;

        public bool angering = false;

        public int sprayCount = 0;

        private bool attacking = false;

        public static List<DoorToSomewhere> allDoors;
        public int mimicIndex;

        public void TouchMimic(PlayerControllerB player)
        {
            if (!attacking)
            {
                MimicNetworker.Instance.MimicAttack((int)player.playerClientId, mimicIndex);
            }
        }

        public IEnumerator Attack(int playerId)
        {
            attacking = true;
            interactTrigger.interactable = false;

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];

            mimicAnimator.SetTrigger("Attack");

            playerTarget.transform.position = player.transform.position;
            yield return new WaitForSeconds(0.1f);
            playerTarget.transform.position = player.transform.position;
            yield return new WaitForSeconds(0.1f);
            playerTarget.transform.position = player.transform.position;
            yield return new WaitForSeconds(0.1f);
            playerTarget.transform.position = player.transform.position;
            yield return new WaitForSeconds(0.1f);

            float proximity = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, frameBox.transform.position);
            if (proximity < 8f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
            else if (proximity < 14f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            }

            yield return new WaitForSeconds(0.2f);

            if (player.IsOwner && Vector3.Distance(player.transform.position, this.transform.position) < 8.75f)
            {
                player.KillPlayer(Vector3.zero, true, CauseOfDeath.Unknown, 0);
            }

            float startTime = Time.timeSinceLevelLoad;
            yield return new WaitUntil(() => player.deadBody != null || Time.timeSinceLevelLoad - startTime > 4f);

            if (player.deadBody != null)
            {
                player.deadBody.attachedTo = grabPoint.transform;
                player.deadBody.attachedLimb = player.deadBody.bodyParts[5];
                player.deadBody.matchPositionExactly = true;

                for (int i = 0; i < player.deadBody.bodyParts.Length; i++)
                {
                    player.deadBody.bodyParts[i].GetComponent<Collider>().excludeLayers = ~0;
                }
            }

            yield return new WaitForSeconds(2f);

            if (player.deadBody != null)
            {
                player.deadBody.attachedTo = null;
                player.deadBody.attachedLimb = null;
                player.deadBody.matchPositionExactly = false;
                player.deadBody.transform.GetChild(0).gameObject.SetActive(false); // don't set the dead body itself inactive or it won't get cleaned up later
                player.deadBody = null;
            }

            yield return new WaitForSeconds(4.5f);
            attacking = false;
            interactTrigger.interactable = true;
            yield break;
        }

        static MethodInfo RetractClaws = typeof(LockPicker).GetMethod("RetractClaws", BindingFlags.NonPublic | BindingFlags.Instance);
        public IEnumerator MimicLockPick(int playerId)
        {
            if (angering) { yield break; }
            if (attacking) { yield break; }

            LockPicker lockPicker = StartOfRound.Instance.allPlayerScripts[playerId].currentlyHeldObjectServer as LockPicker;
            if (lockPicker == null) { yield break; }

            attacking = true;
            interactTrigger.interactable = false;

            AudioSource lockPickerAudio = lockPicker.GetComponent<AudioSource>();
            lockPickerAudio.PlayOneShot(lockPicker.placeLockPickerClips[UnityEngine.Random.Range(0, lockPicker.placeLockPickerClips.Length)]);
            lockPicker.armsAnimator.SetBool("mounted", true);
            lockPicker.armsAnimator.SetBool("picking", true);
            lockPickerAudio.Play();
            lockPickerAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            lockPicker.isOnDoor = true;
            lockPicker.isPickingLock = true;
            lockPicker.grabbable = false;

            if (lockPicker.IsOwner)
            {
                lockPicker.playerHeldBy.DiscardHeldObject(true, MimicNetworker.Instance.NetworkObject, this.transform.position + this.transform.up * 1.5f - this.transform.forward * 1.15f, true);
            }

            float startTime = Time.timeSinceLevelLoad;
            yield return new WaitUntil(() => !lockPicker.isHeld || Time.timeSinceLevelLoad - startTime > 10f);
            lockPicker.transform.localEulerAngles = new Vector3(this.transform.eulerAngles.x, this.transform.eulerAngles.y + 90f, this.transform.eulerAngles.z);

            yield return new WaitForSeconds(5f); // wait 5 seconds before the lockpicker falls off

            RetractClaws.Invoke(lockPicker, null);
            lockPicker.transform.SetParent(null);
            lockPicker.startFallingPosition = lockPicker.transform.position;
            lockPicker.FallToGround();
            lockPicker.grabbable = true;

            yield return new WaitForSeconds(1f);

            anger = 3;
            attacking = false;
            interactTrigger.interactable = false;
            PlayerControllerB closestPlayer = null;
            float closestDistance = 9999f;
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                float distance = Vector3.Distance(this.transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
            if (closestPlayer != null)
            {
                MimicNetworker.Instance.MimicAttackClientRpc((int)closestPlayer.playerClientId, this.mimicIndex);
            }
            else
            {
                interactTrigger.interactable = true;
            }

            yield break;
        }

        public IEnumerator AddAnger(int amount)
        {
            if (angering) { yield break; }
            if (attacking) { yield break; }

            angering = true;
            anger += amount;

            if (anger == 1)
            {
                Sprite oldIcon = interactTrigger.hoverIcon;
                interactTrigger.hoverIcon = LostFingersIcon;
                mimicAnimator.SetTrigger("Growl");
                yield return new WaitForSeconds(2.75f);
                interactTrigger.hoverIcon = oldIcon;

                sprayCount = 0;
                angering = false;
                yield break;
            }
            else if (anger == 2)
            {
                interactTrigger.holdTip = "DIE : [LMB]";
                interactTrigger.timeToHold = 0.25f;

                Sprite oldIcon = interactTrigger.hoverIcon;
                interactTrigger.hoverIcon = LostFingersIcon;
                mimicAnimator.SetTrigger("Growl");
                yield return new WaitForSeconds(2.75f);
                interactTrigger.hoverIcon = oldIcon;

                sprayCount = 0;
                angering = false;
                yield break;
            }
            else if (anger > 2)
            {
                PlayerControllerB closestPlayer = null;
                float closestDistance = 9999f;
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    float distance = Vector3.Distance(this.transform.position, player.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = player;
                    }
                }
                if (closestPlayer != null)
                {
                    MimicNetworker.Instance.MimicAttackClientRpc((int)closestPlayer.playerClientId, this.mimicIndex);
                }
            }

            sprayCount = 0;
            angering = false;
            yield break;
        }
    }

}
