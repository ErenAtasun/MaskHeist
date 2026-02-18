using UnityEngine;
using System.Collections.Generic;
using MaskHeist.Gameplay;

namespace MaskHeist.Loot
{
    /// <summary>
    /// Singleton that manages the pool of available items for each round.
    /// Provides weighted random selection for hideable items and bonus loots.
    /// Place this on a manager object in the scene.
    /// </summary>
    public class LootPool : MonoBehaviour
    {
        public static LootPool Instance { get; private set; }

        [Header("Hideable Items Pool")]
        [Tooltip("Items that Hider can be given to hide each round")]
        [SerializeField] private List<HideableItemEntry> hideableItems = new List<HideableItemEntry>();

        [Header("Bonus Loot Pool")]
        [Tooltip("Bonus loot items that can spawn on the map for Seekers to collect")]
        [SerializeField] private List<LootEntry> bonusLoots = new List<LootEntry>();

        [Header("Bonus Loot Settings")]
        [Tooltip("How many bonus loots to spawn per round")]
        [SerializeField] private int bonusLootCount = 3;

        [Header("Spawn Points")]
        [Tooltip("Possible positions where bonus loots can spawn")]
        [SerializeField] private List<Transform> lootSpawnPoints = new List<Transform>();

        // Track what was used last round to avoid repeats
        private int lastHideableIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== HIDEABLE ITEM SELECTION ====================

        /// <summary>
        /// Get a random hideable item prefab from the pool (weighted).
        /// Avoids repeating the same item as last round when possible.
        /// </summary>
        public GameObject GetRandomHideableItemPrefab()
        {
            if (hideableItems.Count == 0)
            {
                Debug.LogWarning("[LootPool] No hideable items in pool!");
                return null;
            }

            // Single item? Just return it
            if (hideableItems.Count == 1)
            {
                lastHideableIndex = 0;
                return hideableItems[0].itemData?.prefab;
            }

            // Weighted random selection (avoid last pick)
            int selectedIndex = WeightedRandomSelect(hideableItems, lastHideableIndex);
            lastHideableIndex = selectedIndex;

            var selected = hideableItems[selectedIndex];
            Debug.Log($"[LootPool] Selected hideable item: {selected.itemData?.itemName} (index {selectedIndex})");
            
            return selected.itemData?.prefab;
        }

        /// <summary>
        /// Get the HideableItemData of a random selection.
        /// </summary>
        public HideableItemData GetRandomHideableItemData()
        {
            if (hideableItems.Count == 0) return null;

            if (hideableItems.Count == 1)
            {
                lastHideableIndex = 0;
                return hideableItems[0].itemData;
            }

            int selectedIndex = WeightedRandomSelect(hideableItems, lastHideableIndex);
            lastHideableIndex = selectedIndex;

            Debug.Log($"[LootPool] Selected hideable item: {hideableItems[selectedIndex].itemData?.itemName}");
            return hideableItems[selectedIndex].itemData;
        }

        // ==================== BONUS LOOT SELECTION ====================

        /// <summary>
        /// Get a list of random bonus loot prefabs to spawn on the map.
        /// </summary>
        public List<LootSpawnInfo> GetBonusLootSpawns()
        {
            var result = new List<LootSpawnInfo>();

            if (bonusLoots.Count == 0 || lootSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[LootPool] No bonus loots or spawn points configured!");
                return result;
            }

            // Shuffle spawn points
            var shuffledPoints = new List<Transform>(lootSpawnPoints);
            ShuffleList(shuffledPoints);

            int spawnCount = Mathf.Min(bonusLootCount, shuffledPoints.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                // Random weighted selection from bonus pool
                int lootIndex = WeightedRandomSelect(bonusLoots, -1);
                var lootEntry = bonusLoots[lootIndex];

                if (lootEntry.lootData?.prefab != null && shuffledPoints[i] != null)
                {
                    result.Add(new LootSpawnInfo
                    {
                        prefab = lootEntry.lootData.prefab,
                        position = shuffledPoints[i].position,
                        rotation = shuffledPoints[i].rotation,
                        lootData = lootEntry.lootData
                    });
                }
            }

            Debug.Log($"[LootPool] Prepared {result.Count} bonus loot spawns");
            return result;
        }

        // ==================== UTILITY ====================

        /// <summary>
        /// Weighted random selection. Optionally avoids a specific index.
        /// Works with any list that has IWeightedEntry items.
        /// </summary>
        private int WeightedRandomSelect<T>(List<T> entries, int avoidIndex) where T : IWeightedEntry
        {
            // Calculate total weight (excluding avoided index)
            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i == avoidIndex && entries.Count > 1) continue;
                totalWeight += entries[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                // Fallback: uniform random
                int idx = Random.Range(0, entries.Count);
                return idx;
            }

            // Pick random value
            float randomValue = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                if (i == avoidIndex && entries.Count > 1) continue;
                
                cumulative += entries[i].Weight;
                if (randomValue <= cumulative)
                    return i;
            }

            // Fallback
            return entries.Count - 1;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    // ==================== DATA STRUCTURES ====================

    public interface IWeightedEntry
    {
        float Weight { get; }
    }

    [System.Serializable]
    public class HideableItemEntry : IWeightedEntry
    {
        public HideableItemData itemData;
        
        [Tooltip("Higher weight = more likely to be selected")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;

        public float Weight => spawnWeight;
    }

    [System.Serializable]
    public class LootEntry : IWeightedEntry
    {
        public LootData lootData;
        
        [Tooltip("Higher weight = more likely to be selected")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;

        public float Weight => spawnWeight;
    }

    public struct LootSpawnInfo
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
        public LootData lootData;
    }
}
