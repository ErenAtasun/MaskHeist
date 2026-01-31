using UnityEngine;
using TMPro;

namespace MaskHeist.UI
{
    /// <summary>
    /// Displays interaction prompt when player looks at interactable objects.
    /// Shows text like "[Left Click] Pick up Diamond"
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private string keyPromptFormat = "[LMB] {0}";

        private string currentPrompt;
        private float targetAlpha;
        private bool isShowing;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (promptText == null)
                promptText = GetComponentInChildren<TextMeshProUGUI>();

            // Start hidden
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            UIEvents.OnInteractableChanged += HandleInteractableChanged;
        }

        private void OnDisable()
        {
            UIEvents.OnInteractableChanged -= HandleInteractableChanged;
        }

        private void Update()
        {
            // Smooth fade
            if (canvasGroup != null && canvasGroup.alpha != targetAlpha)
            {
                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha, 
                    targetAlpha, 
                    Time.deltaTime / fadeDuration
                );
            }
        }

        private void HandleInteractableChanged(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                HidePrompt();
            }
            else
            {
                ShowPrompt(prompt);
            }
        }

        public void ShowPrompt(string prompt)
        {
            currentPrompt = prompt;
            isShowing = true;
            targetAlpha = 1f;

            if (promptText != null)
            {
                promptText.text = string.Format(keyPromptFormat, prompt);
            }
        }

        public void HidePrompt()
        {
            isShowing = false;
            targetAlpha = 0f;
        }
    }
}
