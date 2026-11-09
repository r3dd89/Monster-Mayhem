
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player walking, running, jumping,
/// damage, and death sounds.
/// Uses a separate looping AudioSource for movement
/// so walking and running sounds switch correctly.
/// </summary>
public class PlayerAudioController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Component References")]
    [SerializeField]
    private CharacterController characterController;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private AudioSource effectsAudioSource;

    [SerializeField]
    private AudioSource footstepAudioSource;

    [Header("Footstep Settings")]
    [SerializeField]
    private AudioClip[] walkSounds;

    [SerializeField]
    private AudioClip[] runSounds;

    [SerializeField]
    private bool useLoopingMovementAudio = true;

    [SerializeField, Min(0.1f)]
    private float walkStepInterval = 0.48f;

    [SerializeField, Min(0.1f)]
    private float runStepInterval = 0.30f;

    [SerializeField, Min(0f)]
    private float movementThreshold = 0.2f;

    [SerializeField, Min(0f)]
    private float runSpeedThreshold = 6.5f;

    [Header("Sprint Input Settings")]
    [SerializeField]
    private string sprintActionName = "Sprint";

    [Header("Jump Settings")]
    [SerializeField]
    private AudioClip jumpSound;

    [SerializeField]
    private string jumpActionName = "Jump";

    [Header("Damage and Death Settings")]
    [SerializeField]
    private AudioClip[] damageSounds;

    [SerializeField]
    private AudioClip deathSound;

    [Header("Audio Volume Settings")]
    [SerializeField, Range(0f, 1f)]
    private float footstepVolume = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float jumpVolume = 0.75f;

    [SerializeField, Range(0f, 1f)]
    private float damageVolume = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float deathVolume = 0.9f;

    #endregion

    #region Private Fields

    private InputAction jumpAction;
    private InputAction sprintAction;

    private float nextFootstepTime;
    private float previousHealth;

    private bool wasGrounded;

    private AudioClip currentMovementClip;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Find the existing Player components.
        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        // Reuse the existing sound effects AudioSource.
        if (effectsAudioSource == null)
        {
            effectsAudioSource = GetComponent<AudioSource>();
        }

        // Movement needs a separate AudioSource.
        // This prevents footstep sounds from interfering
        // with shooting, jumping, or damage sounds.
        if (footstepAudioSource == null ||
            footstepAudioSource == effectsAudioSource)
        {
            footstepAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        // Configure the movement AudioSource.
        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = useLoopingMovementAudio;
        footstepAudioSource.spatialBlend = 0f;
        footstepAudioSource.volume = footstepVolume;

        if (characterController != null)
        {
            wasGrounded = characterController.isGrounded;
        }
    }

    private void OnEnable()
    {
        // Subscribe to health updates.
        if (playerHealth != null)
        {
            previousHealth = playerHealth.CurrentHealth;

            playerHealth.HealthChanged +=
                HandleHealthChanged;
        }
    }

    private void Start()
    {
        // Find existing Input System actions.
        if (playerInput != null &&
            playerInput.actions != null)
        {
            jumpAction = playerInput.actions.FindAction(
                jumpActionName,
                false
            );

            sprintAction = playerInput.actions.FindAction(
                sprintActionName,
                false
            );
        }
    }

    private void Update()
    {
        // Stop footsteps when paused or dead.
        if (Time.timeScale <= 0f ||
            (playerHealth != null && playerHealth.IsDead))
        {
            StopMovementAudio();
            return;
        }

        HandleJump();
        HandleFootsteps();

        if (characterController != null)
        {
            wasGrounded = characterController.isGrounded;
        }
    }

    private void OnDisable()
    {
        // Remove the existing health event subscription.
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -=
                HandleHealthChanged;
        }

        StopMovementAudio();
    }

    #endregion

    #region Footstep Audio

    /// <summary>
    /// Determines whether the player is walking,
    /// running, or standing still.
    /// </summary>
    private void HandleFootsteps()
    {
        if (characterController == null ||
            !characterController.isGrounded)
        {
            StopMovementAudio();
            return;
        }

        Vector3 movementVelocity =
            characterController.velocity;

        // Ignore vertical velocity when measuring speed.
        movementVelocity.y = 0f;

        float movementSpeed = movementVelocity.magnitude;

        // Stop footsteps when the player stops moving.
        if (movementSpeed < movementThreshold)
        {
            StopMovementAudio();
            return;
        }

        // Use Sprint input when available.
        // Otherwise, determine running from movement speed.
        bool isRunning = sprintAction != null
            ? sprintAction.IsPressed()
            : movementSpeed >= runSpeedThreshold;

        AudioClip[] selectedSounds = isRunning
            ? runSounds
            : walkSounds;

        if (useLoopingMovementAudio)
        {
            UpdateLoopingMovementAudio(selectedSounds);
        }
        else
        {
            UpdateIndividualFootsteps(
                selectedSounds,
                isRunning
            );
        }
    }

    /// <summary>
    /// Switches immediately between walking and
    /// running audio when movement state changes.
    /// </summary>
    private void UpdateLoopingMovementAudio(
        AudioClip[] selectedSounds)
    {
        if (footstepAudioSource == null)
        {
            return;
        }

        AudioClip selectedClip =
            GetFirstValidClip(selectedSounds);

        if (selectedClip == null)
        {
            StopMovementAudio();
            return;
        }

        // Avoid restarting the same clip every frame.
        if (currentMovementClip == selectedClip &&
            footstepAudioSource.isPlaying)
        {
            return;
        }

        // Stop the previous walking or running sound.
        footstepAudioSource.Stop();

        // Assign the new movement clip.
        currentMovementClip = selectedClip;

        footstepAudioSource.clip = selectedClip;
        footstepAudioSource.loop = true;
        footstepAudioSource.volume = footstepVolume;

        // Immediately start the correct movement sound.
        footstepAudioSource.Play();
    }

    /// <summary>
    /// Plays individual footsteps when looping
    /// movement audio is disabled.
    /// </summary>
    private void UpdateIndividualFootsteps(
        AudioClip[] selectedSounds,
        bool isRunning)
    {
        if (footstepAudioSource == null ||
            Time.time < nextFootstepTime)
        {
            return;
        }

        AudioClip clip =
            GetRandomValidClip(selectedSounds);

        if (clip != null)
        {
            footstepAudioSource.Stop();
            footstepAudioSource.loop = false;

            footstepAudioSource.clip = clip;
            footstepAudioSource.volume = footstepVolume;

            footstepAudioSource.Play();
        }

        float stepInterval = isRunning
            ? runStepInterval
            : walkStepInterval;

        nextFootstepTime = Time.time + stepInterval;
    }

    /// <summary>
    /// Stops all walking or running audio.
    /// </summary>
    private void StopMovementAudio()
    {
        if (footstepAudioSource != null &&
            footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }

        currentMovementClip = null;
        nextFootstepTime = Time.time;
    }

    /// <summary>
    /// Returns the first assigned clip in an array.
    /// </summary>
    private AudioClip GetFirstValidClip(
        AudioClip[] clips)
    {
        if (clips == null)
        {
            return null;
        }

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a random valid sound from an array.
    /// </summary>
    private AudioClip GetRandomValidClip(
        AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        // Collect only assigned clips.
        AudioClip[] validClips =
            System.Array.FindAll(
                clips,
                clip => clip != null
            );

        if (validClips.Length == 0)
        {
            return null;
        }

        return validClips[
            Random.Range(0, validClips.Length)
        ];
    }

    #endregion

    #region Jump Audio

    /// <summary>
    /// Plays the jump sound when Jump is pressed
    /// while the player is grounded.
    /// </summary>
    private void HandleJump()
    {
        if (jumpAction == null ||
            characterController == null)
        {
            return;
        }

        if (jumpAction.WasPressedThisFrame() &&
            (characterController.isGrounded || wasGrounded))
        {
            PlaySound(jumpSound, jumpVolume);
        }
    }

    #endregion

    #region Damage and Death Audio

    /// <summary>
    /// Plays damage sounds when health decreases.
    /// Plays the death sound when health reaches zero.
    /// </summary>
    private void HandleHealthChanged(
        float currentHealth,
        float maxHealth)
    {
        if (currentHealth < previousHealth)
        {
            if (currentHealth <= 0f)
            {
                StopMovementAudio();

                PlaySound(
                    deathSound,
                    deathVolume
                );
            }
            else
            {
                PlayRandomClip(
                    damageSounds,
                    damageVolume
                );
            }
        }

        previousHealth = currentHealth;
    }

    #endregion

    #region Audio Helpers

    /// <summary>
    /// Plays a randomly selected sound effect.
    /// </summary>
    private void PlayRandomClip(
        AudioClip[] clips,
        float volume)
    {
        AudioClip selectedClip =
            GetRandomValidClip(clips);

        PlaySound(selectedClip, volume);
    }

    /// <summary>
    /// Plays an effect without stopping other
    /// sound effects using the AudioSource.
    /// </summary>
    private void PlaySound(
        AudioClip clip,
        float volume)
    {
        if (effectsAudioSource == null ||
            clip == null)
        {
            return;
        }

        effectsAudioSource.PlayOneShot(
            clip,
            volume
        );
    }

    #endregion
}
