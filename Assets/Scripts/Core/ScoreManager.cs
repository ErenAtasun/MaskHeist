using Mirror;
using UnityEngine;
using MaskHeist.UI;

namespace MaskHeist.Core
{
    /// <summary>
    /// Server-side score management.
    /// Tracks scores for all players and broadcasts updates.
    /// </summary>
    public class ScoreManager : NetworkBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Settings")]
        [Tooltip("Points for Hider successfully hiding the item")]
        public int hiderHidePoints = 100;
        
        [Tooltip("Points for Hider surviving per second during Seeking phase")]
        public int hiderSurvivalPointsPerSec = 5;
        
        [Tooltip("Points for Seeker finding the hidden item")]
        public int seekerFindPoints = 200;
        
        [Tooltip("Points for Seeker when Hider is caught")]
        public int seekerCatchPoints = 150;

        [Header("Current Scores")]
        [SyncVar(hook = nameof(OnHiderScoreChanged))]
        private int hiderScore;
        
        [SyncVar(hook = nameof(OnSeekerScoreChanged))]
        private int totalSeekerScore;

        // Events for UI updates
        public System.Action<int, int> OnScoreUpdated; // (score, delta)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== SCORE METHODS ====================

        /// <summary>
        /// Add points to Hider (called on server).
        /// </summary>
        [Server]
        public void AddHiderScore(int points, string reason = "")
        {
            int oldScore = hiderScore;
            hiderScore += points;
            
            Debug.Log($"[ScoreManager] Hider +{points} ({reason}) = {hiderScore}");
            
            // Notify clients
            RpcBroadcastScoreUpdate("Hider", hiderScore, points);
        }

        /// <summary>
        /// Add points to Seekers (called on server).
        /// </summary>
        [Server]
        public void AddSeekerScore(int points, string reason = "")
        {
            int oldScore = totalSeekerScore;
            totalSeekerScore += points;
            
            Debug.Log($"[ScoreManager] Seekers +{points} ({reason}) = {totalSeekerScore}");
            
            // Notify clients
            RpcBroadcastScoreUpdate("Seekers", totalSeekerScore, points);
        }

        // ==================== GAME EVENTS ====================

        /// <summary>
        /// Called when Hider places the item.
        /// </summary>
        [Server]
        public void OnItemHidden()
        {
            AddHiderScore(hiderHidePoints, "Eşya saklandı");
        }

        /// <summary>
        /// Called when Seeker finds the hidden item.
        /// </summary>
        [Server]
        public void OnItemFound()
        {
            AddSeekerScore(seekerFindPoints, "Eşya bulundu!");
        }

        /// <summary>
        /// Award survival points to Hider (call periodically).
        /// </summary>
        [Server]
        public void AwardSurvivalPoints()
        {
            AddHiderScore(hiderSurvivalPointsPerSec, "Hayatta kalma");
        }

        // ==================== HOOKS & RPCs ====================

        private void OnHiderScoreChanged(int oldVal, int newVal)
        {
            // Local client update
            UIEvents.TriggerScoreChanged(newVal, newVal - oldVal);
        }

        private void OnSeekerScoreChanged(int oldVal, int newVal)
        {
            // Local client update
            UIEvents.TriggerScoreChanged(newVal, newVal - oldVal);
        }

        [ClientRpc]
        private void RpcBroadcastScoreUpdate(string team, int newScore, int delta)
        {
            Debug.Log($"[ScoreManager] {team}: {newScore} (+{delta})");
            OnScoreUpdated?.Invoke(newScore, delta);
        }

        // ==================== UTILITY ====================

        public int GetHiderScore() => hiderScore;
        public int GetSeekerScore() => totalSeekerScore;

        [Server]
        public void ResetScores()
        {
            hiderScore = 0;
            totalSeekerScore = 0;
        }
    }
}
