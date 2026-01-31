using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Tracker ability - Shows recent footprints/trails of other players.
    /// Press E to activate.
    /// </summary>
    public class TrackerAbility : MaskAbility
    {
        [Header("Tracker Settings")]
        [SerializeField] private float trailVisibleDistance = 20f;
        [SerializeField] private GameObject footprintPrefab;
        [SerializeField] private Material trailMaterial;
        
        private List<GameObject> visibleTrails = new List<GameObject>();
        
        protected override void OnAbilityActivated()
        {
            Debug.Log($"[Server] {gameObject.name} activated Tracker! ({duration}s)");
        }
        
        protected override void OnAbilityDeactivated()
        {
            Debug.Log($"[Server] {gameObject.name} Tracker ended!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityActivated()
        {
            if (!isLocalPlayer) return;
            
            // Show trails - for now just a visual indicator
            // In full implementation, this would reveal FootprintManager's stored trails
            ShowTrails();
            Debug.Log("Tracker vision active - you can see footprints!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityDeactivated()
        {
            if (!isLocalPlayer) return;
            
            HideTrails();
            Debug.Log("Tracker vision ended.");
        }
        
        private void ShowTrails()
        {
            // TODO: Connect to FootprintManager system
            // For now, highlight other players with an outline or indicator
            
            // Find all players and show their trail indicators
            PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                if (player.gameObject == gameObject) continue; // Skip self
                
                // Create a simple trail indicator (placeholder)
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= trailVisibleDistance)
                {
                    Debug.Log($"Tracker: Player detected at {dist:F1}m");
                }
            }
        }
        
        private void HideTrails()
        {
            foreach (var trail in visibleTrails)
            {
                if (trail != null) Destroy(trail);
            }
            visibleTrails.Clear();
        }
        
        private void OnDestroy()
        {
            HideTrails();
        }
    }
}
