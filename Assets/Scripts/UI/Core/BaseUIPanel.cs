using UnityEngine;

namespace MaskHeist.UI
{
    /// <summary>
    /// Base class for all UI panels that can be shown/hidden.
    /// Provides fade animation support via CanvasGroup.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUIPanel : MonoBehaviour
    {
        [Header("Panel Settings")]
        [SerializeField] protected float fadeDuration = 0.2f;
        [SerializeField] protected bool startHidden = true;

        protected CanvasGroup canvasGroup;
        protected bool isVisible;
        
        private Coroutine fadeCoroutine;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (startHidden)
            {
                SetVisibility(false, instant: true);
            }
        }

        /// <summary>
        /// Show the panel with optional fade animation.
        /// </summary>
        public virtual void Show()
        {
            SetVisibility(true);
        }

        /// <summary>
        /// Hide the panel with optional fade animation.
        /// </summary>
        public virtual void Hide()
        {
            SetVisibility(false);
        }

        /// <summary>
        /// Toggle panel visibility.
        /// </summary>
        public virtual void Toggle()
        {
            if (isVisible)
                Hide();
            else
                Show();
        }

        protected void SetVisibility(bool visible, bool instant = false)
        {
            isVisible = visible;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (instant || fadeDuration <= 0)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(visible));
            }
        }

        private System.Collections.IEnumerator FadeRoutine(bool fadeIn)
        {
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            // Enable interaction immediately when fading in
            if (fadeIn)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            // Disable interaction after fading out
            if (!fadeIn)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            fadeCoroutine = null;
        }

        /// <summary>
        /// Called when panel becomes visible.
        /// Override for custom behavior.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Called when panel becomes hidden.
        /// Override for custom behavior.
        /// </summary>
        protected virtual void OnHide() { }
    }
}
