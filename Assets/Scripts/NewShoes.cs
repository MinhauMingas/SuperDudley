using UnityEngine;

public class CollisionSoundAndDestroy : MonoBehaviour
{
    public AudioClip collisionSound;
    private AudioSource audioSource;
    private bool played = false; // Add a boolean to track if the sound has played

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.clip = collisionSound;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !played) // Check if played is false
        {
            played = true; // Set played to true

            if (collisionSound != null)
            {
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("Collision sound is not assigned!");
            }

            if (audioSource.clip != null)
            {
                Destroy(gameObject, audioSource.clip.length);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !played) // Check if played is false
        {
            played = true; // Set played to true

             if (collisionSound != null)
            {
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("Collision sound is not assigned!");
            }

            if (audioSource.clip != null)
            {
                Destroy(gameObject, audioSource.clip.length);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}