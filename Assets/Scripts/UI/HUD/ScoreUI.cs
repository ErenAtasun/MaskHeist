using UnityEngine;
using TMPro;

namespace MaskHeist.UI
{
    /// <summary>
    /// Displays current score with punch animation on change.
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI scoreDeltaText;

        [Header("Format")]
        [SerializeField] private string scoreFormat = "Score: {0}";
        [SerializeField] private string deltaFormat = "+{0}";

        [Header("Animation")]
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private float deltaFadeDuration = 1f;

        private int currentScore;
        private RectTransform scoreTransform;
        private float punchTimer;
        private float deltaFadeTimer;

        private void Awake()
        {
            if (scoreText == null)
                scoreText = GetComponentInChildren<TextMeshProUGUI>();
            
            scoreTransform = scoreText?.GetComponent<RectTransform>();
            
            UpdateDisplay(0);
            
            if (scoreDeltaText != null)
                scoreDeltaText.alpha = 0f;
        }

        private void OnEnable()
        {
            UIEvents.OnScoreChanged += HandleScoreChanged;
            UIEvents.OnLootCollected += HandleLootCollected;
            UIEvents.OnRoleChanged += HandleRoleChanged;
        }

        private void OnDisable()
        {
            UIEvents.OnScoreChanged -= HandleScoreChanged;
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
            if (deltaFadeTimer > 0 && scoreDeltaText != null)
            {
                deltaFadeTimer -= Time.deltaTime;
                scoreDeltaText.alpha = deltaFadeTimer / deltaFadeDuration;
            }
        }

        private void HandleScoreChanged(int newScore, int delta)
        {
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
                    // Format: "Hider | Score: 100"
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
