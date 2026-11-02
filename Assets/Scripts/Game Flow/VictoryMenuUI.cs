
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles the final wave victory sequence, disables
/// player controls, and displays the victory menu
/// after the final enemy's death animation.
/// </summary>
public class VictoryMenuUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private GameObject player;

    [Header("Victory Settings")]
    [SerializeField, Min(0f)]
    private float victoryDelay = 3f;

    [Header("Victory Menu UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Gameplay HUD Elements")]
    [SerializeField] private GameObject waveText;
    [SerializeField] private GameObject enemiesRemainingText;

    #endregion

    #region Private Fields

    private bool victoryTriggered;

    private PlayerInput playerInput;
    private PlayerLook playerLook;
    private PlayerController playerController;
    private WeaponController weaponController;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the existing player components.
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            playerLook = player.GetComponent<PlayerLook>();

            playerController =
                player.GetComponent<PlayerController>();

            weaponController =
                player.GetComponent<WeaponController>();
        }

        // Hide the victory panel until the game is won.
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // Connect the victory menu buttons.
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(
                OnMainMenuClicked
            );
        }
    }

    private void OnEnable()
    {
        // Subscribe to GameManager state changes.
        if (gameManager != null)
        {
            gameManager.GameStateChanged +=
                HandleGameStateChanged;
        }
    }

    private void Start()
    {
        // Synchronize the victory panel at scene start.
        if (gameManager != null)
        {
            HandleGameStateChanged(gameManager.CurrentState);
        }
    }

    private void Update()
    {
        // Process victory only once.
        if (victoryTriggered ||
            gameManager == null ||
            waveSpawner == null)
        {
            return;
        }

        // Do not start victory during pause or defeat.
        if (gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        // Wait for the final wave completion.
        if (!waveSpawner.IsVictory)
        {
            return;
        }

        victoryTriggered = true;

        // Prevent movement, aiming, and firing.
        DisablePlayerControls();

        // Hide the old wave completion messages.
        HideWaveHUD();

        // Allow the final death animation to finish.
        StartCoroutine(ShowVictoryAfterDelay());
    }

    private void OnDisable()
    {
        // Unsubscribe from game state events.
        if (gameManager != null)
        {
            gameManager.GameStateChanged -=
                HandleGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        // Remove registered button callbacks.
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

    #region Victory Sequence

    /// <summary>
    /// Stops player input after the final wave is cleared.
    /// </summary>
    private void DisablePlayerControls()
    {
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
        }

        if (playerLook != null)
        {
            playerLook.enabled = false;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (weaponController != null)
        {
            weaponController.enabled = false;
        }
    }

    /// <summary>
    /// Hides the Wave HUD messages so they do not
    /// overlap the dedicated victory screen.
    /// </summary>
    private void HideWaveHUD()
    {
        if (waveText != null)
        {
            waveText.SetActive(false);
        }

        if (enemiesRemainingText != null)
        {
            enemiesRemainingText.SetActive(false);
        }
    }

    /// <summary>
    /// Allows the last enemy's death animation to play
    /// before transitioning into the Victory state.
    /// </summary>
    private IEnumerator ShowVictoryAfterDelay()
    {
        yield return new WaitForSeconds(victoryDelay);

        // Do not override defeat or another game state.
        if (gameManager == null ||
            gameManager.CurrentState != GameState.Playing)
        {
            yield break;
        }

        gameManager.TriggerVictory();
    }

    #endregion

    #region Game State Handling

    /// <summary>
    /// Displays the victory panel when the
    /// GameManager enters the Victory state.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        bool showVictory = newState == GameState.Victory;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(showVictory);
        }

        if (showVictory)
        {
            // Make the cursor available for menu buttons.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// Restarts gameplay from the first wave.
    /// </summary>
    private void OnRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    /// <summary>
    /// Returns to the main menu.
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
