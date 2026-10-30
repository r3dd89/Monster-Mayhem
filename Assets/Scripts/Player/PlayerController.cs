
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages first-person player movement, sprinting, and gravity.
/// Supports keyboard and gamepad input through Unity's Input System.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;

    #endregion

    #region Private Fields

    private CharacterController characterController;

    private Vector2 moveInput;
    private float verticalVelocity;
    private bool isSprinting;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the CharacterController for movement and collision handling.
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void OnDisable()
    {
        // Reset movement states when the player is disabled.
        moveInput = Vector2.zero;
        verticalVelocity = 0f;
        isSprinting = false;
    }

    #endregion

    #region Movement Methods

    /// <summary>
    /// Calculates horizontal movement, applies the appropriate
    /// movement speed, and processes gravity.
    /// </summary>
    private void HandleMovement()
    {
        // Maintain a small downward velocity while grounded.
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Apply gravity over time.
        verticalVelocity += gravity * Time.deltaTime;

        // Calculate movement relative to the player's facing direction.
        Vector3 moveDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        // Prevent increased movement speed along diagonal directions.
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Select the appropriate speed based on the sprint state.
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Combine horizontal movement with vertical velocity.
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        // Apply movement through the CharacterController.
        characterController.Move(velocity * Time.deltaTime);
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Receives directional movement input from the keyboard
    /// or the gamepad's left analog stick.
    /// </summary>
    /// <param name="value">Two-dimensional movement input.</param>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Updates the sprint state while the assigned sprint
    /// button is pressed or released.
    /// </summary>
    /// <param name="value">Current sprint button input.</param>
    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    #endregion
}
