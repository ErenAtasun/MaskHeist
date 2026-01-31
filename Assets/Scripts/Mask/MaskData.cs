using UnityEngine;

namespace MaskHeist.Mask
{
    /// <summary>
    /// ScriptableObject that defines a mask's properties and abilities.
    /// All masks have invisibility (common), but duration varies.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMask", menuName = "MaskHeist/Mask Data")]
    public class MaskData : ScriptableObject
    {
        [Header("General Info")]
        public string maskName = "Default Mask";
        [TextArea] public string description;
        public Sprite icon;
        public Color maskColor = Color.white;
        
        [Header("Visual")]
        public GameObject maskPrefab; // 3D model of the mask
        
        [Header("Invisibility (All Masks Have This)")]
        [Tooltip("Duration of invisibility in seconds (Default: 5s, Shadow: 30s)")]
        public float invisibilityDuration = 5f;
        
        [Tooltip("Cooldown between uses in seconds")]
        public float invisibilityCooldown = 45f;
        
        [Tooltip("Extra footstep noise while invisible (0.1 = 10% louder)")]
        [Range(0f, 0.5f)]
        public float invisibilityNoiseMultiplier = 0.15f;
        
        [Header("Unique Ability (Optional - in addition to invisibility)")]
        [Tooltip("Special ability unique to this mask")]
        public MaskAbilityType uniqueAbilityType = MaskAbilityType.None;
        
        [Tooltip("Unique ability duration in seconds")]
        public float uniqueAbilityDuration = 5f;
        
        [Tooltip("Unique ability cooldown in seconds")]
        public float uniqueAbilityCooldown = 60f;
        
        [Header("Ability-Specific Settings")]
        [Tooltip("For Sprinter: Speed multiplier")]
        public float speedMultiplier = 1.5f;
    }
    
    /// <summary>
    /// Types of unique abilities masks can have (in addition to invisibility)
    /// </summary>
    public enum MaskAbilityType
    {
        None,           // Only invisibility, no extra ability
        Tracker,        // See footprints/trails
        Scanner,        // Short-range ping for loot
        Sprinter,       // Extra speed boost
        Silent,         // Reduced footstep noise
        Disruptor       // Disable nearby traps
    }
}
