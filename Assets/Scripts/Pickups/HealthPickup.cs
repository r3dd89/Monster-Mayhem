
using UnityEngine;

/// <summary>
/// Restores player health when the player enters or
/// remains inside the pickup trigger.
/// The pickup is consumed only when healing is needed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    #region Serialized Fields

    [Header("Pickup Settings")]
    [SerializeField, Min(1f)]
    private float healAmount = 25f;

    #endregion

    #region Private Fields

    private bool isCollected;

    #endregion

    #region Trigger Handling

    /// <summary>
    /// Attempts collection when the Player first
    /// enters the pickup trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    /// <summary>
    /// Checks again while the Player remains inside
    /// the trigger to improve collection reliability.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    #endregion

    #region Pickup Collection

    /// <summary>
    /// Validates the Player's health and collects
    /// the pickup when healing is possible.
    /// </summary>
    private void TryCollect(Collider other)
    {
        // Prevent duplicate collection.
        if (isCollected)
        {
            return;
        }

        // Find PlayerHealth on the entering collider
        // or one of its parent GameObjects.
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        // Ignore enemies and unrelated objects.
        if (playerHealth == null)
        {
            return;
        }

        // Dead players cannot collect health.
        if (playerHealth.IsDead)
        {
            return;
        }

        // Preserve the pickup when health is full.
        if (playerHealth.CurrentHealth >=
            playerHealth.MaxHealth)
        {
            return;
        }

        // Prevent additional collection attempts.
        isCollected = true;

        // Restore health through the existing system.
        playerHealth.Heal(healAmount);

        // Remove the collected pickup.
        Destroy(gameObject);
    }

    #endregion
}
