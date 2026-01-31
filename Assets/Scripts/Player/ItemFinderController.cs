using Mirror;
using UnityEngine;
using MaskHeist.Core;
using MaskHeist.Gameplay;

namespace MaskHeist.Player
{
    /// <summary>
    /// Allows Seeker to find hidden items using SPACE key.
    /// Only active for Seeker role during Seeking phase.
    /// </summary>
    public class ItemFinderController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float findDistance = 3f;
        [SerializeField] private LayerMask itemLayer;
        [SerializeField] private Transform cameraTransform;

        [Header("UI Feedback")]
        [SerializeField] private string findPrompt = "[SPACE] Eşyayı Al";

        // References
        private Camera playerCamera;
        private MaskHeistGamePlayer gamePlayer;
        private HideableItem targetItem;

        private void Awake()
        {
            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;

            playerCamera = cameraTransform?.GetComponent<Camera>();
            gamePlayer = GetComponent<MaskHeistGamePlayer>();

            if (itemLayer == 0)
                itemLayer = LayerMask.GetMask("Default", "HideableItem");
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // Only Seeker can find items
            if (gamePlayer == null || gamePlayer.role != PlayerRole.Seeker) return;

            // Check for item in view
            CheckForItem();

            // SPACE key to find item
            if (targetItem != null && Input.GetKeyDown(KeyCode.Space))
            {
                CmdFindItem(targetItem.gameObject);
            }
        }

        private void CheckForItem()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, findDistance, itemLayer))
            {
                HideableItem item = hit.collider.GetComponent<HideableItem>();
                
                if (item != null && !item.IsHeld)
                {
                    if (targetItem != item)
                    {
                        targetItem = item;
                        // Show UI prompt
                        UI.UIEvents.TriggerInteractableChanged(findPrompt);
                    }
                }
                else
                {
                    ClearTarget();
                }
            }
            else
            {
                ClearTarget();
            }
        }

        private void ClearTarget()
        {
            if (targetItem != null)
            {
                targetItem = null;
                UI.UIEvents.TriggerInteractableChanged(null);
            }
        }

        [Command]
        private void CmdFindItem(GameObject itemObject)
        {
            if (itemObject == null) return;

            HideableItem item = itemObject.GetComponent<HideableItem>();
            if (item == null) return;

            // Verify distance
            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist > findDistance * 1.5f) return;

            // Award score to Seeker
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnItemFound();
            }

            // Notify all clients
            RpcItemFound(item.ItemName);

            // Destroy or disable the item
            NetworkServer.Destroy(itemObject);

            Debug.Log($"[ItemFinder] {gamePlayer.displayName} found the item!");
        }

        [ClientRpc]
        private void RpcItemFound(string itemName)
        {
            Debug.Log($"*** ITEM FOUND: {itemName} ***");
            // Could trigger UI celebration here
        }
    }
}
