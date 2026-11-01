
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's current health using text and
/// a horizontal health bar with adjustable color warnings.
/// </summary>
public class HealthHUD : MonoBehaviour
{
    #region Serialized Fields

    [Header("Player Reference")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Health UI References")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthBarFill;

    [Header("Health Bar Colors")]
    [SerializeField]
    private Color healthyColor =
        new Color(0.216f, 0.839f, 0.478f);

    [SerializeField]
    private Color warningColor =
        new Color(0.98f, 0.8f, 0.08f);

    [SerializeField]
    private Color criticalColor =
        new Color(0.94f, 0.27f, 0.27f);

    [Header("Health Warning Thresholds")]
    [SerializeField, Range(0f, 1f)]
    private float warningThreshold = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float criticalThreshold = 0.4f;

    #endregion

    #region Private Fields

    private Vector3 originalFillScale = Vector3.one;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Find the player if a reference was not assigned.
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        // Store the original scale of the health bar.
        if (healthBarFill != null)
        {
            originalFillScale =
                healthBarFill.rectTransform.localScale;
        }
    }

    private void OnEnable()
    {
        // Listen for player health changes.
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthDisplay;
        }
    }

    private void Start()
    {
        // Display the player's starting health.
        if (playerHealth != null)
        {
            UpdateHealthDisplay(
                playerHealth.CurrentHealth,
                playerHealth.MaxHealth
            );
        }
    }

    private void OnDisable()
    {
        // Stop listening when the HUD is disabled.
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthDisplay;
        }
    }

    #endregion

    #region Health Display

    /// <summary>
    /// Updates the health numbers, bar length, and color.
    /// </summary>
    private void UpdateHealthDisplay(
        float currentHealth,
        float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            return;
        }

        // Calculate the remaining health percentage.
        float healthPercentage = Mathf.Clamp01(
            currentHealth / maxHealth
        );

        // Update the numerical health display.
        if (healthText != null)
        {
            healthText.text =
                "HEALTH " +
                Mathf.CeilToInt(currentHealth) +
                "/" +
                Mathf.CeilToInt(maxHealth);
        }

        if (healthBarFill != null)
        {
            // Shrink the bar horizontally from the left pivot.
            healthBarFill.rectTransform.localScale =
                new Vector3(
                    originalFillScale.x * healthPercentage,
                    originalFillScale.y,
                    originalFillScale.z
                );

            UpdateHealthColor(healthPercentage);
        }
    }

    #endregion

    #region Health Bar Colors

    /// <summary>
    /// Changes the bar color based on adjustable
    /// warning and critical health thresholds.
    /// </summary>
    private void UpdateHealthColor(float healthPercentage)
    {
        // Critical health takes priority over warning health.
        if (healthPercentage <= criticalThreshold)
        {
            healthBarFill.color = criticalColor;
        }
        else if (healthPercentage <= warningThreshold)
        {
            healthBarFill.color = warningColor;
        }
        else
        {
            healthBarFill.color = healthyColor;
        }
    }

    #endregion
}
