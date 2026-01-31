using UnityEngine;
using UnityEngine.UI;

namespace MaskHeist.UI
{
    /// <summary>
    /// Simple crosshair at screen center.
    /// Changes color when looking at interactable.
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image crosshairImage;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color interactableColor = Color.green;
        [SerializeField] private Color fadeSpeed = new Color(5f, 5f, 5f, 5f);

        [Header("Scale Animation")]
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float interactableScale = 1.2f;
        [SerializeField] private float scaleSpeed = 10f;

        private Color targetColor;
        private float targetScale;
        private RectTransform rectTransform;

        private void Awake()
        {
            if (crosshairImage == null)
                crosshairImage = GetComponent<Image>();
            
            rectTransform = GetComponent<RectTransform>();
            
            targetColor = normalColor;
            targetScale = normalScale;
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
            // Smooth color transition
            if (crosshairImage != null)
            {
                crosshairImage.color = Color.Lerp(
                    crosshairImage.color, 
                    targetColor, 
                    Time.deltaTime * fadeSpeed.r
                );
            }

            // Smooth scale transition
            if (rectTransform != null)
            {
                float currentScale = rectTransform.localScale.x;
                float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
                rectTransform.localScale = Vector3.one * newScale;
            }
        }

        private void HandleInteractableChanged(string prompt)
        {
            bool hasInteractable = !string.IsNullOrEmpty(prompt);
            targetColor = hasInteractable ? interactableColor : normalColor;
            targetScale = hasInteractable ? interactableScale : normalScale;
        }

        /// <summary>
        /// Set crosshair visibility.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (crosshairImage != null)
                crosshairImage.enabled = visible;
        }
    }
}
