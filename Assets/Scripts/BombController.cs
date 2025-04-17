using UnityEngine;
using System.Collections;

public class BombController : MonoBehaviour
{
    [Header("Explosion Settings")]
    public GameObject explosionPrefab; // Optional: For visual effects
    public float explosionLifetime = 2f; // Optional: For the prefab
    [Tooltip("The mesh renderer (and potentially collider) that becomes active upon explosion.")]
    public GameObject explosionMeshObject; // Assign the GameObject containing your new sphere mesh
    [Tooltip("How long the explosion mesh object lasts before being destroyed (in seconds).")]
    public float explosionAreaDuration = 1f; // Duration for the explosion area

    [Header("Colliders and Mesh")]
    [Tooltip("The initial active collider of the bomb for triggering the explosion.")]
    public Collider initialCollider;

    [Header("Audio")]
    public AudioClip[] explosionSounds; // Array to hold multiple explosion sounds
    [Range(0f, 1f)] public float soundVolume = 1f;
    [Tooltip("Sets how much the sound is affected by distance (0 = 2D, 1 = 3D).")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Tooltip("The minimum distance the listener can be from the source before volume starts to decrease.")]
    public float minDistance = 1f;
    [Tooltip("The distance at which the sound will be inaudible.")]
    public float maxDistance = 10f;
    [Tooltip("The type of volume rolloff to use over distance.")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    private AudioSource audioSource;

    private bool hasExploded = false; // Flag to prevent multiple explosions

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // Configure spatial audio settings
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = rolloffMode;

        // Error checking for the assigned components
        if (initialCollider == null)
        {
            Debug.LogError("Initial Collider not assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        // It's generally fine for the initial collider to be a trigger or non-trigger
        // depending on how you want the bomb to behave before exploding.
        // If it MUST be a non-trigger, keep the warning.
        // if (!initialCollider.enabled || initialCollider.isTrigger)
        // {
        //     Debug.LogWarning("Initial Collider should be enabled and not be a trigger.", this);
        // }

        if (explosionMeshObject == null)
        {
            Debug.LogError("Explosion Mesh Object not assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        // Ensure the explosion mesh object is initially inactive
        explosionMeshObject.SetActive(false);

        hasExploded = false; // Initialize the flag
    }

    void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple explosions
        if (hasExploded)
        {
            return;
        }

        // --- REMOVED BOMB TAG CHECK ---
        // Previously, there was a check here:
        // if (collision.gameObject.CompareTag("Bomb"))
        // {
        //     return; // Do nothing if colliding with another bomb
        // }
        // Now it will proceed to explode regardless of the tag.

        // Example using Layer (still here, commented out):
        // if (collision.gameObject.layer == LayerMask.NameToLayer("Bomb"))
        // {
        //     return;
        // }

        // Explode using the first contact point
        Explode(collision.contacts[0].point);
    }

    // Alternative: Use OnTriggerEnter if your initialCollider IS a trigger
    // void OnTriggerEnter(Collider other)
    // {
    //     if (hasExploded) return;
    //
    //     // --- REMOVED BOMB TAG CHECK (from commented section) ---
    //     // Previously: // if (other.gameObject.CompareTag("Bomb")) return;
    //
    //     // Explode at the trigger's position (or other.transform.position)
    //     Explode(transform.position); // Or choose a better point based on 'other'
    // }


    void Explode(Vector3 explosionPosition) // Receive the explosion position
    {
        if (hasExploded) return; // Double check
        hasExploded = true; // Set the flag to prevent further explosions

        // Optional: Disable the initial collider immediately
        if (initialCollider != null)
        {
            initialCollider.enabled = false;
        }

        // Activate and manage the explosion mesh object
        if (explosionMeshObject != null)
        {
            // Detach from parent
            // Ensure it doesn't get destroyed when the bomb is destroyed
            explosionMeshObject.transform.parent = null;

            // Position and activate the explosion mesh
            explosionMeshObject.transform.position = explosionPosition;
            explosionMeshObject.SetActive(true);

            // Schedule destruction
            // Destroy the explosion mesh object after the specified duration
            Destroy(explosionMeshObject, explosionAreaDuration);
        }

        // Instantiate particle effect (optional)
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
            Destroy(explosion, explosionLifetime);
        }

        // Play random explosion sound
        if (explosionSounds != null && explosionSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, explosionSounds.Length);
            AudioClip clipToPlay = explosionSounds[randomIndex];

            // Use PlayClipAtPoint for simplicity unless specific AudioSource settings are vital
            AudioSource.PlayClipAtPoint(clipToPlay, explosionPosition, soundVolume);

            // --- OR --- If you need the specific 3D settings from the original bomb:
            /*
            GameObject soundObject = new GameObject("ExplosionSound");
            soundObject.transform.position = explosionPosition;
            AudioSource tempSource = soundObject.AddComponent<AudioSource>();
            tempSource.clip = clipToPlay;
            tempSource.volume = soundVolume; // Use the configured volume
            tempSource.spatialBlend = spatialBlend; // Copy settings
            tempSource.minDistance = minDistance;
            tempSource.maxDistance = maxDistance;
            tempSource.rolloffMode = rolloffMode;
            tempSource.Play();
            Destroy(soundObject, clipToPlay.length);
            */
        }

        // Destroy the original bomb object immediately
        Destroy(gameObject);
    }

    // Public methods to control spatial audio properties (optional, for runtime adjustments)
    public void SetSpatialBlend(float blend)
    {
        spatialBlend = Mathf.Clamp01(blend);
        if (audioSource != null)
        {
            audioSource.spatialBlend = spatialBlend;
        }
    }

    public void SetMinDistance(float distance)
    {
        minDistance = Mathf.Max(0f, distance);
        if (audioSource != null)
        {
            audioSource.minDistance = minDistance;
        }
    }

    public void SetMaxDistance(float distance)
    {
        maxDistance = Mathf.Max(minDistance, distance);
        if (audioSource != null)
        {
            audioSource.maxDistance = maxDistance;
        }
    }

    public void SetRolloffMode(AudioRolloffMode mode)
    {
        rolloffMode = mode;
        if (audioSource != null)
        {
            audioSource.rolloffMode = rolloffMode;
        }
    }

    // Editor helper to ensure spatial audio properties are valid
    private void OnValidate()
    {
        // Clamp values in editor
        spatialBlend = Mathf.Clamp01(spatialBlend);
        minDistance = Mathf.Max(0f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        explosionAreaDuration = Mathf.Max(0.1f, explosionAreaDuration); // Ensure duration is positive
        explosionLifetime = Mathf.Max(0.1f, explosionLifetime); // Ensure duration is positive
    }
}