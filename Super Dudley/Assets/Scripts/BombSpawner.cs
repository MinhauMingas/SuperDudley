using UnityEngine;
using System.Collections;

public class BombSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float bombLifetime = 10f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private Vector3 maxRotationAngles = new Vector3(15f, 360f, 15f); // Added for adjustable rotation limits

    private bool isSpawning = false;
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (autoStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            spawnRoutine = StartCoroutine(SpawnBombs());
        }
    }

    public void StopSpawning()
    {
        if (isSpawning && spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            isSpawning = false;
        }
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.1f, newInterval);
        
        if (isSpawning)
        {
            StopSpawning();
            StartSpawning();
        }
    }

    private IEnumerator SpawnBombs()
    {
        while (true)
        {
            SpawnBomb();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnBomb()
    {
        if (bombPrefab != null)
        {
            // Generate random rotation for all three axes
            Vector3 randomRotation = new Vector3(
                Random.Range(-maxRotationAngles.x, maxRotationAngles.x),
                Random.Range(0f, maxRotationAngles.y),
                Random.Range(-maxRotationAngles.z, maxRotationAngles.z)
            );
            
            Quaternion spawnRotation = Quaternion.Euler(randomRotation);
            GameObject bomb = Instantiate(bombPrefab, transform.position, spawnRotation);
            Destroy(bomb, bombLifetime);
        }
    }
}