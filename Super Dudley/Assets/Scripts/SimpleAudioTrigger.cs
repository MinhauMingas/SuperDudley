using UnityEngine;

/// <summary>
/// Plays or stops specified music and/or ambience tracks when the player enters the trigger volume.
/// Tracks are selected by name from the AudioManager and use default fade durations.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SimpleAudioTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("The tag identifying the player GameObject.")]
    public string playerTag = "Player";
    [Tooltip("If true, this trigger will only activate once.")]
    public bool triggerOnce = true;
    [Tooltip("If Trigger Once is true, should the trigger Collider be disabled after activation?")]
    public bool disableColliderAfterTriggering = true;

    [Header("Music Settings")]
    [Tooltip("Check this box to affect the music track when triggered.")]
    public bool changeMusic = true;
    [Tooltip("If checked, stop the current music. If unchecked, play the 'Music Track Name'.")]
    public bool stopMusicInstead = false; // New flag for stopping
    [Tooltip("The exact name of the music track in AudioManager to play (if Stop Music Instead is unchecked). Case-sensitive.")]
    public string musicTrackName = ""; // Changed from index to name

    // Removed: musicFadeDuration, overrideMusicVolume, targetMusicVolume

    [Header("Ambience Settings")]
    [Tooltip("Check this box to affect the ambience track when triggered.")]
    public bool changeAmbience = true;
    [Tooltip("If checked, stop the current ambience. If unchecked, play the 'Ambience Track Name'.")]
    public bool stopAmbienceInstead = false; // New flag for stopping
    [Tooltip("The exact name of the ambience track in AudioManager to play (if Stop Ambience Instead is unchecked). Case-sensitive.")]
    public string ambienceTrackName = ""; // Changed from index to name

    // Removed: ambienceFadeDuration, overrideAmbienceVolume, targetAmbienceVolume

    [Header("Gizmo Settings")]
    public bool showTriggerBounds = true;
    public Color boundsColor = Color.green;

    private Collider triggerCollider;
    private bool hasTriggered = false;

    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} is not set to 'Is Trigger'. SimpleAudioTrigger requires a trigger collider.", this);
        }
    }

    void Start()
    {
        // Initial validation for AudioManager existence
        if (AudioManager.Instance == null)
        {
            Debug.LogError($"SimpleAudioTrigger on {gameObject.name}: AudioManager instance not found! Disabling trigger.", this);
            enabled = false;
            return;
        }
        // Removed track index validation
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Player entered SimpleAudioTrigger: {gameObject.name}. Applying audio settings.", this);
            ApplyAudioSettings();

            if (triggerOnce)
            {
                hasTriggered = true;
                if (disableColliderAfterTriggering && triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                    Debug.Log($"Disabled trigger collider on {gameObject.name}.", this);
                }
            }
        }
    }

    /// <summary>
    /// Applies the configured music and ambience settings via the AudioManager, using track names.
    /// </summary>
    private void ApplyAudioSettings()
    {
        if (AudioManager.Instance == null) return;

        // --- Apply Music Setting ---
        if (changeMusic)
        {
            if (stopMusicInstead)
            {
                Debug.Log($"SimpleAudioTrigger '{gameObject.name}': Stopping music.", this);
                AudioManager.Instance.StopMusic();
            }
            else if (!string.IsNullOrEmpty(musicTrackName))
            {
                // Play music by name - AudioManager handles finding it, using its volume and default fade
                Debug.Log($"SimpleAudioTrigger '{gameObject.name}': Playing music '{musicTrackName}'.", this);
                AudioManager.Instance.PlayMusic(musicTrackName);
            }
            else
            {
                // changeMusic is true, stopMusicInstead is false, but no name provided
                 Debug.LogWarning($"SimpleAudioTrigger '{gameObject.name}': Change Music is enabled, but no Music Track Name specified and Stop Music Instead is false. No music action taken.", this);
            }
        }

        // --- Apply Ambience Setting ---
        if (changeAmbience)
        {
            if (stopAmbienceInstead)
            {
                Debug.Log($"SimpleAudioTrigger '{gameObject.name}': Stopping ambience.", this);
                AudioManager.Instance.StopAmbience();
            }
            else if (!string.IsNullOrEmpty(ambienceTrackName))
            {
                 // Play ambience by name
                 Debug.Log($"SimpleAudioTrigger '{gameObject.name}': Playing ambience '{ambienceTrackName}'.", this);
                 AudioManager.Instance.PlayAmbience(ambienceTrackName);
            }
             else
            {
                 Debug.LogWarning($"SimpleAudioTrigger '{gameObject.name}': Change Ambience is enabled, but no Ambience Track Name specified and Stop Ambience Instead is false. No ambience action taken.", this);
            }
        }
    }

    // Removed ValidateTrackIndex function

    // --- Gizmos ---
    void OnDrawGizmos()
    {
        if (showTriggerBounds && triggerCollider != null)
        {
            // Gizmo drawing code remains the same as before
            Gizmos.color = new Color(boundsColor.r, boundsColor.g, boundsColor.b, 0.3f);
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
    private void DrawWireCapsule(Vector3 center, float radius, float height, int direction) {
        Vector3 pointOffset = Vector3.zero; height = Mathf.Max(radius * 2, height);
        switch (direction) { case 0: pointOffset = Vector3.right * (height * 0.5f - radius); break; case 1: pointOffset = Vector3.up * (height * 0.5f - radius); break; case 2: pointOffset = Vector3.forward * (height * 0.5f - radius); break; }
        Vector3 p1 = center + pointOffset; Vector3 p2 = center - pointOffset;
        Gizmos.DrawWireSphere(p1, radius); Gizmos.DrawWireSphere(p2, radius);
        Vector3 up = direction == 1 ? Vector3.forward : Vector3.up; Vector3 right = direction == 0 ? Vector3.up : Vector3.right; if (direction == 2) right = Vector3.right;
        Gizmos.DrawLine(p1 + right * radius, p2 + right * radius); Gizmos.DrawLine(p1 - right * radius, p2 - right * radius); Gizmos.DrawLine(p1 + up * radius, p2 + up * radius); Gizmos.DrawLine(p1 - up * radius, p2 - up * radius);
    }
}