
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player health, incoming damage, and death.
/// Disables player controls when health reaches zero.
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
        if (isDead || damageAmount <= 0f)
        {
            return;
        }

        // Clamp health to prevent negative values.
        currentHealth = Mathf.Max(
            currentHealth - damageAmount,
            0f
        );

        // Temporary feedback until the health HUD is implemented.
        Debug.Log(
            $"Player Health: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
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

        Debug.Log("Player has been defeated.");

        // Disable movement after death.
        if (TryGetComponent(out PlayerController movement))
        {
            movement.enabled = false;
        }

        // Disable camera movement and release the cursor.
        if (TryGetComponent(out PlayerLook playerLook))
        {
            playerLook.enabled = false;
        }

        // Prevent additional weapon actions.
        if (TryGetComponent(out WeaponController weapon))
        {
            weapon.enabled = false;
        }

        // Disable the player's input processing.
        if (TryGetComponent(out PlayerInput input))
        {
            input.enabled = false;
        }

        // TODO: Display the game-over screen.
        // TODO: Implement player respawning and health restoration.
    }

    #endregion
}
