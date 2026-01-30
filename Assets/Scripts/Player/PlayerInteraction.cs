using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using MaskHeist.Interaction;

namespace MaskHeist.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private Transform cameraTransform;

        private IInteractable currentInteractable;
        private Camera playerCamera;

        private void Reset()
        {
            // Editörde scripti eklediğinde otomatik ayarları yap
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
            interactionLayer = LayerMask.GetMask("Default", "Loot", "Interactable");
            if (interactionLayer == 0) interactionLayer = Physics.DefaultRaycastLayers;
        }

        private void Awake()
        {
            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
                
            playerCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            
            // Eğer layer seçilmemişse varsayılanı kullan
            if (interactionLayer == 0) 
                interactionLayer = Physics.DefaultRaycastLayers;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            CheckForInteractable();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryInteract();
            }
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    currentInteractable = interactable;
                    // TODO: Show UI prompt (e.g. "Press Left Click to Pickup")
                    // Debug.Log($"Looking at: {interactable.InteractionPrompt}");
                }
                else
                {
                    currentInteractable = null;
                }
            }
            else
            {
                currentInteractable = null;
            }
        }

        private void TryInteract()
        {
            if (currentInteractable != null)
            {
                // Interaction logic is server-sided for important things like Loot
                if (currentInteractable is MonoBehaviour interactableMono)
                {
                    CmdInteract(interactableMono.gameObject);
                }
            }
        }

        [Command]
        private void CmdInteract(GameObject target)
        {
            if (target == null) return;

            var interactable = target.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                // Verify distance on server to prevent cheating
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist <= interactionDistance * 1.5f) // Tolerance for latency
                {
                    interactable.OnInteract(gameObject);
                }
            }
        }
    }
}
