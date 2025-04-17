using UnityEngine;
using System.Collections;

public class SplashOnCollision : MonoBehaviour
{
    [Header("Particle Effect")]
    [SerializeField]
    [Tooltip("The particle system prefab to instantiate on collision.")]
    private GameObject splashParticlePrefab;

    [Header("Timing")]
    [SerializeField]
    [Tooltip("Delay in seconds before the splash effect appears after collision.")]
    [Min(0f)]
    private float instantiationDelay = 0f;

    // --- OnTriggerEnter ---
    // This should ONLY calculate the point and START the coroutine.
    // It should NOT instantiate the particle directly.
    void OnTriggerEnter(Collider other)
    {
        // Check if the prefab is assigned
        if (splashParticlePrefab != null)
        {
            // Calculate where the effect should appear
            Vector3 contactPoint = GetContactPoint(other);

            // ***CRITICAL: Only start the coroutine here. NO Instantiate() call should be in OnTriggerEnter.***
            StartCoroutine(SpawnSplashWithDelay(contactPoint));
             //Debug.Log($"OnTriggerEnter: Starting coroutine for {other.name} at {Time.time}"); // Optional Debug Log
        }
        else
        {
            //Debug.LogWarning("Splash particle prefab is not assigned in the inspector.", this);
        }
    }

    // --- Coroutine ---
    // This handles the delay AND the instantiation.
    private IEnumerator SpawnSplashWithDelay(Vector3 position)
    {
         //Debug.Log($"Coroutine Started: Waiting for {instantiationDelay} seconds. Time: {Time.time}"); // Optional Debug Log

        // Wait for the specified delay duration
        if (instantiationDelay > 0f)
        {
            yield return new WaitForSeconds(instantiationDelay);
        }

        //Debug.Log($"Coroutine Resumed: Instantiating splash. Time: {Time.time}"); // Optional Debug Log

        // Check prefab again (safety)
        if (splashParticlePrefab == null)
        {
             Debug.LogWarning("Splash particle prefab became null before instantiation could occur after delay.", this);
             yield break; // Stop the coroutine
        }

        // ***CRITICAL: Instantiate the particle effect HERE, after the delay.***
        GameObject splash = Instantiate(splashParticlePrefab, position, Quaternion.identity);

        // --- Rest of the particle cleanup logic ---
        ParticleSystem particleSystem = splash.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            float lifetime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
            if (!particleSystem.main.loop)
            {
                 Destroy(splash, lifetime);
            }
            else
            {
                 //Debug.LogWarning("Splash particle system is set to loop. Destroying after 5 seconds.", splash);
                 Destroy(splash, 5f); // Example fixed time for looping effects
            }
        }
        else
        {
            //Debug.LogWarning("Instantiated splash object does not have a ParticleSystem component.", splash);
            Destroy(splash, 2f);
        }
    }

    // Helper function to get an approximate contact point
    private Vector3 GetContactPoint(Collider other)
    {
        return other.ClosestPoint(transform.position);
    }

    // --- Notes on Triggers vs. Collisions ---
    // (Keep the previous note about OnTriggerEnter vs OnCollisionEnter)
}