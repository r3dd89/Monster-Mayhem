
using TMPro;
using UnityEngine;

/// <summary>
/// Updates the gameplay HUD with the current wave
/// and the number of enemies remaining.
/// Gets wave information from the WaveSpawner.
/// </summary>
public class WaveHUD : MonoBehaviour
{
    #region Serialized Fields

    [Header("Wave Manager")]
    [SerializeField] private WaveSpawner waveSpawner;

    [Header("HUD Text References")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemiesRemainingText;

    #endregion

    #region Private Fields

    private int lastWave = -1;
    private int lastTotalWaves = -1;
    private int lastEnemyCount = -1;
    private bool lastVictoryState;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Find the WaveSpawner if one was not assigned.
        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }
    }

    private void Update()
    {
        // Stop updating if the WaveSpawner is missing.
        if (waveSpawner == null)
        {
            return;
        }

        UpdateWaveText();
        UpdateEnemyCount();
    }

    #endregion

    #region Wave Display

    /// <summary>
    /// Displays the current wave number and total waves.
    /// Shows a victory message when all waves are completed.
    /// </summary>
    private void UpdateWaveText()
    {
        if (waveText == null)
        {
            return;
        }

        int currentWave = waveSpawner.CurrentWave;
        int totalWaves = waveSpawner.TotalWaves;
        bool isVictory = waveSpawner.IsVictory;

        // Only update the text when the wave changes.
        if (currentWave == lastWave &&
            totalWaves == lastTotalWaves &&
            isVictory == lastVictoryState)
        {
            return;
        }

        lastWave = currentWave;
        lastTotalWaves = totalWaves;
        lastVictoryState = isVictory;

        if (isVictory)
        {
            waveText.text = "VICTORY!";
        }
        else if (currentWave == 0)
        {
            waveText.text = "GET READY!";
        }
        else
        {
            waveText.text =
                "WAVE " + currentWave + " / " + totalWaves;
        }
    }

    #endregion

    #region Enemy Counter

    /// <summary>
    /// Displays the number of living enemies
    /// currently tracked by the WaveSpawner.
    /// </summary>
    private void UpdateEnemyCount()
    {
        if (enemiesRemainingText == null)
        {
            return;
        }

        // Display a completion message after victory.
        if (waveSpawner.IsVictory)
        {
            enemiesRemainingText.text = "ALL ENEMIES DEFEATED!";
            return;
        }

        int enemyCount = waveSpawner.EnemiesRemaining;

        // Avoid updating the text when the count has not changed.
        if (enemyCount == lastEnemyCount)
        {
            return;
        }

        lastEnemyCount = enemyCount;

        // Keep the enemy counter short for the HUD corner.
        enemiesRemainingText.text = "ENEMIES: " + enemyCount;
    }

    #endregion
}
