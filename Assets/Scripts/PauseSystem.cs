using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System

public class PauseSystem : MonoBehaviour
{
    public GameObject pauseMenu;
    public AudioClip pauseSound;
    public AudioClip resumeSound;

    private AudioSource audioSource;
    private bool isPaused = false; // Instance variable for internal logic/events if needed

    // --- ADD THIS STATIC PROPERTY ---
    // Allows other scripts to easily check the pause state globally
    public static bool IsGamePaused { get; private set; }
    // --------------------------------

    // Use Awake for initialization that should happen before Start
    void Awake()
    {
        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Ensure the static state matches the actual time scale on scene load/start
        // Handles cases where the game might start paused (e.g., Time.timeScale was left at 0 in editor)
        if (Time.timeScale == 0f)
        {
            isPaused = true;
            IsGamePaused = true;
            // Ensure menu is visible if starting paused
             if (pauseMenu != null) pauseMenu.SetActive(true);
        }
        else
        {
            isPaused = false;
            IsGamePaused = false;
             if (pauseMenu != null) pauseMenu.SetActive(false);
        }
    }


    // Start is fine for hiding the menu initially if not starting paused
    // void Start()
    // {
    //     if (pauseMenu != null && !isPaused) // Check initial state
    //     {
    //         pauseMenu.SetActive(false);
    //     }
    // }


    void Update()
    {
        // Check for the pause input
        if (Input.GetKeyDown(KeyCode.Escape) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // Optional: Prevent toggling pause extremely rapidly
        // if(Time.unscaledTime < lastToggleTime + 0.2f) return;
        // lastToggleTime = Time.unscaledTime;

        if (isPaused) // Use the instance variable for the toggle logic
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;
        IsGamePaused = true; // << SET STATIC PROPERTY

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }

        if (pauseSound != null)
        {
            // Play sound even when time scale is 0 - Use PlayOneShot or ensure AudioSource ignores time scale
            // By default, PlayOneShot often works okay, but setting ignoreListenerPause might be safer
            audioSource.ignoreListenerPause = true; // Ensure it plays if AudioListener pauses
            audioSource.PlayOneShot(pauseSound);
        }

        // Optional: Could add cursor lock/visibility changes here
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        IsGamePaused = false; // << SET STATIC PROPERTY

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        if (resumeSound != null)
        {
             audioSource.ignoreListenerPause = true; // Ensure it plays if AudioListener pauses
            audioSource.PlayOneShot(resumeSound);
        }

         // Optional: Restore cursor lock/visibility changes here
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    // --- Button Methods ---
    public void ResumeButton()
    {
        ResumeGame();
    }

    public void QuitButton()
    {
        // Important: Reset time scale before quitting if paused
        if (isPaused)
        {
            Time.timeScale = 1f;
        }

        Debug.Log("Quitting Game..."); // For editor testing
        Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}