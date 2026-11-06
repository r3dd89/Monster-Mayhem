
using UnityEngine;

/// <summary>
/// Adds reserve ammunition when the player enters or
/// remains inside the pickup trigger.
/// The pickup is destroyed after successful collection.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour
{
    #region Serialized Fields

    [Header("Ammo Pickup Settings")]
    [SerializeField, Min(1)]
    private int ammoAmount = 24;

    #endregion

    #region Private Fields

    private bool isCollected;

    #endregion

    #region Trigger Handling

    /// <summary>
    /// Attempts collection when the Player first
    /// enters the pickup trigger.
    /// </summary>
    /// <param name="other">Collider entering the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    /// <summary>
    /// Attempts collection again while the Player
    /// remains inside the trigger.
    /// </summary>
    /// <param name="other">Collider inside the trigger.</param>
    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    #endregion

    #region Pickup Collection

    /// <summary>
    /// Validates the Player and adds ammunition
    /// using the existing WeaponController.
    /// </summary>
    /// <param name="other">Collider attempting collection.</param>
    private void TryCollect(Collider other)
    {
        // Prevent the same pickup from being
        // collected more than once.
        if (isCollected)
        {
            return;
        }

        // Identify the Player using PlayerHealth.
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        // Ignore enemies and unrelated objects.
        if (playerHealth == null)
        {
            return;
        }

        // Dead players cannot collect ammunition.
        if (playerHealth.IsDead)
        {
            return;
        }

        // Find the existing Player weapon controller.
        WeaponController weaponController =
            playerHealth.GetComponent<WeaponController>();

        // Do not collect ammunition if the weapon
        // controller is missing or disabled.
        if (weaponController == null ||
            !weaponController.isActiveAndEnabled)
        {
            return;
        }

        // Prevent duplicate collection attempts.
        isCollected = true;

        // Add reserve ammunition using the
        // existing weapon system.
        weaponController.AddReserveAmmo(ammoAmount);

        // Remove the pickup after successful collection.
        Destroy(gameObject);

        // TODO: Add an ammunition collection sound.
        // TODO: Add a brief collection visual effect.
    }

    #endregion
}
