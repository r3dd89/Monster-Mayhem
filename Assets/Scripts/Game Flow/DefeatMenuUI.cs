
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detects player defeat and displays the defeat menu.
/// Allows the player to restart or return to the main menu.
/// </summary>
public class DefeatMenuUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Defeat Menu UI")]
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    #endregion

    #region Private Fields

    private bool defeatTriggered;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Hide the defeat screen when gameplay begins.
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }

        // Connect the menu buttons.
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
        // Listen for changes to the overall game state.
        if (gameManager != null)
        {
            gameManager.GameStateChanged +=
                HandleGameStateChanged;
        }
    }

    private void Start()
    {
        // Synchronize the menu with the starting state.
        if (gameManager != null)
        {
            HandleGameStateChanged(gameManager.CurrentState);
        }
    }

    private void Update()
    {
        // Process player defeat only once.
        if (defeatTriggered ||
            gameManager == null ||
            playerHealth == null)
        {
            return;
        }

        // Trigger defeat when the player has died.
        if (playerHealth.IsDead &&
            gameManager.CurrentState == GameState.Playing)
        {
            defeatTriggered = true;

            // Let GameManager control the defeat state.
            gameManager.TriggerDefeat();
        }
    }

    private void OnDisable()
    {
        // Remove the game-state event listener.
        if (gameManager != null)
        {
            gameManager.GameStateChanged -=
                HandleGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        // Remove button callbacks during cleanup.
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(
                OnRestartClicked
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(
                OnMainMenuClicked
            );
        }
    }

    #endregion

    #region Game State Handling

    /// <summary>
    /// Displays the defeat panel when the game
    /// enters the Defeat state.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        bool showDefeat = newState == GameState.Defeat;

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(showDefeat);
        }

        if (showDefeat)
        {
            // Allow mouse interaction with the defeat menu.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// Reloads the gameplay scene.
    /// </summary>
    private void OnRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    /// <summary>
    /// Returns to the title screen.
    /// </summary>
    private void OnMainMenuClicked()
    {
        if (gameManager != null)
        {
            gameManager.ReturnToMainMenu();
        }
    }

    #endregion
}
