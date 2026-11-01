
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player health, incoming damage, healing, and death.
/// Notifies the HUD when health changes and disables player
/// controls when health reaches zero.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    #region Serialized Fields

    [Header("Health Settings")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    #endregion

    #region Private Fields

    private float currentHealth;
    private bool isDead;

    #endregion

    #region Public Properties

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    #endregion

    #region Events

    /// <summary>
    /// Notifies listeners when the player's health changes.
    /// Provides the current health and maximum health.
    /// </summary>
    public event Action<float, float> HealthChanged;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Initialize the player's health when the object is created.
        currentHealth = maxHealth;
        isDead = false;
    }

    #endregion

    #region Damage Handling

    /// <summary>
    /// Reduces player health when damage is received.
    /// Additional damage is ignored after death.
    /// </summary>
    /// <param name="damageAmount">Amount of incoming damage.</param>
    public void TakeDamage(float damageAmount)
    {
        // Ignore damage if the player is dead
        // or the damage amount is invalid.
        if (isDead || damageAmount <= 0f)
        {
            return;
        }

        // Reduce health without allowing negative values.
        currentHealth = Mathf.Max(
            currentHealth - damageAmount,
            0f
        );

        // Notify the HUD of the updated health.
        NotifyHealthChanged();

        // Trigger death when health is depleted.
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    #endregion

    #region Healing

    /// <summary>
    /// Restores player health without exceeding maximum health.
    /// Dead players cannot be healed by regular health pickups.
    /// </summary>
    /// <param name="healingAmount">Amount of health to restore.</param>
    public void Heal(float healingAmount)
    {
        // Prevent invalid healing and healing after death.
        if (isDead || healingAmount <= 0f)
        {
            return;
        }

        // Restore health without exceeding the maximum.
        currentHealth = Mathf.Min(
            currentHealth + healingAmount,
            maxHealth
        );

        // Update the health bar and numerical display.
        NotifyHealthChanged();
    }

    #endregion

    #region Health Events

    /// <summary>
    /// Notifies any subscribed HUD components
    /// that the player's health has changed.
    /// </summary>
    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    #endregion

    #region Death Handling

    /// <summary>
    /// Handles player death by disabling gameplay controls.
    /// </summary>
    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        // Temporary death feedback until the game-over UI exists.
        Debug.Log("Player has been defeated.");

        // Disable player movement.
        if (TryGetComponent(out PlayerController movement))
        {
            movement.enabled = false;
        }

        // Disable camera movement and release the cursor.
        if (TryGetComponent(out PlayerLook playerLook))
        {
            playerLook.enabled = false;
        }

        // Prevent the player from firing or reloading.
        if (TryGetComponent(out WeaponController weapon))
        {
            weapon.enabled = false;
        }

        // Disable player input processing.
        if (TryGetComponent(out PlayerInput input))
        {
            input.enabled = false;
        }

        // TODO: Display the game-over screen.
        // TODO: Implement player respawning.
    }

    #endregion
}
