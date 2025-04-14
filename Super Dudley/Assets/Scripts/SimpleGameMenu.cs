using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Require one AudioSource component - this will be used for SFX
[RequireComponent(typeof(AudioSource))]
public class SimpleGameMenu : MonoBehaviour
{
    // --- Music Settings ---
    [Header("Music Configuration")]
    [Tooltip("The AudioSource component dedicated to playing background music. ASSIGN THIS MANUALLY.")]
    [SerializeField] private AudioSource musicAudioSource; // ASSIGN IN INSPECTOR
    [Tooltip("The music clip to play in the menu.")]
    [SerializeField] private AudioClip menuMusicClip;
    [Tooltip("Initial and saved volume for the menu music.")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;

    // --- Button SFX Settings ---
    [Header("UI SFX Configuration")]
    [Tooltip("Sound effect for the start game button click. Played by the primary AudioSource on this GameObject.")]
    [SerializeField] private AudioClip startGameSfx;
    [Tooltip("Volume for the button click sound effect.")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;
    // SFX Source is found automatically using GetComponent<AudioSource>()
    private AudioSource sfxAudioSource; // Automatically assigned in Awake

    // --- Music Fade Settings ---
    [Header("Music Fadeout")]
    [Tooltip("How long the music fade-out should take before transition starts.")]
    [SerializeField] private float musicFadeDuration = 1.5f;

    // --- State ---
    private bool isStartingGame = false;
    private Coroutine musicFadeCoroutine = null;

    // --- Initialization ---
    void Awake()
    {
        // --- Duplicate Check ---
        var existingMenus = FindObjectsByType<SimpleGameMenu>(FindObjectsSortMode.None);
        if (existingMenus.Length > 1) {
             bool isDuplicate = false;
             foreach(var menu in existingMenus){
                 if(menu.gameObject != this.gameObject){ isDuplicate = true; break; }
             }
             if(isDuplicate){
                 Debug.LogWarning($"Duplicate SimpleGameMenu found on '{gameObject.name}'. Destroying self.", gameObject);
                 Destroy(gameObject);
                 return;
             }
        }

        // --- Assign Audio Sources ---
        // Get the required AudioSource component on this GameObject for SFX
        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) {
             // This should technically not happen due to [RequireComponent]
             Debug.LogError($"SimpleGameMenu: Critical error - Could not find the required AudioSource for SFX on '{gameObject.name}'.", gameObject);
             // Attempt to add one dynamically as a last resort, though setup is likely incorrect
             sfxAudioSource = gameObject.AddComponent<AudioSource>();
             Debug.LogWarning($"SimpleGameMenu: Dynamically added AudioSource for SFX. Please check GameObject setup.", gameObject);
        } else {
            Debug.Log($"SimpleGameMenu: SFX AudioSource automatically assigned from '{gameObject.name}'.", gameObject);
        }


        // Check if the Music source was assigned in the Inspector
        if (musicAudioSource == null) {
            Debug.LogError($"SimpleGameMenu: 'Music Audio Source' has NOT been assigned in the Inspector on '{gameObject.name}'. Music will not play!", gameObject);
        } else {
             Debug.Log($"SimpleGameMenu: Music AudioSource assigned from Inspector: '{musicAudioSource.gameObject.name}'.", gameObject);
        }


        // --- Configure Audio Sources ---
        ConfigureSfxSource();        // Configure the automatically found SFX source
        LoadMusicVolumeSettings();   // Load volume settings
        ConfigureMusicSource();      // Configure the manually assigned Music source
        PlayMenuMusic();             // Start playing music (if configured)
    }


    /// <summary>
    /// Configures the SFX AudioSource (found via GetComponent).
    /// </summary>
    void ConfigureSfxSource()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            // Initial volume setting (PlayButtonClickSound will use sfxVolume directly)
            sfxAudioSource.volume = sfxVolume;
        }
    }

    /// <summary>
    /// Configures the Music AudioSource (assigned via Inspector).
    /// </summary>
    void ConfigureMusicSource()
    {
         // Check if assigned AND has a clip
         if (musicAudioSource != null && menuMusicClip != null)
         {
              musicAudioSource.playOnAwake = false;
              musicAudioSource.loop = true;
              musicAudioSource.clip = menuMusicClip;
              musicAudioSource.volume = musicVolume; // Apply loaded/default volume
         }
         else if (musicAudioSource != null && menuMusicClip == null)
         {
              Debug.LogWarning($"SimpleGameMenu: Music Audio Source is assigned, but 'Menu Music Clip' is missing in the Inspector.", this);
         }
         // Error for null musicAudioSource is handled in Awake
    }

    /// <summary>
    /// Starts playing the menu music using the assigned music AudioSource.
    /// </summary>
    void PlayMenuMusic()
    {
         if (musicAudioSource != null && musicAudioSource.clip != null)
         {
              if (!musicAudioSource.isPlaying)
              {
                   musicAudioSource.Play();
                   Debug.Log("SimpleGameMenu: Playing menu music.");
              }
         }
         // Warnings/Errors handled by ConfigureMusicSource and Awake
    }


    // --- Public Methods ---

    public void StartGame() // Unchanged logic
    {
        if (!isStartingGame) {
            StartCoroutine(StartGameSequence());
        } else {
            Debug.LogWarning("SimpleGameMenu: StartGame called while already starting.", this);
        }
    }

    private IEnumerator StartGameSequence() // Unchanged logic
    {
        isStartingGame = true;

        PlayButtonClickSound(); // Uses the automatically found sfxAudioSource

        // Fade music using the manually assigned musicAudioSource
        if (musicAudioSource != null && musicAudioSource.isPlaying) {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            Debug.Log("SimpleGameMenu: Starting music fade out...");
            musicFadeCoroutine = StartCoroutine(FadeOutAudio(musicAudioSource, musicFadeDuration));
            yield return musicFadeCoroutine;
            musicFadeCoroutine = null;
            Debug.Log("SimpleGameMenu: Music fade out complete.");
        } else {
             if (musicAudioSource == null) Debug.LogWarning("SimpleGameMenu: Music Audio Source not assigned, cannot fade.", this);
             else if (!musicAudioSource.isPlaying) Debug.Log("SimpleGameMenu: Music not playing, skipping fade.", this);
        }

        // Transition logic (unchanged)
        CircleTransition transitionController = Object.FindFirstObjectByType<CircleTransition>();
        if (transitionController != null) {
            Debug.Log("SimpleGameMenu: Triggering external CircleTransition...");
            transitionController.LoadNextSceneWithTransition();
        } else {
            Debug.LogError("SimpleGameMenu: CircleTransition instance not found!", this);
            isStartingGame = false;
        }
    }


    /// <summary>
    /// Plays the button click SFX using the primary AudioSource on this GameObject.
    /// </summary>
    private void PlayButtonClickSound()
    {
        // Use the sfxAudioSource found automatically via GetComponent
        if (sfxAudioSource != null && startGameSfx != null)
        {
            // PlayOneShot allows playing sounds without interrupting the source's main clip (if any)
            // It also lets us specify volume per-shot.
            sfxAudioSource.PlayOneShot(startGameSfx, sfxVolume);
             // Debug.Log("SimpleGameMenu: Played button click SFX."); // Optional log
        }
        else if (sfxAudioSource == null)
        {
             // Error should have been logged in Awake
             Debug.LogError("SimpleGameMenu: Cannot play SFX because the required AudioSource component is missing!", this);
        }
        else // sfxAudioSource != null but startGameSfx == null
        {
             Debug.LogWarning("SimpleGameMenu: 'Start Game Sfx' clip is not assigned in the Inspector. Cannot play sound.", this);
        }
    }

    /// <summary>
    /// Coroutine to fade audio (Unchanged).
    /// </summary>
    private IEnumerator FadeOutAudio(AudioSource audioSrc, float duration) // Unchanged
    {
        if (audioSrc == null) {
             Debug.LogError("FadeOutAudio called with a null AudioSource.", this);
             yield break;
        }
        float startVol = audioSrc.volume;
        float timer = 0f;
        while (timer < duration) {
            if (audioSrc == null) yield break;
            audioSrc.volume = Mathf.Lerp(startVol, 0f, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        if (audioSrc != null) {
            audioSrc.volume = 0f;
            audioSrc.Stop();
        }
    }

    // --- Quit Game (Unchanged) ---
    public void QuitGame() // Unchanged
    {
        Debug.Log("SimpleGameMenu: Quit Game requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Music Volume Control Methods (Unchanged) ---
    public void SetMusicVolume(float newVolume) // Unchanged
    {
        musicVolume = Mathf.Clamp01(newVolume);
        if (musicAudioSource != null && musicFadeCoroutine == null) {
            musicAudioSource.volume = musicVolume;
        }
        PlayerPrefs.SetFloat("MenuMusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    private void LoadMusicVolumeSettings() // Unchanged
    {
        if (PlayerPrefs.HasKey("MenuMusicVolume")) {
            musicVolume = PlayerPrefs.GetFloat("MenuMusicVolume");
        }
        // Volume applied in ConfigureMusicSource
    }

    public float CurrentMusicVolumeSetting => musicVolume; // Unchanged
}