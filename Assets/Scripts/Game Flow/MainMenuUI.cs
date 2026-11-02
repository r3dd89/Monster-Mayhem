
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the main menu and loads the gameplay scene
/// when the player selects Start Game.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    [Header("Menu Buttons")]
    [SerializeField] private Button startButton;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Restore normal game time when entering the menu.
        Time.timeScale = 1f;

        // Unlock the cursor for menu interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Connect the Start Game button.
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDestroy()
    {
        // Remove the button callback when the menu closes.
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// Starts a new game by loading the configured
    /// gameplay scene.
    /// </summary>
    public void StartGame()
    {
        // Verify that the scene is available for loading.
        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError(
                "Gameplay scene is unavailable: " +
                gameplaySceneName
            );

            return;
        }

        // Ensure gameplay starts at normal speed.
        Time.timeScale = 1f;

        // Restore the FPS cursor configuration.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Load the gameplay scene.
        SceneManager.LoadScene(gameplaySceneName);
    }

    #endregion
}
