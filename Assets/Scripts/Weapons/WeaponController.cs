
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages hitscan weapon firing, ammunition, manual and
/// automatic reloading, and damage against enemies.
/// Notifies the HUD whenever ammunition changes.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class WeaponController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Weapon Settings")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float fireRange = 100f;
    [SerializeField] private float fireInterval = 0.25f;

    [Header("Ammunition Settings")]
    [SerializeField, Min(1)] private int magazineSize = 12;
    [SerializeField, Min(0)] private int startingReserveAmmo = 48;
    [SerializeField, Min(0.1f)] private float reloadDuration = 1.5f;

    [Header("Reload Settings")]
    [SerializeField] private bool automaticReload = true;

    #endregion

    #region Private Fields

    private PlayerInput playerInput;
    private Camera playerCamera;

    private int currentAmmo;
    private int reserveAmmo;

    private float nextFireTime;
    private bool isReloading;

    #endregion

    #region Public Properties

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;

    #endregion

    #region Events

    /// <summary>
    /// Notifies listeners when magazine or reserve
    /// ammunition changes.
    /// </summary>
    public event Action<int, int> AmmoChanged;

    /// <summary>
    /// Notifies listeners when reloading starts or finishes.
    /// </summary>
    public event Action<bool> ReloadStateChanged;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the PlayerInput and camera references.
        playerInput = GetComponent<PlayerInput>();
        playerCamera = playerInput.camera;

        // Initialize starting ammunition.
        currentAmmo = magazineSize;
        reserveAmmo = startingReserveAmmo;
    }

    private void OnDisable()
    {
        // Cancel active reloads when the weapon is disabled.
        // This also prevents reloading after player death.
        StopAllCoroutines();

        if (isReloading)
        {
            isReloading = false;
            ReloadStateChanged?.Invoke(false);
        }
    }

    #endregion

    #region Weapon Firing

    /// <summary>
    /// Fires a hitscan shot, consumes ammunition,
    /// and automatically reloads when the magazine empties.
    /// </summary>
    private void Fire()
    {
        // Prevent firing during a reload or weapon cooldown.
        if (isReloading || Time.time < nextFireTime)
        {
            return;
        }

        // Attempt to reload if the magazine is empty.
        if (currentAmmo <= 0)
        {
            if (automaticReload)
            {
                TryReload();
            }

            return;
        }

        // A valid camera is required for hitscan shooting.
        if (playerCamera == null)
        {
            return;
        }

        // Consume one round and enforce the firing cooldown.
        currentAmmo--;
        nextFireTime = Time.time + fireInterval;

        // Immediately update the ammunition counter.
        NotifyAmmoChanged();

        // Fire from the center of the player's camera.
        Ray shotRay = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            shotRay,
            out RaycastHit hit,
            fireRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            // Search the hit collider and its parents
            // for an enemy health component.
            EnemyHealth enemyHealth =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }

        // Reload immediately after firing the final round.
        if (currentAmmo == 0 && automaticReload)
        {
            TryReload();
        }

        // TODO: Add muzzle flashes and weapon firing audio.
        // TODO: Support additional player weapons.
    }

    #endregion

    #region Reloading

    /// <summary>
    /// Starts a reload when the weapon is eligible.
    /// Supports both manual and automatic reloading.
    /// </summary>
    private void TryReload()
    {
        // Prevent multiple reloads from starting at once.
        if (isReloading)
        {
            return;
        }

        // Do not reload a full magazine.
        if (currentAmmo >= magazineSize)
        {
            return;
        }

        // Reserve ammunition is required.
        if (reserveAmmo <= 0)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    /// <summary>
    /// Waits for the reload duration, then transfers
    /// ammunition from the reserve to the magazine.
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        // Mark the weapon as reloading.
        isReloading = true;

        // Notify other systems that reloading has started.
        ReloadStateChanged?.Invoke(true);

        // Prevent instant ammunition replenishment.
        yield return new WaitForSeconds(reloadDuration);

        // Calculate the ammunition required to refill.
        int ammoNeeded = magazineSize - currentAmmo;

        // Transfer only the ammunition currently available.
        int ammoToLoad = Mathf.Min(
            ammoNeeded,
            reserveAmmo
        );

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        // The weapon is ready to fire again.
        isReloading = false;

        // Update the ammunition counter.
        NotifyAmmoChanged();

        // Notify listeners that reloading has finished.
        ReloadStateChanged?.Invoke(false);

        // TODO: Add a reload animation.
        // TODO: Add weapon reload audio.
    }

    #endregion

    #region Ammunition Management

    /// <summary>
    /// Adds ammunition to the player's reserve.
    /// Automatically reloads an empty magazine when enabled.
    /// </summary>
    /// <param name="amount">Number of rounds to add.</param>
    public void AddReserveAmmo(int amount)
    {
        // Ignore invalid ammunition amounts.
        if (amount <= 0)
        {
            return;
        }

        // Add the collected ammunition to the reserve.
        reserveAmmo += amount;

        // Update the ammo HUD immediately.
        NotifyAmmoChanged();

        // Automatically reload if ammunition was collected
        // while the magazine was completely empty.
        if (currentAmmo == 0 && automaticReload)
        {
            TryReload();
        }
    }

    /// <summary>
    /// Notifies the HUD whenever ammunition changes.
    /// </summary>
    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Processes attack input from mouse or gamepad.
    /// </summary>
    /// <param name="value">Current attack input state.</param>
    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            Fire();
        }
    }

    /// <summary>
    /// Processes manual reload input from keyboard or gamepad.
    /// </summary>
    /// <param name="value">Current reload input state.</param>
    public void OnReload(InputValue value)
    {
        if (value.isPressed)
        {
            TryReload();
        }
    }

    #endregion
}
