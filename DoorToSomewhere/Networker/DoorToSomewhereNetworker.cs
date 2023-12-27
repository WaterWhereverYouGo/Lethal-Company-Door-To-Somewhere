using Unity.Netcode;
using DoorToSomewhereMod.Object;

namespace DoorToSomewhereMod.Networker
{
    public class DoorToSomewhereNetworker : NetworkBehaviour
    {
        public static DoorToSomewhereNetworker Instance;

        public static NetworkVariable<int[]> SpawnWeights = new NetworkVariable<int[]>();
        /*
        public static NetworkVariable<int> SpawnWeight0 = new NetworkVariable<int>();
        public static NetworkVariable<int> SpawnWeight1 = new NetworkVariable<int>();
        public static NetworkVariable<int> SpawnWeight2 = new NetworkVariable<int>();
        public static NetworkVariable<int> SpawnWeight3 = new NetworkVariable<int>();
        public static NetworkVariable<int> SpawnWeight4 = new NetworkVariable<int>();
        public static NetworkVariable<int> SpawnWeightMax = new NetworkVariable<int>();
        */
        public static NetworkVariable<bool> SpawnRateDynamic = new NetworkVariable<bool>();

        private void Awake()
        {
            Instance = this;
        }

        public void MimicAttack(int playerId, int mimicIndex, bool ownerOnly = false)
        {
            if (base.IsOwner)
            {
                DoorToSomewhereNetworker.Instance.MimicAttackClientRpc(playerId, mimicIndex);
            }
            else if (!ownerOnly)
            {
                DoorToSomewhereNetworker.Instance.MimicAttackServerRpc(playerId, mimicIndex);
            }
        }

        [ClientRpc]
        public void MimicAttackClientRpc(int playerId, int mimicIndex)
        {
            StartCoroutine(DoorToSomewhere.allMimics[mimicIndex].Attack(playerId));
        }

        [ServerRpc(RequireOwnership = false)]
        public void MimicAttackServerRpc(int playerId, int mimicIndex)
        {
            DoorToSomewhereNetworker.Instance.MimicAttackClientRpc(playerId, mimicIndex);
        }

        public void MimicAddAnger(int amount, int mimicIndex)
        {
            if (base.IsOwner)
            {
                DoorToSomewhereNetworker.Instance.MimicAddAngerClientRpc(amount, mimicIndex);
            }
            else
            {
                DoorToSomewhereNetworker.Instance.MimicAddAngerServerRpc(amount, mimicIndex);
            }
        }

        [ClientRpc]
        public void MimicAddAngerClientRpc(int amount, int mimicIndex)
        {
            StartCoroutine(DoorToSomewhere.allMimics[mimicIndex].AddAnger(amount));
        }

        [ServerRpc(RequireOwnership = false)]
        public void MimicAddAngerServerRpc(int amount, int mimicIndex)
        {
            DoorToSomewhereNetworker.Instance.MimicAddAngerClientRpc(amount, mimicIndex);
        }

        public void MimicLockPick(LockPicker lockPicker, int mimicIndex, bool ownerOnly = false)
        {
            int playerId = (int)lockPicker.playerHeldBy.playerClientId;
            if (base.IsOwner)
            {
                DoorToSomewhereNetworker.Instance.MimicLockPickClientRpc(playerId, mimicIndex);
            }
            else if (!ownerOnly)
            {
                DoorToSomewhereNetworker.Instance.MimicLockPickServerRpc(playerId, mimicIndex);
            }
        }

        [ClientRpc]
        public void MimicLockPickClientRpc(int playerId, int mimicIndex)
        {
            StartCoroutine(DoorToSomewhere.allMimics[mimicIndex].MimicLockPick(playerId));
        }

        [ServerRpc(RequireOwnership = false)]
        public void MimicLockPickServerRpc(int playerId, int mimicIndex)
        {
            DoorToSomewhereNetworker.Instance.MimicLockPickClientRpc(playerId, mimicIndex);
        }
    }

}
