using UnityEngine;
using Mirror;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Silent ability - Reduces or eliminates footstep sounds.
    /// Press E to activate.
    /// </summary>
    public class SilentAbility : MaskAbility
    {
        [Header("Silent Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float noiseReduction = 0.9f; // 90% quieter
        
        // Reference to audio source or footstep system
        private AudioSource footstepAudio;
        private float originalVolume;
        
        private void Start()
        {
            // Find footstep audio source if exists
            footstepAudio = GetComponentInChildren<AudioSource>();
            if (footstepAudio != null)
            {
                originalVolume = footstepAudio.volume;
            }
        }
        
        protected override void OnAbilityActivated()
        {
            Debug.Log($"[Server] {gameObject.name} activated Silent! ({duration}s at {noiseReduction * 100}% quieter)");
        }
        
        protected override void OnAbilityDeactivated()
        {
            Debug.Log($"[Server] {gameObject.name} Silent ended!");
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityActivated()
        {
            // Reduce footstep volume
            if (footstepAudio != null)
            {
                footstepAudio.volume = originalVolume * (1f - noiseReduction);
            }
            
            if (isLocalPlayer)
            {
                Debug.Log("Silent mode active - your footsteps are muffled!");
            }
        }
        
        [ClientRpc]
        protected override void RpcOnAbilityDeactivated()
        {
            // Restore footstep volume
            if (footstepAudio != null)
            {
                footstepAudio.volume = originalVolume;
            }
            
            if (isLocalPlayer)
            {
                Debug.Log("Silent mode ended.");
            }
        }
    }
}
