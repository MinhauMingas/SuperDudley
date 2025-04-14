using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using System.Collections;

public class CircleTransition : MonoBehaviour
{
    [Header("Video Player Objects")]
    public VideoPlayer startVideoPlayer;
    public VideoPlayer endVideoPlayer;

    [Header("Raw Image Objects")]
    public RawImage startRawImage;
    public RawImage endRawImage;

    [Header("Audio Clips")]
    public AudioClip startTransitionSound;
    public AudioClip endTransitionSound;

    [Header("Transition Timing")]
    public float startVideoDuration = 1.7f;
    public float endVideoDuration = 1.25f;

    private AudioSource audioSource;
    private bool isTransitioning;
    private Canvas transitionEffectCanvas;
    private string targetSceneNameToLoad = null; // Holds the specific scene target

    void Awake()
    {
        var existingTransitions = FindObjectsByType<CircleTransition>(FindObjectsSortMode.None);
        if (existingTransitions.Length > 1)
        {
            Debug.LogWarning("CircleTransition: Duplicate instance detected. Destroying self.", gameObject);
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        InitializeReferences();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void InitializeReferences()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        transitionEffectCanvas = GetComponentInChildren<Canvas>(true);
        if (transitionEffectCanvas == null)
        {
            Debug.LogError("CircleTransition: TransitionEffectCanvas not found as a child! Script will be disabled.", gameObject);
            enabled = false; // Disable script if canvas not found
            return;
        }

        // Attempt to find components - Consider assigning these via Inspector for reliability
        startVideoPlayer = transitionEffectCanvas.transform.Find("Transition Start - VideoPlayer")?.GetComponent<VideoPlayer>();
        endVideoPlayer = transitionEffectCanvas.transform.Find("Transition End - VideoPlayer")?.GetComponent<VideoPlayer>();
        startRawImage = transitionEffectCanvas.transform.Find("Transition Start - RawImage")?.GetComponent<RawImage>();
        endRawImage = transitionEffectCanvas.transform.Find("Transition End - RawImage")?.GetComponent<RawImage>();

        if (startVideoPlayer == null || endVideoPlayer == null || startRawImage == null || endRawImage == null)
        {
             Debug.LogError("CircleTransition: One or more components (VideoPlayer/RawImage) not found under the canvas! Please check names or assign via Inspector.", gameObject);
             // Script continues but transitions might fail partially.
        }

        // Setup even if some components are missing, but check within SetupVideoPlayer
        SetupVideoPlayer(startVideoPlayer, startRawImage);
        SetupVideoPlayer(endVideoPlayer, endRawImage);

        // Initial state - Make sure the canvas itself is active, but children are not
        transitionEffectCanvas.gameObject.SetActive(true);
        if (startVideoPlayer) startVideoPlayer.gameObject.SetActive(false);
        if (startRawImage) startRawImage.gameObject.SetActive(false);
        if (endVideoPlayer) endVideoPlayer.gameObject.SetActive(false);
        if (endRawImage) endRawImage.gameObject.SetActive(false);
    }

    void SetupVideoPlayer(VideoPlayer player, RawImage rawImage)
    {
        if (player == null)
        {
            // No need to log here, InitializeReferences already warned if null
            return;
        }

        player.playOnAwake = false;
        player.Stop();
        player.isLooping = false;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.prepareCompleted -= OnVideoPrepared; // Remove first to prevent duplicates
        player.prepareCompleted += OnVideoPrepared;


        if (rawImage != null) // Only configure RawImage if it exists
        {
            if (player.renderMode == VideoRenderMode.RenderTexture)
            {
                if (player.targetTexture != null)
                {
                    rawImage.texture = player.targetTexture;
                    rawImage.color = Color.white; // Ensure visible
                }
                else
                {
                    Debug.LogWarning($"VideoPlayer {player.name} is set to RenderTexture mode but has no target texture assigned. RawImage will be hidden.", player);
                    rawImage.gameObject.SetActive(false); // Hide if no texture
                }
            }
            else
            {
                // Hide RawImage if the VideoPlayer isn't rendering to a texture
                // Debug.Log($"VideoPlayer {player.name} is not using RenderTexture mode. Hiding RawImage.", player);
                rawImage.gameObject.SetActive(false);
            }
        } else {
             // No need to log here, InitializeReferences already warned if null
        }
    }


    void OnVideoPrepared(VideoPlayer source)
    {
        // Debug.Log($"{source.name} Prepared and ready to play.");
        // This callback confirms the video is ready internally.
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"CircleTransition - Scene loaded: {scene.name} (Mode: {mode})");

        // Explicitly hide end transition elements from the *previous* transition,
        // just in case they were somehow left active.
        if (endVideoPlayer != null && endVideoPlayer.gameObject.activeSelf)
        {
            Debug.LogWarning("CircleTransition: End VideoPlayer was still active on scene load. Stopping and hiding.", endVideoPlayer);
            endVideoPlayer.Stop();
            endVideoPlayer.gameObject.SetActive(false);
        }
        if (endRawImage != null && endRawImage.gameObject.activeSelf)
        {
            Debug.LogWarning("CircleTransition: End RawImage was still active on scene load. Hiding.", endRawImage);
            endRawImage.gameObject.SetActive(false);
        }

        // Reset state flag after potential cleanup
        isTransitioning = false;
        Debug.Log("CircleTransition: Ready for next transition.");
    }

    // Public method to start transition to a SPECIFIC scene
    public void StartTransitionToScene(string sceneName, Action onTransitionComplete = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("CircleTransition: Transition already in progress. Request ignored.", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(sceneName))
        {
             Debug.LogError("CircleTransition: Scene name provided to StartTransitionToScene is null or empty.", gameObject);
             return;
        }
        if (SceneManager.GetActiveScene().name == sceneName)
        {
             Debug.LogWarning($"CircleTransition: Already in scene '{sceneName}'. Transition request ignored.", gameObject);
             return;
        }
        Debug.Log($"CircleTransition: Received request to transition to scene '{sceneName}'");
        targetSceneNameToLoad = sceneName; // Store the target scene
        StartCoroutine(TransitionSequence(onTransitionComplete));
    }

    // Public method to start transition to the NEXT scene in build settings
    public void LoadNextSceneWithTransition(Action onTransitionComplete = null)
    {
        if (isTransitioning)
        {
             Debug.LogWarning("CircleTransition: Transition already in progress. Request ignored.", gameObject);
             return;
        }

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("CircleTransition: No next scene in build settings. Transition request ignored.", gameObject);
            return;
        }

        Debug.Log($"CircleTransition: Received request to transition to next scene (Index: {nextSceneIndex}).");
        targetSceneNameToLoad = null; // Ensure no specific target is set
        StartCoroutine(TransitionSequence(onTransitionComplete));
    }


    // Kept for potential backward compatibility if needed
     public void StartTransition(Action onTransitionComplete = null)
     {
         LoadNextSceneWithTransition(onTransitionComplete); // Forward to the explicit next scene logic
     }


    IEnumerator TransitionSequence(Action onTransitionComplete = null)
    {
        // Double-check state flag at the very start
        if (isTransitioning)
        {
             Debug.LogWarning("CircleTransition: TransitionSequence started while already transitioning. Exiting.", gameObject);
             yield break;
        }
        isTransitioning = true;
        Debug.Log("CircleTransition: TransitionSequence started.");

        // --- Start Transition ---
        if (startTransitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startTransitionSound);
        }

        bool startTransitionPlayed = false; // Flag to track if start transition was attempted
        if (startVideoPlayer != null) // Check player exists first
        {
            // Activate the player object
            startVideoPlayer.gameObject.SetActive(true);

            // Handle RawImage setup only if it exists and player uses RenderTexture
            if (startRawImage != null && startVideoPlayer.renderMode == VideoRenderMode.RenderTexture)
            {
                if (startVideoPlayer.targetTexture != null) {
                    startRawImage.texture = startVideoPlayer.targetTexture;
                    startRawImage.color = Color.white; // Ensure it's visible when activated
                    // Don't activate the RawImage GameObject yet
                } else {
                    Debug.LogWarning($"CircleTransition: Start VideoPlayer has no target texture for RawImage.", startVideoPlayer);
                    startRawImage.gameObject.SetActive(false); // Ensure it's off if no texture
                }
            }
            else if (startRawImage != null) // If RawImage exists but player doesn't use RT
            {
                 startRawImage.gameObject.SetActive(false); // Ensure it's off
            }

            // Prepare the video
            startVideoPlayer.Prepare();
            Debug.Log("CircleTransition: Preparing Start Video...");
            yield return new WaitUntil(() => startVideoPlayer.isPrepared);
            Debug.Log("CircleTransition: Start Video Prepared.");

            // Activate RawImage *after* Prepare, only if needed
            if (startRawImage != null && startVideoPlayer.renderMode == VideoRenderMode.RenderTexture && startVideoPlayer.targetTexture != null)
            {
                startRawImage.gameObject.SetActive(true); // Activate the image NOW
            }

            // Play the video
            Debug.Log("CircleTransition: Playing Start Video...");
            startVideoPlayer.Play();
            startTransitionPlayed = true; // Mark that we started the video
        }
        else
        {
            Debug.LogWarning("CircleTransition: Start VideoPlayer is missing. Skipping start transition video.", gameObject);
             // We still need to wait for the duration if startRawImage might be used alone (unlikely setup)
             // or just to maintain timing. If no visual start is possible, consider shortening this wait.
        }

        // Wait for the duration of the start video segment
        yield return new WaitForSeconds(startVideoDuration);
        Debug.Log("CircleTransition: Start transition video duration elapsed. Scene load starts now.");

        // --- Scene Loading ---
        AsyncOperation asyncLoad = null;
        bool sceneLoadInitiated = false;
        string sceneToLoadDebugName = "N/A";
        int sceneIndexToLoad = -1;

        if (!string.IsNullOrEmpty(targetSceneNameToLoad))
        {
            // Validate scene existence before attempting load
            sceneIndexToLoad = SceneUtility.GetBuildIndexByScenePath(targetSceneNameToLoad); // More robust check
            if (sceneIndexToLoad >= 0) // Scene exists in build settings
            {
                 sceneToLoadDebugName = targetSceneNameToLoad;
                 Debug.Log($"CircleTransition: Loading target scene: {sceneToLoadDebugName} (Index: {sceneIndexToLoad})");
                 asyncLoad = SceneManager.LoadSceneAsync(sceneIndexToLoad, LoadSceneMode.Single); // Use index for LoadSceneAsync
                 sceneLoadInitiated = true;
            } else if (Application.CanStreamedLevelBeLoaded(targetSceneNameToLoad)) {
                 // Fallback for potentially valid scene path not found by GetBuildIndexByScenePath (less common)
                 sceneToLoadDebugName = targetSceneNameToLoad;
                 Debug.LogWarning($"CircleTransition: GetBuildIndexByScenePath failed for '{targetSceneNameToLoad}', but CanStreamedLevelBeLoaded is true. Attempting load by name/path.", gameObject);
                 asyncLoad = SceneManager.LoadSceneAsync(targetSceneNameToLoad, LoadSceneMode.Single);
                 sceneLoadInitiated = true;
            } else {
                 Debug.LogError($"CircleTransition: Scene '{targetSceneNameToLoad}' cannot be loaded. Check it exists and is added to Build Settings.", gameObject);
            }
            // Clear target name after deciding to load or erroring
            targetSceneNameToLoad = null;
        }
        else // Load next scene
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            sceneIndexToLoad = currentSceneIndex + 1;

            if (sceneIndexToLoad < SceneManager.sceneCountInBuildSettings)
            {
                // Get scene path for better logging
                string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndexToLoad);
                sceneToLoadDebugName = System.IO.Path.GetFileNameWithoutExtension(scenePath); // Extract scene name
                Debug.Log($"CircleTransition: Loading next scene: {sceneToLoadDebugName} (Index: {sceneIndexToLoad})");
                asyncLoad = SceneManager.LoadSceneAsync(sceneIndexToLoad, LoadSceneMode.Single);
                sceneLoadInitiated = true;
            }
            else
            {
                Debug.LogWarning("CircleTransition: No next scene in build settings.", gameObject);
            }
        }


        if (!sceneLoadInitiated || asyncLoad == null)
        {
            Debug.LogError("CircleTransition: Scene load operation could not be initiated. Aborting transition.", gameObject);
             // Clean up start transition visuals immediately if load fails
             if (startVideoPlayer != null && startVideoPlayer.gameObject.activeSelf) {
                  startVideoPlayer.Stop();
                  startVideoPlayer.gameObject.SetActive(false);
             }
             if (startRawImage != null && startRawImage.gameObject.activeSelf) {
                  startRawImage.gameObject.SetActive(false);
             }
            isTransitioning = false; // Reset flag on failure
            onTransitionComplete?.Invoke(); // Signal completion (or failure)
            yield break; // Exit coroutine
        }

        // --- Wait for Load, Keeping Start Transition Visible ---
        asyncLoad.allowSceneActivation = false;
        Debug.Log("CircleTransition: Waiting for scene load (allowSceneActivation=false)...");
        while (asyncLoad.progress < 0.9f) // Wait until scene is almost ready
        {
             // You could update a progress bar here using asyncLoad.progress
            yield return null;
        }
        Debug.Log("CircleTransition: Scene loaded to 90%. Ready for activation.");
        // ** Start transition video/image is still visible here **

        // --- Activate Scene ---
        Debug.Log("CircleTransition: Activating scene...");
        asyncLoad.allowSceneActivation = true;
        // Wait until the scene is fully loaded and activated
        yield return new WaitUntil(() => asyncLoad.isDone);
        // SceneManager.sceneLoaded callback will fire around here
        Debug.Log($"CircleTransition: Scene '{sceneToLoadDebugName}' activated.");


        // --- End Transition --- (Plays *after* new scene is active)
        Debug.Log("CircleTransition: Preparing end transition video/sound...");
        if (endTransitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(endTransitionSound);
        }

        bool endTransitionWillPlay = false; // Flag to track if end transition is valid
        if (endVideoPlayer != null) // Check player exists
        {
            endVideoPlayer.gameObject.SetActive(true);

            // Handle RawImage setup only if it exists and player uses RenderTexture
            if (endRawImage != null && endVideoPlayer.renderMode == VideoRenderMode.RenderTexture)
            {
                if (endVideoPlayer.targetTexture != null) {
                    endRawImage.texture = endVideoPlayer.targetTexture;
                    endRawImage.color = Color.white;
                    // Don't activate GameObject yet
                } else {
                     Debug.LogWarning($"CircleTransition: End VideoPlayer has no target texture for RawImage.", endVideoPlayer);
                     endRawImage.gameObject.SetActive(false); // Ensure it's off
                }
            } else if (endRawImage != null) {
                 endRawImage.gameObject.SetActive(false); // Ensure it's off
            }

            // Prepare the end video
            endVideoPlayer.Prepare();
            Debug.Log("CircleTransition: Preparing End Video...");
            yield return new WaitUntil(() => endVideoPlayer.isPrepared);
            Debug.Log("CircleTransition: End Video Prepared.");

            // Activate End RawImage *after* prepare, only if needed
            if (endRawImage != null && endVideoPlayer.renderMode == VideoRenderMode.RenderTexture && endVideoPlayer.targetTexture != null)
            {
                 endRawImage.gameObject.SetActive(true); // Make end image visible NOW
            }

            endTransitionWillPlay = true; // Mark that we are ready to play the end video
        }
        else
        {
            Debug.LogWarning("CircleTransition: End VideoPlayer is missing. Skipping end transition video.", gameObject);
        }

        // --- Hide Start Transition NOW, just before playing End ---
        if (startTransitionPlayed) // Only hide if it was shown in the first place
        {
            Debug.Log("CircleTransition: Hiding start transition elements.");
            if (startVideoPlayer != null && startVideoPlayer.gameObject.activeSelf) {
                 startVideoPlayer.Stop(); // Stop playback if it was running
                 startVideoPlayer.gameObject.SetActive(false);
            }
            if (startRawImage != null && startRawImage.gameObject.activeSelf) {
                 // No need to stop RawImage, just hide
                 startRawImage.gameObject.SetActive(false);
            }
        }
        // --- End of Hiding Start Transition ---

        // Now play the end video if it's ready
        if (endTransitionWillPlay)
        {
            Debug.Log("CircleTransition: Playing End Video...");
            endVideoPlayer.Play(); // Play the end video
            yield return new WaitForSeconds(endVideoDuration); // Wait for its duration
            Debug.Log("CircleTransition: End transition video duration elapsed.");
        }
        else
        {
             // If no end video, maybe wait a very short fixed time or proceed immediately?
             // This prevents an abrupt jump if the end transition fails.
             // yield return new WaitForSeconds(0.1f);
             Debug.Log("CircleTransition: Skipping end transition duration wait.");
        }


        // --- Clean Up End Transition ---
        Debug.Log("CircleTransition: Cleaning up end transition elements.");
        if (endVideoPlayer != null && endVideoPlayer.gameObject.activeSelf)
        {
            endVideoPlayer.Stop(); // Ensure stopped
            endVideoPlayer.gameObject.SetActive(false);
        }
        if (endRawImage != null && endRawImage.gameObject.activeSelf)
        {
            endRawImage.gameObject.SetActive(false);
        }

        // --- Final State Reset ---
        isTransitioning = false;
        Debug.Log("CircleTransition: TransitionSequence finished successfully.");
        onTransitionComplete?.Invoke(); // Signal completion
    }


    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks and errors
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (startVideoPlayer != null) {
             startVideoPlayer.prepareCompleted -= OnVideoPrepared;
             // Optional: Explicitly release resources if needed, though Unity often handles this.
             // startVideoPlayer.targetTexture?.Release();
        }
        if (endVideoPlayer != null) {
             endVideoPlayer.prepareCompleted -= OnVideoPrepared;
             // endVideoPlayer.targetTexture?.Release();
        }

        Debug.Log("CircleTransition: Destroyed and cleaned up listeners.", gameObject);
    }
}