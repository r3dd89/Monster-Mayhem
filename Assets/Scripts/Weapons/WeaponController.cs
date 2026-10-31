
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages hitscan weapon firing, ammunition, reloading,
/// and damage against enemies with an EnemyHealth component.
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
        isReloading = false;
    }

    #endregion

    #region Weapon Firing

    /// <summary>
    /// Fires a hitscan shot and applies damage when
    /// an enemy is detected by the raycast.
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

        // Consume ammunition and enforce the firing interval.
        currentAmmo--;
        nextFireTime = Time.time + fireInterval;

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
            // Check the collider and its parents for enemy health.
            // This supports characters with colliders on child objects.
            EnemyHealth enemyHealth =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            else
            {
                Debug.Log($"Weapon hit: {hit.collider.name}");
            }
        }

        // Temporary ammunition feedback until the HUD is expanded.
        Debug.Log($"Ammo: {currentAmmo}/{reserveAmmo}");
    }

    #endregion

    #region Reloading

    /// <summary>
    /// Begins reloading when ammunition is available
    /// and the magazine is not already full.
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
    /// Transfers ammunition from the reserve into the
    /// magazine after the configured reload duration.
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadDuration);

        // Transfer only the rounds needed to refill the magazine.
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
