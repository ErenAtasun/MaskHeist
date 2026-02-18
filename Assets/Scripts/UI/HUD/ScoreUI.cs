using UnityEngine;
using TMPro;
using Mirror;

namespace MaskHeist.UI
{
    /// <summary>
    /// Displays current player's personal score with punch animation on change.
    /// Shows role and personal score (not team score).
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI scoreDeltaText;
        [SerializeField] private TextMeshProUGUI reasonText;

        [Header("Format")]
        [SerializeField] private string scoreFormat = "Score: {0}";
        [SerializeField] private string deltaFormat = "+{0}";

        [Header("Animation")]
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private float deltaFadeDuration = 1.5f;

        private int currentScore;
        private RectTransform scoreTransform;
        private float punchTimer;
        private float deltaFadeTimer;
        private string currentRole;

        private void Awake()
        {
            if (scoreText == null)
                scoreText = GetComponentInChildren<TextMeshProUGUI>();
            
            scoreTransform = scoreText?.GetComponent<RectTransform>();
            
            UpdateDisplay(0);
            
            if (scoreDeltaText != null)
                scoreDeltaText.alpha = 0f;
            if (reasonText != null)
                reasonText.alpha = 0f;
        }

        private void OnEnable()
        {
            UIEvents.OnScoreChanged += HandleScoreChanged;
            UIEvents.OnPlayerScoreChanged += HandlePlayerScoreChanged;
            UIEvents.OnLootCollected += HandleLootCollected;
            UIEvents.OnRoleChanged += HandleRoleChanged;
        }

        private void OnDisable()
        {
            UIEvents.OnScoreChanged -= HandleScoreChanged;
            UIEvents.OnPlayerScoreChanged -= HandlePlayerScoreChanged;
            UIEvents.OnLootCollected -= HandleLootCollected;
            UIEvents.OnRoleChanged -= HandleRoleChanged;
        }

        private void Update()
        {
            // Punch animation
            if (punchTimer > 0)
            {
                punchTimer -= Time.deltaTime;
                float t = punchTimer / punchDuration;
                float scale = Mathf.Lerp(1f, punchScale, t);
                
                if (scoreTransform != null)
                    scoreTransform.localScale = Vector3.one * scale;
            }

            // Delta fade out
            if (deltaFadeTimer > 0)
            {
                deltaFadeTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(deltaFadeTimer / deltaFadeDuration);
                
                if (scoreDeltaText != null)
                    scoreDeltaText.alpha = alpha;
                if (reasonText != null)
                    reasonText.alpha = alpha;
            }
        }

        /// <summary>
        /// Handles per-player score event — only updates if it's the local player.
        /// </summary>
        private void HandlePlayerScoreChanged(uint netId, int newScore, int delta)
        {
            // Only show our own score
            if (NetworkClient.localPlayer == null) return;
            if (NetworkClient.localPlayer.netId != netId) return;
            
            currentScore = newScore;
            UpdateDisplay(newScore);

            if (delta > 0)
            {
                ShowDelta(delta);
                PlayPunchAnimation();
            }
        }

        private void HandleScoreChanged(int newScore, int delta)
        {
            // This is now a fallback — per-player event is preferred
            // Only update if we haven't received a per-player event
            if (NetworkClient.localPlayer != null) return;
            
            currentScore = newScore;
            UpdateDisplay(newScore);

            if (delta > 0)
            {
                ShowDelta(delta);
                PlayPunchAnimation();
            }
        }

        private void HandleLootCollected(string lootName, int scoreValue)
        {
            currentScore += scoreValue;
            UpdateDisplay(currentScore);
            ShowDelta(scoreValue);
            PlayPunchAnimation();
        }

        private void HandleRoleChanged(string roleName)
        {
            currentRole = roleName;
            currentScore = 0; // Reset score display on role change
            UpdateDisplay(currentScore);
        }

        private void UpdateDisplay(int score)
        {
            if (scoreText != null)
            {
                if (string.IsNullOrEmpty(currentRole))
                {
                    scoreText.text = string.Format(scoreFormat, score);
                }
                else
                {
                    scoreText.text = $"{currentRole} | {string.Format(scoreFormat, score)}";
                }
            }
        }

        private void ShowDelta(int delta)
        {
            if (scoreDeltaText != null)
            {
                scoreDeltaText.text = string.Format(deltaFormat, delta);
                scoreDeltaText.alpha = 1f;
                deltaFadeTimer = deltaFadeDuration;
            }
        }

        private void PlayPunchAnimation()
        {
            punchTimer = punchDuration;
        }

        /// <summary>
        /// Get current score value.
        /// </summary>
        public int GetScore() => currentScore;

        /// <summary>
        /// Reset score to zero.
        /// </summary>
        public void ResetScore()
        {
            currentScore = 0;
            UpdateDisplay(0);
        }
    }
}
