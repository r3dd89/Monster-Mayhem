
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the pause menu and handles
/// Resume, Restart, and Main Menu buttons.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerPauseInput playerPauseInput;

    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Hide the pause panel when gameplay begins.
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Register button callbacks.
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void OnEnable()
    {
        // Listen for changes to the current game state.
        if (gameManager != null)
        {
            gameManager.GameStateChanged += HandleGameStateChanged;
        }
    }

    private void Start()
    {
        // Synchronize the pause panel with the game state.
        if (gameManager != null)
        {
            HandleGameStateChanged(gameManager.CurrentState);
        }
    }

    private void OnDisable()
    {
        // Remove the game-state listener.
        if (gameManager != null)
        {
            gameManager.GameStateChanged -= HandleGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        // Remove button callbacks when the scene closes.
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(OnResumeClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }
    }

    #endregion

    #region Game State Handling

    /// <summary>
    /// Shows the pause panel only while gameplay is paused.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        if (pausePanel == null)
        {
            return;
        }

        pausePanel.SetActive(
            newState == GameState.Paused
        );
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// Resumes gameplay through the Player's pause controller.
    /// </summary>
    public void OnResumeClicked()
    {
        if (playerPauseInput == null)
        {
            return;
        }

        playerPauseInput.ResumeFromMenu();
    }

    /// <summary>
    /// Restarts the current gameplay scene.
    /// </summary>
    public void OnRestartClicked()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.RestartGame();
    }

    /// <summary>
    /// Returns the player to the main menu scene.
    /// </summary>
    public void OnMainMenuClicked()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.ReturnToMainMenu();
    }

    #endregion
}
