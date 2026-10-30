
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles first-person camera rotation using mouse or gamepad input.
/// Rotates the player horizontally and limits vertical camera movement.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerLook : MonoBehaviour
{
    #region Serialized Fields

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float controllerLookSpeed = 150f;
    [SerializeField] private float maxLookAngle = 80f;

    #endregion

    #region Private Fields

    private PlayerInput playerInput;
    private Transform playerCamera;
    private InputAction lookAction;

    private Vector2 lookInput;
    private float cameraPitch;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the PlayerInput component and assigned camera.
        playerInput = GetComponent<PlayerInput>();

        if (playerInput.camera != null)
        {
            playerCamera = playerInput.camera.transform;

            // Preserve the camera's starting vertical rotation.
            cameraPitch = Mathf.DeltaAngle(
                0f,
                playerCamera.localEulerAngles.x
            );
        }
        else
        {
            Debug.LogError(
                "PlayerLook requires a camera assigned in Player Input.",
                this
            );
        }

        // Reference the Look action to identify the active input device.
        lookAction = playerInput.actions.FindAction("Look");
    }

    private void Start()
    {
        // Lock and hide the cursor during first-person gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
    }

    private void OnDisable()
    {
        // Clear stored input and release the cursor.
        lookInput = Vector2.zero;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion

    #region Camera Rotation

    /// <summary>
    /// Processes horizontal and vertical look input using
    /// separate sensitivity calculations for mouse and gamepad.
    /// </summary>
    private void HandleLook()
    {
        if (playerCamera == null)
        {
            return;
        }

        // Determine whether the Look action is using a gamepad.
        bool usingGamepad = lookAction != null &&
                            lookAction.activeControl?.device is Gamepad;

        // Analog sticks represent continuous input, while mouse
        // delta already represents movement accumulated per frame.
        float sensitivity = usingGamepad
            ? controllerLookSpeed * Time.deltaTime
            : mouseSensitivity;

        Vector2 rotation = lookInput * sensitivity;

        // Rotate the player around the vertical world axis.
        transform.Rotate(Vector3.up * rotation.x);

        // Calculate and restrict vertical camera rotation.
        cameraPitch -= rotation.y;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        // Apply pitch to the camera without tilting the player.
        playerCamera.localRotation = Quaternion.Euler(
            cameraPitch,
            0f,
            0f
        );
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Receives look input from the mouse or right analog stick.
    /// </summary>
    /// <param name="value">Two-dimensional camera input.</param>
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    #endregion
}
