
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages first-person player movement, sprinting,
/// jumping, and gravity.
/// Supports keyboard and gamepad input through Unity's Input System.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Jump Settings")]
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    #endregion

    #region Private Fields

    private CharacterController characterController;

    private Vector2 moveInput;
    private float verticalVelocity;
    private bool isSprinting;
    private bool jumpRequested;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the CharacterController for movement
        // and collision handling.
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void OnDisable()
    {
        // Reset movement and input states when
        // the PlayerController is disabled.
        moveInput = Vector2.zero;
        verticalVelocity = 0f;
        isSprinting = false;
        jumpRequested = false;
    }

    #endregion

    #region Movement Methods

    /// <summary>
    /// Processes grounded movement, jumping,
    /// sprinting, and gravity.
    /// </summary>
    private void HandleMovement()
    {
        // Check whether the CharacterController
        // is currently touching the ground.
        bool isGrounded = characterController.isGrounded;

        // Apply a small downward force while grounded
        // to maintain stable ground contact.
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Jump only when the Player is grounded.
        if (jumpRequested && isGrounded && gravity < 0f)
        {
            // Calculate the upward velocity required
            // to reach the configured jump height.
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );
        }

        // Consume the jump request so holding Jump
        // does not automatically trigger another jump.
        jumpRequested = false;

        // Apply gravity every frame.
        verticalVelocity += gravity * Time.deltaTime;

        // Calculate movement relative to the
        // Player's facing direction.
        Vector3 moveDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        // Prevent diagonal movement from being faster.
        moveDirection = Vector3.ClampMagnitude(
            moveDirection,
            1f
        );

        // Select normal or sprinting movement speed.
        float currentSpeed = isSprinting
            ? sprintSpeed
            : moveSpeed;

        // Combine horizontal and vertical movement.
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        // Move the Player using CharacterController.
        characterController.Move(
            velocity * Time.deltaTime
        );
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Receives movement input from the keyboard
    /// or left analog stick.
    /// </summary>
    /// <param name="value">Two-dimensional movement input.</param>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Updates sprinting based on the assigned input.
    /// </summary>
    /// <param name="value">Current sprint input.</param>
    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    /// <summary>
    /// Requests a jump when the assigned button is pressed.
    /// </summary>
    /// <param name="value">Current jump button input.</param>
    public void OnJump(InputValue value)
    {
        // Register a jump only on button press.
        if (value.isPressed)
        {
            jumpRequested = true;
        }
    }

    #endregion
}
