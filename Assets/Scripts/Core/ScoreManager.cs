using Mirror;
using UnityEngine;
using MaskHeist.UI;

namespace MaskHeist.Core
{
    /// <summary>
    /// Server-side score management with per-player tracking.
    /// Uses SyncDictionary so all clients see individual player scores.
    /// </summary>
    public class ScoreManager : NetworkBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Hider Score Settings")]
        [Tooltip("Points for Hider successfully hiding the item")]
        public int hiderHidePoints = 100;
        
        [Tooltip("Points for Hider surviving (awarded every survivalInterval seconds)")]
        public int hiderSurvivalPoints = 10;
        
        [Tooltip("How often survival points are awarded (seconds)")]
        public float survivalInterval = 5f;
        
        [Tooltip("Points for Hider killing a Seeker")]
        public int hiderKillPoints = 75;
        
        [Tooltip("Points for catching a Seeker via trap")]
        public int hiderTrapCatchPoints = 50;
        
        [Tooltip("Bonus for Hider winning the round (time ran out)")]
        public int hiderWinBonus = 150;

        [Header("Seeker Score Settings")]
        [Tooltip("Points for Seeker finding the hidden item")]
        public int seekerFindPoints = 200;
        
        [Tooltip("Bonus for all Seekers when team wins")]
        public int seekerTeamWinBonus = 50;

        [Header("Current Scores")]
        /// <summary>
        /// Per-player score dictionary: netId -> score.
        /// Automatically synced to all clients.
        /// </summary>
        public readonly SyncDictionary<uint, int> playerScores = new SyncDictionary<uint, int>();
        
        // Backward compatibility: team scores
        [SyncVar(hook = nameof(OnHiderScoreChanged))]
        private int hiderScore;
        
        [SyncVar(hook = nameof(OnSeekerScoreChanged))]
        private int totalSeekerScore;

        // Events
        public System.Action<int, int> OnScoreUpdated; // (score, delta)
        public System.Action OnItemFoundEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== PER-PLAYER SCORE METHODS ====================

        /// <summary>
        /// Add points to a specific player (called on server).
        /// </summary>
        [Server]
        public void AddPlayerScore(NetworkIdentity player, int points, string reason = "")
        {
            if (player == null) return;
            
            uint netId = player.netId;
            
            if (!playerScores.ContainsKey(netId))
                playerScores[netId] = 0;
            
            playerScores[netId] += points;
            
            Debug.Log($"[ScoreManager] Player {player.gameObject.name} (#{netId}) +{points} ({reason}) = {playerScores[netId]}");
            
            // Notify all clients about this specific player's score change
            RpcPlayerScoreUpdated(netId, playerScores[netId], points, reason);
        }

        /// <summary>
        /// Get a specific player's score.
        /// </summary>
        public int GetPlayerScore(uint netId)
        {
            return playerScores.TryGetValue(netId, out int score) ? score : 0;
        }

        /// <summary>
        /// Get a specific player's score by NetworkIdentity.
        /// </summary>
        public int GetPlayerScore(NetworkIdentity player)
        {
            return player != null ? GetPlayerScore(player.netId) : 0;
        }

        // ==================== TEAM SCORE METHODS (backward compat) ====================

        [Server]
        public void AddHiderScore(int points, string reason = "")
        {
            hiderScore += points;
            Debug.Log($"[ScoreManager] Hider Team +{points} ({reason}) = {hiderScore}");
            RpcBroadcastScoreUpdate("Hider", hiderScore, points);
        }

        [Server]
        public void AddSeekerScore(int points, string reason = "")
        {
            totalSeekerScore += points;
            Debug.Log($"[ScoreManager] Seeker Team +{points} ({reason}) = {totalSeekerScore}");
            RpcBroadcastScoreUpdate("Seekers", totalSeekerScore, points);
        }

        // ==================== GAME EVENTS ====================

        /// <summary>
        /// Called when Hider places the item.
        /// </summary>
        [Server]
        public void OnItemHidden(NetworkIdentity hiderPlayer)
        {
            AddPlayerScore(hiderPlayer, hiderHidePoints, "Eşya saklandı");
            AddHiderScore(hiderHidePoints, "Eşya saklandı");
        }

        /// <summary>
        /// Overload for backward compatibility (no player reference).
        /// </summary>
        [Server]
        public void OnItemHidden()
        {
            // Find hider and award
            var hider = FindHiderPlayer();
            if (hider != null)
                OnItemHidden(hider.netIdentity);
            else
                AddHiderScore(hiderHidePoints, "Eşya saklandı");
        }

        /// <summary>
        /// Called when a Seeker finds the hidden item.
        /// </summary>
        [Server]
        public void OnItemFound(NetworkIdentity finderPlayer)
        {
            // Award finder personal score
            AddPlayerScore(finderPlayer, seekerFindPoints, "Eşya bulundu!");
            AddSeekerScore(seekerFindPoints, "Eşya bulundu!");
            
            OnItemFoundEvent?.Invoke();
        }

        /// <summary>
        /// Overload for backward compatibility.
        /// </summary>
        [Server]
        public void OnItemFound()
        {
            AddSeekerScore(seekerFindPoints, "Eşya bulundu!");
            OnItemFoundEvent?.Invoke();
        }

        /// <summary>
        /// Award survival points to Hider. Called periodically from GameFlowManager.
        /// </summary>
        [Server]
        public void AwardSurvivalPoints()
        {
            var hider = FindHiderPlayer();
            if (hider != null)
            {
                AddPlayerScore(hider.netIdentity, hiderSurvivalPoints, "Hayatta kalma");
            }
            AddHiderScore(hiderSurvivalPoints, "Hayatta kalma");
        }

        /// <summary>
        /// Called when Hider kills a Seeker.
        /// </summary>
        [Server]
        public void OnSeekerKilled(NetworkIdentity killer)
        {
            if (killer != null)
            {
                AddPlayerScore(killer, hiderKillPoints, "Seeker öldürüldü");
            }
            AddHiderScore(hiderKillPoints, "Seeker öldürüldü");
        }

        /// <summary>
        /// Called when a trap catches a Seeker.
        /// </summary>
        [Server]
        public void OnTrapCatch(NetworkIdentity trapOwner)
        {
            if (trapOwner != null)
            {
                AddPlayerScore(trapOwner, hiderTrapCatchPoints, "Tuzakla yakalandı");
            }
            AddHiderScore(hiderTrapCatchPoints, "Tuzakla yakalandı");
        }

        /// <summary>
        /// Award round-end bonuses based on winner.
        /// </summary>
        [Server]
        public void AwardRoundEndBonus(PlayerRole winnerRole)
        {
            var allPlayers = FindObjectsOfType<MaskHeistGamePlayer>();
            
            foreach (var player in allPlayers)
            {
                if (player.role == winnerRole)
                {
                    if (winnerRole == PlayerRole.Hider)
                    {
                        AddPlayerScore(player.netIdentity, hiderWinBonus, "Round kazanma bonusu");
                    }
                    else if (winnerRole == PlayerRole.Seeker)
                    {
                        AddPlayerScore(player.netIdentity, seekerTeamWinBonus, "Takım kazanma bonusu");
                    }
                }
            }
        }

        // ==================== HOOKS & RPCs ====================

        private void OnHiderScoreChanged(int oldVal, int newVal)
        {
            UIEvents.TriggerScoreChanged(newVal, newVal - oldVal);
        }

        private void OnSeekerScoreChanged(int oldVal, int newVal)
        {
            UIEvents.TriggerScoreChanged(newVal, newVal - oldVal);
        }

        [ClientRpc]
        private void RpcBroadcastScoreUpdate(string team, int newScore, int delta)
        {
            Debug.Log($"[ScoreManager] {team}: {newScore} (+{delta})");
            OnScoreUpdated?.Invoke(newScore, delta);
        }

        [ClientRpc]
        private void RpcPlayerScoreUpdated(uint netId, int newScore, int delta, string reason)
        {
            Debug.Log($"[ScoreManager] Player #{netId}: {newScore} (+{delta}) - {reason}");
            
            // Fire per-player event
            UIEvents.TriggerPlayerScoreChanged(netId, newScore, delta);
            
            // If this is the local player, also update the generic score event
            if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == netId)
            {
                UIEvents.TriggerScoreChanged(newScore, delta);
            }
        }

        // ==================== UTILITY ====================

        public int GetHiderScore() => hiderScore;
        public int GetSeekerScore() => totalSeekerScore;

        [Server]
        public void ResetScores()
        {
            hiderScore = 0;
            totalSeekerScore = 0;
            playerScores.Clear();
        }

        private MaskHeistGamePlayer FindHiderPlayer()
        {
            foreach (var player in FindObjectsOfType<MaskHeistGamePlayer>())
            {
                if (player.role == PlayerRole.Hider)
                    return player;
            }
            return null;
        }
    }
}
