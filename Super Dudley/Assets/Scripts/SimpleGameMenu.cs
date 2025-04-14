using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SimpleGameMenu : MonoBehaviour
{
    public AudioClip[] soundEffects; // Array to hold multiple sound effects
    public float soundVolume = 1.0f;

    private bool canPlaySound = false;
    private bool isSoundPlaying = false;
    private float soundDelay = 4.0f;

    void Start()
    {
        StartCoroutine(EnableSoundAfterDelay());
    }

    IEnumerator EnableSoundAfterDelay()
    {
        yield return new WaitForSeconds(soundDelay);
        canPlaySound = true;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("level1");
    }

    public void PlaySound()
    {
        if (canPlaySound && !isSoundPlaying)
        {
            if (soundEffects.Length > 0)
            {
                AudioClip randomSound = soundEffects[Random.Range(0, soundEffects.Length)];
                AudioSource audioSource = gameObject.AddComponent<AudioSource>(); // Add AudioSource dynamically
                audioSource.clip = randomSound;
                audioSource.volume = soundVolume;
                audioSource.Play();

                isSoundPlaying = true;
                StartCoroutine(ResetSoundAvailability(audioSource.clip.length));
            }
            else
            {
                Debug.LogWarning("No sound effects assigned to the array!");
            }
        }
    }

    IEnumerator ResetSoundAvailability(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        isSoundPlaying = false;
        Destroy(GetComponent<AudioSource>()); // Clean up the dynamically added AudioSource
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}