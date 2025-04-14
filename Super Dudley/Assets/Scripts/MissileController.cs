using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    private Transform target;

    [Header("Explosion Settings")]
    public GameObject explosionPrefab; // Optional: For visual effects
    public float explosionLifetime = 2f; // Optional: For the prefab
    [Tooltip("The GameObject containing the explosion visuals and/or collider that becomes active upon explosion.")]
    public GameObject explosionAreaObject;
    [Tooltip("How long the explosion area object lasts before being destroyed (in seconds).")]
    public float explosionAreaDuration = 1f; // Default to 1 second

    [Header("Audio - General")]
    [Range(0f, 1f)] public float generalVolume = 1f;

    [Header("Audio - Spawn")]
    public AudioClip[] spawnSounds; // Array to hold multiple spawn sounds
    [Range(0f, 1f)] public float spawnVolumeMultiplier = 1f;
    [Tooltip("Sets how much the spawn sound is affected by distance (0 = 2D, 1 = 3D).")]
    [Range(0f, 1f)] public float spawnSpatialBlend = 1f;
    [Tooltip("The minimum distance for the spawn sound.")]
    public float spawnMinDistance = 1f;
    [Tooltip("The maximum distance for the spawn sound.")]
    public float spawnMaxDistance = 10f;
    [Tooltip("The rolloff mode for the spawn sound.")]
    public AudioRolloffMode spawnRolloffMode = AudioRolloffMode.Linear;

    [Header("Audio - Explosion")]
    public AudioClip[] explosionSounds; // Array to hold multiple explosion sounds
    [Range(0f, 1f)] public float explosionVolumeMultiplier = 1f;
    [Tooltip("Sets how much the explosion sound is affected by distance (0 = 2D, 1 = 3D).")]
    [Range(0f, 1f)] public float explosionSpatialBlend = 1f;
    [Tooltip("The minimum distance for the explosion sound.")]
    public float explosionMinDistance = 1f;
    [Tooltip("The maximum distance for the explosion sound.")]
    public float explosionMaxDistance = 10f;
    [Tooltip("The rolloff mode for the explosion sound.")]
    public AudioRolloffMode explosionRolloffMode = AudioRolloffMode.Linear;

    [Header("Audio - Homing")]
    public AudioClip[] homingSounds; // Array for multiple homing sounds
    [Range(0f, 1f)] public float homingVolumeMultiplier = 1f;
    [Tooltip("Sets how much the homing sound is affected by distance (0 = 2D, 1 = 3D).")]
    [Range(0f, 1f)] public float homingSpatialBlend = 1f;
    [Tooltip("The minimum distance for the homing sound.")]
    public float homingMinDistance = 1f;
    [Tooltip("The maximum distance for the homing sound.")]
    public float homingMaxDistance = 10f;
    [Tooltip("The rolloff mode for the homing sound.")]
    public AudioRolloffMode homingRolloffMode = AudioRolloffMode.Linear;

    private AudioSource audioSource; // General audio source (can be used for other missile sounds if needed)
    private AudioSource homingAudioSource; // Separate AudioSource for homing sound
    private AudioSource spawnAudioSource; // Separate AudioSource for spawn sound

    private Rigidbody rb;
    private bool isHomingActive = false;
    private bool isInitialUpwardPhase = true;
    private float timeAlive = 0f;
    private Vector3 worldUp = Vector3.up;
    private Vector3 randomPathOffset;
    private float nextPathChangeTime;
    private bool hasExploded = false; // Flag to prevent multiple explosions
    private float homingStopDistanceSqr; // Squared distance for performance
    private bool homingSoundStarted = false; // Flag to track if the homing sound has started

    // Cached values from the spawner
    private float initialUpwardDuration;
    private float homingDuration;
    private float initialUpwardSpeed;
    private float homingSpeed;
    private float turnSpeed;
    private float verticalAimOffset;
    private float randomPathStrength;
    private float randomPathFrequency;
    private float homingStopDistance;

    public void Initialize(
        float initialUpwardDuration,
        float homingDuration,
        float initialUpwardSpeed,
        float homingSpeed,
        float turnSpeed,
        float verticalAimOffset,
        float randomPathStrength,
        float randomPathFrequency,
        float homingStopDistance
    )
    {
        this.initialUpwardDuration = initialUpwardDuration;
        this.homingDuration = homingDuration;
        this.initialUpwardSpeed = initialUpwardSpeed;
        this.homingSpeed = homingSpeed;
        this.turnSpeed = turnSpeed;
        this.verticalAimOffset = verticalAimOffset;
        this.randomPathStrength = randomPathStrength;
        this.randomPathFrequency = randomPathFrequency;
        this.homingStopDistance = homingStopDistance;
        this.homingStopDistanceSqr = homingStopDistance * homingStopDistance;

        StartCoroutine(ActivateHomingAfterDelay(this.initialUpwardDuration));
        nextPathChangeTime = Time.time + 1f / this.randomPathFrequency;
        randomPathOffset = Random.insideUnitSphere * this.randomPathStrength;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody missing!", this);
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = generalVolume;
        // General audio source can have default settings if needed

        // Create a separate AudioSource for the homing sound
        GameObject homingAudioObject = new GameObject("HomingSound");
        homingAudioObject.transform.SetParent(transform); // Make it follow the missile
        homingAudioObject.transform.localPosition = Vector3.zero; // Position it at the missile's origin
        homingAudioSource = homingAudioObject.AddComponent<AudioSource>();
        homingAudioSource.playOnAwake = false;
        homingAudioSource.loop = true; // Homing sound should loop
        homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
        ConfigureHomingAudio(homingAudioSource);

        // Create a separate AudioSource for the spawn sound
        GameObject spawnAudioObject = new GameObject("SpawnSound");
        spawnAudioObject.transform.SetParent(transform); // Make it follow the missile
        spawnAudioObject.transform.localPosition = Vector3.zero; // Position it at the missile's origin
        spawnAudioSource = spawnAudioObject.AddComponent<AudioSource>();
        spawnAudioSource.playOnAwake = false;
        spawnAudioSource.loop = false; // Spawn sound plays once
        spawnAudioSource.volume = generalVolume * spawnVolumeMultiplier;
        ConfigureSpawnAudio(spawnAudioSource);

        target = GameObject.FindGameObjectWithTag(playerTag)?.transform;
        if (!target) Debug.LogWarning("Player target not found", this);

        // Set initial linearVelocity based on the initial forward direction
        rb.linearVelocity = transform.forward * initialUpwardSpeed;

        Debug.Log($"Missile Start Rotation: {transform.rotation.eulerAngles}, Initial Velocity: {rb.linearVelocity}"); // DEBUG

        if (explosionAreaObject == null)
        {
            Debug.LogError("Explosion Area Object not assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        // Ensure the explosion area object is initially inactive
        explosionAreaObject.SetActive(false);

        hasExploded = false; // Initialize the flag

        // Play spawn sound
        PlaySpawnSound();
    }

    void ConfigureExplosionAudio(AudioSource source)
    {
        source.spatialBlend = explosionSpatialBlend;
        source.minDistance = explosionMinDistance;
        source.maxDistance = explosionMaxDistance;
        source.rolloffMode = explosionRolloffMode;
    }

    void ConfigureHomingAudio(AudioSource source)
    {
        source.spatialBlend = homingSpatialBlend;
        source.minDistance = homingMinDistance;
        source.maxDistance = homingMaxDistance;
        source.rolloffMode = homingRolloffMode;
    }

    void ConfigureSpawnAudio(AudioSource source)
    {
        source.spatialBlend = spawnSpatialBlend;
        source.minDistance = spawnMinDistance;
        source.maxDistance = spawnMaxDistance;
        source.rolloffMode = spawnRolloffMode;
    }

    void PlaySpawnSound()
    {
        if (spawnSounds != null && spawnSounds.Length > 0 && spawnAudioSource != null)
        {
            int randomIndex = Random.Range(0, spawnSounds.Length);
            spawnAudioSource.clip = spawnSounds[randomIndex];
            spawnAudioSource.volume = generalVolume * spawnVolumeMultiplier;
            spawnAudioSource.Play();
        }
    }

    IEnumerator ActivateHomingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isHomingActive = true;
        StartHomingSound(); // Call the function to start the sound
        isInitialUpwardPhase = false;
    }

    void StartHomingSound()
    {
        if (homingSounds != null && homingSounds.Length > 0 && homingAudioSource != null && !homingAudioSource.isPlaying)
        {
            int randomIndex = Random.Range(0, homingSounds.Length);
            homingAudioSource.clip = homingSounds[randomIndex];
            homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
            homingAudioSource.Play();
            homingSoundStarted = true; // Set the flag
        }
        else if (homingSounds != null && homingSounds.Length > 0 && homingAudioSource != null && !homingSoundStarted)
        {
            // If homing started without this function being called (e.g., initial state)
            int randomIndex = Random.Range(0, homingSounds.Length);
            homingAudioSource.clip = homingSounds[randomIndex];
            homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
            homingAudioSource.Play();
            homingSoundStarted = true;
        }
    }

    void StopHomingSound()
    {
        // We no longer stop the homing sound here, as it should play until destruction.
        // homingSoundStarted = false; // No need to reset the flag here
    }

    void FixedUpdate()
    {
        timeAlive += Time.fixedDeltaTime;

        if (isInitialUpwardPhase)
        {
            rb.linearVelocity = transform.forward * initialUpwardSpeed; // Move in the initial forward direction
            return;
        }

        if (isHomingActive && target != null)
        {
            if (timeAlive > initialUpwardDuration + homingDuration)
            {
                isHomingActive = false;
                // We no longer stop the homing sound here
                return;
            }

            // Check if close enough to stop homing
            if ((target.position - transform.position).sqrMagnitude <= homingStopDistanceSqr)
            {
                isHomingActive = false;
                // We no longer stop the homing sound here
                return;
            }

            Vector3 aimPoint = target.position + (worldUp * verticalAimOffset);
            Vector3 direction = (aimPoint - transform.position).normalized;

            // Apply random path
            if (Time.time >= nextPathChangeTime)
            {
                randomPathOffset = Random.insideUnitSphere * randomPathStrength;
                nextPathChangeTime = Time.time + 1f / randomPathFrequency;
            }

            direction += randomPathOffset;
            direction.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            ));

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, transform.forward * homingSpeed, 5f * Time.fixedDeltaTime);
        }
        else
        {
            // We no longer stop the homing sound here
        }

        // Ensure the homing sound keeps playing if homing has started
        if (homingSounds != null && homingSounds.Length > 0 && homingAudioSource != null && !homingAudioSource.isPlaying && homingSoundStarted)
        {
            int randomIndex = Random.Range(0, homingSounds.Length);
            homingAudioSource.clip = homingSounds[randomIndex];
            homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
            homingAudioSource.Play();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple explosions
        if (hasExploded)
        {
            return;
        }

        // Check if the colliding object is another missile (optional)
        if (collision.gameObject.GetComponent<MissileController>() != null)
        {
            return; // Do nothing if colliding with another missile
        }

        // Explode
        Explode(collision.contacts[0].point);
    }

    void Explode(Vector3 explosionPosition)
    {
        if (hasExploded) return; // Double check
        hasExploded = true; // Set the flag to prevent further explosions

        // IMPORTANT: Make sure explosion area is detached from the missile before activating
        if (explosionAreaObject != null)
        {
            // Detach the explosion area from the missile
            explosionAreaObject.transform.parent = null;

            // Move it to the explosion position
            explosionAreaObject.transform.position = explosionPosition;

            // Activate it
            explosionAreaObject.SetActive(true);

            // *** THE MINIMAL FIX IS HERE ***
            // Schedule the destruction of the explosion area object after its duration.
            // This replaces the StartCoroutine call.
            Destroy(explosionAreaObject, explosionAreaDuration);
            // *** END OF FIX ***
        }

        // Instantiate particle effect (optional)
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
            Destroy(explosion, explosionLifetime);
        }

        // Play random explosion sound
        if (explosionSounds != null && explosionSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, explosionSounds.Length);

            // Create a new GameObject to hold the AudioSource for the sound
            GameObject soundObject = new GameObject("ExplosionSound");
            soundObject.transform.position = explosionPosition;

            // Copy the AudioSource settings
            AudioSource tempSource = soundObject.AddComponent<AudioSource>();
            tempSource.clip = explosionSounds[randomIndex];
            tempSource.volume = generalVolume * explosionVolumeMultiplier; // Apply explosion volume multiplier
            ConfigureExplosionAudio(tempSource); // Configure explosion audio settings

            // Play the sound on the new AudioSource
            tempSource.Play();

            // Destroy the temporary GameObject after the sound finishes
            Destroy(soundObject, explosionSounds[randomIndex].length);
        }

        // Destroy the missile object immediately (as per your original code)
        Destroy(gameObject);
    }

    // // Coroutine to clean up the explosion area after some time - REMOVED
    // IEnumerator CleanupExplosionArea(GameObject explosionArea, float delay)
    // {
    //     // This coroutine is no longer needed
    // }

    // Public methods to control spatial audio properties (optional, for runtime adjustments)
    public void SetExplosionSpatialBlend(float blend)
    {
        explosionSpatialBlend = Mathf.Clamp01(blend);
    }

    public void SetExplosionMinDistance(float distance)
    {
        explosionMinDistance = Mathf.Max(0f, distance);
    }

    public void SetExplosionMaxDistance(float distance)
    {
        explosionMaxDistance = Mathf.Max(explosionMinDistance, distance);
    }

    public void SetExplosionRolloffMode(AudioRolloffMode mode)
    {
        explosionRolloffMode = mode;
    }

    public void SetExplosionVolumeMultiplier(float multiplier)
    {
        explosionVolumeMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetHomingSpatialBlend(float blend)
    {
        homingSpatialBlend = Mathf.Clamp01(blend);
        if (homingAudioSource != null) ConfigureHomingAudio(homingAudioSource);
    }

    public void SetHomingMinDistance(float distance)
    {
        homingMinDistance = Mathf.Max(0f, distance);
        if (homingAudioSource != null) ConfigureHomingAudio(homingAudioSource);
    }

    public void SetHomingMaxDistance(float distance)
    {
        homingMaxDistance = Mathf.Max(homingMinDistance, distance);
        if (homingAudioSource != null) ConfigureHomingAudio(homingAudioSource);
    }

    public void SetHomingRolloffMode(AudioRolloffMode mode)
    {
        homingRolloffMode = mode;
        if (homingAudioSource != null) ConfigureHomingAudio(homingAudioSource);
    }

    public void SetHomingVolumeMultiplier(float multiplier)
    {
        homingVolumeMultiplier = Mathf.Max(0f, multiplier);
        if (homingAudioSource != null && homingAudioSource.isPlaying)
        {
            homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
        }
    }

    public void SetSpawnSpatialBlend(float blend)
    {
        spawnSpatialBlend = Mathf.Clamp01(blend);
        if (spawnAudioSource != null) ConfigureSpawnAudio(spawnAudioSource);
    }

    public void SetSpawnMinDistance(float distance)
    {
        spawnMinDistance = Mathf.Max(0f, distance);
        if (spawnAudioSource != null) ConfigureSpawnAudio(spawnAudioSource);
    }

    public void SetSpawnMaxDistance(float distance)
    {
        spawnMaxDistance = Mathf.Max(spawnMinDistance, distance);
        if (spawnAudioSource != null) ConfigureSpawnAudio(spawnAudioSource);
    }

    public void SetSpawnRolloffMode(AudioRolloffMode mode)
    {
        spawnRolloffMode = mode;
        if (spawnAudioSource != null) ConfigureSpawnAudio(spawnAudioSource);
    }

    public void SetSpawnVolumeMultiplier(float multiplier)
    {
        spawnVolumeMultiplier = Mathf.Max(0f, multiplier);
        if (spawnAudioSource != null && spawnAudioSource.isPlaying)
        {
            spawnAudioSource.volume = generalVolume * spawnVolumeMultiplier;
        }
    }

    public void SetGeneralVolume(float volume)
    {
        generalVolume = Mathf.Clamp01(volume);
        if (audioSource != null) audioSource.volume = generalVolume;
        if (homingAudioSource != null && homingAudioSource.isPlaying)
        {
            homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
        }
        if (spawnAudioSource != null && spawnAudioSource.isPlaying)
        {
            spawnAudioSource.volume = generalVolume * spawnVolumeMultiplier;
        }
    }

    // New public methods for setting homing sounds
    public void SetHomingSound(AudioClip clip)
    {
        if (homingSounds == null) homingSounds = new AudioClip[0];
        if (homingSounds.Length > 0) homingSounds[0] = clip;
        else homingSounds = new AudioClip[] { clip };
        if (homingAudioSource != null && homingAudioSource.isPlaying)
        {
            homingAudioSource.clip = homingSounds[0];
        }
    }

    public void SetHomingSounds(AudioClip[] clips)
    {
        homingSounds = clips;
        if (homingAudioSource != null && homingAudioSource.isPlaying && homingSounds != null && homingSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, homingSounds.Length);
            homingAudioSource.clip = homingSounds[randomIndex];
        }
    }

    // New public methods for setting spawn sounds
    public void SetSpawnSound(AudioClip clip)
    {
        if (spawnSounds == null) spawnSounds = new AudioClip[0];
        if (spawnSounds.Length > 0) spawnSounds[0] = clip;
        else spawnSounds = new AudioClip[] { clip };
    }

    public void SetSpawnSounds(AudioClip[] clips)
    {
        spawnSounds = clips;
    }

    // Editor helper to ensure spatial audio properties are valid
    private void OnValidate()
    {
        explosionSpatialBlend = Mathf.Clamp01(explosionSpatialBlend);
        explosionMinDistance = Mathf.Max(0f, explosionMinDistance);
        explosionMaxDistance = Mathf.Max(explosionMinDistance, explosionMaxDistance);
        explosionVolumeMultiplier = Mathf.Max(0f, explosionVolumeMultiplier);

        homingSpatialBlend = Mathf.Clamp01(homingSpatialBlend);
        homingMinDistance = Mathf.Max(0f, homingMinDistance);
        homingMaxDistance = Mathf.Max(homingMinDistance, homingMaxDistance);
        homingVolumeMultiplier = Mathf.Max(0f, homingVolumeMultiplier);

        spawnSpatialBlend = Mathf.Clamp01(spawnSpatialBlend);
        spawnMinDistance = Mathf.Max(0f, spawnMinDistance);
        spawnMaxDistance = Mathf.Max(spawnMinDistance, spawnMaxDistance);
        spawnVolumeMultiplier = Mathf.Max(0f, spawnVolumeMultiplier);

        generalVolume = Mathf.Clamp01(generalVolume);
        if (audioSource != null && !Application.isPlaying) audioSource.volume = generalVolume;
        // Note: Child audio sources (homing/spawn) might not exist until runtime
        // or might be part of the prefab structure. Direct access in OnValidate
        // for child objects created at runtime is not reliable. Try/catch or null checks needed if accessed here.
        // if (homingAudioSource != null && !Application.isPlaying) homingAudioSource.volume = generalVolume * homingVolumeMultiplier;
        // if (spawnAudioSource != null && !Application.isPlaying) spawnAudioSource.volume = generalVolume * spawnVolumeMultiplier;
    }
}