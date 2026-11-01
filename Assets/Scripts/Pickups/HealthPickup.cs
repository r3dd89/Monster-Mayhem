
using UnityEngine;

/// <summary>
/// Restores player health when the player enters the pickup trigger.
/// The pickup is consumed only when the player needs healing.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    #region Serialized Fields

    [Header("Pickup Settings")]
    [SerializeField, Min(1f)] private float healAmount = 25f;

    #endregion

    #region Private Fields

    private bool isCollected;

    #endregion

    #region Trigger Handling

    /// <summary>
    /// Checks whether the player entered the pickup's trigger.
    /// Restores health and removes the pickup when collected.
    /// </summary>
    /// <param name="other">Collider entering the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Prevent the pickup from being collected multiple times.
        if (isCollected)
        {
            return;
        }

        // Find PlayerHealth on the entering object or its parent.
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        // Ignore enemies and objects without PlayerHealth.
        if (playerHealth == null)
        {
            return;
        }

        // Dead players cannot collect health.
        if (playerHealth.IsDead)
        {
            return;
        }

        // Keep the pickup available if health is already full.
        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            return;
        }

        // Mark the pickup as collected.
        isCollected = true;

        // Restore health using the existing PlayerHealth system.
        playerHealth.Heal(healAmount);

        // Remove the pickup after successful collection.
        Destroy(gameObject);
    }

    #endregion
}
