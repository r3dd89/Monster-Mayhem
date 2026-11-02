
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Represents the current overall game state.
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Defeat,
    Victory
}

/// <summary>
/// Controls the overall game state, pausing,
/// restarting, and returning to the main menu.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    #endregion

    #region Private Fields

    private GameState currentState = GameState.Playing;

    #endregion

    #region Public Properties

    public GameState CurrentState => currentState;

    public bool IsPlaying => currentState == GameState.Playing;
    public bool IsPaused => currentState == GameState.Paused;

    #endregion

    #region Events

    /// <summary>
    /// Notifies listeners when the game state changes.
    /// </summary>
    public event Action<GameState> GameStateChanged;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Ensure gameplay starts at normal speed.
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // Gameplay starts immediately until the
        // complete main-menu flow is integrated.
        ChangeState(GameState.Playing);
    }

    private void OnDestroy()
    {
        // Prevent paused time from affecting another scene.
        Time.timeScale = 1f;
    }

    #endregion

    #region Game State Management

    /// <summary>
    /// Updates the game state and notifies listeners.
    /// </summary>
    private void ChangeState(GameState newState)
    {
        currentState = newState;

        // Gameplay runs only during the Playing state.
        Time.timeScale =
            newState == GameState.Playing ? 1f : 0f;

        GameStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Pauses active gameplay.
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        ChangeState(GameState.Paused);
    }

    /// <summary>
    /// Resumes gameplay from the paused state.
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
        {
            return;
        }

        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// Enters the defeat state.
    /// </summary>
    public void TriggerDefeat()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        ChangeState(GameState.Defeat);
    }

    /// <summary>
    /// Enters the victory state.
    /// </summary>
    public void TriggerVictory()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        ChangeState(GameState.Victory);
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// Restarts the current gameplay scene.
    /// </summary>
    public void RestartGame()
    {
        // Restore normal time before reloading.
        Time.timeScale = 1f;

        // Restore the first-person cursor settings.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reload the current scene.
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    /// <summary>
    /// Returns the player to the main menu scene.
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Confirm the menu scene is included in Build Profiles.
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError(
                "Main menu scene is unavailable: " +
                mainMenuSceneName
            );

            return;
        }

        // Restore time before leaving paused gameplay.
        Time.timeScale = 1f;

        // Unlock the cursor for menu navigation.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load the main menu scene.
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion
}
