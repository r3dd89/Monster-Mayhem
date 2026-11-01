
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the player's current weapon name,
/// magazine ammunition, and reserve ammunition.
/// Updates automatically when ammunition changes.
/// </summary>
public class AmmoHUD : MonoBehaviour
{
    #region Serialized Fields

    [Header("Weapon Reference")]
    [SerializeField] private WeaponController weaponController;

    [Header("Ammo UI References")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text ammoText;

    [Header("Weapon Display")]
    [SerializeField]
    private string weaponDisplayName =
        "PRIMARY WEAPON";

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Find the player's weapon if one was not assigned.
        if (weaponController == null)
        {
            weaponController =
                FindFirstObjectByType<WeaponController>();
        }
    }

    private void OnEnable()
    {
        // Listen for ammunition changes.
        if (weaponController != null)
        {
            weaponController.AmmoChanged += UpdateAmmoDisplay;
        }
    }

    private void Start()
    {
        // Display the current weapon name.
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponDisplayName;
        }

        // Initialize the ammunition counter.
        if (weaponController != null)
        {
            UpdateAmmoDisplay(
                weaponController.CurrentAmmo,
                weaponController.ReserveAmmo
            );
        }
    }

    private void OnDisable()
    {
        // Remove the event subscription when disabled.
        if (weaponController != null)
        {
            weaponController.AmmoChanged -= UpdateAmmoDisplay;
        }
    }

    #endregion

    #region Ammunition Display

    /// <summary>
    /// Updates magazine and reserve ammunition values.
    /// </summary>
    /// <param name="magazineAmmo">Rounds in the magazine.</param>
    /// <param name="reserveAmmo">Rounds in reserve.</param>
    private void UpdateAmmoDisplay(
        int magazineAmmo,
        int reserveAmmo)
    {
        if (ammoText == null)
        {
            return;
        }

        ammoText.text = magazineAmmo + " / " + reserveAmmo;
    }

    #endregion
}
