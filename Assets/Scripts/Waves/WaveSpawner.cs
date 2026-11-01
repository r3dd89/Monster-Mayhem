
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages enemy spawning, wave progression, player recovery,
/// and victory conditions for Monster Mayhem.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    #region Wave States

    private enum WaveState
    {
        Waiting,
        Spawning,
        Fighting,
        Victory,
        PlayerDefeated,
        SetupError
    }

    #endregion

    #region Serialized Fields

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject meleeEnemyPrefab;
    [SerializeField] private GameObject rangedEnemyPrefab;

    [Header("Spawn Locations")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Wave Settings")]
    [SerializeField, Min(1)] private int totalWaves = 5;
    [SerializeField, Min(1)] private int startingEnemyCount = 2;
    [SerializeField, Min(0)] private int additionalEnemiesPerWave = 1;
    [SerializeField, Min(1)] private int rangedEnemyStartWave = 2;

    [Header("Spawn Timing")]
    [SerializeField, Min(0f)] private float firstWaveDelay = 3f;
    [SerializeField, Min(0f)] private float betweenWaveDelay = 5f;
    [SerializeField, Min(0f)] private float spawnInterval = 1f;

    [Header("Enemy Variety")]
    [SerializeField, Range(0f, 1f)]
    private float rangedSpawnChance = 0.35f;

    [Header("Wave Health Recovery")]
    [SerializeField, Range(0f, 1f)]
    private float waveRecoveryPercent = 0.15f;

    [Header("Current Wave Status")]
    [SerializeField] private WaveState currentState = WaveState.Waiting;
    [SerializeField] private int currentWave;
    [SerializeField] private int enemiesRemaining;

    #endregion

    #region Private Fields

    private PlayerHealth playerHealth;

    private readonly List<EnemyHealth> activeEnemies =
        new List<EnemyHealth>();

    private int lastSpawnPointIndex = -1;

    #endregion

    #region Public Properties

    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public int EnemiesRemaining => enemiesRemaining;
    public bool IsVictory => currentState == WaveState.Victory;

    #endregion

    #region Unity Lifecycle Methods

    private void Start()
    {
        // Locate the player before starting the wave system.
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        // Prevent spawning if required scene references are missing.
        if (!ValidateSetup())
        {
            currentState = WaveState.SetupError;
            return;
        }

        // Begin the survival wave sequence.
        StartCoroutine(RunWaves());
    }

    #endregion

    #region Setup Validation

    /// <summary>
    /// Checks that enemy prefabs, spawn points,
    /// and the player health component are available.
    /// </summary>
    private bool ValidateSetup()
    {
        if (playerHealth == null)
        {
            Debug.LogError(
                "WaveSpawner: PlayerHealth was not found."
            );

            return false;
        }

        if (meleeEnemyPrefab == null ||
            rangedEnemyPrefab == null)
        {
            Debug.LogError(
                "WaveSpawner: Enemy prefabs are missing."
            );

            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "WaveSpawner: No spawn points assigned."
            );

            return false;
        }

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                Debug.LogError(
                    "WaveSpawner: A spawn point is missing."
                );

                return false;
            }
        }

        return true;
    }

    #endregion

    #region Wave Management

    /// <summary>
    /// Runs each wave in order, waits for all enemies
    /// to be defeated, and restores health between waves.
    /// </summary>
    private IEnumerator RunWaves()
    {
        for (int wave = 1; wave <= totalWaves; wave++)
        {
            currentWave = wave;
            currentState = WaveState.Waiting;

            // Allow preparation time before the wave begins.
            float delay = wave == 1
                ? firstWaveDelay
                : betweenWaveDelay;

            yield return new WaitForSeconds(delay);

            // Stop if the player was defeated during the break.
            if (CheckPlayerDefeat())
            {
                yield break;
            }

            // Increase the enemy count with each wave.
            int enemyCount = startingEnemyCount +
                (wave - 1) * additionalEnemiesPerWave;

            currentState = WaveState.Spawning;

            // Temporary feedback for wave progression testing.
            Debug.Log("Wave " + currentWave + " started.");

            // Spawn enemies one at a time.
            for (int i = 0; i < enemyCount; i++)
            {
                if (CheckPlayerDefeat())
                {
                    yield break;
                }

                EnemyHealth spawnedEnemy = SpawnEnemy(i);

                // Stop the wave sequence if spawning fails.
                if (spawnedEnemy == null)
                {
                    currentState = WaveState.SetupError;
                    yield break;
                }

                // Track the new enemy.
                activeEnemies.Add(spawnedEnemy);
                UpdateEnemyCount();

                // Space out individual enemy spawns.
                if (i < enemyCount - 1)
                {
                    yield return new WaitForSeconds(
                        spawnInterval
                    );
                }
            }

            currentState = WaveState.Fighting;

            // Wait until the entire wave is defeated.
            while (true)
            {
                UpdateEnemyCount();

                // A defeated player cannot complete the wave.
                if (CheckPlayerDefeat())
                {
                    yield break;
                }

                if (enemiesRemaining == 0)
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("Wave " + currentWave + " cleared!");

            // Restore health after clearing a wave.
            // No recovery is needed after the final victory.
            if (wave < totalWaves)
            {
                RestorePlayerHealth();
            }
        }

        // All configured waves have been completed.
        currentState = WaveState.Victory;

        Debug.Log("Victory! All waves completed.");

        // TODO: Display a dedicated victory screen.
        // TODO: Add a final wave completion sound.
    }

    /// <summary>
    /// Stops wave progression when the player is defeated.
    /// </summary>
    private bool CheckPlayerDefeat()
    {
        if (playerHealth != null && !playerHealth.IsDead)
        {
            return false;
        }

        currentState = WaveState.PlayerDefeated;

        // TODO: Connect this state to the game-over screen.

        return true;
    }

    #endregion

    #region Wave Health Recovery

    /// <summary>
    /// Restores a percentage of maximum player health
    /// after a successfully completed wave.
    /// </summary>
    private void RestorePlayerHealth()
    {
        // Recovery cannot revive a defeated player.
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        // Do not restore health when already at maximum.
        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            return;
        }

        // Calculate the reward using maximum health.
        // Example: 100 maximum health * 0.15 = 15 HP.
        float recoveryAmount =
            playerHealth.MaxHealth * waveRecoveryPercent;

        if (recoveryAmount <= 0f)
        {
            return;
        }

        // PlayerHealth.Heal prevents exceeding maximum health.
        playerHealth.Heal(recoveryAmount);

        // HealthHUD receives the change through HealthChanged.
        // TODO: Display a brief wave recovery notification.
        // TODO: Add a health recovery sound or visual effect.
    }

    #endregion

    #region Enemy Spawning

    /// <summary>
    /// Creates an enemy at a valid NavMesh location
    /// using one of the configured enemy prefabs.
    /// </summary>
    private EnemyHealth SpawnEnemy(int enemyIndex)
    {
        GameObject selectedPrefab =
            SelectEnemyPrefab(enemyIndex);

        Transform spawnPoint = SelectSpawnPoint();

        // Locate nearby walkable ground for the enemy.
        if (!NavMesh.SamplePosition(
            spawnPoint.position,
            out NavMeshHit navHit,
            2f,
            NavMesh.AllAreas))
        {
            Debug.LogError(
                "WaveSpawner: No NavMesh found near " +
                spawnPoint.name
            );

            return null;
        }

        // Spawn the selected enemy on the NavMesh.
        GameObject spawnedEnemy = Instantiate(
            selectedPrefab,
            navHit.position,
            spawnPoint.rotation
        );

        // Activate instances of inactive prefab assets.
        if (!spawnedEnemy.activeSelf)
        {
            spawnedEnemy.SetActive(true);
        }

        // Retrieve the enemy health component for tracking.
        EnemyHealth enemyHealth =
            spawnedEnemy.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            Debug.LogError(
                "WaveSpawner: Spawned enemy has no EnemyHealth."
            );

            Destroy(spawnedEnemy);
            return null;
        }

        return enemyHealth;
    }

    /// <summary>
    /// Selects melee or ranged enemies based on
    /// the current wave and configured variety.
    /// </summary>
    private GameObject SelectEnemyPrefab(int enemyIndex)
    {
        // Wave 1 contains melee enemies only.
        if (currentWave < rangedEnemyStartWave)
        {
            return meleeEnemyPrefab;
        }

        // Guarantee at least one ranged enemy
        // from Wave 2 onward with the current wave sizes.
        if (enemyIndex == 1)
        {
            return rangedEnemyPrefab;
        }

        // Randomly add more ranged enemies.
        if (Random.value < rangedSpawnChance)
        {
            return rangedEnemyPrefab;
        }

        return meleeEnemyPrefab;
    }

    /// <summary>
    /// Chooses a spawn point while avoiding the
    /// previously used location when possible.
    /// </summary>
    private Transform SelectSpawnPoint()
    {
        int index = Random.Range(0, spawnPoints.Length);

        if (spawnPoints.Length > 1 &&
            index == lastSpawnPointIndex)
        {
            index = (index + 1) % spawnPoints.Length;
        }

        lastSpawnPointIndex = index;

        return spawnPoints[index];
    }

    #endregion

    #region Enemy Tracking

    /// <summary>
    /// Counts living enemies without waiting for
    /// death animations or despawn delays to finish.
    /// </summary>
    private void UpdateEnemyCount()
    {
        // Remove enemies that have died or been destroyed.
        activeEnemies.RemoveAll(
            enemy => enemy == null || enemy.IsDead
        );

        enemiesRemaining = activeEnemies.Count;
    }

    #endregion
}
