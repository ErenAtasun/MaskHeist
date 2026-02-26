using UnityEngine;
using Mirror;

namespace MaskHeist.Mask.Abilities
{
    /// <summary>
    /// A holographic decoy clone that runs forward and disappears after a set time.
    /// Spawned by DecoyAbility on the server, visible to all clients.
    /// The clone has no collider, it's purely visual to trick the Hider.
    /// 
    /// IMPORTANT: This prefab shares materials with the Player prefab.
    /// ApplyHologramEffect MUST create material instances to avoid
    /// contaminating the shared material asset.
    /// </summary>
    public class DecoyClone : NetworkBehaviour
    {
        [Header("Decoy Settings")]
        [SyncVar] private float moveSpeed = 7f;
        [SyncVar] private float lifetime = 4f;
        [SyncVar] private Vector3 moveDirection;
        
        [Header("Visual Settings")]
        [SerializeField] private float hologramAlpha = 0.4f;
        [SerializeField] private Color hologramTint = new Color(0.3f, 0.8f, 1f, 0.4f);
        
        private float spawnTime;
        private bool isInitialized = false;
        
        // Server-side destroy timer (replaces Invoke which breaks with Mirror's [Server] weaver)
        private float destroyTime = -1f;
        
        /// <summary>
        /// Initialize the decoy with movement parameters.
        /// Called on server after spawning.
        /// </summary>
        [Server]
        public void Initialize(Vector3 direction, float speed, float life)
        {
            moveDirection = direction.normalized;
            moveSpeed = speed;
            lifetime = life;
            
            // Set destroy timer (NOT using Invoke — Mirror weaver breaks [Server] + Invoke)
            destroyTime = Time.time + lifetime;
            Debug.Log($"[DecoyClone] Initialized. Will destroy at {destroyTime} (in {lifetime}s)");
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            spawnTime = Time.time;
            isInitialized = true;
            
            // Apply holographic visual effect with INSTANCED materials
            ApplyHologramEffect();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Move forward
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            
            // Fade out near end of life (client-side visual only)
            float elapsed = Time.time - spawnTime;
            float remainingRatio = 1f - (elapsed / lifetime);
            
            if (remainingRatio < 0.3f && remainingRatio > 0f)
            {
                float flicker = Mathf.PingPong(Time.time * 10f, 1f);
                SetRenderersAlpha(hologramAlpha * remainingRatio * (0.5f + flicker * 0.5f));
            }
            
            // Server-side: destroy when timer expires
            if (isServer && destroyTime > 0f && Time.time >= destroyTime)
            {
                destroyTime = -1f; // Prevent multiple calls
                Debug.Log($"[DecoyClone] Timer expired, destroying decoy via NetworkServer.Destroy");
                NetworkServer.Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Makes all renderers on this object look like a holographic clone.
        /// CRITICAL: Creates material INSTANCES first to avoid modifying
        /// the shared material asset (which is also used by the Player).
        /// </summary>
        private void ApplyHologramEffect()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer) continue;
                
                // CRITICAL: Force material instancing by reading + writing back
                // This ensures we modify PER-INSTANCE materials, NOT the shared asset
                Material[] instancedMats = renderer.materials; // Creates instances
                
                for (int i = 0; i < instancedMats.Length; i++)
                {
                    Material mat = instancedMats[i];
                    
                    // URP Lit Shader
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = hologramTint;
                        c.a = hologramAlpha;
                        mat.SetColor("_BaseColor", c);
                        
                        if (mat.HasProperty("_Surface"))
                            mat.SetFloat("_Surface", 1); // Transparent
                        
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                    // Built-in RP Standard Shader fallback
                    else if (mat.HasProperty("_Color"))
                    {
                        Color c = hologramTint;
                        c.a = hologramAlpha;
                        mat.color = c;
                        
                        mat.SetFloat("_Mode", 3);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                }
                
                // Assign instanced materials back to renderer
                renderer.materials = instancedMats;
                
                // Disable shadows for hologram
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            
            Debug.Log("[DecoyClone] Hologram effect applied with instanced materials");
        }
        
        /// <summary>
        /// Update alpha on all renderers (for fade-out effect).
        /// </summary>
        private void SetRenderersAlpha(float alpha)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer) continue;
                
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor");
                        c.a = alpha;
                        mat.SetColor("_BaseColor", c);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup instanced materials
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null) continue;
                foreach (var mat in renderer.materials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
            Debug.Log("[DecoyClone] Destroyed and materials cleaned up");
        }
    }
}

