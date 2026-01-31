using Mirror;
using UnityEngine;
using MaskHeist.Core;

namespace MaskHeist.Gameplay
{
    /// <summary>
    /// A hideable item that can be picked up and placed by the Hider.
    /// Syncs position and held state across network.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class HideableItem : NetworkBehaviour
    {
        [Header("Data")]
        [SerializeField] private HideableItemData itemData;

        [Header("State")]
        [SyncVar(hook = nameof(OnHeldChanged))]
        private bool isHeld = false;

        [SyncVar]
        private uint holderNetId;

        // Components
        private Rigidbody rb;
        private Collider col;

        // Local state
        private Transform holderTransform;
        private bool isBeingPlaced = false;

        // Properties
        public bool IsHeld => isHeld;
        public string ItemName => itemData != null ? itemData.itemName : "Unknown Item";
        public float HoldDistance => itemData != null ? itemData.holdDistance : 2f;
        public float HoldHeight => itemData != null ? itemData.holdHeight : 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        private void Start()
        {
            // Ensure physics are set up correctly
            if (rb != null)
            {
                rb.isKinematic = true; // Start kinematic, will be controlled by holder
            }
        }

        private void Update()
        {
            // If held, follow the holder
            if (isHeld && holderTransform != null)
            {
                UpdateHeldPosition();
            }
        }

        private void UpdateHeldPosition()
        {
            if (holderTransform == null) return;

            // Calculate target position in front of holder
            Vector3 targetPos = holderTransform.position 
                              + holderTransform.forward * HoldDistance 
                              + Vector3.up * HoldHeight;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
            
            // Face the holder
            Vector3 lookDir = holderTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation, 
                    Quaternion.LookRotation(lookDir), 
                    Time.deltaTime * 10f
                );
            }
        }

        private void OnHeldChanged(bool oldValue, bool newValue)
        {
            // Update physics based on held state
            if (rb != null)
            {
                rb.isKinematic = newValue;
                rb.useGravity = !newValue;
            }

            // Disable collision when held to prevent issues
            if (col != null)
            {
                col.isTrigger = newValue;
            }

            // Find holder transform
            if (newValue && holderNetId != 0)
            {
                if (NetworkClient.spawned.TryGetValue(holderNetId, out NetworkIdentity identity))
                {
                    holderTransform = identity.transform;
                }
            }
            else
            {
                holderTransform = null;
            }
        }

        /// <summary>
        /// Check if this item can be picked up by the given player.
        /// </summary>
        public bool CanPickUp(GameObject player)
        {
            return !isHeld;
        }

        /// <summary>
        /// Pick up the item (called on server).
        /// </summary>
        [Server]
        public void PickUp(NetworkIdentity holder)
        {
            if (isHeld) return;

            holderNetId = holder.netId;
            isHeld = true;

            Debug.Log($"[HideableItem] {ItemName} picked up by {holder.name}");
        }

        /// <summary>
        /// Drop/place the item (called on server).
        /// </summary>
        [Server]
        public void Drop()
        {
            if (!isHeld) return;

            isHeld = false;
            holderNetId = 0;

            // Let physics take over
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Note: Hider does NOT get points for placing
            // Only Seeker gets points when finding the item

            Debug.Log($"[HideableItem] {ItemName} placed at {transform.position}");
        }

        /// <summary>
        /// Force set position (for spawning).
        /// </summary>
        [Server]
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
