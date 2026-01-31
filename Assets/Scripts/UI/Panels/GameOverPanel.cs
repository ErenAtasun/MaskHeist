using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaskHeist.UI
{
    /// <summary>
    /// Game over panel showing final score and options.
    /// </summary>
    public class GameOverPanel : BaseUIPanel
    {
        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Title Text")]
        [SerializeField] private string winTitle = "VICTORY!";
        [SerializeField] private string loseTitle = "GAME OVER";
        [SerializeField] private Color winColor = Color.green;
        [SerializeField] private Color loseColor = Color.red;

        protected override void Awake()
        {
            base.Awake();
            
            // Setup button listeners
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnEnable()
        {
            UIEvents.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            UIEvents.OnGameOver -= HandleGameOver;
        }

        private void OnDestroy()
        {
            if (playAgainButton != null)
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void HandleGameOver(int finalScore, bool isWinner)
        {
            SetupDisplay(finalScore, isWinner);
            Show();
        }

        public override void Show()
        {
            base.Show();
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Setup the game over display.
        /// </summary>
        public void SetupDisplay(int finalScore, bool isWinner)
        {
            // Title
            if (titleText != null)
            {
                titleText.text = isWinner ? winTitle : loseTitle;
                titleText.color = isWinner ? winColor : loseColor;
            }

            // Score
            if (scoreText != null)
            {
                scoreText.text = $"Final Score: {finalScore}";
            }

            // Optional message
            if (messageText != null)
            {
                messageText.text = isWinner 
                    ? "Congratulations! You escaped with the loot!" 
                    : "Better luck next time!";
            }
        }

        private void OnPlayAgainClicked()
        {
            // Reload current scene
            Hide();
            Time.timeScale = 1f;
            
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
        }

        private void OnMainMenuClicked()
        {
            // Load main menu scene
            Hide();
            Time.timeScale = 1f;
            
            // Try to load MainMenu, fallback to scene 0
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
            catch
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }
    }
}
