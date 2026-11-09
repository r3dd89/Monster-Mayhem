
using UnityEngine;

/// <summary>
/// Handles visual and audio effects for weapon firing.
/// Can be reused by player and ranged enemy weapons.
/// Does not calculate damage or consume ammunition.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WeaponShotEffects : MonoBehaviour
{
    #region Serialized Fields

    [Header("Muzzle Flash Settings")]
    [SerializeField]
    private Transform muzzlePoint;

    [SerializeField]
    private GameObject muzzleFlashPrefab;

    [SerializeField, Range(0.1f, 3f)]
    private float muzzleFlashLifetime = 0.5f;

    [Header("Bullet Tracer Settings")]
    [SerializeField]
    private bool showBulletTracer = true;

    [SerializeField]
    private Material tracerMaterial;

    [SerializeField]
    private Color tracerColor = Color.cyan;

    [SerializeField, Range(0.005f, 0.1f)]
    private float tracerWidth = 0.025f;

    [SerializeField, Range(0.02f, 0.5f)]
    private float tracerLifetime = 0.08f;

    [Header("Bullet Impact Settings")]
    [SerializeField]
    private GameObject impactEffectPrefab;

    [SerializeField, Range(0.1f, 10f)]
    private float impactEffectLifetime = 2f;

    [Header("Audio Settings")]
    [SerializeField]
    private AudioSource effectsAudioSource;

    [SerializeField]
    private AudioClip gunshotSound;

    [SerializeField]
    private AudioClip reloadSound;

    [SerializeField]
    private AudioClip emptySound;

    [SerializeField]
    private AudioClip impactSound;

    [Header("Audio Volume Settings")]
    [SerializeField, Range(0f, 1f)]
    private float gunshotVolume = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float reloadVolume = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float impactVolume = 0.6f;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Use the AudioSource on this object
        // when one has not been assigned.
        if (effectsAudioSource == null)
        {
            effectsAudioSource = GetComponent<AudioSource>();
        }
    }

    #endregion

    #region Weapon Firing Effects

    /// <summary>
    /// Plays the gunshot sound, muzzle flash, tracer,
    /// and an impact effect when something is hit.
    /// </summary>
    public void PlayShot(
        Vector3 hitPosition,
        Vector3 hitNormal,
        bool hasHit)
    {
        PlaySound(gunshotSound, gunshotVolume);

        SpawnMuzzleFlash();

        SpawnBulletTracer(hitPosition);

        if (hasHit)
        {
            SpawnImpactEffect(hitPosition, hitNormal);

            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(
                    impactSound,
                    hitPosition,
                    impactVolume
                );
            }
        }
    }

    #endregion

    #region Muzzle Flash

    /// <summary>
    /// Spawns a temporary muzzle flash
    /// at the weapon's barrel.
    /// </summary>
    private void SpawnMuzzleFlash()
    {
        if (muzzlePoint == null ||
            muzzleFlashPrefab == null)
        {
            return;
        }

        GameObject flash = Instantiate(
            muzzleFlashPrefab,
            muzzlePoint.position,
            muzzlePoint.rotation,
            muzzlePoint
        );

        // Start particle systems even when the prefab
        // does not have Play On Awake enabled.
        ParticleSystem[] particles =
            flash.GetComponentsInChildren
            <ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }

        Destroy(flash, muzzleFlashLifetime);
    }

    #endregion

    #region Bullet Tracer

    /// <summary>
    /// Creates a short-lived visual line showing
    /// the bullet traveling toward its target.
    /// Damage continues to use the existing raycast.
    /// </summary>
    private void SpawnBulletTracer(Vector3 targetPosition)
    {
        if (!showBulletTracer ||
            muzzlePoint == null ||
            tracerMaterial == null)
        {
            return;
        }

        GameObject tracerObject =
            new GameObject("Bullet Tracer");

        LineRenderer line =
            tracerObject.AddComponent<LineRenderer>();

        line.material = tracerMaterial;

        line.positionCount = 2;
        line.useWorldSpace = true;

        line.startWidth = tracerWidth;
        line.endWidth = tracerWidth * 0.5f;

        line.startColor = tracerColor;
        line.endColor = tracerColor;

        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        line.receiveShadows = false;

        // Start the visual at the barrel.
        line.SetPosition(0, muzzlePoint.position);

        // End at the actual raycast hit or maximum range.
        line.SetPosition(1, targetPosition);

        Destroy(tracerObject, tracerLifetime);
    }

    #endregion

    #region Impact Effects

    /// <summary>
    /// Spawns a temporary impact effect
    /// at the bullet's collision point.
    /// </summary>
    private void SpawnImpactEffect(
        Vector3 hitPosition,
        Vector3 hitNormal)
    {
        if (impactEffectPrefab == null)
        {
            return;
        }

        GameObject impact = Instantiate(
            impactEffectPrefab,
            hitPosition + hitNormal * 0.01f,
            Quaternion.LookRotation(hitNormal)
        );

        Destroy(impact, impactEffectLifetime);
    }

    #endregion

    #region Audio Management

    /// <summary>
    /// Plays the configured reload sound.
    /// </summary>
    public void PlayReload()
    {
        PlaySound(reloadSound, reloadVolume);
    }

    /// <summary>
    /// Plays the empty magazine click.
    /// </summary>
    public void PlayEmpty()
    {
        PlaySound(emptySound, gunshotVolume);
    }

    /// <summary>
    /// Plays an audio clip using the existing AudioSource.
    /// </summary>
    private void PlaySound(AudioClip clip, float volume)
    {
        if (effectsAudioSource == null || clip == null)
        {
            return;
        }

        effectsAudioSource.PlayOneShot(clip, volume);
    }

    #endregion
}
