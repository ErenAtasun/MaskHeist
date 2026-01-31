using UnityEngine;
using Mirror;
using MaskHeist.Loot;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Scanner ability - Sends a pulse that highlights nearby loot.
    /// Press E to activate.
    /// </summary>
    public class ScannerAbility : MaskAbility
    {
        [Header("Scanner Settings")]
        [SerializeField] private float scanRadius = 15f;
        [SerializeField] private GameObject pingEffectPrefab;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        protected override void OnAbilityActivated()
        {
            Debug.Log($"[Server] {gameObject.name} activated Scanner! (Radius: {scanRadius}m)");
        }
        
        protected override void OnAbilityDeactivated()
        {
            Debug.Log($"[Server] {gameObject.name} Scanner pulse ended!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityActivated()
        {
            if (!isLocalPlayer) return;
            
            ScanForLoot();
            Debug.Log("Scanner pulse sent!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityDeactivated()
        {
            if (!isLocalPlayer) return;
            
            // Remove highlight effects
            Debug.Log("Scanner highlights fading...");
        }
        
        private void ScanForLoot()
        {
            // Find all loot items within radius
            LootItem[] allLoot = FindObjectsByType<LootItem>(FindObjectsSortMode.None);
            
            int foundCount = 0;
            foreach (var loot in allLoot)
            {
                float dist = Vector3.Distance(transform.position, loot.transform.position);
                if (dist <= scanRadius)
                {
                    foundCount++;
                    HighlightLoot(loot, dist);
                }
            }
            
            if (foundCount > 0)
            {
                Debug.Log($"Scanner found {foundCount} loot items nearby!");
            }
            else
            {
                Debug.Log("Scanner found no loot within range.");
            }
        }
        
        private void HighlightLoot(LootItem loot, float distance)
        {
            // TODO: Add visual highlight effect to the loot
            // For now, just log it
            Debug.Log($"  - {loot.LootName} at {distance:F1}m");
            
            // Could instantiate a ping effect here
            // if (pingEffectPrefab != null)
            // {
            //     Instantiate(pingEffectPrefab, loot.transform.position, Quaternion.identity);
            // }
        }
    }
}
