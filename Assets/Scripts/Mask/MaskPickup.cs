using UnityEngine;
using Mirror;
using MaskHeist.Interaction;

namespace MaskHeist.Mask
{
    /// <summary>
    /// A physical mask object that can be picked up from a table.
    /// When interacted with, assigns the mask to the player.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class MaskPickup : NetworkBehaviour, IInteractable
    {
        [Header("Mask Settings")]
        [SerializeField] private MaskData maskData;
        
        [Header("Visual")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobSpeed = 2f;
        
        [Header("State")]
        [SyncVar(hook = nameof(OnPickedUpChanged))]
        private bool isPickedUp = false;
        
        private Vector3 startPosition;
        private float bobTime;
        
        // IInteractable implementation
        public string InteractionPrompt => maskData != null ? 
            $"Pick up {maskData.maskName}" : "Pick up Mask";
        
        public MaskData MaskData => maskData;
        
        private void Start()
        {
            startPosition = transform.position;
            
            // Create visual from mask prefab if not set
            if (visualModel == null && maskData != null && maskData.maskPrefab != null)
            {
                visualModel = Instantiate(maskData.maskPrefab, transform);
                visualModel.transform.localPosition = Vector3.zero;
            }
        }
        
        private void Update()
        {
            if (isPickedUp) return;
            
            // Rotate the mask for visual appeal
            if (visualModel != null)
            {
                visualModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            // Bob up and down
            bobTime += Time.deltaTime * bobSpeed;
            Vector3 newPos = startPosition;
            newPos.y += Mathf.Sin(bobTime) * bobHeight;
            transform.position = newPos;
        }
        
        public bool CanInteract(GameObject interactor)
        {
            if (isPickedUp) return false;
            
            // Player can always pick up a mask (swap if they have one)
            PlayerMask playerMask = interactor.GetComponent<PlayerMask>();
            return playerMask != null;
        }
        
        public void OnInteract(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            
            PlayerMask playerMask = interactor.GetComponent<PlayerMask>();
            if (playerMask != null && maskData != null)
            {
                // Get mask index from registry
                int maskIndex = -1;
                if (MaskRegistry.Instance != null)
                {
                    maskIndex = MaskRegistry.Instance.GetMaskIndex(maskData);
                    Debug.Log($"Mask index from registry: {maskIndex}");
                }
                
                // If no registry or mask not in registry, use direct assignment
                if (maskIndex < 0)
                {
                    Debug.Log("MaskRegistry not found or mask not registered, using direct assignment");
                    // Direct assignment without registry, pass this pickup so it can be returned later
                    playerMask.EquipMaskDirect(maskData, this);
                }
                else
                {
                    // Return old mask first if player has one, then set new pickup reference
                    playerMask.EquipMaskDirect(maskData, this);
                }
                
                // Mark as picked up via server command
                CmdSetPickedUp();
                
                Debug.Log($"Player picked up: {maskData.maskName}");
            }
        }
        
        [Command(requiresAuthority = false)]
        private void CmdSetPickedUp()
        {
            isPickedUp = true;
        }
        
        [Server]
        public void SetPickedUp(bool picked)
        {
            isPickedUp = picked;
        }
        
        /// <summary>
        /// Command to reset mask from client (when player swaps masks)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdResetMask()
        {
            isPickedUp = false;
            Debug.Log($"Mask returned: {maskData?.maskName}");
        }
        
        /// <summary>
        /// Reset the mask for a new round
        /// </summary>
        [Server]
        public void ResetMask()
        {
            isPickedUp = false;
            transform.position = startPosition;
        }
        
        private void OnPickedUpChanged(bool oldVal, bool newVal)
        {
            Debug.Log($"MaskPickup OnPickedUpChanged: {oldVal} -> {newVal}");
            
            // Hide the visual model when picked up
            if (visualModel != null)
            {
                visualModel.SetActive(!newVal);
            }
            
            // Also hide all renderers on this object (in case visualModel is null)
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = !newVal;
            }
            
            // Disable collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = !newVal;
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw mask info in editor
            if (maskData != null)
            {
                Gizmos.color = maskData.maskColor;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }
}
