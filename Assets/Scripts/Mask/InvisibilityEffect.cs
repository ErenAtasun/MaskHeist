using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Handles the visual effects of invisibility.
    /// Supports both URP and Built-in Render Pipeline.
    /// This is NOT a NetworkBehaviour - all network logic is in PlayerMask.
    /// </summary>
    public class InvisibilityEffect : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float invisibleAlpha = 0.3f;
        [SerializeField] private Color screenTintColor = new Color(0.3f, 0.6f, 1f, 0.15f);
        
        private List<Renderer> playerRenderers = new List<Renderer>();
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private bool isInvisible = false;
        private bool isLocalPlayer = false;
        
        // Screen overlay for local player feedback
        private Texture2D overlayTexture;
        private bool showOverlay = false;
        private float overlayFadeTimer = 0f;
        private float overlayPulseDuration = 2f;
        
        public bool IsInvisible => isInvisible;
        
        public void Initialize(bool localPlayer)
        {
            isLocalPlayer = localPlayer;
            
            // Cache all renderers (skip particle systems)
            playerRenderers.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                playerRenderers.Add(r);
            }
            
            // Store original materials (deep copy)
            originalMaterials.Clear();
            foreach (var renderer in playerRenderers)
            {
                if (renderer != null)
                {
                    // Clone materials so we can restore them later
                    Material[] clonedMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        clonedMats[i] = new Material(renderer.materials[i]);
                    }
                    originalMaterials[renderer] = clonedMats;
                }
            }
            
            // Create overlay texture for local player
            if (isLocalPlayer)
            {
                overlayTexture = new Texture2D(1, 1);
                overlayTexture.SetPixel(0, 0, Color.white);
                overlayTexture.Apply();
            }
            
            Debug.Log($"InvisibilityEffect.Initialize: Found {playerRenderers.Count} renderers (isLocalPlayer={isLocalPlayer})");
        }
        
        /// <summary>
        /// Apply invisibility visual effect
        /// </summary>
        public void SetInvisible(bool invisible)
        {
            isInvisible = invisible;
            
            if (playerRenderers.Count == 0)
            {
                Debug.LogWarning("[InvisibilityEffect] No renderers found! Re-initializing...");
                Initialize(isLocalPlayer);
            }
            
            int processedCount = 0;
            
            foreach (var renderer in playerRenderers)
            {
                if (renderer == null) continue;
                processedCount++;
                
                if (invisible)
                {
                    if (isLocalPlayer)
                    {
                        // Local player: make semi-transparent
                        MakeRendererTransparent(renderer, invisibleAlpha);
                    }
                    else
                    {
                        // Other players: completely invisible
                        renderer.enabled = false;
                    }
                    
                    // Disable shadows for all
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                else
                {
                    // Restore visibility
                    renderer.enabled = true;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    RestoreRendererMaterials(renderer);
                }
            }
            
            // Screen overlay for local player
            if (isLocalPlayer)
            {
                showOverlay = invisible;
                overlayFadeTimer = 0f;
            }
            
            Debug.Log($"InvisibilityEffect: SetInvisible({invisible}) for {(isLocalPlayer ? "local" : "remote")} player - processed {processedCount} renderers");
        }
        
        /// <summary>
        /// Makes a renderer transparent. Supports both URP and Built-in RP shaders.
        /// </summary>
        private void MakeRendererTransparent(Renderer renderer, float alpha)
        {
            foreach (var mat in renderer.materials)
            {
                // === URP Lit/Simple Lit Shader ===
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                    
                    // URP: Set surface type to Transparent
                    if (mat.HasProperty("_Surface"))
                    {
                        mat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
                    }
                    
                    // URP blend mode
                    mat.SetFloat("_Blend", 0); // 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    
                    // URP keywords
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    
                    Debug.Log($"[InvisibilityEffect] URP shader: {mat.name} alpha -> {alpha}");
                }
                // === Built-in RP Standard Shader (fallback) ===
                else if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                    
                    mat.SetFloat("_Mode", 3); // Transparent mode
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    
                    Debug.Log($"[InvisibilityEffect] Built-in shader: {mat.name} alpha -> {alpha}");
                }
                else
                {
                    // Unknown shader - just try to disable renderer as last resort
                    Debug.LogWarning($"[InvisibilityEffect] Unknown shader on {mat.name} ({mat.shader.name}) - no _BaseColor or _Color property");
                }
            }
        }
        
        private void RestoreRendererMaterials(Renderer renderer)
        {
            if (originalMaterials.TryGetValue(renderer, out Material[] originalMats))
            {
                // Restore from our cloned originals
                Material[] restoredMats = new Material[originalMats.Length];
                for (int i = 0; i < originalMats.Length; i++)
                {
                    restoredMats[i] = new Material(originalMats[i]);
                }
                renderer.materials = restoredMats;
            }
        }
        
        private void Update()
        {
            if (showOverlay && isLocalPlayer)
            {
                overlayFadeTimer += Time.deltaTime;
            }
        }
        
        /// <summary>
        /// Screen overlay for local player - shows a subtle tint to indicate invisibility.
        /// OnGUI is the simplest way to draw a full-screen overlay without extra UI setup.
        /// </summary>
        private void OnGUI()
        {
            if (!showOverlay || !isLocalPlayer || overlayTexture == null) return;
            
            // Pulsing alpha effect
            float pulse = 0.5f + 0.5f * Mathf.Sin(overlayFadeTimer * Mathf.PI / overlayPulseDuration);
            Color tint = screenTintColor;
            tint.a = screenTintColor.a * pulse;
            
            GUI.color = tint;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);
            GUI.color = Color.white;
        }
        
        private void OnDestroy()
        {
            // Cleanup cloned materials
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Value != null)
                {
                    foreach (var mat in kvp.Value)
                    {
                        if (mat != null) Destroy(mat);
                    }
                }
            }
            originalMaterials.Clear();
            playerRenderers.Clear();
            
            if (overlayTexture != null)
                Destroy(overlayTexture);
        }
    }
}
