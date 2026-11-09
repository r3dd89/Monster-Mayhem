
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls player hitscan shooting, automatic fire,
/// ammunition, reloading, aiming, and weapon animations.
/// Uses WeaponShotEffects for visuals and audio.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class WeaponController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Weapon Settings")]
    [SerializeField, Range(1f, 100f)]
    private float damage = 20f;

    [SerializeField, Range(10f, 250f)]
    private float fireRange = 100f;

    [SerializeField, Range(0.05f, 2f)]
    private float fireInterval = 0.25f;

    [Header("Ammunition Settings")]
    [SerializeField, Min(1)]
    private int magazineSize = 12;

    [SerializeField, Min(0)]
    private int startingReserveAmmo = 48;

    [SerializeField, Range(0.1f, 5f)]
    private float reloadDuration = 1.5f;

    [Header("Reload Settings")]
    [SerializeField]
    private bool automaticReload = true;

    [Header("Animation Settings")]
    [SerializeField]
    private Animator soldierAnimator;

    [Header("FPS Animation Settings")]
    [SerializeField]
    private Animator fpsAnimator;

    [Header("Aim Settings")]
    [SerializeField]
    private Transform fpsWeaponTransform;

    [SerializeField, Range(1f, 30f)]
    private float aimTransitionSpeed = 12f;

    [SerializeField]
    private Vector3 aimPosition =
        new Vector3(-0.035f, -1.262f, 0.152f);

    [SerializeField]
    private Vector3 aimRotation = Vector3.zero;

    [Header("Weapon Effects")]
    [SerializeField]
    private WeaponShotEffects weaponShotEffects;

    #endregion

    #region Private Fields

    private PlayerInput playerInput;
    private Camera playerCamera;

    private InputAction attackAction;
    private InputAction aimAction;

    private int currentAmmo;
    private int reserveAmmo;

    private float nextFireTime;

    private bool isReloading;
    private bool isAiming;
    private bool isFiringHeld;

    private Vector3 hipFirePosition;
    private Quaternion hipFireRotation;

    private bool hasFpsAimParameter;
    private bool hasFpsReloadParameter;

    #endregion

    #region Public Properties

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
    public bool IsAiming => isAiming;

    #endregion

    #region Events

    public event Action<int, int> AmmoChanged;

    public event Action<bool> ReloadStateChanged;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        playerCamera = playerInput.camera;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        currentAmmo = magazineSize;
        reserveAmmo = startingReserveAmmo;

        isReloading = false;
        isAiming = false;
        isFiringHeld = false;

        // Preserve the existing hip-fire positioning.
        if (fpsWeaponTransform != null)
        {
            hipFirePosition =
                fpsWeaponTransform.localPosition;

            hipFireRotation =
                fpsWeaponTransform.localRotation;
        }

        // Check the Easy FPS Animator's parameters.
        if (fpsAnimator != null &&
            fpsAnimator.runtimeAnimatorController != null)
        {
            hasFpsAimParameter =
                HasBoolParameter(fpsAnimator, "aiming");

            hasFpsReloadParameter =
                HasBoolParameter(fpsAnimator, "reloading");
        }
    }

    private void Start()
    {
        // Find the existing Input System actions.
        if (playerInput.actions != null)
        {
            attackAction =
                playerInput.actions.FindAction("Attack", false);

            aimAction =
                playerInput.actions.FindAction("Aim", false);
        }

        NotifyAmmoChanged();
        UpdateAnimationStates();
    }

    private void Update()
    {
        // Do not process weapon inputs while paused.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        // Read the fire button continuously.
        if (attackAction != null)
        {
            isFiringHeld = attackAction.IsPressed();
        }

        // Read the aim button continuously.
        if (aimAction != null)
        {
            SetAiming(aimAction.IsPressed());
        }

        // Allow automatic fire in hip-fire and ADS.
        if (isFiringHeld && !isReloading)
        {
            Fire();
        }

        // Smoothly move between hip-fire and ADS.
        UpdateAimPosition();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        isFiringHeld = false;
        isAiming = false;

        if (isReloading)
        {
            isReloading = false;
            ReloadStateChanged?.Invoke(false);
        }

        UpdateAnimationStates();
    }

    #endregion

    #region Weapon Firing

    /// <summary>
    /// Fires one hitscan shot when the cooldown allows.
    /// Plays visual and audio effects for each shot.
    /// </summary>
    private void Fire()
    {
        // Prevent firing during reload or cooldown.
        if (isReloading || Time.time < nextFireTime)
        {
            return;
        }

        // Handle an empty magazine.
        if (currentAmmo <= 0)
        {
            nextFireTime = Time.time + fireInterval;

            if (automaticReload && reserveAmmo > 0)
            {
                TryReload();
            }
            else if (weaponShotEffects != null)
            {
                weaponShotEffects.PlayEmpty();
            }

            return;
        }

        if (playerCamera == null)
        {
            return;
        }

        // Consume a round.
        currentAmmo--;

        nextFireTime = Time.time + fireInterval;

        NotifyAmmoChanged();

        // The camera determines where the shot lands,
        // even when the visible weapon is off-center.
        Ray shotRay = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        Vector3 hitPosition =
            shotRay.GetPoint(fireRange);

        Vector3 hitNormal = Vector3.up;

        bool hasHit = false;

        if (Physics.Raycast(
            shotRay,
            out RaycastHit hit,
            fireRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            hasHit = true;

            hitPosition = hit.point;
            hitNormal = hit.normal;

            // Preserve the existing enemy damage system.
            EnemyHealth enemyHealth =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }

        // Play the gunshot, muzzle flash, tracer,
        // and optional bullet impact.
        if (weaponShotEffects != null)
        {
            weaponShotEffects.PlayShot(
                hitPosition,
                hitNormal,
                hasHit
            );
        }

        // Immediately reload after the last round.
        if (currentAmmo == 0 && automaticReload)
        {
            TryReload();
        }

        // TODO: Add an FPS firing/recoil animation
        // once its Animator state is confirmed.
        // TODO: Support additional weapons.
    }

    #endregion

    #region Weapon Aiming

    /// <summary>
    /// Updates the held aiming state.
    /// </summary>
    private void SetAiming(bool aiming)
    {
        if (isAiming == aiming)
        {
            return;
        }

        isAiming = aiming;

        UpdateAnimationStates();
    }

    /// <summary>
    /// Moves the visible rifle between the saved
    /// hip-fire position and the ADS position.
    /// </summary>
    private void UpdateAimPosition()
    {
        if (fpsWeaponTransform == null)
        {
            return;
        }

        Vector3 targetPosition = isAiming
            ? aimPosition
            : hipFirePosition;

        Quaternion targetRotation = isAiming
            ? Quaternion.Euler(aimRotation)
            : hipFireRotation;

        float transition = Mathf.Clamp01(
            aimTransitionSpeed * Time.deltaTime
        );

        fpsWeaponTransform.localPosition = Vector3.Lerp(
            fpsWeaponTransform.localPosition,
            targetPosition,
            transition
        );

        fpsWeaponTransform.localRotation =
            Quaternion.Slerp(
                fpsWeaponTransform.localRotation,
                targetRotation,
                transition
            );
    }

    #endregion

    #region Reloading

    /// <summary>
    /// Starts reloading when the magazine needs ammo.
    /// </summary>
    private void TryReload()
    {
        if (isReloading ||
            currentAmmo >= magazineSize ||
            reserveAmmo <= 0)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    /// <summary>
    /// Plays the reload animation and sound,
    /// then transfers reserve ammunition.
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        ReloadStateChanged?.Invoke(true);

        UpdateAnimationStates();

        // Play the existing reload sound.
        if (weaponShotEffects != null)
        {
            weaponShotEffects.PlayReload();
        }

        yield return new WaitForSeconds(reloadDuration);

        int ammoNeeded = magazineSize - currentAmmo;

        int ammoToLoad = Mathf.Min(
            ammoNeeded,
            reserveAmmo
        );

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;

        NotifyAmmoChanged();

        ReloadStateChanged?.Invoke(false);

        UpdateAnimationStates();

        // TODO: Add a magazine animation event.
    }

    #endregion

    #region Animation Management

    /// <summary>
    /// Updates the PolygonR and Easy FPS Animators.
    /// </summary>
    private void UpdateAnimationStates()
    {
        // Preserve the original soldier parameters.
        if (soldierAnimator != null)
        {
            soldierAnimator.SetBool(
                "Reloading",
                isReloading
            );

            soldierAnimator.SetBool(
                "Aiming",
                isAiming
            );
        }

        // Use the lowercase Easy FPS parameters.
        if (fpsAnimator != null)
        {
            if (hasFpsReloadParameter)
            {
                fpsAnimator.SetBool(
                    "reloading",
                    isReloading
                );
            }

            if (hasFpsAimParameter)
            {
                fpsAnimator.SetBool(
                    "aiming",
                    isAiming && !isReloading
                );
            }
        }
    }

    /// <summary>
    /// Checks whether an Animator contains
    /// a specified Bool parameter.
    /// </summary>
    private bool HasBoolParameter(
        Animator animator,
        string parameterName)
    {
        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type ==
                AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Ammunition Management

    /// <summary>
    /// Adds ammunition to the reserve from pickups.
    /// </summary>
    public void AddReserveAmmo(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        reserveAmmo += amount;

        NotifyAmmoChanged();

        if (currentAmmo == 0 && automaticReload)
        {
            TryReload();
        }
    }

    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Handles mouse and controller attack input.
    /// </summary>
    public void OnAttack(InputValue value)
    {
        isFiringHeld = value.isPressed;

        if (isFiringHeld && Time.timeScale > 0f)
        {
            Fire();
        }
    }

    /// <summary>
    /// Handles manual reload input.
    /// </summary>
    public void OnReload(InputValue value)
    {
        if (value.isPressed && Time.timeScale > 0f)
        {
            TryReload();
        }
    }

    /// <summary>
    /// Handles mouse and controller aiming input.
    /// </summary>
    public void OnAim(InputValue value)
    {
        SetAiming(value.isPressed);
    }

    #endregion
}
