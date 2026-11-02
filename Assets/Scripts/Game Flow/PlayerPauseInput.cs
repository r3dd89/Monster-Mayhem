
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles pause and resume input for the player.
/// Restores the complete gameplay action map after pausing.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerPauseInput : MonoBehaviour
{
    #region Serialized Fields

    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerLook playerLook;

    #endregion

    #region Private Fields

    private PlayerInput playerInput;
    private InputActionMap gameplayActionMap;

    private bool lookWasEnabled;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the PlayerInput on the same GameObject.
        playerInput = GetComponent<PlayerInput>();

        // Find the GameManager if the Inspector reference is empty.
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // Locate the player's existing look controller.
        if (playerLook == null)
        {
            playerLook = GetComponent<PlayerLook>();
        }
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Toggles pause when Escape or the controller
    /// menu button is pressed.
    /// </summary>
    public void OnPause(InputValue value)
    {
        // Ignore button releases and missing references.
        if (!value.isPressed || gameManager == null)
        {
            return;
        }

        if (gameManager.CurrentState == GameState.Playing)
        {
            PauseGameplay();
        }
        else if (gameManager.CurrentState == GameState.Paused)
        {
            ResumeGameplay();
        }
    }

    #endregion

    #region Pause Management

    /// <summary>
    /// Pauses gameplay and disables gameplay input
    /// while keeping the Pause action active.
    /// </summary>
    private void PauseGameplay()
    {
        if (gameManager == null ||
            gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        // Remember which action map was active.
        gameplayActionMap = playerInput.currentActionMap;

        // Remember whether camera look was enabled.
        lookWasEnabled =
            playerLook != null && playerLook.enabled;

        // Disable gameplay actions, but preserve Pause.
        if (gameplayActionMap != null)
        {
            foreach (InputAction action in gameplayActionMap.actions)
            {
                if (action.name != "Pause")
                {
                    action.Disable();
                }
            }
        }

        // Disable camera look while the menu is open.
        if (playerLook != null)
        {
            playerLook.enabled = false;
        }

        // Freeze the game.
        gameManager.PauseGame();

        // Unlock the cursor for UI interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Restores game time, input actions, camera look,
    /// and the locked FPS cursor.
    /// </summary>
    private void ResumeGameplay()
    {
        if (gameManager == null ||
            gameManager.CurrentState != GameState.Paused)
        {
            return;
        }

        // Restore normal gameplay time.
        gameManager.ResumeGame();

        // Restore PlayerInput if it became inactive.
        if (playerInput != null)
        {
            if (!playerInput.inputIsActive)
            {
                playerInput.ActivateInput();
            }

            // Restore the original action map if necessary.
            if (gameplayActionMap != null)
            {
                if (playerInput.currentActionMap != gameplayActionMap)
                {
                    playerInput.SwitchCurrentActionMap(
                        gameplayActionMap.name
                    );
                }

                // Re-enable every gameplay action.
                gameplayActionMap.Enable();
            }
            else
            {
                playerInput.currentActionMap?.Enable();
            }
        }

        // Restore the camera's previous enabled state.
        if (playerLook != null)
        {
            playerLook.enabled = lookWasEnabled;
        }

        // Lock the cursor for first-person gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Temporary diagnostics for the pause/resume test.
        // TODO: Remove after input restoration is verified.
        Debug.Log(
            "RESUME TEST | Input Active: " +
            (playerInput != null && playerInput.inputIsActive) +
            " | Move Enabled: " +
            (gameplayActionMap?.FindAction("Move")?.enabled ?? false) +
            " | Look Enabled: " +
            (gameplayActionMap?.FindAction("Look")?.enabled ?? false) +
            " | PlayerLook Enabled: " +
            (playerLook != null && playerLook.enabled)
        );
    }

    /// <summary>
    /// Allows the Resume button to use the same
    /// resume behavior as the keyboard and controller.
    /// </summary>
    public void ResumeFromMenu()
    {
        ResumeGameplay();
    }

    #endregion
}
