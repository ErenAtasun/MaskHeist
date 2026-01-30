using UnityEngine;

namespace MaskHeist.Loot
{
    public enum LootCategory
    {
        Small,
        Medium,
        Large,
        Special
    }

    [CreateAssetMenu(fileName = "NewLoot", menuName = "MaskHeist/Loot Data")]
    public class LootData : ScriptableObject
    {
        [Header("General Info")]
        public string lootName;
        public LootCategory category;
        public int scoreValue = 10;
        
        [Header("Visuals")]
        public GameObject prefab;
        public Sprite icon;
        
        [Header("Gameplay")]
        [Tooltip("Time in seconds to pick up/steal this item")]
        public float stealDuration = 1.5f;
        
        [Tooltip("Time in seconds needed to hide/place this item")]
        public float hideDuration = 1.0f;
    }
}
