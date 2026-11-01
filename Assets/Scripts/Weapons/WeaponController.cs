
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages hitscan weapon firing, ammunition, reloading,
/// and damage against enemies with an EnemyHealth component.
/// Notifies the HUD when ammunition or reload status changes.
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
    /// Notifies listeners when magazine or reserve ammo changes.
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
        // Cache the camera used for raycast targeting.
        playerInput = GetComponent<PlayerInput>();
        playerCamera = playerInput.camera;

        // Initialize the magazine and reserve ammunition.
        currentAmmo = magazineSize;
        reserveAmmo = startingReserveAmmo;
    }

    private void OnDisable()
    {
        // Cancel an active reload when the weapon is disabled.
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
    /// Fires a hitscan shot and applies damage when
    /// an enemy is detected by the raycast.
    /// </summary>
    private void Fire()
    {
        // Prevent firing during a reload or weapon cooldown.
        if (isReloading || Time.time < nextFireTime)
        {
            return;
        }

        // The weapon cannot fire without ammunition.
        if (currentAmmo <= 0 || playerCamera == null)
        {
            return;
        }

        // Consume one round and apply the firing cooldown.
        currentAmmo--;
        nextFireTime = Time.time + fireInterval;

        // Update the ammunition display immediately.
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
            // Check the collider and its parent objects.
            // This supports enemies with child colliders.
            EnemyHealth enemyHealth =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }

        // TODO: Add muzzle flash and weapon firing audio.
        // TODO: Support additional weapon firing modes.
    }

    #endregion

    #region Reloading

    /// <summary>
    /// Begins reloading when ammunition is available
    /// and the magazine is not already full.
    /// </summary>
    private void Reload()
    {
        // Prevent unnecessary or overlapping reloads.
        if (isReloading || currentAmmo >= magazineSize)
        {
            return;
        }

        // Reloading requires reserve ammunition.
        if (reserveAmmo <= 0)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    /// <summary>
    /// Transfers ammunition from the reserve into the
    /// magazine after the configured reload duration.
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // Show the reloading message on the HUD.
        ReloadStateChanged?.Invoke(true);

        yield return new WaitForSeconds(reloadDuration);

        // Calculate the number of rounds needed.
        int ammoNeeded = magazineSize - currentAmmo;

        // Do not transfer more rounds than are available.
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;

        // Update the counter and hide the reload message.
        NotifyAmmoChanged();
        ReloadStateChanged?.Invoke(false);
    }

    #endregion

    #region Ammunition Management

    /// <summary>
    /// Adds ammunition to the weapon's reserve.
    /// Intended for future ammunition pickups.
    /// </summary>
    /// <param name="amount">Number of rounds to add.</param>
    public void AddReserveAmmo(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Add ammunition without changing the magazine.
        reserveAmmo += amount;

        // Notify the HUD of the new reserve amount.
        NotifyAmmoChanged();
    }

    /// <summary>
    /// Notifies the HUD of current magazine and reserve ammo.
    /// </summary>
    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Processes attack input from the mouse or gamepad.
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
    /// Processes reload input from the keyboard or gamepad.
    /// </summary>
    /// <param name="value">Current reload input state.</param>
    public void OnReload(InputValue value)
    {
        if (value.isPressed)
        {
            Reload();
        }
    }

    #endregion
}
