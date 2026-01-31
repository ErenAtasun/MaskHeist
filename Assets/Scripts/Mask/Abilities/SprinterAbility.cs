using UnityEngine;
using Mirror;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Sprinter ability - Gives a temporary speed boost.
    /// Press E to activate.
    /// </summary>
    public class SprinterAbility : MaskAbility
    {
        private PlayerController playerController;
        private float speedMultiplier = 1.5f;
        
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }
        
        private void Start()
        {
            playerController = GetComponent<PlayerController>();
        }
        
        protected override void OnAbilityActivated()
        {
            Debug.Log($"[Server] {gameObject.name} activated Sprinter! ({duration}s at {speedMultiplier}x speed)");
            
            // Apply speed boost
            if (playerController != null)
            {
                playerController.SetSpeedMultiplier(speedMultiplier);
            }
        }
        
        protected override void OnAbilityDeactivated()
        {
            Debug.Log($"[Server] {gameObject.name} Sprinter ended!");
            
            // Reset speed
            if (playerController != null)
            {
                playerController.ResetSpeedMultiplier();
            }
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityActivated()
        {
            // Visual feedback - could add particle effect or screen tint
            Debug.Log("Sprint boost active!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityDeactivated()
        {
            Debug.Log("Sprint boost ended.");
        }
    }
}
