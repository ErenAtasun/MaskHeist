using System;
using UnityEngine;

namespace MaskHeist.UI
{
    /// <summary>
    /// Static event hub for UI communication.
    /// All UI components subscribe to these events.
    /// </summary>
    public static class UIEvents
    {
        // ==================== PLAYER ====================
        
        /// <summary>
        /// Fired when local player role changes.
        /// Parameter: role name
        /// </summary>
        public static event Action<string> OnRoleChanged;
        
        public static void TriggerRoleChanged(string roleName)
        {
            OnRoleChanged?.Invoke(roleName);
        }

        // ==================== INTERACTION ====================
        
        /// <summary>
        /// Fired when player looks at or away from an interactable.
        /// Parameter: prompt text (null or empty = no interactable)
        /// </summary>
        public static event Action<string> OnInteractableChanged;
        
        public static void TriggerInteractableChanged(string prompt)
        {
            OnInteractableChanged?.Invoke(prompt);
        }

        // ==================== SCORE ====================
        
        /// <summary>
        /// Fired when player score changes.
        /// Parameters: new score, score delta (amount added)
        /// </summary>
        public static event Action<int, int> OnScoreChanged;
        
        public static void TriggerScoreChanged(int newScore, int delta)
        {
            OnScoreChanged?.Invoke(newScore, delta);
        }

        // ==================== PER-PLAYER SCORE ====================
        
        /// <summary>
        /// Fired when a specific player's score changes.
        /// Parameters: player netId, new score, score delta
        /// </summary>
        public static event Action<uint, int, int> OnPlayerScoreChanged;
        
        public static void TriggerPlayerScoreChanged(uint netId, int newScore, int delta)
        {
            OnPlayerScoreChanged?.Invoke(netId, newScore, delta);
        }

        // ==================== TIMER ====================
        
        /// <summary>
        /// Fired every second with remaining time.
        /// Parameter: remaining seconds
        /// </summary>
        public static event Action<float> OnTimerUpdated;
        
        public static void TriggerTimerUpdated(float remainingSeconds)
        {
            OnTimerUpdated?.Invoke(remainingSeconds);
        }

        // ==================== GAME STATE ====================
        
        /// <summary>
        /// Fired when game pauses or resumes.
        /// Parameter: true = paused, false = resumed
        /// </summary>
        public static event Action<bool> OnGamePaused;
        
        public static void TriggerGamePaused(bool isPaused)
        {
            OnGamePaused?.Invoke(isPaused);
        }

        /// <summary>
        /// Fired when game ends.
        /// Parameters: final score, is winner
        /// </summary>
        public static event Action<int, bool> OnGameOver;
        
        public static void TriggerGameOver(int finalScore, bool isWinner)
        {
            OnGameOver?.Invoke(finalScore, isWinner);
        }

        // ==================== LOOT ====================
        
        /// <summary>
        /// Fired when player collects loot.
        /// Parameters: loot name, score value
        /// </summary>
        public static event Action<string, int> OnLootCollected;
        
        public static void TriggerLootCollected(string lootName, int scoreValue)
        {
            OnLootCollected?.Invoke(lootName, scoreValue);
        }

        // ==================== WEAPON/AMMO ====================
        
        /// <summary>
        /// Fired when ammo count changes.
        /// Parameters: current ammo, max ammo
        /// </summary>
        public static event Action<int, int> OnAmmoChanged;
        
        public static void TriggerAmmoChanged(int currentAmmo, int maxAmmo)
        {
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        /// <summary>
        /// Fired when weapon is equipped or unequipped.
        /// Parameter: true = has weapon
        /// </summary>
        public static event Action<bool> OnWeaponEquipped;
        
        public static void TriggerWeaponEquipped(bool hasWeapon)
        {
            OnWeaponEquipped?.Invoke(hasWeapon);
        }
    }
}
