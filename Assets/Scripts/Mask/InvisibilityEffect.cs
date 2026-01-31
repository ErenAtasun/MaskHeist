using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Handles the visual effects of invisibility.
    /// This is NOT a NetworkBehaviour - all network logic is in PlayerMask.
    /// </summary>
    public class InvisibilityEffect : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float invisibleAlpha = 0.3f;
        
        private List<Renderer> playerRenderers = new List<Renderer>();
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private bool isInvisible = false;
        private bool isLocalPlayer = false;
        
        public bool IsInvisible => isInvisible;
        
        public void Initialize(bool localPlayer)
        {
            isLocalPlayer = localPlayer;
            
            // Cache all renderers
            playerRenderers.Clear();
            playerRenderers.AddRange(GetComponentsInChildren<Renderer>());
            
            // Store original materials
            originalMaterials.Clear();
            foreach (var renderer in playerRenderers)
            {
                if (renderer != null)
                {
                    originalMaterials[renderer] = renderer.materials;
                }
            }
            
            Debug.Log($"InvisibilityEffect.Initialize: Found {playerRenderers.Count} renderers (isLocalPlayer={isLocalPlayer})");
        }
        
        /// <summary>
        /// Apply invisibility visual effect
        /// </summary>
        public void SetInvisible(bool invisible)
        {
            isInvisible = invisible;
            
            foreach (var renderer in playerRenderers)
            {
                if (renderer == null) continue;
                
                if (invisible)
                {
                    // Disable shadows when invisible
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    
                    if (isLocalPlayer)
                    {
                        // Local player sees themselves as semi-transparent
                        SetRendererAlpha(renderer, invisibleAlpha);
                    }
                    else
                    {
                        // Other players are completely invisible
                        renderer.enabled = false;
                    }
                }
                else
                {
                    // Restore visibility
                    renderer.enabled = true;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    RestoreRendererMaterials(renderer);
                }
            }
            
            Debug.Log($"InvisibilityEffect: SetInvisible({invisible}) for {(isLocalPlayer ? "local" : "remote")} player");
        }
        
        private void SetRendererAlpha(Renderer renderer, float alpha)
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                
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
        
        private void RestoreRendererMaterials(Renderer renderer)
        {
            if (originalMaterials.TryGetValue(renderer, out Material[] mats))
            {
                renderer.materials = mats;
            }
        }
    }
}
