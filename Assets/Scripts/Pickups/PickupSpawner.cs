
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns health and ammunition pickups when a new wave begins.
/// Uncollected pickups remain available between waves.
/// </summary>
public class PickupSpawner : MonoBehaviour
{
    #region Serialized Fields

    [Header("Game References")]
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject ammoPickupPrefab;

    [Header("Pickup Spawn Points")]
    [SerializeField] private Transform[] pickupSpawnPoints;

    [Header("Spawn Settings")]
    [SerializeField, Min(0)] private int healthPickupsPerWave = 1;
    [SerializeField, Min(0)] private int ammoPickupsPerWave = 1;
    [SerializeField, Min(1)] private int maxActivePickups = 4;

    #endregion

    #region Private Fields

    private int lastProcessedWave;

    private readonly List<Transform> availableSpawnPoints =
        new List<Transform>();

    #endregion

    #region Unity Lifecycle Methods

    private void Update()
    {
        // Stop if required gameplay references are missing.
        if (waveSpawner == null || playerHealth == null)
        {
            return;
        }

        // Do not spawn supplies after defeat or victory.
        if (playerHealth.IsDead || waveSpawner.IsVictory)
        {
            return;
        }

        int currentWave = waveSpawner.CurrentWave;

        // The first wave has not started yet.
        if (currentWave <= 0)
        {
            return;
        }

        // Ignore waves that have already received supplies.
        if (currentWave == lastProcessedWave)
        {
            return;
        }

        // A lower wave number indicates a reset.
        if (currentWave < lastProcessedWave)
        {
            lastProcessedWave = 0;
        }

        // Record the wave before spawning to prevent duplicates.
        lastProcessedWave = currentWave;

        SpawnWavePickups();
    }

    #endregion

    #region Pickup Spawning

    /// <summary>
    /// Attempts to spawn ammunition and health pickups
    /// while respecting available locations and the active limit.
    /// </summary>
    private void SpawnWavePickups()
    {
        // Collect spawn locations that do not hold a pickup.
        RefreshAvailableSpawnPoints();

        // Prioritize ammunition if only one location is available.
        for (int i = 0; i < ammoPickupsPerWave; i++)
        {
            SpawnPickup(ammoPickupPrefab);
        }

        for (int i = 0; i < healthPickupsPerWave; i++)
        {
            SpawnPickup(healthPickupPrefab);
        }
    }

    /// <summary>
    /// Finds empty spawn points that can receive a new pickup.
    /// </summary>
    private void RefreshAvailableSpawnPoints()
    {
        availableSpawnPoints.Clear();

        if (pickupSpawnPoints == null)
        {
            return;
        }

        foreach (Transform spawnPoint in pickupSpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            // Spawned pickups are children of their spawn points.
            // An occupied point cannot receive another pickup.
            if (spawnPoint.childCount > 0)
            {
                continue;
            }

            availableSpawnPoints.Add(spawnPoint);
        }
    }

    /// <summary>
    /// Instantiates a pickup at a randomly selected empty point.
    /// </summary>
    private void SpawnPickup(GameObject pickupPrefab)
    {
        if (pickupPrefab == null)
        {
            return;
        }

        // Respect the maximum number of active pickups.
        if (CountActivePickups() >= maxActivePickups)
        {
            return;
        }

        if (availableSpawnPoints.Count == 0)
        {
            return;
        }

        // Select a random available spawn location.
        int randomIndex =
            Random.Range(0, availableSpawnPoints.Count);

        Transform selectedPoint =
            availableSpawnPoints[randomIndex];

        // Remove the location so it cannot be used twice
        // during the current spawning operation.
        availableSpawnPoints.RemoveAt(randomIndex);

        // Create the pickup as a child of its spawn point.
        // This makes it possible to detect occupied locations.
        Instantiate(
            pickupPrefab,
            selectedPoint.position,
            selectedPoint.rotation,
            selectedPoint
        );

        // TODO: Add a pickup spawning visual effect.
    }

    /// <summary>
    /// Counts all pickups currently occupying spawn points.
    /// </summary>
    private int CountActivePickups()
    {
        int activeCount = 0;

        if (pickupSpawnPoints == null)
        {
            return activeCount;
        }

        foreach (Transform spawnPoint in pickupSpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            // Each point is allowed to hold only one pickup.
            if (spawnPoint.childCount > 0)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    #endregion
}
