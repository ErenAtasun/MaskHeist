using Mirror;
using UnityEngine;
using MaskHeist.Interaction;
using MaskHeist.Core;
using MaskHeist.UI;

namespace MaskHeist.Loot
{
    /// <summary>
    /// Represents a physical loot object in the game world.
    /// When collected by a Seeker, awards points via ScoreManager.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LootItem : NetworkBehaviour, IInteractable
    {
        [Header("Data")]
        [SerializeField] private LootData lootData;

        // Properties for external access
        public int ScoreValue => lootData != null ? lootData.scoreValue : 0;
        public float StealDuration => lootData != null ? lootData.stealDuration : 1f;
        public string LootName => lootData != null ? lootData.lootName : "Unknown Loot";

        public string InteractionPrompt => $"Pick up {LootName} (+{ScoreValue} puan)";

        [SyncVar(hook = nameof(OnCollectedChanged))]
        private bool isCollected = false;

        private void OnCollectedChanged(bool oldVal, bool newVal)
        {
            gameObject.SetActive(!newVal);
        }

        [Server]
        public void SetCollected(bool collected)
        {
            isCollected = collected;
        }

        /// <summary>
        /// Set the loot data at runtime (used by LootPool spawning).
        /// </summary>
        public void SetLootData(LootData data)
        {
            lootData = data;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (isCollected) return false;
            
            // Only Seekers can collect bonus loot
            var gamePlayer = interactor.GetComponent<MaskHeistGamePlayer>();
            if (gamePlayer != null && gamePlayer.role != PlayerRole.Seeker)
                return false;
            
            return true;
        }

        public void OnInteract(GameObject interactor)
        {
            Interact(interactor);
        }

        /// <summary>
        /// Collect the loot — awards score to the collecting player.
        /// </summary>
        [Server]
        public void Interact(GameObject player)
        {
            if (isCollected) return;

            Debug.Log($"[LootItem] Player {player.name} collected {LootName} (+{ScoreValue})");
            
            // Award score to the player who collected
            var netIdentity = player.GetComponent<NetworkIdentity>();
            if (netIdentity != null && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddPlayerScore(netIdentity, ScoreValue, $"Loot: {LootName}");
            }

            // Notify UI
            RpcNotifyLootCollected(LootName, ScoreValue);

            SetCollected(true);
        }

        [ClientRpc]
        private void RpcNotifyLootCollected(string name, int score)
        {
            UIEvents.TriggerLootCollected(name, score);
        }
    }
}
