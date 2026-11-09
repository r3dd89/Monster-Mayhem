
using UnityEngine;

/// <summary>
/// Updates the Player's movement animations using
/// the CharacterController's current movement speed.
/// Supports idle, walking, running, and sprinting.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimationController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Component References")]
    [SerializeField] private Animator playerAnimator;

    [Space(10)]
    [Header("Movement Animation Settings")]
    [SerializeField, Min(0.1f)]
    private float walkSpeed = 2f;

    [SerializeField, Min(0.1f)]
    private float runSpeed = 5f;

    [SerializeField, Min(0.1f)]
    private float sprintSpeed = 8f;

    [Space(10)]
    [Header("Animation Smoothing")]
    [SerializeField, Range(0f, 0.5f)]
    private float animationDampTime = 0.15f;

    #endregion

    #region Private Fields

    private CharacterController characterController;

    // Cache Animator parameters for better performance.
    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int OnGroundHash =
        Animator.StringToHash("OnGround");

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the Player's existing CharacterController.
        characterController =
            GetComponent<CharacterController>();

        // Find the soldier's Animator if no reference
        // has been assigned in the Inspector.
        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponentInChildren<Animator>();
        }

        if (playerAnimator == null)
        {
            Debug.LogWarning(
                "PlayerAnimationController: " +
                "No Animator was found.",
                this
            );
        }
    }

    private void LateUpdate()
    {
        if (playerAnimator == null)
        {
            return;
        }

        UpdateMovementAnimation();
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Converts the Player's horizontal movement speed
    /// into the Space Soldier's Blend Tree values.
    /// </summary>
    private void UpdateMovementAnimation()
    {
        // Read actual CharacterController movement.
        Vector3 velocity = characterController.velocity;

        // Ignore vertical movement caused by jumping.
        velocity.y = 0f;

        float currentSpeed = velocity.magnitude;
        float blendSpeed = 0f;

        // Idle to walking.
        if (currentSpeed <= walkSpeed)
        {
            blendSpeed = Mathf.Lerp(
                0f,
                0.25f,
                Mathf.InverseLerp(
                    0f,
                    walkSpeed,
                    currentSpeed
                )
            );
        }
        // Walking to running.
        else if (currentSpeed <= runSpeed)
        {
            blendSpeed = Mathf.Lerp(
                0.25f,
                1f,
                Mathf.InverseLerp(
                    walkSpeed,
                    runSpeed,
                    currentSpeed
                )
            );
        }
        // Running to sprinting.
        else
        {
            blendSpeed = Mathf.Lerp(
                1f,
                2f,
                Mathf.InverseLerp(
                    runSpeed,
                    sprintSpeed,
                    currentSpeed
                )
            );
        }

        // Smoothly transition between movement animations.
        playerAnimator.SetFloat(
            SpeedHash,
            blendSpeed,
            animationDampTime,
            Time.deltaTime
        );

        // Inform the Animator whether the Player
        // is standing on the ground.
        playerAnimator.SetBool(
            OnGroundHash,
            characterController.isGrounded
        );
    }

    #endregion
}
