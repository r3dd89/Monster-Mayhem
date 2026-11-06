
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

    [Header("Component References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyHealth enemyHealth;

    [Space(10)]
    [Header("Current State")]
    [SerializeField]
    private RangedState currentState =
        RangedState.Idle;

    [Space(10)]
    [Header("Target Settings")]
    [SerializeField] private Transform playerTarget;

    [Space(10)]
    [Header("Combat Settings")]
    [SerializeField, Range(0.1f, 30f)]
    private float attackRange = 12f;

    [SerializeField, Range(0f, 50f)]
    private float attackDamage = 10f;

    [SerializeField, Range(0.1f, 5f)]
    private float attackCooldown = 1.5f;

    [Space(10)]
    [Header("Navigation Settings")]
    [SerializeField, Range(0.05f, 1f)]
    private float pathUpdateInterval = 0.2f;

    [SerializeField, Range(90f, 720f)]
    private float rotationSpeed = 360f;

    [Space(10)]
    [Header("Shooting Settings")]
    [SerializeField, Range(0.5f, 3f)]
    private float shotOriginHeight = 1.4f;

    #endregion

    #region Private Fields

    private PlayerHealth playerHealth;

    private float nextPathUpdateTime;
    private float nextAttackTime;
    private float nextPlayerSearchTime;

    // Prevent repeated scene searches every frame.
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
        // Locate required components if they
        // have not been assigned in the Inspector.
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        // Retrieve the player when no target is assigned.
        if (playerTarget != null)
        {
            playerHealth =
                playerTarget.GetComponent<PlayerHealth>();
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
