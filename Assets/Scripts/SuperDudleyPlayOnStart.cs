using UnityEngine;

public class SuperDudleyPlayOnStart : MonoBehaviour
{
    [SerializeField] private AudioClip startClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool playOnAwake = true;
    
    private AudioSource audioSource;
    private bool hasPlayed = false;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = startClip;
        audioSource.volume = volume;
    }

    void Start()
    {
        if (playOnAwake && !hasPlayed)
        {
            PlayStartSound();
        }
    }

    public void PlayStartSound()
    {
        if (startClip != null && !hasPlayed)
        {
            audioSource.PlayOneShot(startClip, volume);
            hasPlayed = true;
            
            // Optional: Destroy after playing if this is a temporary object
            // Destroy(gameObject, startClip.length);
        }
    }
}