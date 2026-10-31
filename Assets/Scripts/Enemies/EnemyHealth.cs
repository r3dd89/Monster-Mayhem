
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages enemy health, incoming damage, and death behavior.
/// Coordinates the death animation and disables enemy activity
/// before removing the character from the scene.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    #region Serialized Fields

    [Header("Health Settings")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Header("Death Settings")]
    [SerializeField, Min(0f)] private float deathDespawnDelay = 4f;

    #endregion

    #region Private Fields

    private float currentHealth;
    private bool isDead;

    private Animator enemyAnimator;
    private EnemyAI enemyAI;
    private NavMeshAgent navAgent;
    private Collider enemyCollider;

    // Cache the Animator parameter used to trigger death.
    private static readonly int DeadHash =
        Animator.StringToHash("Dead");

    #endregion

    #region Public Properties

    public bool IsDead => isDead;

    public float CurrentHealth => currentHealth;

    public float MaxHealth => maxHealth;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Initialize the enemy's health and combat state.
        currentHealth = maxHealth;
        isDead = false;

        // Cache components required during the death sequence.
        enemyAnimator = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        navAgent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponent<Collider>();
    }

    #endregion

    #region Damage Handling

    /// <summary>
    /// Applies incoming damage and initiates the death
    /// sequence when the enemy's health reaches zero.
    /// </summary>
    /// <param name="damageAmount">Amount of damage received.</param>
    public void TakeDamage(float damageAmount)
    {
        // Prevent invalid damage or additional damage after death.
        if (isDead || damageAmount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(
            currentHealth - damageAmount,
            0f
        );

        // Temporary health feedback for combat testing.
        Debug.Log(
            $"{gameObject.name} Health: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    #endregion

    #region Death Handling

    /// <summary>
    /// Disables enemy combat and navigation, triggers the
    /// death animation, and schedules object removal. 
    /// </summary>
    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        // Prevent the AI from initiating additional attacks.
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Stop NavMesh movement before disabling navigation.
        if (navAgent != null && navAgent.enabled)
        {
            if (navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }

            navAgent.enabled = false;
        }

        // Disable the main collider to prevent the defeated
        // enemy from obstructing movement or further shots.
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // Activate the existing Death_A animation transition.
        // The Animator must remain enabled during this sequence.
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool(DeadHash, true);
        }

        Debug.Log($"{gameObject.name} defeated.");

        // Allow the death animation to play before cleanup.
        Destroy(gameObject, deathDespawnDelay);
    }

    #endregion
}
