using UnityEngine;
using TMPro;

namespace MaskHeist.UI
{
    /// <summary>
    /// Displays game timer with color change when time is low.
    /// </summary>
    public class TimerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Format")]
        [SerializeField] private string timerFormat = "{0}:{1:00}";

        [Header("Warning Settings")]
        [SerializeField] private float warningThreshold = 30f;
        [SerializeField] private float criticalThreshold = 10f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        [Header("Critical Animation")]
        [SerializeField] private bool pulseOnCritical = true;
        [SerializeField] private float pulseSpeed = 3f;

        private float remainingTime;
        private Color targetColor;

        private void Awake()
        {
            if (timerText == null)
                timerText = GetComponentInChildren<TextMeshProUGUI>();
            
            targetColor = normalColor;
        }

        private void OnEnable()
        {
            UIEvents.OnTimerUpdated += HandleTimerUpdated;
        }

        private void OnDisable()
        {
            UIEvents.OnTimerUpdated -= HandleTimerUpdated;
        }

        private void Update()
        {
            // Pulse animation for critical time
            if (pulseOnCritical && remainingTime <= criticalThreshold && remainingTime > 0)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) / 2f;
                if (timerText != null)
                {
                    timerText.color = Color.Lerp(criticalColor, Color.white, pulse * 0.3f);
                }
            }
            else if (timerText != null)
            {
                timerText.color = Color.Lerp(timerText.color, targetColor, Time.deltaTime * 5f);
            }
        }

        private void HandleTimerUpdated(float seconds)
        {
            remainingTime = seconds;
            UpdateDisplay(seconds);
            UpdateColor(seconds);
        }

        private void UpdateDisplay(float seconds)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            
            timerText.text = string.Format(timerFormat, minutes, secs);
        }

        private void UpdateColor(float seconds)
        {
            if (seconds <= criticalThreshold)
            {
                targetColor = criticalColor;
            }
            else if (seconds <= warningThreshold)
            {
                targetColor = warningColor;
            }
            else
            {
                targetColor = normalColor;
            }
        }

        /// <summary>
        /// Manually set timer display (for initialization).
        /// </summary>
        public void SetTime(float seconds)
        {
            HandleTimerUpdated(seconds);
        }
    }
}
