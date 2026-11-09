
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the player's death presentation.
/// Hides the first-person arms and weapon, reveals the
/// full-body Space Soldier, activates the existing Dead
/// animation, and moves the camera behind the character.
/// Displays the Game Over menu after the death sequence.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDeathCamera : MonoBehaviour
{
    #region Serialized Fields

    [Header("Camera References")]

    [Tooltip("The player's existing first-person camera.")]
    [SerializeField]
    private Camera playerCamera;

    [Tooltip("The Space Soldier character transform.")]
    [SerializeField]
    private Transform soldierTransform;

    [Header("Death Camera Settings")]

    [Tooltip("Time required for the camera to pull behind the soldier.")]
    [SerializeField, Range(0.1f, 4f)]
    private float pullbackDuration = 1.2f;

    [Tooltip("Distance behind the soldier during the death sequence.")]
    [SerializeField, Range(1f, 6f)]
    private float cameraDistance = 3f;

    [Tooltip("Height of the camera above the soldier.")]
    [SerializeField, Range(0.5f, 4f)]
    private float cameraHeight = 1.8f;

    [Tooltip("Height of the point the camera looks toward.")]
    [SerializeField, Range(0.1f, 3f)]
    private float lookAtHeight = 1f;

    [Header("Death Animation Settings")]

    [Tooltip("Animator controlling the full-body Space Soldier.")]
    [SerializeField]
    private Animator soldierAnimator;

    [Tooltip("Name of the existing death Bool parameter.")]
    [SerializeField]
    private string deathBoolName = "Dead";

    [Header("Character Visibility")]

    [Tooltip("The Easy FPS arms and rifle visible during gameplay.")]
    [SerializeField]
    private GameObject fpsCharacter;

    [Tooltip("The full-body PolygonR Space Soldier.")]
    [SerializeField]
    private GameObject fullBodySoldier;

    [Tooltip("The original rifle attached to the soldier's hand.")]
    [SerializeField]
    private GameObject originalRifleVisualRoot;

    [Tooltip("Layer containing the hidden head and helmet meshes.")]
    [SerializeField]
    private string hiddenHeadLayerName = "FirstPersonHidden";

    [Header("Game Over Settings")]

    [Tooltip("Optional Game Over UI panel.")]
    [SerializeField]
    private GameObject gameOverPanel;

    [Tooltip("Total time before displaying the Game Over panel.")]
    [SerializeField, Range(0f, 8f)]
    private float gameOverDelay = 3f;

    [Tooltip("Pause gameplay after displaying Game Over.")]
    [SerializeField]
    private bool pauseOnGameOver = true;

    [Tooltip("Unlock the cursor so Game Over buttons can be selected.")]
    [SerializeField]
    private bool unlockCursorOnGameOver = true;

    #endregion

    #region Private Fields

    private PlayerHealth playerHealth;

    private bool deathSequenceStarted;

    // Stores the full-body character's renderers.
    private Renderer[] soldierRenderers;

    // Stores the original rifle's renderers.
    private Renderer[] originalRifleRenderers;

    // Tracks whether the original rifle was hidden
    // through its GameObject rather than its renderers.
    private bool originalRifleWasActive;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the existing PlayerHealth component.
        playerHealth = GetComponent<PlayerHealth>();

        deathSequenceStarted = false;

        // Find the camera assigned to PlayerInput
        // if no camera was assigned manually.
        if (playerCamera == null)
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                playerCamera = playerInput.camera;
            }
        }

        // Find the existing player camera if needed.
        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<Camera>(true);
        }

        // Find the soldier from its assigned Transform.
        if (fullBodySoldier == null &&
            soldierTransform != null)
        {
            fullBodySoldier = soldierTransform.gameObject;
        }

        // Assign the soldier Transform if necessary.
        if (soldierTransform == null &&
            fullBodySoldier != null)
        {
            soldierTransform = fullBodySoldier.transform;
        }

        // Preserve the original fallback.
        if (soldierTransform == null)
        {
            soldierTransform = transform;
        }

        // Find the soldier Animator if needed.
        if (soldierAnimator == null &&
            fullBodySoldier != null)
        {
            soldierAnimator =
                fullBodySoldier.GetComponentInChildren
                <Animator>(true);
        }

        // Cache visual components before hiding anything.
        CacheCharacterRenderers();

        // Initialize first-person gameplay visibility.
        SetGameplayVisibility();

        // Reset the existing Dead Bool.
        if (HasDeadBoolParameter())
        {
            soldierAnimator.SetBool(deathBoolName, false);
        }

        // Hide the Game Over panel at startup.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Detect player death through PlayerHealth.
        // The sequence should start only once.
        if (!deathSequenceStarted &&
            playerHealth != null &&
            playerHealth.IsDead)
        {
            BeginDeathSequence();
        }
    }

    #endregion

    #region Character Visibility

    /// <summary>
    /// Finds the full-body soldier and rifle renderers.
    /// Keeps the original skeleton and Animator available.
    /// </summary>
    private void CacheCharacterRenderers()
    {
        if (fullBodySoldier != null)
        {
            // The full-body soldier must remain active
            // so its Animator can play when death occurs.
            if (!fullBodySoldier.activeSelf)
            {
                fullBodySoldier.SetActive(true);
            }

            soldierRenderers =
                fullBodySoldier.GetComponentsInChildren
                <Renderer>(true);
        }

        if (originalRifleVisualRoot != null)
        {
            originalRifleWasActive =
                originalRifleVisualRoot.activeSelf;

            originalRifleRenderers =
                originalRifleVisualRoot
                .GetComponentsInChildren<Renderer>(true);
        }
    }

    /// <summary>
    /// Shows the FPS weapon and hides the full-body
    /// soldier during normal gameplay.
    /// </summary>
    private void SetGameplayVisibility()
    {
        // The first-person arms and rifle are visible.
        if (fpsCharacter != null)
        {
            fpsCharacter.SetActive(true);
        }

        // Hide all full-body soldier meshes.
        SetSoldierVisible(false);

        // Keep the original rifle hidden.
        SetOriginalRifleVisible(false);
    }

    /// <summary>
    /// Enables or disables the full-body soldier meshes
    /// without disabling its Animator or skeleton.
    /// </summary>
    private void SetSoldierVisible(bool visible)
    {
        if (fullBodySoldier == null)
        {
            return;
        }

        if (!fullBodySoldier.activeSelf)
        {
            fullBodySoldier.SetActive(true);
        }

        if (soldierRenderers == null)
        {
            soldierRenderers =
                fullBodySoldier.GetComponentsInChildren
                <Renderer>(true);
        }

        foreach (Renderer bodyRenderer in soldierRenderers)
        {
            if (bodyRenderer == null)
            {
                continue;
            }

            // The original rifle is controlled separately.
            if (originalRifleVisualRoot != null &&
                bodyRenderer.transform.IsChildOf(
                    originalRifleVisualRoot.transform))
            {
                continue;
            }

            // Reveal the soldier's body at death.
            bodyRenderer.enabled = visible;
        }
    }

    /// <summary>
    /// Controls the original soldier rifle separately
    /// from the Easy FPS weapon.
    /// </summary>
    private void SetOriginalRifleVisible(bool visible)
    {
        if (originalRifleVisualRoot == null)
        {
            return;
        }

        // Preserve the original weapon hierarchy.
        // If the rifle was inactive, activate it only
        // when it needs to appear during death.
        if (visible &&
            !originalRifleVisualRoot.activeSelf)
        {
            originalRifleVisualRoot.SetActive(true);
        }

        if (originalRifleRenderers == null)
        {
            originalRifleRenderers =
                originalRifleVisualRoot
                .GetComponentsInChildren<Renderer>(true);
        }

        foreach (Renderer rifleRenderer
                 in originalRifleRenderers)
        {
            if (rifleRenderer != null)
            {
                rifleRenderer.enabled = visible;
            }
        }

        // Preserve the original active state until
        // the death sequence needs the weapon.
        if (!visible &&
            !originalRifleWasActive)
        {
            originalRifleVisualRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Switches from the first-person weapon to
    /// the visible full-body soldier at death.
    /// </summary>
    private void SetDeathVisibility()
    {
        // Hide the Easy FPS arms and gun.
        if (fpsCharacter != null)
        {
            fpsCharacter.SetActive(false);
        }

        // Reveal the full-body soldier.
        SetSoldierVisible(true);

        // Restore the rifle attached to its hand.
        SetOriginalRifleVisible(true);

        // Restore visibility of hidden head meshes.
        ShowHeadMeshes();
    }

    /// <summary>
    /// Includes the hidden head layer in the camera mask
    /// and restores head renderers at death.
    /// </summary>
    private void ShowHeadMeshes()
    {
        int headLayer =
            LayerMask.NameToLayer(hiddenHeadLayerName);

        if (headLayer < 0)
        {
            Debug.LogWarning(
                "PlayerDeathCamera: Hidden head layer " +
                "was not found: " + hiddenHeadLayerName,
                this
            );

            return;
        }

        // Restore camera visibility for the head layer.
        if (playerCamera != null)
        {
            playerCamera.cullingMask |= 1 << headLayer;
        }

        // Ensure head renderers are enabled.
        if (soldierRenderers != null)
        {
            foreach (Renderer bodyRenderer in soldierRenderers)
            {
                if (bodyRenderer != null &&
                    bodyRenderer.gameObject.layer == headLayer)
                {
                    bodyRenderer.enabled = true;
                }
            }
        }

        // TODO: Restore missing head mesh objects from
        // the original soldier prefab if they were deleted.
    }

    #endregion

    #region Death Sequence

    /// <summary>
    /// Switches character visuals, plays death animation,
    /// and starts the camera and Game Over sequence.
    /// </summary>
    private void BeginDeathSequence()
    {
        if (deathSequenceStarted)
        {
            return;
        }

        deathSequenceStarted = true;

        // Change the character's visual presentation.
        SetDeathVisibility();

        // Activate the existing death animation.
        PlayDeathAnimation();

        // Keep the Game Over interface hidden
        // while the death sequence begins.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Start the complete presentation sequence.
        StartCoroutine(DeathSequenceRoutine());
    }

    /// <summary>
    /// Moves the camera and waits until the configured
    /// delay has passed before displaying Game Over.
    /// </summary>
    private IEnumerator DeathSequenceRoutine()
    {
        float startTime = Time.unscaledTime;

        if (playerCamera != null)
        {
            // Detach the camera from the Player so its
            // movement is independent of the character.
            playerCamera.transform.SetParent(null, true);

            yield return StartCoroutine(PullCameraBack());
        }
        else
        {
            Debug.LogWarning(
                "PlayerDeathCamera requires a camera.",
                this
            );
        }

        // The menu must not interrupt camera movement.
        float elapsedTime =
            Time.unscaledTime - startTime;

        float remainingTime = Mathf.Max(
            0f,
            gameOverDelay - elapsedTime
        );

        if (remainingTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remainingTime
            );
        }

        ShowGameOverPanel();
    }

    #endregion

    #region Death Animation

    /// <summary>
    /// Activates the existing Dead Bool parameter
    /// in the soldier's Animator.
    /// </summary>
    private void PlayDeathAnimation()
    {
        if (soldierAnimator == null)
        {
            Debug.LogWarning(
                "PlayerDeathCamera has no Soldier Animator assigned.",
                this
            );

            return;
        }

        // Keep the Animator active during death.
        soldierAnimator.enabled = true;

        // Allow the animation to continue even if
        // another system pauses scaled game time.
        soldierAnimator.updateMode =
            AnimatorUpdateMode.UnscaledTime;

        // Check that the configured Bool exists.
        if (!HasDeadBoolParameter())
        {
            Debug.LogWarning(
                "Death Bool parameter '" + deathBoolName +
                "' was not found in the Soldier Animator.",
                this
            );

            return;
        }

        // Trigger the existing tested death animation.
        soldierAnimator.SetBool(
            deathBoolName,
            true
        );
    }

    /// <summary>
    /// Verifies the soldier Animator has the expected
    /// Bool parameter before assigning its value.
    /// </summary>
    private bool HasDeadBoolParameter()
    {
        if (soldierAnimator == null ||
            soldierAnimator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter
                 in soldierAnimator.parameters)
        {
            if (parameter.name == deathBoolName &&
                parameter.type ==
                AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Camera Transition

    /// <summary>
    /// Smoothly moves the camera from first person
    /// to a third-person view behind the soldier.
    /// </summary>
    private IEnumerator PullCameraBack()
    {
        // Save the camera's starting world position.
        Vector3 startPosition =
            playerCamera.transform.position;

        Quaternion startRotation =
            playerCamera.transform.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < pullbackDuration)
        {
            // Use unscaled time so the transition
            // continues even if gameplay was paused.
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / pullbackDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            // Calculate the current target position.
            CalculateDeathCameraPose(
                out Vector3 targetPosition,
                out Quaternion targetRotation
            );

            // Smoothly move the camera toward its target.
            playerCamera.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress
            );

            playerCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothProgress
            );

            yield return null;
        }

        // Ensure the final camera position is accurate.
        CalculateDeathCameraPose(
            out Vector3 finalPosition,
            out Quaternion finalRotation
        );

        playerCamera.transform.position = finalPosition;
        playerCamera.transform.rotation = finalRotation;

        // TODO: Add camera collision detection.
    }

    /// <summary>
    /// Calculates the camera position and rotation
    /// relative to the full-body soldier.
    /// </summary>
    private void CalculateDeathCameraPose(
        out Vector3 targetPosition,
        out Quaternion targetRotation)
    {
        // Find the soldier's horizontal forward direction.
        Vector3 forward = Vector3.ProjectOnPlane(
            soldierTransform.forward,
            Vector3.up
        );

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
        }

        forward.Normalize();

        // Position the camera behind and above the soldier.
        targetPosition =
            soldierTransform.position -
            forward * cameraDistance +
            Vector3.up * cameraHeight;

        // Aim the camera toward the soldier's upper body.
        Vector3 lookTarget =
            soldierTransform.position +
            Vector3.up * lookAtHeight;

        Vector3 lookDirection =
            lookTarget - targetPosition;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            targetRotation =
                playerCamera.transform.rotation;

            return;
        }

        targetRotation = Quaternion.LookRotation(
            lookDirection,
            Vector3.up
        );
    }

    #endregion

    #region Game Over

    /// <summary>
    /// Displays the Game Over interface after the
    /// camera transition and death sequence delay.
    /// </summary>
    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Pause gameplay after death presentation.
        if (pauseOnGameOver)
        {
            Time.timeScale = 0f;
        }

        // Unlock the cursor for menu interaction.
        if (unlockCursorOnGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // TODO: Ensure Restart and Main Menu buttons
        // restore Time.timeScale before loading scenes.
        // TODO: Connect final GameManager defeat flow.
    }

    #endregion
}
