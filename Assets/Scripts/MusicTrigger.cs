using UnityEngine;

/// <summary>
/// Plays or stops specific audio tracks based on the player's CURRENT direction of movement
/// relative to the trigger's forward axis WHILE INSIDE the trigger volume.
/// Tracks selected by name from AudioManager, uses default fade durations.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DirectionalAudioTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("The tag identifying the player GameObject.")]
    public string playerTag = "Player";
    [Tooltip("Minimum velocity magnitude along the trigger's forward axis to register directional movement.")]
    [Range(0.01f, 2f)]
    public float velocityThreshold = 0.1f;

    [Header("Forward Direction Audio (Moving ->)")]
    [Tooltip("Enable applying these settings when moving forward.")]
    public bool enableForwardAudio = true;
    [Tooltip("If checked, stop the current music when moving forward.")]
    public bool stopForwardMusic = false;
    [Tooltip("Music track name to play (if Stop Forward Music is unchecked).")]
    public string forwardMusicName = ""; // Name instead of index
    // Removed: forwardMusicFadeDuration, overrideForwardMusicVolume, forwardMusicVolume
    [Space(5)]
    [Tooltip("If checked, stop the current ambience when moving forward.")]
    public bool stopForwardAmbience = false;
    [Tooltip("Ambience track name to play (if Stop Forward Ambience is unchecked).")]
    public string forwardAmbienceName = ""; // Name instead of index
    // Removed: forwardAmbienceFadeDuration, overrideForwardAmbienceVolume, forwardAmbienceVolume


    [Header("Backward Direction Audio (Moving <-)")]
    [Tooltip("Enable applying these settings when moving backward.")]
    public bool enableBackwardAudio = true;
    [Tooltip("If checked, stop the current music when moving backward.")]
    public bool stopBackwardMusic = false;
    [Tooltip("Music track name to play (if Stop Backward Music is unchecked).")]
    public string backwardMusicName = ""; // Name instead of index
    // Removed: backwardMusicFadeDuration, overrideBackwardMusicVolume, backwardMusicVolume
    [Space(5)]
    [Tooltip("If checked, stop the current ambience when moving backward.")]
    public bool stopBackwardAmbience = false;
    [Tooltip("Ambience track name to play (if Stop Backward Ambience is unchecked).")]
    public string backwardAmbienceName = ""; // Name instead of index
     // Removed: backwardAmbienceFadeDuration, overrideBackwardAmbienceVolume, backwardAmbienceVolume


    [Header("Gizmo Settings")]
    public float gizmoLength = 2.0f;
    public Color forwardDirectionColor = Color.blue;
    public Color backwardDirectionColor = Color.red;
    public bool showTriggerBounds = true;

    private Collider triggerCollider;
    private AudioState lastAppliedState = AudioState.Idle; // Track last action

    // Enum to represent the last applied audio state to prevent spamming AudioManager
    private enum AudioState { Idle, Forward, Backward, StoppedMusicForward, StoppedAmbienceForward, StoppedMusicBackward, StoppedAmbienceBackward }


    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (!triggerCollider.isTrigger) Debug.LogWarning($"{gameObject.name}: Collider not set to 'Is Trigger'.", this);
    }

    void Start()
    {
         if (AudioManager.Instance == null) {
             Debug.LogError($"{gameObject.name}: AudioManager instance not found! Disabling.", this);
             enabled = false; return;
         }
         // Removed index validation
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
             lastAppliedState = AudioState.Idle; // Reset state on enter
             CheckDirectionAndApplyAudio(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Optimization: Only check if the player is still tagged correctly
        if (other.CompareTag(playerTag))
        {
             CheckDirectionAndApplyAudio(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
         if (other.CompareTag(playerTag))
         {
             lastAppliedState = AudioState.Idle; // Reset state on exit
             // Optional: Decide here if you want audio to revert to something else or just stop
             // e.g., AudioManager.Instance.PlayMusic("DefaultAreaMusic");
             // Or just let whatever was playing continue.
         }
    }


    private void CheckDirectionAndApplyAudio(Collider playerCollider)
    {
         if (AudioManager.Instance == null) return;
         Vector3 playerVelocity = GetPlayerVelocity(playerCollider);
         float directionalVelocity = Vector3.Dot(playerVelocity, transform.forward);

         AudioState currentState = AudioState.Idle; // Determine desired state

         if (directionalVelocity > velocityThreshold) { // Moving Forward
             currentState = AudioState.Forward;
         } else if (directionalVelocity < -velocityThreshold) { // Moving Backward
             currentState = AudioState.Backward;
         }
         // Else: Velocity below threshold, remain Idle (do nothing new)

         // Apply settings only if the desired state is different from the last applied one
         if (currentState != AudioState.Idle && currentState != lastAppliedState)
         {
             if (currentState == AudioState.Forward && enableForwardAudio) {
                 ApplyAudioSettings(
                     forwardMusicName, stopForwardMusic,
                     forwardAmbienceName, stopForwardAmbience,
                     "Forward");
                 lastAppliedState = AudioState.Forward; // Update last state
             }
             else if (currentState == AudioState.Backward && enableBackwardAudio) {
                  ApplyAudioSettings(
                     backwardMusicName, stopBackwardMusic,
                     backwardAmbienceName, stopBackwardAmbience,
                     "Backward");
                 lastAppliedState = AudioState.Backward; // Update last state
             }
         }
         // If velocity drops below threshold, lastAppliedState remains, so audio continues until direction changes again or player exits.
    }


    // *** UPDATED ApplyAudioSettings Function ***
    private void ApplyAudioSettings(
        string musicName, bool stopMusic,
        string ambienceName, bool stopAmbience,
        string directionLabel)
    {
        // --- Apply Music Setting ---
        if (stopMusic)
        {
             // Optional check: Only stop if not already stopped by this trigger's last action
            // if(lastAppliedState != AudioState.StoppedMusicForward && lastAppliedState != AudioState.StoppedMusicBackward) // Example check
            // {
                 Debug.Log($"DirectionalTrigger '{gameObject.name}': Stopping music for {directionLabel} direction.", this);
                 AudioManager.Instance.StopMusic();
            // }
        }
        else if (!string.IsNullOrEmpty(musicName))
        {
             // Optional check: Only play if this track isn't already the target
             // if(AudioManager.Instance.currentMusicTrack?.name != musicName) // Requires currentMusicTrack to be public or have a getter
             // {
                 Debug.Log($"DirectionalTrigger '{gameObject.name}': Playing music '{musicName}' for {directionLabel} direction.", this);
                 AudioManager.Instance.PlayMusic(musicName); // Uses default fade & track volume
             // }
        }
        // else: No name provided and not stopping, do nothing for music.


        // --- Apply Ambience Setting --- (Mirrors Music Logic)
        if (stopAmbience)
        {
             Debug.Log($"DirectionalTrigger '{gameObject.name}': Stopping ambience for {directionLabel} direction.", this);
             AudioManager.Instance.StopAmbience();
        }
        else if (!string.IsNullOrEmpty(ambienceName))
        {
            Debug.Log($"DirectionalTrigger '{gameObject.name}': Playing ambience '{ambienceName}' for {directionLabel} direction.", this);
            AudioManager.Instance.PlayAmbience(ambienceName); // Uses default fade & track volume
        }
        // else: No name provided and not stopping, do nothing for ambience.

        // Note: Simple state tracking added in CheckDirectionAndApplyAudio is usually enough
        // to prevent spamming. More complex state tracking (like commented optional checks)
        // can be added if needed, but requires exposing more state from AudioManager.
    }


    // --- Helper Functions (remain the same) ---
     private Vector3 GetPlayerVelocity(Collider playerCollider) {
         // Rigidbody check is generally preferred for physics-based movement
         Rigidbody rb = playerCollider.attachedRigidbody; // Use attachedRigidbody for compound colliders
         if (rb != null) return rb.linearVelocity; // Use rb.velocity (more standard than linearVelocity)

         // Fallback for Rigidbody2D or non-RB movement (less common for 3D triggers)
         Rigidbody2D rb2D = playerCollider.GetComponent<Rigidbody2D>();
         if (rb2D != null) return new Vector3(rb2D.linearVelocity.x, rb2D.linearVelocity.y, 0);

         // Very basic fallback if no Rigidbody (e.g., CharacterController moved via script)
         // This would require tracking position changes manually, which is outside this scope.
         // Debug.LogWarning($"DirectionalAudioTrigger: Could not find Rigidbody or Rigidbody2D on {playerCollider.name}. Velocity detection might not work.", this);
         return Vector3.zero;
     }

    // Removed ValidateTrackIndex function


    // --- Gizmos (remain the same) ---
    void OnDrawGizmos()
    {
        // Draw direction arrows
        if (gizmoLength > 0)
        {
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            Gizmos.color = forwardDirectionColor;
            Gizmos.DrawLine(position, position + forward * gizmoLength);
            Gizmos.DrawRay(position + forward * gizmoLength, forward * -0.2f + transform.right * 0.1f * gizmoLength); // Arrow head parts
            Gizmos.DrawRay(position + forward * gizmoLength, forward * -0.2f - transform.right * 0.1f * gizmoLength);

             Gizmos.color = backwardDirectionColor;
             Gizmos.DrawLine(position, position - forward * gizmoLength);
             Gizmos.DrawRay(position - forward * gizmoLength, forward * 0.2f + transform.right * 0.1f * gizmoLength); // Arrow head parts
             Gizmos.DrawRay(position - forward * gizmoLength, forward * 0.2f - transform.right * 0.1f * gizmoLength);
        }

        // Draw trigger bounds (same as SimpleAudioTrigger)
         if (showTriggerBounds && triggerCollider != null)
        {
            Gizmos.color = new Color(forwardDirectionColor.r, forwardDirectionColor.g, forwardDirectionColor.b, 0.2f); // Use forward color slightly transparent
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            if (triggerCollider is BoxCollider box) { Gizmos.DrawWireCube(box.center, box.size); }
            else if (triggerCollider is SphereCollider sphere) { Gizmos.DrawWireSphere(sphere.center, sphere.radius); }
            else if (triggerCollider is CapsuleCollider capsule) { DrawWireCapsule(capsule.center, capsule.radius, capsule.height, capsule.direction); }
            else { Gizmos.DrawWireCube(triggerCollider.bounds.center - transform.position, triggerCollider.bounds.size); }

            Gizmos.matrix = originalMatrix;
        }
    }
    // Gizmo helper function remains the same
    private void DrawWireCapsule(Vector3 center, float radius, float height, int direction) { /* ... same as in SimpleAudioTrigger ... */ }
}