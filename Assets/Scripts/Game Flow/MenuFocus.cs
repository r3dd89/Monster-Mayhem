
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Automatically selects a default button whenever
/// a menu panel becomes active.
/// Supports keyboard and controller UI navigation.
/// </summary>
public class MenuFocus : MonoBehaviour
{
    #region Serialized Fields

    [Header("Menu Navigation")]
    [SerializeField] private Button firstSelectedButton;

    #endregion

    #region Unity Lifecycle Methods

    private void OnEnable()
    {
        // Wait one frame for the menu and UI system
        // to finish enabling before selecting a button.
        StartCoroutine(SelectFirstButton());
    }

    #endregion

    #region Menu Selection

    /// <summary>
    /// Sets the initial selected button for this menu.
    /// </summary>
    private IEnumerator SelectFirstButton()
    {
        // Frame-based waiting also works while
        // gameplay is paused with Time.timeScale = 0.
        yield return null;

        // Ignore missing or unavailable buttons.
        if (firstSelectedButton == null ||
            !firstSelectedButton.IsActive() ||
            !firstSelectedButton.interactable)
        {
            yield break;
        }

        // Find the active Unity UI EventSystem.
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            yield break;
        }

        // Clear any selection left from a previous menu.
        eventSystem.SetSelectedGameObject(null);

        // Select the configured first button.
        eventSystem.SetSelectedGameObject(
            firstSelectedButton.gameObject
        );
    }

    #endregion
}
