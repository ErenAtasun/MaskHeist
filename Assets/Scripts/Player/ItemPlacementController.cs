using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using MaskHeist.Core;
using MaskHeist.Gameplay;

namespace MaskHeist.Player
{
    /// <summary>
    /// Controls picking up and placing hideable items.
    /// Only active for Hider role during Hiding phase.
    /// </summary>
    public class ItemPlacementController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float pickupDistance = 3f;
        [SerializeField] private LayerMask itemLayer;
        [SerializeField] private Transform cameraTransform;

        [Header("State")]
        [SyncVar]
        private bool isHoldingItem = false;

        // References
        private HideableItem heldItem;
        private Camera playerCamera;
        private MaskHeistGamePlayer gamePlayer;

        // Public property for other scripts to check
        public bool IsHoldingItem => isHoldingItem;

        private void Awake()
        {
            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;

            playerCamera = cameraTransform?.GetComponent<Camera>();
            gamePlayer = GetComponent<MaskHeistGamePlayer>();

            // Default layer if not set
            if (itemLayer == 0)
                itemLayer = LayerMask.GetMask("Default", "HideableItem");
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // Only Hider can place items
            if (gamePlayer != null && gamePlayer.role != PlayerRole.Hider) return;

            // Check for pickup/drop input
            HandleInput();
        }

        private void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (isHoldingItem)
            {
                // Drop item when left click released
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    CmdDropItem();
                }
            }
            else
            {
                // Try to pick up item when left click pressed
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    TryPickupItem();
                }
            }
        }

        private void TryPickupItem()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, pickupDistance, itemLayer))
            {
                HideableItem item = hit.collider.GetComponent<HideableItem>();
                if (item != null && item.CanPickUp(gameObject))
                {
                    CmdPickupItem(item.gameObject);
                }
            }
        }

        [Command]
        private void CmdPickupItem(GameObject itemObject)
        {
            if (itemObject == null) return;

            HideableItem item = itemObject.GetComponent<HideableItem>();
            if (item != null && item.CanPickUp(gameObject))
            {
                item.PickUp(netIdentity);
                isHoldingItem = true;
                RpcSetHeldItem(itemObject);
            }
        }

        [ClientRpc]
        private void RpcSetHeldItem(GameObject itemObject)
        {
            if (itemObject != null)
            {
                heldItem = itemObject.GetComponent<HideableItem>();
            }
        }

        [Command]
        private void CmdDropItem()
        {
            if (heldItem != null)
            {
                heldItem.Drop();
                heldItem = null;
            }
            isHoldingItem = false;
            RpcClearHeldItem();
        }

        [ClientRpc]
        private void RpcClearHeldItem()
        {
            heldItem = null;
        }

        /// <summary>
        /// Called when a hideable item is spawned for this player.
        /// </summary>
        [TargetRpc]
        public void TargetNotifyItemSpawned(NetworkConnection conn, GameObject itemObject)
        {
            Debug.Log($"[ItemPlacement] Item spawned for me: {itemObject.name}");
            // Could trigger UI notification here
        }
    }
}
