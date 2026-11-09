
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the crosshair during hip-fire and hides
/// it while the player is aiming down sights.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CrosshairADSUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Component References")]
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private CanvasGroup crosshairGroup;

    [Header("Aim Settings")]
    [SerializeField]
    private string aimActionName = "Aim";

    [SerializeField, Min(0.1f)]
    private float fadeSpeed = 12f;

    [Header("Visibility Settings")]
    [SerializeField]
    private bool hideWhenPaused = true;

    [SerializeField]
    private bool hideWhenDead = true;

    #endregion

    #region Private Fields

    private InputAction aimAction;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        if (crosshairGroup == null)
        {
            crosshairGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // Find the existing Aim input action.
        if (playerInput != null &&
            playerInput.actions != null)
        {
            aimAction = playerInput.actions.FindAction(
                aimActionName,
                false
            );
        }
    }

    private void Update()
    {
        bool isAiming = aimAction != null &&
                        aimAction.IsPressed();

        bool isDead = hideWhenDead &&
                      playerHealth != null &&
                      playerHealth.IsDead;

        bool isPaused = hideWhenPaused &&
                        Time.timeScale == 0f;

        // Hide the crosshair while aiming,
        // paused, or dead.
        float targetAlpha =
            (isAiming || isDead || isPaused)
            ? 0f
            : 1f;

        // Smoothly fade between visible and hidden.
        crosshairGroup.alpha = Mathf.MoveTowards(
            crosshairGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );
    }

    #endregion
}
