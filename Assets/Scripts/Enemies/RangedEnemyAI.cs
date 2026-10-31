
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls ranged enemy detection, navigation, aiming,
/// shooting, and damage to the player.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyHealth))]
public class RangedEnemyAI : MonoBehaviour
{
    #region Enemy States

    private enum RangedState
    {
        Idle,
        Chase,
        Aim
    }

    [Header("Current State")]
    [SerializeField] private RangedState currentState = RangedState.Idle;

    #endregion

    #region Serialized Fields

    [Header("Player Detection")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 12f;

    [Header("Movement Settings")]
    [SerializeField] private float pathUpdateInterval = 0.2f;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Ranged Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float shotOriginHeight = 1.4f;

    #endregion

    #region Private References

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth enemyHealth;

    private float nextPathUpdateTime;
    private float nextAttackTime;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        // Cache the components required for enemy behavior.
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();

        // Automatically find the player when a scene reference
        // has not been assigned, such as when spawning a prefab.
        if (playerTarget == null)
        {
            FindPlayer();
        }
    }

    private void Update()
    {
        // Prevent enemy behavior after death.
        if (enemyHealth.IsDead)
        {
            return;
        }

        // Find the player if the reference is missing.
        if (playerTarget == null)
        {
            FindPlayer();

            if (playerTarget == null)
            {
                return;
            }
        }

        // Navigation requires a valid NavMesh.
        if (!agent.isOnNavMesh)
        {
            return;
        }

        // Determine the distance to the player.
        float distanceToPlayer = Vector3.Distance(
            transform.position,
            playerTarget.position
        );

        // Choose the appropriate behavior.
        if (distanceToPlayer > detectionRange)
        {
            UpdateIdle();
        }
        else if (distanceToPlayer > attackRange)
        {
            UpdateChase();
        }
        else
        {
            UpdateAim();
        }

        // Update the movement animation.
        animator.SetFloat(
            "Speed",
            agent.isStopped ? 0f : agent.velocity.magnitude
        );
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// Locates the active PlayerHealth component and
    /// uses its Transform as the enemy's target.
    /// </summary>
    private void FindPlayer()
    {
        PlayerHealth playerHealth =
            FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerTarget = playerHealth.transform;
        }
    }

    #endregion

    #region Enemy Behaviors

    /// <summary>
    /// Stops navigation while the player is outside
    /// the detection range.
    /// </summary>
    private void UpdateIdle()
    {
        currentState = RangedState.Idle;

        StopMoving();

        animator.SetBool("Aiming", false);
    }

    /// <summary>
    /// Pursues the player until they enter attack range.
    /// </summary>
    private void UpdateChase()
    {
        currentState = RangedState.Chase;

        animator.SetBool("Aiming", false);

        // Resume movement toward the player.
        agent.isStopped = false;

        // Refresh the destination periodically.
        if (Time.time >= nextPathUpdateTime)
        {
            agent.SetDestination(playerTarget.position);

            nextPathUpdateTime =
                Time.time + pathUpdateInterval;
        }
    }

    /// <summary>
    /// Stops movement, faces the player, and fires
    /// when the attack cooldown is complete.
    /// </summary>
    private void UpdateAim()
    {
        currentState = RangedState.Aim;

        StopMoving();
        FacePlayer();

        // Raise the rifle.
        animator.SetBool("Aiming", true);

        // Attempt a ranged attack.
        TryShoot();
    }

    #endregion

    #region Ranged Combat

    /// <summary>
    /// Fires a raycast toward the player's collider
    /// and applies damage when the shot connects.
    /// </summary>
    private void TryShoot()
    {
        // Wait until the attack cooldown finishes.
        if (Time.time < nextAttackTime)
        {
            return;
        }

        // Retrieve the player's existing collision capsule.
        CharacterController playerCollider =
            playerTarget.GetComponent<CharacterController>();

        if (playerCollider == null || !playerCollider.enabled)
        {
            return;
        }

        // Schedule the next available shot.
        nextAttackTime = Time.time + attackCooldown;

        // Play the rifle shooting animation.
        animator.SetTrigger("Shoot");

        // Approximate the rifle's firing position.
        Vector3 shotOrigin =
            transform.position +
            Vector3.up * shotOriginHeight +
            transform.forward * 0.65f;

        // Aim at the center of the player's collider.
        Vector3 shotTarget = playerCollider.bounds.center;

        Vector3 shotDirection = shotTarget - shotOrigin;
        float shotDistance = shotDirection.magnitude;

        if (shotDistance <= 0.01f)
        {
            return;
        }

        // Fire a raycast toward the player.
        if (Physics.Raycast(
            shotOrigin,
            shotDirection.normalized,
            out RaycastHit hit,
            shotDistance + 0.2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            // Only damage objects containing PlayerHealth.
            PlayerHealth playerHealth =
                hit.collider.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        // TODO: Use an actual rifle muzzle Transform
        // instead of the approximate firing position.

        // TODO: Add rifle muzzle flashes and impact effects.

        // TODO: Add weapon audio and reloading.

        // TODO: Add a close-range rifle bash attack.
    }

    #endregion

    #region Movement Helpers

    /// <summary>
    /// Stops navigation and clears the current path.
    /// </summary>
    private void StopMoving()
    {
        agent.isStopped = true;

        if (agent.hasPath)
        {
            agent.ResetPath();
        }
    }

    /// <summary>
    /// Rotates the enemy horizontally toward the player.
    /// </summary>
    private void FacePlayer()
    {
        Vector3 direction =
            playerTarget.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
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
}
