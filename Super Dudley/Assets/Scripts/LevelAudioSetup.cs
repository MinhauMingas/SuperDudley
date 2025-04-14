using UnityEngine;

/// <summary>
/// Configures and plays the initial music and/or ambience for the level/scene
/// by selecting tracks by name from the AudioManager. Uses default fade durations.
/// Attach this to an object loaded with your scene (e.g., a LevelManager).
/// </summary>
public class LevelAudioSetup : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("If true, this script will attempt to set audio settings on Start.")]
    public bool configureAudioOnStart = true;

    [Header("Music Settings")]
    [Tooltip("Check this box to set the music track when the scene starts.")]
    public bool setMusic = true;
    [Tooltip("The exact name of the music track in AudioManager's list to play (case-sensitive).")]
    public string musicTrackName = "Default Level Music"; // Example name
    // Removed musicTrackIndex, musicFadeDuration, overrideMusicVolume, targetMusicVolume


    [Header("Ambience Settings")]
    [Tooltip("Check this box to set the ambience track when the scene starts.")]
    public bool setAmbience = true;
    [Tooltip("The exact name of the ambience track in AudioManager's list to play (case-sensitive).")]
    public string ambienceTrackName = "Forest Ambience"; // Example name
    // Removed ambienceTrackIndex, ambienceFadeDuration, overrideAmbienceVolume, targetAmbienceVolume


    void Start()
    {
        if (!configureAudioOnStart)
        {
            Debug.Log($"LevelAudioSetup on {gameObject.name}: Audio configuration on start is disabled.", this);
            return; // Do nothing if configuration is disabled
        }

        // Ensure AudioManager instance exists
        if (AudioManager.Instance == null)
        {
            Debug.LogError($"LevelAudioSetup on {gameObject.name}: AudioManager instance not found! Cannot configure audio. Make sure AudioManager is loaded before this script runs.", this);
            enabled = false; // Disable this script
            return;
        }

        // --- Configure Music ---
        if (setMusic)
        {
            if (!string.IsNullOrEmpty(musicTrackName))
            {
                // Find the track index by name
                int musicIndex = AudioManager.Instance.FindMusicTrackIndex(musicTrackName);

                if (musicIndex != -1)
                {
                    Debug.Log($"LevelAudioSetup: Requesting AudioManager to play music track '{musicTrackName}' (Index: {musicIndex}) using default fade.", this);
                    // Play the music using the found index and AudioManager's default fade duration
                    AudioManager.Instance.PlayMusic(musicIndex);
                    // Volume is now handled entirely by the AudioTrack definition in AudioManager
                }
                else
                {
                    // Error message is already logged by FindMusicTrackIndex if not found
                    Debug.LogError($"LevelAudioSetup on {gameObject.name}: Cannot play music - track name '{musicTrackName}' not found or has null clip in AudioManager.", this);
                }
            }
            else
            {
                 Debug.LogWarning($"LevelAudioSetup on {gameObject.name}: Music track name is empty. Skipping music setup.", this);
            }
        }
        else
        {
            Debug.Log($"LevelAudioSetup on {gameObject.name}: Music setup is disabled via inspector setting.", this);
            // Optional: Stop music if transitioning from a scene with music?
            // if (AudioManager.Instance.IsMusicPlaying()) // You'd need to add an IsMusicPlaying method to AudioManager
            // {
            //     AudioManager.Instance.StopMusic();
            // }
        }

        // --- Configure Ambience ---
        if (setAmbience)
        {
             if (!string.IsNullOrEmpty(ambienceTrackName))
            {
                // Find the track index by name
                int ambienceIndex = AudioManager.Instance.FindAmbienceTrackIndex(ambienceTrackName);

                 if (ambienceIndex != -1)
                 {
                    Debug.Log($"LevelAudioSetup: Requesting AudioManager to play ambience track '{ambienceTrackName}' (Index: {ambienceIndex}) using default fade.", this);
                     // Play the ambience using the found index and AudioManager's default fade duration
                    AudioManager.Instance.PlayAmbience(ambienceIndex);
                     // Volume is now handled entirely by the AudioTrack definition in AudioManager
                 }
                 else
                 {
                    Debug.LogError($"LevelAudioSetup on {gameObject.name}: Cannot play ambience - track name '{ambienceTrackName}' not found or has null clip in AudioManager.", this);
                 }
            }
             else
             {
                  Debug.LogWarning($"LevelAudioSetup on {gameObject.name}: Ambience track name is empty. Skipping ambience setup.", this);
             }
        }
        else
        {
             Debug.Log($"LevelAudioSetup on {gameObject.name}: Ambience setup is disabled via inspector setting.", this);
             // Optional: Stop ambience if needed
             // if (AudioManager.Instance.IsAmbiencePlaying())
             // {
             //     AudioManager.Instance.StopAmbience();
             // }
        }
    }
}