// CoinCollector.cs (Modified)
using UnityEngine;
using System.Collections; // Required for Coroutines

public class CoinCollector : MonoBehaviour
{
    // Remove coinCount and coinText reference from here
    // private int coinCount = 0;
    // [SerializeField] private TextMeshProUGUI coinText;

    [Header("Sound Settings")]
    public AudioClip collectionSoundClip;
    [Range(0f, 1f)] public float collectionSoundVolume = 1f;
    private AudioSource audioSource;

    [Header("Particle Effect")]
    public ParticleSystem coinParticleEffectPrefab; // Link the Particle System PREFAB here

    private void Awake()
    {
        // AudioSource setup remains the same
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.clip = collectionSoundClip;
        audioSource.volume = collectionSoundVolume;
    }

    // Remove the Start method if it only called UpdateCoinText

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Play the particle effect (same as before)
            if (coinParticleEffectPrefab != null)
            {
                ParticleSystem effectInstance = Instantiate(coinParticleEffectPrefab, transform.position, Quaternion.identity);
                effectInstance.Play();
                // Make sure the effect cleans itself up or use Destroy
                 Destroy(effectInstance.gameObject, effectInstance.main.duration + effectInstance.main.startLifetime.constantMax); // Destroy particle system object after it finishes
            }
            else
            {
                 Debug.LogWarning("Coin collected by Player but no Particle System Prefab assigned.");
            }

            // --- Core Change ---
            // Tell the ScoreManager to add a coin
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddCoin(1); // Add 1 coin
            }
            else
            {
                Debug.LogError("ScoreManager Instance not found!");
            }
            // --- End Change ---


            // Disable the coin's collider and renderer immediately so it can't be collected again
            // while the sound plays.
            GetComponent<Collider>().enabled = false;
            Renderer rend = GetComponent<Renderer>();
            if(rend != null) rend.enabled = false;
            // Also disable any child renderers if your coin is complex
            foreach(Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }


             // Play the sound effect and schedule destruction (same as before, but destruction is safer now)
            if (collectionSoundClip != null && audioSource != null)
            {
                 audioSource.Play();
                 // Destroy the *entire* coin GameObject after the sound finishes
                 Destroy(gameObject, collectionSoundClip.length);
            }
            else
            {
                // If no sound, destroy immediately
                 Debug.LogWarning("Collection sound clip is not assigned or AudioSource missing!");
                 Destroy(gameObject);
            }

            // Remove coinCount++ and UpdateCoinText() call from here
            // coinCount++;
            // UpdateCoinText();
        }
    }

    // Remove the DestroyAfterSound Coroutine - simple timed Destroy is sufficient now
    // Remove UpdateCoinText method
    // Remove SetCollectionSoundVolume unless needed for other reasons
}