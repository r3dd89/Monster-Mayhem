
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the interactive main door in Monster Mayhem.
/// Press E to open it. The door automatically closes
/// after a delay when the player leaves its trigger area.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class MainDoorController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Component References")]
    [SerializeField] private Transform doorPanel;
    [SerializeField] private AudioSource doorAudioSource;

    [Space(10)]
    [Header("Additional Moving Parts")]
    [SerializeField]
    private Transform[] additionalMovingParts =
        new Transform[0];

    [Space(10)]
    [Header("Interaction Settings")]
    [SerializeField] private Key interactionKey = Key.E;

    [Space(10)]
    [Header("Door Opening Settings")]
    [SerializeField, Range(1f, 6f)]
    private float openHeight = 3.5f;

    [SerializeField, Range(0.25f, 5f)]
    private float openDuration = 1.5f;

    [Space(10)]
    [Header("Door Closing Settings")]
    [SerializeField] private bool autoClose = true;

    [SerializeField, Min(0f)]
    private float closeDelay = 1.5f;

    [Space(10)]
    [Header("Door Audio Settings")]
    [SerializeField] private AudioClip openSound;

    [SerializeField, Range(0f, 1f)]
    private float openSoundVolume = 1f;

    [SerializeField] private AudioClip closeSound;

    [SerializeField, Range(0f, 1f)]
    private float closeSoundVolume = 1f;

    [Space(10)]
    [Header("Door Events")]
    [SerializeField]
    private UnityEvent onFullyOpened =
        new UnityEvent();

    #endregion

    #region Private Fields

    private BoxCollider triggerCollider;
    private PlayerHealth nearbyPlayer;

    private Vector3 closedPosition;
    private Vector3[] additionalClosedPositions;

    private float currentHeightOffset;

    private bool isOpening;
    private bool isClosing;
    private bool isOpen;
    private bool closeInterrupted;

    // Prevents Wave 1 from being triggered repeatedly.
    private bool hasInvokedFullyOpened;

    #endregion

    #region Public Properties

    public bool IsOpen => isOpen;
    public bool IsOpening => isOpening;
    public bool IsClosing => isClosing;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Prepare the interaction collider.
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // Save the original closed door position.
        if (doorPanel != null)
        {
            closedPosition = doorPanel.position;
        }

        // Prepare the additional moving parts array.
        if (additionalMovingParts == null)
        {
            additionalMovingParts = new Transform[0];
        }

        additionalClosedPositions =
            new Vector3[additionalMovingParts.Length];

        // Remember where the keypad and other parts started.
        for (int i = 0; i < additionalMovingParts.Length; i++)
        {
            if (additionalMovingParts[i] != null)
            {
                additionalClosedPositions[i] =
                    additionalMovingParts[i].position;
            }
        }
    }

    private void Update()
    {
        // Ignore interaction while the door is moving
        // or already open.
        if (isOpening || isClosing || isOpen)
        {
            return;
        }

        // The player must be nearby and alive.
        if (!IsPlayerNearby())
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // Open the door when E is pressed.
        if (keyboard[interactionKey].wasPressedThisFrame)
        {
            OpenDoor();
        }
    }

    private void OnDisable()
    {
        nearbyPlayer = null;
    }

    #endregion

    #region Player Detection

    private void OnTriggerEnter(Collider other)
    {
        DetectPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Handles players already standing in the area.
        if (nearbyPlayer == null)
        {
            DetectPlayer(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth exitingPlayer =
            other.GetComponentInParent<PlayerHealth>();

        if (exitingPlayer != null &&
            exitingPlayer == nearbyPlayer)
        {
            nearbyPlayer = null;
        }
    }

    private void DetectPlayer(Collider other)
    {
        PlayerHealth detectedPlayer =
            other.GetComponentInParent<PlayerHealth>();

        if (detectedPlayer == null || detectedPlayer.IsDead)
        {
            return;
        }

        nearbyPlayer = detectedPlayer;

        // TODO: Display "Press E to Open" on the HUD.
    }

    private bool IsPlayerNearby()
    {
        return nearbyPlayer != null && !nearbyPlayer.IsDead;
    }

    #endregion

    #region Door Controls

    /// <summary>
    /// Starts opening the door if it is currently closed.
    /// </summary>
    public void OpenDoor()
    {
        if (doorPanel == null || isOpening ||
            isClosing || isOpen)
        {
            return;
        }

        isOpening = true;

        StartCoroutine(DoorCycleRoutine());
    }

    /// <summary>
    /// Handles opening, waiting, and closing.
    /// If a player enters while closing, the door reopens.
    /// </summary>
    private IEnumerator DoorCycleRoutine()
    {
        // Repeat only if closing is interrupted.
        while (true)
        {
            #region Opening

            isOpening = true;
            isClosing = false;

            PlayDoorSound(openSound, openSoundVolume);

            // Slide the door and keypad upward.
            yield return MoveDoorRoutine(openHeight, false);

            isOpening = false;
            isOpen = true;

            // Invoke the opening event once only.
            // This prevents restarting Wave 1 on reopening.
            if (!hasInvokedFullyOpened)
            {
                hasInvokedFullyOpened = true;
                onFullyOpened.Invoke();
            }

            #endregion

            #region Open Delay

            // Leave the door open if automatic closing
            // has been disabled in the Inspector.
            if (!autoClose)
            {
                yield break;
            }

            // Wait after the door finishes opening.
            yield return new WaitForSeconds(closeDelay);

            // Do not close while the player is still
            // inside the interaction trigger.
            while (IsPlayerNearby())
            {
                yield return null;
            }

            #endregion

            #region Closing

            isOpen = false;
            isClosing = true;
            closeInterrupted = false;

            PlayDoorSound(closeSound, closeSoundVolume);

            // Return the panel and keypad to their
            // original positions.
            yield return MoveDoorRoutine(0f, true);

            isClosing = false;

            // Reopen automatically if the player
            // entered the trigger during closing.
            if (closeInterrupted)
            {
                continue;
            }

            // Closing finished successfully.
            yield break;

            #endregion
        }
    }

    #endregion

    #region Door Movement

    /// <summary>
    /// Moves the door between its current height
    /// and the requested height.
    /// </summary>
    private IEnumerator MoveDoorRoutine(
        float targetHeight,
        bool checkForPlayer)
    {
        float startingHeight = currentHeightOffset;

        // Adjust duration when reversing a partially
        // closed door so its movement stays smooth.
        float distance =
            Mathf.Abs(targetHeight - startingHeight);

        float duration = openDuration *
            (distance / Mathf.Max(openHeight, 0.01f));

        duration = Mathf.Max(duration, 0.01f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Interrupt closing when a player approaches.
            if (checkForPlayer && IsPlayerNearby())
            {
                closeInterrupted = true;
                yield break;
            }

            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            currentHeightOffset = Mathf.Lerp(
                startingHeight,
                targetHeight,
                smoothProgress
            );

            MoveDoorParts(currentHeightOffset);

            yield return null;
        }

        // Apply the exact final position.
        currentHeightOffset = targetHeight;
        MoveDoorParts(currentHeightOffset);
    }

    /// <summary>
    /// Moves the door panel and all assigned
    /// additional objects by the same vertical offset.
    /// </summary>
    private void MoveDoorParts(float heightOffset)
    {
        Vector3 movementOffset =
            Vector3.up * heightOffset;

        doorPanel.position =
            closedPosition + movementOffset;

        for (int i = 0; i < additionalMovingParts.Length; i++)
        {
            if (additionalMovingParts[i] == null)
            {
                continue;
            }

            additionalMovingParts[i].position =
                additionalClosedPositions[i] + movementOffset;
        }
    }

    #endregion

    #region Door Audio

    private void PlayDoorSound(AudioClip clip, float volume)
    {
        if (doorAudioSource == null || clip == null)
        {
            return;
        }

        doorAudioSource.PlayOneShot(clip, volume);
    }

    #endregion
}
