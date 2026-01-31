using UnityEngine;

namespace MaskHeist.Gameplay
{
    /// <summary>
    /// ScriptableObject for hideable item types.
    /// Create different items: car exhaust, strange statue, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHideableItem", menuName = "MaskHeist/Hideable Item Data")]
    public class HideableItemData : ScriptableObject
    {
        [Header("General Info")]
        public string itemName = "Mysterious Object";
        
        [TextArea(2, 4)]
        public string description = "A suspicious looking item that doesn't belong here.";

        [Header("Visuals")]
        public GameObject prefab;
        public Sprite icon;

        [Header("Gameplay")]
        [Tooltip("How far in front of player the item floats when held")]
        public float holdDistance = 2f;
        
        [Tooltip("Height offset when held")]
        public float holdHeight = 1f;
    }
}
