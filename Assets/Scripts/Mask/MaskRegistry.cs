using UnityEngine;
using System.Collections.Generic;

namespace MaskHeist.Mask
{
    /// <summary>
    /// Singleton registry that holds all available masks in the game.
    /// Place this on a manager object in the scene.
    /// </summary>
    public class MaskRegistry : MonoBehaviour
    {
        public static MaskRegistry Instance { get; private set; }
        
        [Header("Available Masks")]
        [Tooltip("All masks available in the game")]
        [SerializeField] private List<MaskData> availableMasks = new List<MaskData>();
        
        public IReadOnlyList<MaskData> AvailableMasks => availableMasks;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        /// <summary>
        /// Get mask by index
        /// </summary>
        public MaskData GetMask(int index)
        {
            if (index >= 0 && index < availableMasks.Count)
                return availableMasks[index];
            return null;
        }
        
        /// <summary>
        /// Get mask by name
        /// </summary>
        public MaskData GetMaskByName(string maskName)
        {
            return availableMasks.Find(m => m.maskName == maskName);
        }
        
        /// <summary>
        /// Get index of a mask
        /// </summary>
        public int GetMaskIndex(MaskData mask)
        {
            return availableMasks.IndexOf(mask);
        }
    }
}
