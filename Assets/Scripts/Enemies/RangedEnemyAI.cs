
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls ranged enemy navigation, player pursuit,
/// aiming, shooting, and damage.
/// Automatically pursues the player after spawning.
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

    #endregion

    #region Serialized Fields

    [Header("Current State")]
    [SerializeField] private RangedState currentState = RangedState.Idle;

    [Header("Player Target")]
    [SerializeField] private Transform playerTarget;

    [Header("Combat Range")]
    [SerializeField, Min(0.1f)] private float attackRange = 12f;

    [Header("Movement Settings")]
    [SerializeField, Min(0.05f)] private float pathUpdateInterval = 0.2f;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Ranged Attack")]
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.5f;
    [SerializeField] private float shotOriginHeight = 1.4f;

    #endregion

    #region Private Fields

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth enemyHealth;
    private PlayerHealth playerHealth;

    private float nextPathUpdateTime;
    private float nextAttackTime;
    private float nextPlayerSearchTime;

    private const float PlayerSearchInterval = 1f;

    // Cache frequently used Animator parameters.
    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int AimingHash =
        Animator.StringToHash("Aiming");

    private static readonly int ShootHash =
        Animator.StringToHash("Shoot");

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the components required for ranged combat.
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();

        // Retrieve the player when no target is assigned.
        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<PlayerHealth>();
        }
        else
        {
            FindPlayer();
        }
    }

    private void Update()
    {
        // Dead enemies cannot continue pursuing or shooting.
        if (enemyHealth.IsDead)
        {
            return;
        }

        // Search periodically if the target is missing.
        if (playerTarget == null &&
            Time.time >= nextPlayerSearchTime)
        {
            FindPlayer();

            nextPlayerSearchTime =
                Time.time + PlayerSearchInterval;
        }

        // The agent must be positioned on a valid NavMesh.
        if (!agent.isOnNavMesh)
        {
            return;
        }

        // Remain idle when no player is available.
        if (playerTarget == null)
        {
            UpdateIdle();
            UpdateAnimation();
            return;
        }

        // Stop pursuing and shooting after player death.
        if (playerHealth != null && playerHealth.IsDead)
        {
            UpdateIdle();
            UpdateAnimation();
            return;
        }

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            playerTarget.position
        );

        // Pursue the player until within shooting range.
        if (distanceToPlayer > attackRange)
        {
            UpdateChase();
        }
        else
        {
            UpdateAim();
        }

        UpdateAnimation();
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// Locates the active player when an enemy
    /// is instantiated from its prefab.
    /// </summary>
    private void FindPlayer()
    {
        PlayerHealth foundPlayer =
            FindFirstObjectByType<PlayerHealth>();

        if (foundPlayer == null)
        {
            return;
        }

        playerHealth = foundPlayer;
        playerTarget = foundPlayer.transform;
    }

    #endregion

    #region Enemy Behaviors

    /// <summary>
    /// Stops moving and lowers the rifle when
    /// no valid player target is available.
    /// </summary>
    private void UpdateIdle()
    {
        currentState = RangedState.Idle;

        StopMoving();

        animator.SetBool(AimingHash, false);
    }

    /// <summary>
    /// Continuously pursues the player until
    /// the player enters shooting range.
    /// </summary>
    private void UpdateChase()
    {
        currentState = RangedState.Chase;

        // Stop aiming while navigating toward the player.
        animator.SetBool(AimingHash, false);

        // Resume navigation.
        agent.isStopped = false;

        // Refresh the player's destination periodically.
        if (Time.time >= nextPathUpdateTime)
        {
            agent.SetDestination(playerTarget.position);

            nextPathUpdateTime =
                Time.time + pathUpdateInterval;
        }
    }

    /// <summary>
    /// Stops movement, faces the player, and
    /// attempts a ranged attack.
    /// </summary>
    private void UpdateAim()
    {
        currentState = RangedState.Aim;

        StopMoving();
        FacePlayer();

        // Raise the rifle before firing.
        animator.SetBool(AimingHash, true);

        TryShoot();
    }

    #endregion

    #region Ranged Combat

    /// <summary>
    /// Fires a raycast toward the player's collision
    /// capsule and applies damage on a successful hit.
    /// </summary>
    private void TryShoot()
    {
        // Do not fire until the cooldown has completed.
        if (Time.time < nextAttackTime)
        {
            return;
        }

        // Use the player's existing CharacterController.
        CharacterController playerCollider =
            playerTarget.GetComponent<CharacterController>();

        if (playerCollider == null || !playerCollider.enabled)
        {
            return;
        }

        // Schedule the next available shot.
        nextAttackTime = Time.time + attackCooldown;

        // Trigger the rifle shooting animation.
        animator.SetTrigger(ShootHash);

        // Approximate the rifle's firing position.
        Vector3 shotOrigin =
            transform.position +
            Vector3.up * shotOriginHeight +
            transform.forward * 0.65f;

        // Aim toward the center of the player's collider.
        Vector3 shotTarget = playerCollider.bounds.center;

        Vector3 shotDirection = shotTarget - shotOrigin;
        float shotDistance = shotDirection.magnitude;

        if (shotDistance <= 0.01f)
        {
            return;
        }

        // Fire toward the player.
        // Obstacles can block the raycast.
        if (Physics.Raycast(
            shotOrigin,
            shotDirection.normalized,
            out RaycastHit hit,
            shotDistance + 0.2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            PlayerHealth hitPlayer =
                hit.collider.GetComponentInParent<PlayerHealth>();

            if (hitPlayer != null && !hitPlayer.IsDead)
            {
                hitPlayer.TakeDamage(attackDamage);
            }
        }

        // TODO: Replace the approximate shot origin
        // with a dedicated rifle muzzle Transform.

        // TODO: Add muzzle flashes and impact effects.

        // TODO: Add rifle shooting audio.

        // TODO: Implement ammunition and reloading.

        // TODO: Implement close-range rifle bash combat.
    }

    #endregion

    #region Movement Helpers

    /// <summary>
    /// Stops the enemy and clears its navigation path.
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

    #region Animation Handling

    /// <summary>
    /// Updates the movement animation to match
    /// the NavMesh Agent's current movement speed.
    /// </summary>
    private void UpdateAnimation()
    {
        animator.SetFloat(
            SpeedHash,
            agent.isStopped ? 0f : agent.velocity.magnitude
        );
    }

    #endregion
}
