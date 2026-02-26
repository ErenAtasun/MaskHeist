using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Handles the visual effects of invisibility.
    /// Supports both URP and Built-in Render Pipeline.
    /// This is NOT a NetworkBehaviour - all network logic is in PlayerMask.
    /// 
    /// Instead of cloning entire materials, we store only the specific properties
    /// we modify (color, surface type, blend modes, keywords, render queue) and
    /// explicitly restore them. This avoids issues with URP shader keyword copying.
    /// </summary>
    public class InvisibilityEffect : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float invisibleAlpha = 0.3f;
        [SerializeField] private Color screenTintColor = new Color(0.3f, 0.6f, 1f, 0.15f);
        
        private List<Renderer> playerRenderers = new List<Renderer>();
        private bool isInvisible = false;
        private bool isLocalPlayer = false;
        
        // Stores original colors per renderer (by material index)
        // Simple and reliable — we only need to save/restore colors
        private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
        
        // Stores original enabled state per renderer
        // Prevents initially-disabled renderers (e.g. Capsule) from being forcefully enabled
        private Dictionary<Renderer, bool> originalEnabledStates = new Dictionary<Renderer, bool>();
        
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
            originalEnabledStates.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                playerRenderers.Add(r);
                originalEnabledStates[r] = r.enabled; // Save original enabled state
            }
            
            // Capture original colors for ALL renderers
            originalColors.Clear();
            foreach (var renderer in playerRenderers)
            {
                if (renderer == null) continue;
                SaveOriginalColors(renderer);
            }
            
            // Create overlay texture for local player
            if (isLocalPlayer)
            {
                overlayTexture = new Texture2D(1, 1);
                overlayTexture.SetPixel(0, 0, Color.white);
                overlayTexture.Apply();
            }
            
            Debug.Log($"InvisibilityEffect.Initialize: Found {playerRenderers.Count} renderers, saved {originalColors.Count} color sets (isLocalPlayer={isLocalPlayer})");
        }
        
        /// <summary>
        /// Save original colors from shared materials before any modifications.
        /// </summary>
        private void SaveOriginalColors(Renderer renderer)
        {
            if (originalColors.ContainsKey(renderer)) return;
            
            // Use .materials (not sharedMaterials) to get per-instance copies
            var mats = renderer.materials;
            Color[] colors = new Color[mats.Length];
            
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) { colors[i] = Color.white; continue; }
                
                Color c;
                if (mat.HasProperty("_BaseColor"))
                    c = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    c = mat.color;
                else
                    c = Color.white;
                
                // Detect if color is contaminated by hologram tint (0.3, 0.8, 1.0, 0.4)
                // If RGB matches hologram, the shared material was dirty - use white instead
                if (Mathf.Approximately(c.r, 0.3f) && Mathf.Approximately(c.g, 0.8f) && Mathf.Approximately(c.b, 1f))
                {
                    Debug.LogWarning($"[InvisibilityEffect] Detected contaminated hologram color on {renderer.gameObject.name}, using white");
                    c = Color.white;
                }
                
                colors[i] = c;
            }
            
            // Assign instanced materials back (ensures renderer uses instances, not shared)
            renderer.materials = mats;
            
            originalColors[renderer] = colors;
        }
        
        /// <summary>
        /// Refresh the renderer cache to pick up any newly spawned renderers
        /// (e.g. mask model that is spawned after Initialize).
        /// </summary>
        private void RefreshRenderers()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (playerRenderers.Contains(r)) continue; // Already cached
                
                playerRenderers.Add(r);
                originalEnabledStates[r] = r.enabled; // Save original enabled state
                SaveOriginalColors(r);
                Debug.Log($"[InvisibilityEffect] New renderer discovered: {r.gameObject.name} (enabled={r.enabled})");
            }
        }
        
        /// <summary>
        /// Apply invisibility visual effect
        /// </summary>
        public void SetInvisible(bool invisible)
        {
            if (isInvisible == invisible) return; // Prevent duplicate calls
            isInvisible = invisible;
            
            // Refresh renderer list every time (mask model may have been spawned after init)
            RefreshRenderers();
            
            int processedCount = 0;
            
            foreach (var renderer in playerRenderers)
            {
                if (renderer == null) continue;
                processedCount++;
                
                if (invisible)
                {
                    if (isLocalPlayer)
                    {
                        MakeRendererTransparent(renderer, invisibleAlpha);
                    }
                    else
                    {
                        renderer.enabled = false;
                    }
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                else
                {
                    // Restore original enabled state (don't blindly enable all renderers)
                    bool wasEnabled = true;
                    originalEnabledStates.TryGetValue(renderer, out wasEnabled);
                    renderer.enabled = wasEnabled;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    ForceOpaqueAll(renderer);
                }
            }
            
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
            // Ensure colors are saved
            SaveOriginalColors(renderer);
            
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    // === URP ===
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                    
                    if (mat.HasProperty("_Surface"))
                        mat.SetFloat("_Surface", 1); // Transparent
                    
                    mat.SetFloat("_Blend", 0);
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }
                else if (mat.HasProperty("_Color"))
                {
                    // === Built-in RP ===
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                    
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }
            }
        }
        


        
        /// <summary>
        /// Force ALL materials on a renderer back to opaque with original colors.
        /// </summary>
        private void ForceOpaqueAll(Renderer renderer)
        {
            Color[] savedColors = null;
            originalColors.TryGetValue(renderer, out savedColors);
            
            var mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                ForceOpaque(mats[i]);
                
                // Restore original color (not just alpha)
                if (savedColors != null && i < savedColors.Length)
                {
                    if (mats[i].HasProperty("_BaseColor"))
                        mats[i].SetColor("_BaseColor", savedColors[i]);
                    else if (mats[i].HasProperty("_Color"))
                        mats[i].color = savedColors[i];
                }
            }
            renderer.materials = mats; // Assign back
        }
        
        /// <summary>
        /// Force a single material back to fully opaque mode.
        /// </summary>
        private void ForceOpaque(Material mat)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = 1f;
                mat.SetColor("_BaseColor", c);
                
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 0); // Opaque
                
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = 1f;
                mat.color = c;
                
                if (mat.HasProperty("_Mode"))
                    mat.SetFloat("_Mode", 0); // Opaque
                
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                
                mat.renderQueue = (int)RenderQueue.Geometry;
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
        /// </summary>
        private void OnGUI()
        {
            if (!showOverlay || !isLocalPlayer || overlayTexture == null) return;
            
            float pulse = 0.5f + 0.5f * Mathf.Sin(overlayFadeTimer * Mathf.PI / overlayPulseDuration);
            Color tint = screenTintColor;
            tint.a = screenTintColor.a * pulse;
            
            GUI.color = tint;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);
            GUI.color = Color.white;
        }
        
        private void OnDestroy()
        {
            originalColors.Clear();
            originalEnabledStates.Clear();
            playerRenderers.Clear();
            
            if (overlayTexture != null)
                Destroy(overlayTexture);
        }
    }
}
