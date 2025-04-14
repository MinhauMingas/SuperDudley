using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip menuMusic;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;

    void Awake()
    {
        // Prevent duplicate music players (using the non-obsolete method)
        if (FindObjectsByType<MenuMusicPlayer>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // Set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = menuMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = volume;

        // Load saved volume settings
        LoadVolumeSettings();

        // Start playing the music if it's not already playing
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Start()
    {
        //ensure the music is playing at the start of the scene.
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
        // Subscribe to scene change events
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnSceneChanged(Scene previousScene, Scene newScene)
    {
        audioSource.Stop(); // Stop the music before any scene change.
    }

    // Public method to adjust volume from other scripts
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
        PlayerPrefs.SetFloat("MenuMusicVolume", volume);
    }

    public void LoadVolumeSettings()
    {
        if (PlayerPrefs.HasKey("MenuMusicVolume"))
        {
            volume = PlayerPrefs.GetFloat("MenuMusicVolume");
            audioSource.volume = volume;
        }
    }

    public float CurrentVolume => volume;

    void OnDestroy()
    {
        // Clean up event subscription
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    // Call this to restart music if needed
    public void RestartMusic()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}