
using UnityEngine;

/// <summary>
/// Adds reserve ammunition when the player enters
/// the pickup trigger. The pickup is destroyed after
/// successful collection.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour
{
    #region Serialized Fields

    [Header("Ammo Pickup Settings")]
    [SerializeField, Min(1)] private int ammoAmount = 24;

    #endregion

    #region Private Fields

    private bool isCollected;

    #endregion

    #region Trigger Handling

    /// <summary>
    /// Checks whether the player entered the pickup trigger
    /// and adds ammunition to the player's reserve.
    /// </summary>
    /// <param name="other">Collider entering the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Prevent collecting the same pickup multiple times.
        if (isCollected)
        {
            return;
        }

        // Confirm that the entering object belongs to the player.
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        // Get the player's existing weapon controller.
        WeaponController weaponController =
            playerHealth.GetComponent<WeaponController>();

        // Do not collect ammunition if the weapon is unavailable.
        if (weaponController == null ||
            !weaponController.isActiveAndEnabled)
        {
            return;
        }

        // Mark the pickup as collected before modifying ammo.
        isCollected = true;

        // Add ammunition using the existing weapon system.
        weaponController.AddReserveAmmo(ammoAmount);

        // Remove the pickup after successful collection.
        Destroy(gameObject);

        // TODO: Add an ammunition collection sound.
        // TODO: Add a brief collection visual effect.
    }

    #endregion
}
