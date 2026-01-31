using UnityEngine;
using Mirror;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Disruptor ability - Disables nearby traps temporarily.
    /// Press E to activate.
    /// </summary>
    public class DisruptorAbility : MaskAbility
    {
        [Header("Disruptor Settings")]
        [SerializeField] private float disruptRadius = 8f;
        [SerializeField] private GameObject disruptEffectPrefab;
        
        protected override void OnAbilityActivated()
        {
            Debug.Log($"[Server] {gameObject.name} activated Disruptor! (Radius: {disruptRadius}m for {duration}s)");
            
            // Find and disable traps on server
            DisableNearbyTraps();
        }
        
        protected override void OnAbilityDeactivated()
        {
            Debug.Log($"[Server] {gameObject.name} Disruptor ended!");
            
            // Re-enable traps
            EnableNearbyTraps();
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityActivated()
        {
            if (!isLocalPlayer) return;
            
            // Visual effect
            Debug.Log($"EMP pulse sent! Traps disabled within {disruptRadius}m");
            
            // Spawn visual effect
            // if (disruptEffectPrefab != null)
            // {
            //     Instantiate(disruptEffectPrefab, transform.position, Quaternion.identity);
            // }
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityDeactivated()
        {
            if (!isLocalPlayer) return;
            Debug.Log("Disruptor effect ended. Traps are active again.");
        }
        
        [Server]
        private void DisableNearbyTraps()
        {
            // TODO: Find all ITrap interfaces within radius and disable them
            // For now, find objects with "Trap" tag
            
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, disruptRadius);
            int trapsDisabled = 0;
            
            foreach (var col in nearbyObjects)
            {
                // Look for trap components
                // ITrap trap = col.GetComponent<ITrap>();
                // if (trap != null)
                // {
                //     trap.Disable(duration);
                //     trapsDisabled++;
                // }
                
                // Placeholder: check for tag
                if (col.CompareTag("Trap"))
                {
                    col.gameObject.SetActive(false);
                    trapsDisabled++;
                }
            }
            
            Debug.Log($"[Server] Disabled {trapsDisabled} traps");
        }
        
        [Server]
        private void EnableNearbyTraps()
        {
            // TODO: Re-enable previously disabled traps
            // This needs a proper trap reference system
            Debug.Log("[Server] Re-enabling traps (TODO: implement trap references)");
        }
    }
}
