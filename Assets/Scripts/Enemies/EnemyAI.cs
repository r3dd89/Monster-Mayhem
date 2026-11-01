
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls melee enemy navigation, player pursuit, and combat.
/// Automatically finds and pursues the player after spawning.
/// Uses Chase and Attack states with two melee animations.
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

    [Header("Combat Range")]
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
    private float nextPlayerSearchTime;

    // Prevent repeated scene searches every frame.
    private const float PlayerSearchInterval = 1f;

    // Cache Animator parameter IDs.
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
        // Cache components required for melee enemy behavior.
        navAgent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }

        // Use an assigned target or locate the player automatically.
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
        // Dead enemies must not continue moving or attacking.
        if (enemyHealth.IsDead)
        {
            return;
        }

        // Locate the player if the reference is missing.
        if (playerTarget == null &&
            Time.time >= nextPlayerSearchTime)
        {
            FindPlayer();

            nextPlayerSearchTime =
                Time.time + PlayerSearchInterval;
        }

        // Navigation requires an active NavMesh Agent.
        if (!navAgent.isOnNavMesh)
        {
            return;
        }

        // Remain idle until a valid player is available.
        if (playerTarget == null)
        {
            HandleIdle();
            UpdateAnimation();
            return;
        }

        // Stop pursuing when the player has been defeated.
        if (playerHealth != null && playerHealth.IsDead)
        {
            HandleIdle();
            UpdateAnimation();
            return;
        }

        // Determine whether to chase or attack the player.
        UpdateEnemyState();

        // Synchronize movement with the Animator.
        UpdateAnimation();
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// Finds the active player so enemies instantiated
    /// from prefabs can acquire their target automatically.
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

    #region State Management

    /// <summary>
    /// Selects the enemy behavior according to attack distance.
    /// The enemy no longer requires a detection radius to chase.
    /// </summary>
    private void UpdateEnemyState()
    {
        float distance = GetDistanceToPlayer();

        // Chase from any distance until within melee range.
        if (distance <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else
        {
            currentState = EnemyState.Chase;
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
    /// Stops navigation when the player is unavailable
    /// or has been defeated.
    /// </summary>
    private void HandleIdle()
    {
        currentState = EnemyState.Idle;
        StopNavigation();
    }

    /// <summary>
    /// Continuously pursues the player using NavMesh pathfinding.
    /// </summary>
    private void HandleChase()
    {
        // Allow the agent to move.
        navAgent.isStopped = false;

        // Refresh the player's position periodically.
        if (Time.time >= nextPathUpdate)
        {
            navAgent.SetDestination(playerTarget.position);

            nextPathUpdate =
                Time.time + pathUpdateInterval;
        }
    }

    /// <summary>
    /// Stops movement, faces the player, and attacks
    /// when the melee cooldown has completed.
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
    /// Stops movement without clearing the current path.
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
    /// Randomly selects one of two melee attacks
    /// and starts the damage sequence.
    /// </summary>
    private void PerformAttack()
    {
        // Prevent attacks from occurring too frequently.
        nextAttackTime = Time.time + attackCooldown;

        if (enemyAnimator != null)
        {
            // Type 0 selects Melee Attack B.
            // Type 1 selects Melee Attack A.
            int attackType = Random.Range(0, 2);

            enemyAnimator.SetInteger(TypeHash, attackType);
            enemyAnimator.SetTrigger(AttackHash);
        }

        // Apply damage after the attack windup.
        StartCoroutine(ApplyMeleeDamage());
    }

    /// <summary>
    /// Damages the player after the attack delay,
    /// provided the player remains in melee range.
    /// </summary>
    private IEnumerator ApplyMeleeDamage()
    {
        yield return new WaitForSeconds(attackHitDelay);

        // Cancel damage if either character is defeated.
        if (enemyHealth.IsDead ||
            playerHealth == null ||
            playerHealth.IsDead)
        {
            yield break;
        }

        // Do not damage a player who escaped melee range.
        if (GetDistanceToPlayer() > attackRange)
        {
            yield break;
        }

        playerHealth.TakeDamage(attackDamage);
    }

    #endregion

    #region Rotation Handling

    /// <summary>
    /// Rotates the enemy horizontally toward the player.
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
    /// Calculates horizontal distance to the player.
    /// </summary>
    /// <returns>Distance in Unity units.</returns>
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
    /// Updates the locomotion animation based on
    /// the NavMesh Agent's current velocity.
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
