using UnityEngine;

public class CannonSpawner : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float spawnInterval = 1.5f;
    public bool destroyAfterLeaving = false;
    public bool destroyAfterSpawning = true;
    public int maxSpawns = 0;
    [Range(0f, 90f)] public float launchAngle = 45f;
    public Vector3 launchDirection = Vector3.forward; // Customizable launch direction (local space)
    public bool launchUpwards = false; // New option to control launch direction

    [Header("Random Launch Force")]
    public bool useRandomLaunchForce = false;
    public float minLaunchForce = 10f;
    public float maxLaunchForce = 20f;
    public float baseLaunchForce = 15f; // Used if random is disabled

    [Header("Particle Effect (Scene Object)")]
    public GameObject sceneParticleEffect;

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

    private int currentSpawnCount = 0;
    private bool isPlayerOnTrigger = false;
    private bool isSpawning = false;
    private AudioSource audioSource; // For spawn sounds

    void Start()
    {
        // Ensure the scene particle effect is initially stopped and potentially hidden
        if (sceneParticleEffect != null)
        {
            ParticleSystem ps = sceneParticleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            sceneParticleEffect.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Scene Particle Effect GameObject not assigned in the Inspector!");
        }

        // Ensure minLaunchForce is not greater than maxLaunchForce
        if (minLaunchForce > maxLaunchForce)
        {
            Debug.LogError("Min Launch Force cannot be greater than Max Launch Force!", this);
            enabled = false; // Disable the script to prevent further issues
        }

        // Setup AudioSource for spawn sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        ConfigureSpawnAudio(audioSource);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnTrigger = true;
            if (!isSpawning)
            {
                CancelInvoke("SpawnProjectileRepeating");
                InvokeRepeating("SpawnProjectileRepeating", 0f, spawnInterval);
                isSpawning = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnTrigger = false;
            StopSpawning();
            if (destroyAfterLeaving)
            {
                Destroy(gameObject);
            }
        }
    }

    void SpawnProjectileRepeating()
    {
        if (!isPlayerOnTrigger) return;

        // Calculate the launch rotation based on the direction
        Quaternion launchRotation = Quaternion.LookRotation(launchDirection);

        // Apply the vertical angle
        if (launchUpwards)
        {
            launchRotation *= Quaternion.Euler(launchAngle, 0f, 0f); // Upward angle
        }
        else
        {
            launchRotation *= Quaternion.Euler(-launchAngle, 0f, 0f); // Downward angle
        }

        GameObject projectileInstance = Instantiate(projectilePrefab, projectileSpawnPoint.position, launchRotation);

        // Get the CannonProjectile component and apply the launch force
        CannonProjectile projectile = projectileInstance.GetComponent<CannonProjectile>();
        if (projectile != null)
        {
            float currentLaunchForce = baseLaunchForce;
            if (useRandomLaunchForce)
            {
                currentLaunchForce = Random.Range(minLaunchForce, maxLaunchForce);
                Debug.Log($"Launching projectile with direction: {projectileInstance.transform.forward}, force: {currentLaunchForce} (Random)");
            }
            else
            {
                Debug.Log($"Launching projectile with direction: {projectileInstance.transform.forward}, force: {currentLaunchForce} (Base)");
            }
            projectile.Launch(projectileInstance.transform.forward, currentLaunchForce);
        }
        else
        {
            Debug.LogError("CannonProjectile script not found on the spawned projectile!", projectileInstance);
        }

        // Play spawn sound
        PlaySpawnSound();

        // Ensure the particle effect restarts and plays regardless of its current state
        if (sceneParticleEffect != null)
        {
            sceneParticleEffect.transform.position = projectileSpawnPoint.position;
            ParticleSystem ps = sceneParticleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Stop and clear any existing particles
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // Immediately play the particle system again
                ps.Play();
                sceneParticleEffect.SetActive(true); // Make sure it's visible

                // Optional: Handle non-looping destruction (disable after duration)
                if (!ps.main.loop)
                {
                    CancelInvoke("DisableSceneParticleEffect"); // Cancel any previous disable calls
                    Invoke("DisableSceneParticleEffect", ps.main.duration + ps.main.startLifetime.constantMax + 0.1f);
                }
            }
            else
            {
                Debug.LogWarning("The assigned Scene Particle Effect does not have a ParticleSystem component.");
            }
        }

        currentSpawnCount++;

        if (maxSpawns > 0 && currentSpawnCount >= maxSpawns)
        {
            StopSpawning();
            if (destroyAfterSpawning)
            {
                Destroy(gameObject);
            }
        }
    }

    void StopSpawning()
    {
        CancelInvoke("SpawnProjectileRepeating");
        CancelInvoke("DisableSceneParticleEffect");
        isSpawning = false;
    }

    void DisableSceneParticleEffect()
    {
        if (sceneParticleEffect != null)
        {
            sceneParticleEffect.SetActive(false);
        }
    }

    void ConfigureSpawnAudio(AudioSource source)
    {
        source.spatialBlend = spawnSpatialBlend;
        source.minDistance = spawnMinDistance;
        source.maxDistance = spawnMaxDistance;
        source.rolloffMode = spawnRolloffMode;
        source.volume = spawnVolumeMultiplier;
    }

    void PlaySpawnSound()
    {
        if (spawnSounds != null && spawnSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, spawnSounds.Length);
            audioSource.clip = spawnSounds[randomIndex];
            audioSource.Play();
        }
    }

    private void OnValidate()
    {
        if (minLaunchForce > maxLaunchForce)
        {
            minLaunchForce = maxLaunchForce; // Correct the value in the editor
            Debug.LogError("Min Launch Force cannot be greater than Max Launch Force!", this);
        }
        if (audioSource != null && !Application.isPlaying)
        {
            ConfigureSpawnAudio(audioSource);
        }
    }
}