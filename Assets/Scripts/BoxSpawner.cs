using UnityEngine;
using System.Collections;

public class boxSpawner : MonoBehaviour
{
    public GameObject[] boxPrefabs; // Array to hold the two box prefabs
    public Transform spawnPoint;
    public float spawnInterval = 2f;
    public float destroyAfter = 10f;
    public float respawnOffsetXMin = -1f; // Minimum X offset
    public float respawnOffsetXMax = 1f;  // Maximum X offset
    public float respawnOffsetYMin = -0.5f; // Minimum Y offset
    public float respawnOffsetYMax = 0.5f;  // Maximum Y offset
    public float respawnOffsetZMin = -1f; // Minimum Z offset
    public float respawnOffsetZMax = 1f;  // Maximum Z offset
    [SerializeField] private Vector3 maxRotationAngles = new Vector3(15f, 360f, 15f); // Added for adjustable rotation limits

    private PlayerSpawn playerSpawnScript;
    private bool isPlayerOnTrigger = false;
    private bool isRespawning = false;
    private GameObject currentPlayer; // To track the current player
    private int currentPrefabIndex = 0; // Index to track which prefab to spawn

    void Start()
    {
        // Find PlayerSpawn script
        playerSpawnScript = Object.FindFirstObjectByType<PlayerSpawn>();
        if (playerSpawnScript == null)
        {
            Debug.LogError("PlayerSpawn script not found in the scene!");
        }

        // Ensure there are at least two box prefabs assigned
        if (boxPrefabs.Length < 2)
        {
            Debug.LogError("Please assign at least two box prefabs to the boxPrefabs array in the Inspector!");
            enabled = false; // Disable the script if not enough prefabs are assigned
        }
    }

    void Update()
    {
        // Track the current player and check for respawns
        if (playerSpawnScript != null)
        {
            if (currentPlayer != playerSpawnScript.currentPlayer && playerSpawnScript.currentPlayer != null)
            {
                isRespawning = true;
                StopSpawning(); // Stop spawning
                currentPlayer = playerSpawnScript.currentPlayer; // Update current player
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
                InvokeRepeating("SpawnBox", 0f, spawnInterval);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnTrigger = false;
            StopSpawning();
        }
    }

    void SpawnBox()
    {
        if (!isPlayerOnTrigger && !isRespawning) return;

        Vector3 actualSpawnPosition = spawnPoint.position;

        // Generate random offsets within the specified ranges.
        float randomOffsetX = Random.Range(respawnOffsetXMin, respawnOffsetXMax);
        float randomOffsetY = Random.Range(respawnOffsetYMin, respawnOffsetYMax);
        float randomOffsetZ = Random.Range(respawnOffsetZMin, respawnOffsetZMax);

        // Apply the random offsets relative to the spawnPoint.
        actualSpawnPosition.x += randomOffsetX;
        actualSpawnPosition.y += randomOffsetY;
        actualSpawnPosition.z += randomOffsetZ;

        // Generate random rotation for all three axes
        Vector3 randomRotation = new Vector3(
            Random.Range(-maxRotationAngles.x, maxRotationAngles.x),
            Random.Range(0f, maxRotationAngles.y),
            Random.Range(-maxRotationAngles.z, maxRotationAngles.z)
        );

        Quaternion spawnRotation = Quaternion.Euler(randomRotation);

        // Instantiate the current prefab
        GameObject box = Instantiate(boxPrefabs[currentPrefabIndex], actualSpawnPosition, spawnRotation);
        Destroy(box, destroyAfter);

        // Alternate to the next prefab index
        currentPrefabIndex = (currentPrefabIndex + 1) % boxPrefabs.Length;
    }

    void StopSpawning()
    {
        CancelInvoke("SpawnBox");
        isRespawning = false; // Reset the flag
        currentPrefabIndex = 0; // Reset the prefab index when spawning stops
    }
}