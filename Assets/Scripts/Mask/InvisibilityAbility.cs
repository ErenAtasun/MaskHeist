using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace MaskHeist.Mask
{
    /// <summary>
    /// The common invisibility ability that all Seekers have.
    /// Makes the player invisible to others for a duration.
    /// GDD: 10 seconds duration, 45 seconds cooldown
    /// </summary>
    public class InvisibilityAbility : MaskAbility
    {
        [Header("Invisibility Settings")]
        [SerializeField] private float invisibleAlpha = 0.1f; // For local player feedback
        
        private List<Renderer> playerRenderers = new List<Renderer>();
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        
        private float noiseMultiplier = 0.15f; // Extra footstep noise while invisible
        
        public float NoiseMultiplier => isActive ? noiseMultiplier : 0f;
        
        public void SetNoiseMultiplier(float multiplier)
        {
            noiseMultiplier = multiplier;
        }
        
        private void Start()
        {
            // Cache all renderers on the player
            playerRenderers.AddRange(GetComponentsInChildren<Renderer>());
            
            // Store original materials
            foreach (var renderer in playerRenderers)
            {
                originalMaterials[renderer] = renderer.materials;
            }
        }
        
        protected override void OnAbilityActivated()
        {
            Debug.Log($"[Server] {gameObject.name} activated invisibility!");
        }
        
        protected override void OnAbilityDeactivated()
        {
            Debug.Log($"[Server] {gameObject.name} invisibility ended!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityActivated()
        {
            // Apply invisibility effect
            ApplyInvisibilityVisual(true);
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityDeactivated()
        {
            // Remove invisibility effect
            ApplyInvisibilityVisual(false);
        }
        
        private void ApplyInvisibilityVisual(bool invisible)
        {
            foreach (var renderer in playerRenderers)
            {
                if (renderer == null) continue;
                
                if (invisible)
                {
                    // Check if this is the local player's character
                    bool isLocalCharacter = isLocalPlayer;
                    
                    if (isLocalCharacter)
                    {
                        // Local player sees themselves slightly transparent
                        SetRendererAlpha(renderer, invisibleAlpha);
                    }
                    else
                    {
                        // Other players see them as completely invisible
                        renderer.enabled = false;
                    }
                    
                    // Disable shadows for all
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                else
                {
                    // Restore visibility
                    renderer.enabled = true;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    RestoreRendererMaterials(renderer);
                }
            }
        }
        
        private void SetRendererAlpha(Renderer renderer, float alpha)
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                    
                    // Enable transparency
                    mat.SetFloat("_Mode", 3); // Transparent mode
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
            }
        }
        
        private void RestoreRendererMaterials(Renderer renderer)
        {
            if (originalMaterials.TryGetValue(renderer, out Material[] mats))
            {
                // Reset alpha to 1
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = 1f;
                        mat.color = color;
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup
            originalMaterials.Clear();
            playerRenderers.Clear();
        }
    }
}
