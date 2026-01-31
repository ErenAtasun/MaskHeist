using UnityEngine;

namespace MaskHeist.UI
{
    /// <summary>
    /// Singleton manager for all UI elements.
    /// Provides centralized access to UI components.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD References")]
        [SerializeField] private InteractionPromptUI interactionPrompt;
        [SerializeField] private CrosshairUI crosshair;
        [SerializeField] private ScoreUI scoreUI;
        [SerializeField] private TimerUI timerUI;

        [Header("Panel References")]
        [SerializeField] private PauseMenuPanel pauseMenu;
        [SerializeField] private GameOverPanel gameOverPanel;

        // Public accessors
        public InteractionPromptUI InteractionPrompt => interactionPrompt;
        public CrosshairUI Crosshair => crosshair;
        public ScoreUI Score => scoreUI;
        public TimerUI Timer => timerUI;
        public PauseMenuPanel PauseMenu => pauseMenu;
        public GameOverPanel GameOver => gameOverPanel;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find references if not assigned
            FindUIReferences();
        }

        private void Update()
        {
            // ESC key for pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void FindUIReferences()
        {
            if (interactionPrompt == null)
                interactionPrompt = GetComponentInChildren<InteractionPromptUI>(true);
            if (crosshair == null)
                crosshair = GetComponentInChildren<CrosshairUI>(true);
            if (scoreUI == null)
                scoreUI = GetComponentInChildren<ScoreUI>(true);
            if (timerUI == null)
                timerUI = GetComponentInChildren<TimerUI>(true);
            if (pauseMenu == null)
                pauseMenu = GetComponentInChildren<PauseMenuPanel>(true);
            if (gameOverPanel == null)
                gameOverPanel = GetComponentInChildren<GameOverPanel>(true);
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Show interaction prompt with given text.
        /// </summary>
        public void ShowInteractionPrompt(string prompt)
        {
            UIEvents.TriggerInteractableChanged(prompt);
        }

        /// <summary>
        /// Hide interaction prompt.
        /// </summary>
        public void HideInteractionPrompt()
        {
            UIEvents.TriggerInteractableChanged(null);
        }

        /// <summary>
        /// Update score display.
        /// </summary>
        public void UpdateScore(int newScore, int delta = 0)
        {
            UIEvents.TriggerScoreChanged(newScore, delta);
        }

        /// <summary>
        /// Update timer display.
        /// </summary>
        public void UpdateTimer(float remainingSeconds)
        {
            UIEvents.TriggerTimerUpdated(remainingSeconds);
        }

        /// <summary>
        /// Toggle pause menu.
        /// </summary>
        public void TogglePause()
        {
            if (pauseMenu != null)
            {
                pauseMenu.Toggle();
            }
        }

        /// <summary>
        /// Show game over screen.
        /// </summary>
        public void ShowGameOver(int finalScore, bool isWinner)
        {
            UIEvents.TriggerGameOver(finalScore, isWinner);
            if (gameOverPanel != null)
            {
                gameOverPanel.Show();
            }
        }

        /// <summary>
        /// Hide all panels (for game start/restart).
        /// </summary>
        public void HideAllPanels()
        {
            if (pauseMenu != null) pauseMenu.Hide();
            if (gameOverPanel != null) gameOverPanel.Hide();
        }
    }
}
