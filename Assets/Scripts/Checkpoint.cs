// Checkpoint.cs
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject flagRed;
    public GameObject flagYellow;
    public ParticleSystem activationEffect;

    [Header("Audio")]
    public AudioClip activationSound;
    // Add a volume control variable, visible in the Inspector
    [Range(0f, 1f)] // Adds a slider in the Inspector from 0 to 1
    public float soundVolume = 1.0f; // Default volume is 1 (full volume)
    private AudioSource audioSource;

    [Header("Settings")]
    public string playerTag = "Player";

    private bool isActivated = false;

    void Awake()
    {
        // Visuals null checks
        if (flagRed == null)
            Debug.LogWarning($"[Checkpoint] Flag Red is not assigned on {gameObject.name}!");
        if (flagYellow == null)
            Debug.LogWarning($"[Checkpoint] Flag Yellow is not assigned on {gameObject.name}!");
        if (activationEffect == null)
            Debug.LogWarning($"[Checkpoint] Activation Effect is not assigned on {gameObject.name}!");

        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure the AudioSource
        audioSource.playOnAwake = false;
        audioSource.clip = activationSound;
        // Set the volume based on the public variable
        audioSource.volume = soundVolume;
    }

    void Start()
    {
        // Initial flag setup
        if (flagYellow != null) flagYellow.SetActive(true);
        if (flagRed != null) flagRed.SetActive(false);

        // Ensure particle effect isn't playing initially
        if (activationEffect != null && activationEffect.isPlaying)
        {
            activationEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isActivated)
        {
            ActivateCheckpoint();
        }
    }

    public void ActivateCheckpoint()
    {
        if (isActivated) return; // Prevent reactivation

        isActivated = true;

        // Update visuals
        if (flagYellow != null) flagYellow.SetActive(false);
        if (flagRed != null) flagRed.SetActive(true);

        // Play activation effect
        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        // Play activation sound (volume was set in Awake)
        if (audioSource != null && activationSound != null)
        {
            // Optional: You could also check audioSource.isPlaying if you want
            // to prevent the sound from restarting if somehow activated multiple times quickly.
            audioSource.Play();
        }

        // Set the spawn point
        PlayerSpawn.SetCheckpoint(transform);
        Debug.Log($"Checkpoint '{gameObject.name}' activated!"); // Added name for clarity
    }
}