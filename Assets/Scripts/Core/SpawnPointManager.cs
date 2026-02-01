using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MaskHeist.Core;

namespace MaskHeist.Spawn
{
    /// <summary>
    /// Manages all spawn points in the scene.
    /// </summary>
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        [Header("Fallback")]
        [SerializeField] private Vector3 fallbackSpawnPosition = new Vector3(0, 1, 0);

        private List<SpawnPoint> hiderSpawnPoints = new List<SpawnPoint>();
        private List<SpawnPoint> seekerSpawnPoints = new List<SpawnPoint>();
        private int lastHiderIndex = -1;
        private int lastSeekerIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find all spawn points in scene
            RefreshSpawnPoints();
        }

        public void RefreshSpawnPoints()
        {
            hiderSpawnPoints.Clear();
            seekerSpawnPoints.Clear();

            var allSpawnPoints = FindObjectsOfType<SpawnPoint>();
            foreach (var sp in allSpawnPoints)
            {
                switch (sp.spawnRole)
                {
                    case PlayerRole.Hider:
                        hiderSpawnPoints.Add(sp);
                        break;
                    case PlayerRole.Seeker:
                        seekerSpawnPoints.Add(sp);
                        break;
                }
            }

            Debug.Log($"[SpawnPointManager] Found {hiderSpawnPoints.Count} Hider, {seekerSpawnPoints.Count} Seeker spawn points");
        }

        /// <summary>
        /// Get spawn position for a specific role.
        /// </summary>
        public Vector3 GetSpawnPosition(PlayerRole role)
        {
            SpawnPoint spawnPoint = GetSpawnPoint(role);
            return spawnPoint != null ? spawnPoint.transform.position : fallbackSpawnPosition;
        }

        /// <summary>
        /// Get spawn rotation for a specific role.
        /// </summary>
        public Quaternion GetSpawnRotation(PlayerRole role)
        {
            SpawnPoint spawnPoint = GetSpawnPoint(role);
            return spawnPoint != null ? spawnPoint.transform.rotation : Quaternion.identity;
        }

        private SpawnPoint GetSpawnPoint(PlayerRole role)
        {
            List<SpawnPoint> points;
            ref int lastIndex = ref lastHiderIndex;

            switch (role)
            {
                case PlayerRole.Hider:
                    points = hiderSpawnPoints;
                    lastIndex = ref lastHiderIndex;
                    break;
                case PlayerRole.Seeker:
                    points = seekerSpawnPoints;
                    lastIndex = ref lastSeekerIndex;
                    break;
                default:
                    return null;
            }

            if (points.Count == 0) return null;

            // Round-robin selection
            lastIndex = (lastIndex + 1) % points.Count;
            return points[lastIndex];
        }

        /// <summary>
        /// Get a random spawn position for a role.
        /// </summary>
        public Vector3 GetRandomSpawnPosition(PlayerRole role)
        {
            var points = role == PlayerRole.Hider ? hiderSpawnPoints : seekerSpawnPoints;
            if (points.Count == 0) return fallbackSpawnPosition;

            int randomIndex = Random.Range(0, points.Count);
            return points[randomIndex].transform.position;
        }
    }
}
