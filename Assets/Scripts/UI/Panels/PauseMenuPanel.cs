using UnityEngine;
using UnityEngine.UI;

namespace MaskHeist.UI
{
    /// <summary>
    /// Pause menu panel with Resume, Settings, and Quit options.
    /// </summary>
    public class PauseMenuPanel : BaseUIPanel
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private bool isPaused;

        protected override void Awake()
        {
            base.Awake();
            
            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            // Cleanup listeners
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        public override void Show()
        {
            base.Show();
            isPaused = true;
            
            // Unlock cursor for menu interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Pause game (optional - you may want to handle this differently for multiplayer)
            Time.timeScale = 0f;
            
            UIEvents.TriggerGamePaused(true);
        }

        public override void Hide()
        {
            base.Hide();
            isPaused = false;
            
            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Resume game
            Time.timeScale = 1f;
            
            UIEvents.TriggerGamePaused(false);
        }

        private void OnResumeClicked()
        {
            Hide();
        }

        private void OnSettingsClicked()
        {
            // TODO: Open settings panel
            Debug.Log("[PauseMenu] Settings clicked - not implemented yet");
        }

        private void OnQuitClicked()
        {
            // Resume time before quitting
            Time.timeScale = 1f;
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// Check if game is currently paused.
        /// </summary>
        public bool IsPaused => isPaused;
    }
}
