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
using UnityEngine.SceneManagement;
using DoorToSomewhereMod.Logger;
using DoorToSomewhereMod.Networker;

namespace DoorToSomewhereMod.Object
{
    public class DoorToSomewhere : MonoBehaviour
    {
        public static DoorToSomewhere Instance;

        public GameObject playerTarget;
        public BoxCollider frameBox;
        public GameObject grabPoint;
        public InteractTrigger interactTrigger;
        public ScanNodeProperties scanNode;
        public int sprayCount = 0;

        public static List<DoorToSomewhere> allDoors;
        public int doorIndex;

        private void Awake()
        {
            Instance = this;
        }
    }
}
