
using UnityEngine;

/// <summary>
/// Makes a pickup float up and down while rotating.
/// Uses a kinematic Rigidbody to move the pickup
/// without interfering with trigger detection.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickupFloat : MonoBehaviour
{
    #region Serialized Fields

    [Header("Floating Settings")]
    [SerializeField, Min(0f)] private float floatHeight = 0.15f;
    [SerializeField, Min(0.1f)] private float floatSpeed = 2f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 60f;

    #endregion

    #region Private Fields

    private Rigidbody pickupRigidbody;
    private Vector3 startingPosition;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the Rigidbody for pickup movement.
        pickupRigidbody = GetComponent<Rigidbody>();

        // Keep the pickup controlled by the script.
        pickupRigidbody.isKinematic = true;
        pickupRigidbody.useGravity = false;

        // Store the original world position.
        startingPosition = pickupRigidbody.position;
    }

    private void FixedUpdate()
    {
        // Calculate a smooth up-and-down movement.
        float verticalOffset =
            Mathf.Sin(Time.fixedTime * floatSpeed) *
            floatHeight;

        Vector3 targetPosition =
            startingPosition +
            Vector3.up * verticalOffset;

        // Move the pickup while maintaining physics interactions.
        pickupRigidbody.MovePosition(targetPosition);

        // Rotate the pickup around its vertical axis.
        Quaternion rotationChange = Quaternion.Euler(
            0f,
            rotationSpeed * Time.fixedDeltaTime,
            0f
        );

        pickupRigidbody.MoveRotation(
            pickupRigidbody.rotation * rotationChange
        );
    }

    #endregion
}
