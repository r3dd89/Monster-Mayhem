
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls ranged enemy navigation, player pursuit,
/// aiming, shooting, and damage.
/// Automatically pursues the player after spawning.
/// Adds detection audio, rifle audio, muzzle flashes,
/// bullet tracers, and optional impact effects.
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

    [Space(10)]
    [Header("Enemy Audio Settings")]
    [SerializeField]
    private AudioSource enemyAudioSource;

    [Space(10)]
    [Header("Detection Audio Settings")]
    [SerializeField]
    private AudioClip[] detectionSounds;

    [SerializeField, Range(0f, 1f)]
    private float detectionVolume = 0.8f;

    [Space(10)]
    [Header("Shooting Audio Settings")]
    [SerializeField]
    private AudioClip[] shootingSounds;

    [SerializeField, Range(0f, 1f)]
    private float shootingVolume = 0.8f;

    [Space(10)]
    [Header("Muzzle Flash Settings")]
    [SerializeField]
    private Transform muzzlePoint;

    [SerializeField]
    private GameObject muzzleFlashPrefab;

    [SerializeField, Min(0.05f)]
    private float muzzleFlashLifetime = 0.25f;

    [SerializeField]
    private Vector3 muzzleFlashRotationOffset =
        Vector3.zero;

    [Space(10)]
    [Header("Bullet Tracer Settings")]
    [SerializeField]
    private bool showBulletTracer = true;

    [SerializeField]
    private Material tracerMaterial;

    [SerializeField]
    private Color tracerColor = Color.red;

    [SerializeField, Min(0.001f)]
    private float tracerWidth = 0.012f;

    [SerializeField, Min(0.01f)]
    private float tracerLifetime = 0.09f;

    [Space(10)]
    [Header("Bullet Impact Settings")]
    [SerializeField]
    private GameObject impactEffectPrefab;

    [SerializeField, Min(0.1f)]
    private float impactEffectLifetime = 1.5f;

    #endregion

    #region Private Fields

    private PlayerHealth playerHealth;

    private float nextPathUpdateTime;
    private float nextAttackTime;
    private float nextPlayerSearchTime;

    // Prevent repeated scene searches every frame.
    private const float PlayerSearchInterval = 1f;

    // Play the detection sound once per enemy instance.
    private bool hasPlayedDetectionSound;

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

        // Reuse the AudioSource used by EnemyHealth.
        // Create one only when necessary.
        EnsureAudioSource();

        // Reset the detection sound for this enemy.
        hasPlayedDetectionSound = false;

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

        // Play one screech when the enemy begins engaging.
        if (!hasPlayedDetectionSound)
        {
            PlayRandomSound(
                detectionSounds,
                detectionVolume
            );

            hasPlayedDetectionSound = true;
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
    /// Also plays the shooting effects.
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

        if (playerCollider == null ||
            !playerCollider.enabled)
        {
            return;
        }

        // Schedule the next available shot.
        nextAttackTime = Time.time + attackCooldown;

        // Trigger the existing rifle shooting animation.
        animator.SetTrigger(ShootHash);

        // Preserve the original gameplay raycast origin.
        // This avoids changing existing damage behavior.
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

        // Perform the existing gameplay raycast.
        // Obstacles can block the shot.
        bool hasHit = Physics.Raycast(
            shotOrigin,
            shotDirection.normalized,
            out RaycastHit hit,
            shotDistance + 0.2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hasHit)
        {
            PlayerHealth hitPlayer =
                hit.collider.GetComponentInParent<PlayerHealth>();

            // Damage is applied only here.
            if (hitPlayer != null && !hitPlayer.IsDead)
            {
                hitPlayer.TakeDamage(attackDamage);
            }
        }

        // Determine where the visual shot should end.
        Vector3 tracerEnd = hasHit
            ? hit.point
            : shotTarget;

        // Use the real muzzle for visuals when assigned.
        // Otherwise, use the original shot origin.
        Vector3 visualOrigin = muzzlePoint != null
            ? muzzlePoint.position
            : shotOrigin;

        // Play the laser/gunshot audio.
        PlayRandomSound(
            shootingSounds,
            shootingVolume
        );

        // Display the muzzle flash.
        SpawnMuzzleFlash(
            visualOrigin,
            shotDirection.normalized
        );

        // Display a visual tracer.
        SpawnBulletTracer(
            visualOrigin,
            tracerEnd
        );

        // Display an impact when a raycast hits something.
        if (hasHit)
        {
            SpawnImpactEffect(
                hit.point,
                hit.normal
            );
        }

        // TODO: Implement ammunition and reloading.
        // TODO: Implement close-range rifle bash combat.
    }

    #endregion

    #region Muzzle Flash Effects

    /// <summary>
    /// Creates a temporary muzzle flash at the
    /// assigned rifle muzzle position.
    /// </summary>
    private void SpawnMuzzleFlash(
        Vector3 visualOrigin,
        Vector3 shotDirection)
    {
        if (muzzleFlashPrefab == null)
        {
            return;
        }

        // Use the muzzle Transform's rotation when available.
        Quaternion baseRotation = muzzlePoint != null
            ? muzzlePoint.rotation
            : Quaternion.LookRotation(shotDirection);

        // Allow prefab orientation adjustments in Inspector.
        Quaternion flashRotation =
            baseRotation *
            Quaternion.Euler(muzzleFlashRotationOffset);

        GameObject flash = Instantiate(
            muzzleFlashPrefab,
            visualOrigin,
            flashRotation
        );

        // Restart particle systems included in the prefab.
        ParticleSystem[] particles =
            flash.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }

        // Remove the temporary effect automatically.
        Destroy(flash, muzzleFlashLifetime);
    }

    #endregion

    #region Bullet Tracer Effects

    /// <summary>
    /// Creates a brief visual line from the muzzle
    /// to the actual raycast hit location.
    /// This does not apply additional damage.
    /// </summary>
    private void SpawnBulletTracer(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        if (!showBulletTracer ||
            tracerMaterial == null)
        {
            return;
        }

        GameObject tracerObject =
            new GameObject("Enemy Bullet Tracer");

        LineRenderer tracer =
            tracerObject.AddComponent<LineRenderer>();

        // Configure the line appearance.
        tracer.material = tracerMaterial;
        tracer.useWorldSpace = true;

        tracer.positionCount = 2;

        tracer.startWidth = tracerWidth;
        tracer.endWidth = tracerWidth;

        tracer.startColor = tracerColor;
        tracer.endColor = tracerColor;

        tracer.alignment = LineAlignment.View;

        // Reduce unnecessary lighting calculations.
        tracer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        tracer.receiveShadows = false;

        // Connect the rifle muzzle to the hit position.
        tracer.SetPosition(0, startPosition);
        tracer.SetPosition(1, endPosition);

        // Keep the tracer visible only briefly.
        Destroy(tracerObject, tracerLifetime);
    }

    #endregion

    #region Bullet Impact Effects

    /// <summary>
    /// Spawns an optional visual effect when
    /// the ranged attack hits a collider.
    /// </summary>
    private void SpawnImpactEffect(
        Vector3 hitPosition,
        Vector3 hitNormal)
    {
        if (impactEffectPrefab == null)
        {
            return;
        }

        Quaternion impactRotation =
            Quaternion.LookRotation(hitNormal);

        GameObject impact = Instantiate(
            impactEffectPrefab,
            hitPosition,
            impactRotation
        );

        // Play any particle systems in the prefab.
        ParticleSystem[] particles =
            impact.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }

        // Remove the temporary effect automatically.
        Destroy(impact, impactEffectLifetime);
    }

    #endregion

    #region Enemy Audio Handling

    /// <summary>
    /// Reuses an existing AudioSource or creates
    /// a 3D AudioSource for the enemy.
    /// </summary>
    private void EnsureAudioSource()
    {
        if (enemyAudioSource == null)
        {
            enemyAudioSource = GetComponent<AudioSource>();
        }

        if (enemyAudioSource != null)
        {
            return;
        }

        enemyAudioSource =
            gameObject.AddComponent<AudioSource>();

        enemyAudioSource.playOnAwake = false;
        enemyAudioSource.loop = false;

        // Enemy sounds should be heard in 3D space.
        enemyAudioSource.spatialBlend = 1f;

        enemyAudioSource.minDistance = 2f;
        enemyAudioSource.maxDistance = 20f;
    }

    /// <summary>
    /// Selects a random nonempty audio clip
    /// and plays it at the assigned volume.
    /// </summary>
    private void PlayRandomSound(
        AudioClip[] sounds,
        float volume)
    {
        if (sounds == null || sounds.Length == 0)
        {
            return;
        }

        int validCount = 0;

        // Count assigned clips.
        foreach (AudioClip sound in sounds)
        {
            if (sound != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return;
        }

        int selectedIndex =
            Random.Range(0, validCount);

        // Select a random assigned clip.
        foreach (AudioClip sound in sounds)
        {
            if (sound == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                PlayEnemySound(sound, volume);
                return;
            }

            selectedIndex--;
        }
    }

    /// <summary>
    /// Plays enemy audio without interrupting
    /// other sounds already playing.
    /// </summary>
    private void PlayEnemySound(
        AudioClip clip,
        float volume)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSource();

        if (enemyAudioSource == null)
        {
            return;
        }

        enemyAudioSource.PlayOneShot(
            clip,
            volume
        );
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
