using UnityEngine;

public class SpawnProjectile : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float spawnInterval = 1.5f;
    public bool destroyAfterLeaving = false;
    public bool destroyAfterSpawning = true;
    public int maxSpawns = 0;
    [Range(0f, 90f)] public float launchAngle = 45f;
    public Vector3 initialHorizontalDirection = new Vector3(0, 1, 0); // Y-axis forward

    [Header("Movement Phases")]
    public float initialUpwardDuration = 0.4f;
    public float homingDuration = 1.2f;

    [Header("Movement Stats")]
    public float initialUpwardSpeed = 11f;
    public float homingSpeed = 14f;
    public float turnSpeed = 300f;

    [Header("Vertical Aim")]
    public float verticalAimOffset = 1.65f;

    [Header("Random Pathing")]
    public float randomPathStrength = 0.1f; // Strength of random deviations
    public float randomPathFrequency = 0.1f; // Frequency of path changes

    [Header("Homing Stop")]
    public float homingStopDistance = 4f; // Adjust this value in the Inspector

    [Header("Particle Effect (Scene Object)")]
    public GameObject sceneParticleEffect; // Drag your existing scene particle effect GameObject here

    private PlayerSpawn playerSpawnScript;
    private int currentSpawnCount = 0;
    private bool isPlayerOnTrigger = false;
    private GameObject currentPlayer;
    private bool isRespawning = false;
    private GameObject playerSpawnObject;

    public bool IsPlayerInTrigger => isPlayerOnTrigger;

    void Start()
    {
        playerSpawnObject = GameObject.Find("PlayerSpawn");
        if (playerSpawnObject != null)
        {
            playerSpawnScript = playerSpawnObject.GetComponent<PlayerSpawn>();
        }
        else
        {
            Debug.LogError("PlayerSpawn GameObject not found!");
        }

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
            Debug.LogError("Scene Particle Effect GameObject not assigned in the Inspector!");
        }
    }

    void Update()
    {
        if (playerSpawnScript != null)
        {
            if (currentPlayer != playerSpawnScript.currentPlayer && playerSpawnScript.currentPlayer != null)
            {
                isRespawning = true;
                StopSpawning();
                currentPlayer = playerSpawnScript.currentPlayer;
                isPlayerOnTrigger = false;
            }
            else
            {
                currentPlayer = playerSpawnScript.currentPlayer;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnTrigger = true;
            if (!isRespawning)
            {
                // Ensure any previous invocations are stopped before starting a new one
                CancelInvoke("SpawnProjectileRepeating");
                InvokeRepeating("SpawnProjectileRepeating", 0f, spawnInterval);
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
        if (!isPlayerOnTrigger || isRespawning) return;

        Quaternion horizontalRotation = Quaternion.LookRotation(initialHorizontalDirection);
        Quaternion launchRotation = horizontalRotation * Quaternion.Euler(-launchAngle, 0f, 0f);

        GameObject projectileInstance = Instantiate(projectilePrefab, projectileSpawnPoint.position, launchRotation);

        // Get the MissileController component and initialize it with the spawner's properties
        MissileController missileController = projectileInstance.GetComponent<MissileController>();
        if (missileController != null)
        {
            missileController.Initialize(
                initialUpwardDuration,
                homingDuration,
                initialUpwardSpeed,
                homingSpeed,
                turnSpeed,
                verticalAimOffset,
                randomPathStrength,
                randomPathFrequency,
                homingStopDistance
            );
        }
        else
        {
            Debug.LogError("MissileController script not found on the spawned projectile!", projectileInstance);
        }

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
        CancelInvoke("DisableSceneParticleEffect"); // Clear any pending disable calls
        isRespawning = false;
    }

    void DisableSceneParticleEffect()
    {
        if (sceneParticleEffect != null)
        {
            sceneParticleEffect.SetActive(false);
        }
    }
}