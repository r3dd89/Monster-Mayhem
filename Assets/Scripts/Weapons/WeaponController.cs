
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages hitscan weapon firing, ammunition, and reloading.
/// Supports mouse and gamepad input through Unity's Input System.
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

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the camera used for hitscan targeting.
        playerInput = GetComponent<PlayerInput>();
        playerCamera = playerInput.camera;

        // Initialize ammunition when the player is created.
        currentAmmo = magazineSize;
        reserveAmmo = startingReserveAmmo;
    }

    private void OnDisable()
    {
        // Cancel an active reload if the weapon becomes inactive.
        StopAllCoroutines();
        isReloading = false;
    }

    #endregion

    #region Weapon Firing

    /// <summary>
    /// Fires a hitscan shot if ammunition is available
    /// and the weapon is not reloading or on cooldown.
    /// </summary>
    private void Fire()
    {
        if (isReloading || Time.time < nextFireTime)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("Magazine empty. Reload required.");
            return;
        }

        if (playerCamera == null)
        {
            return;
        }

        // Consume one round and enforce the weapon's fire rate.
        currentAmmo--;
        nextFireTime = Time.time + fireInterval;

        Ray shotRay = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        // Check for the first valid collider hit by the shot.
        if (Physics.Raycast(
            shotRay,
            out RaycastHit hit,
            fireRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            // TODO: Apply damage through the enemy health system.
            Debug.Log(
                $"Weapon hit: {hit.collider.name} | Damage: {damage}"
            );
        }

        // Temporary feedback until the ammunition HUD is implemented.
        Debug.Log($"Ammo: {currentAmmo}/{reserveAmmo}");
    }

    #endregion

    #region Reloading

    /// <summary>
    /// Starts reloading if the magazine is not full
    /// and reserve ammunition is available.
    /// </summary>
    private void Reload()
    {
        if (isReloading || currentAmmo == magazineSize)
        {
            return;
        }

        if (reserveAmmo <= 0)
        {
            Debug.Log("No reserve ammunition available.");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    /// <summary>
    /// Waits for the reload duration before transferring
    /// ammunition from the reserve into the magazine.
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        Debug.Log("Reloading...");

        // Prevent firing until the reload duration has elapsed.
        yield return new WaitForSeconds(reloadDuration);

        // Load only the ammunition needed to fill the magazine.
        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;

        Debug.Log($"Reload complete. Ammo: {currentAmmo}/{reserveAmmo}");
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Receives the Attack action from mouse or gamepad input.
    /// </summary>
    /// <param name="value">Current attack button state.</param>
    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            Fire();
        }
    }

    /// <summary>
    /// Receives the Reload action from keyboard or gamepad input.
    /// </summary>
    /// <param name="value">Current reload button state.</param>
    public void OnReload(InputValue value)
    {
        if (value.isPressed)
        {
            Reload();
        }
    }

    #endregion
}
