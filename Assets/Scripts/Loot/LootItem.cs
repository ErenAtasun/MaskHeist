using Mirror;
using UnityEngine;
using MaskHeist.Interaction;

namespace MaskHeist.Loot
{
    /// <summary>
    /// Represents a physical loot object in the game world.
    /// Handles network synchronization of the loot state (active/inactive).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))] // Etkileşim için Collider şart
    public class LootItem : NetworkBehaviour, IInteractable
    {
        [Header("Data")]
        [SerializeField] private LootData lootData;

        // Properties for external access
        public int ScoreValue => lootData != null ? lootData.scoreValue : 0;
        public float StealDuration => lootData != null ? lootData.stealDuration : 1f;
        public string LootName => lootData != null ? lootData.lootName : "Unknown Loot";

        public string InteractionPrompt => $"Pick up {LootName}";

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

        public bool CanInteract(GameObject interactor)
        {
            return !isCollected;
        }

        public void OnInteract(GameObject interactor)
        {
            Interact(interactor);
        }

        /// <summary>
        /// Interaction logic to be called by Player
        /// </summary>
        [Server]
        public void Interact(GameObject player)
        {
            if (isCollected) return;

            Debug.Log($"Player {player.name} collected {LootName}");
            
            // Add score logic here later
            
            SetCollected(true);
        }
    }
}
