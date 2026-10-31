
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls enemy detection, navigation, and melee combat.
/// Uses Idle, Chase, and Attack states to manage enemy behavior.
/// Supports multiple melee animations through Animator parameters.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    #region Enemy States

    private enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }

    #endregion

    #region Serialized Fields

    [Header("Target Settings")]
    [SerializeField] private Transform playerTarget;

    [Header("Detection Settings")]
    [SerializeField, Min(0.1f)] private float detectionRange = 12f;
    [SerializeField, Min(0.1f)] private float attackRange = 5f;

    [Header("Navigation Settings")]
    [SerializeField, Min(0.05f)] private float pathUpdateInterval = 0.2f;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Attack Settings")]
    [SerializeField, Min(1f)] private float attackDamage = 20f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.5f;
    [SerializeField, Min(0f)] private float attackHitDelay = 0.4f;

    [Header("Animation Settings")]
    [SerializeField] private Animator enemyAnimator;

    #endregion

    #region Private Fields

    private NavMeshAgent navAgent;
    private EnemyHealth enemyHealth;
    private PlayerHealth playerHealth;

    private EnemyState currentState = EnemyState.Idle;

    private float nextPathUpdate;
    private float nextAttackTime;

    // Cache Animator parameter IDs for repeated access.
    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int TypeHash =
        Animator.StringToHash("Type");

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache components required for navigation and combat.
        navAgent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }

        // Reference the player's health component.
        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        // Enemy behavior requires a valid target and NavMesh.
        if (playerTarget == null || !navAgent.isOnNavMesh)
        {
            return;
        }

        // Stop pursuing the player when either character dies.
        if (enemyHealth.IsDead ||
            (playerHealth != null && playerHealth.IsDead))
        {
            StopNavigation();
            UpdateAnimation();
            return;
        }

        UpdateEnemyState();
        UpdateAnimation();
    }

    #endregion

    #region State Management

    /// <summary>
    /// Determines the current enemy state based on
    /// the horizontal distance between enemy and player.
    /// </summary>
    private void UpdateEnemyState()
    {
        float distance = GetDistanceToPlayer();

        if (distance <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (distance <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = EnemyState.Idle;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }

    #endregion

    #region Enemy Behaviors

    /// <summary>
    /// Stops enemy movement when the player is
    /// outside the detection range.
    /// </summary>
    private void HandleIdle()
    {
        StopNavigation();
    }

    /// <summary>
    /// Pursues the player using NavMesh pathfinding.
    /// </summary>
    private void HandleChase()
    {
        navAgent.isStopped = false;

        // Update the destination at a controlled interval.
        if (Time.time >= nextPathUpdate)
        {
            navAgent.SetDestination(playerTarget.position);
            nextPathUpdate = Time.time + pathUpdateInterval;
        }
    }

    /// <summary>
    /// Stops movement, faces the player, and performs
    /// melee attacks when the cooldown allows.
    /// </summary>
    private void HandleAttack()
    {
        StopNavigation();
        FacePlayer();

        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
        }
    }

    /// <summary>
    /// Stops navigation without removing the active path.
    /// </summary>
    private void StopNavigation()
    {
        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
    }

    #endregion

    #region Attack Handling

    /// <summary>
    /// Randomly selects one of two melee animations
    /// and begins the attack damage sequence.
    /// </summary>
    private void PerformAttack()
    {
        // Enforce the cooldown between melee attacks.
        nextAttackTime = Time.time + attackCooldown;

        if (enemyAnimator != null)
        {
            // Type 0 selects Melee Attack B.
            // Type 1 selects Melee Attack A.
            int attackType = Random.Range(0, 2);

            // Set the animation variation before triggering it.
            enemyAnimator.SetInteger(TypeHash, attackType);
            enemyAnimator.SetTrigger(AttackHash);
        }

        StartCoroutine(ApplyMeleeDamage());
    }

    /// <summary>
    /// Applies melee damage after the attack windup,
    /// provided the player remains within attack range.
    /// </summary>
    private IEnumerator ApplyMeleeDamage()
    {
        // Approximate the impact point of the attack animation.
        yield return new WaitForSeconds(attackHitDelay);

        if (enemyHealth.IsDead ||
            playerHealth == null ||
            playerHealth.IsDead)
        {
            yield break;
        }

        // Cancel the hit if the player escapes attack range.
        if (GetDistanceToPlayer() > attackRange)
        {
            yield break;
        }

        playerHealth.TakeDamage(attackDamage);
    }

    #endregion

    #region Rotation Handling

    /// <summary>
    /// Gradually rotates the enemy toward the player.
    /// </summary>
    private void FacePlayer()
    {
        Vector3 direction =
            playerTarget.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Calculates horizontal distance between the enemy
    /// and the player.
    /// </summary>
    /// <returns>Horizontal distance in Unity units.</returns>
    private float GetDistanceToPlayer()
    {
        Vector3 offset =
            playerTarget.position - transform.position;

        offset.y = 0f;

        return offset.magnitude;
    }

    #endregion

    #region Animation Handling

    /// <summary>
    /// Updates locomotion animations according to
    /// the NavMesh Agent's movement velocity.
    /// </summary>
    private void UpdateAnimation()
    {
        if (enemyAnimator == null)
        {
            return;
        }

        float movementSpeed = navAgent.velocity.magnitude;

        enemyAnimator.SetFloat(
            SpeedHash,
            movementSpeed,
            0.1f,
            Time.deltaTime
        );
    }

    #endregion
}
