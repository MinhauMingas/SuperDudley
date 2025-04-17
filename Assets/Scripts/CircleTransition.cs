using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using System.Collections;

public class CircleTransition : MonoBehaviour
{
    // --- Singleton Accessor ---
    // Provides a public static way to get the single instance of CircleTransition
    public static CircleTransition Instance { get; private set; }
    // --- End Singleton ---

    [Header("References (Assign in Inspector)")]
    [Tooltip("VideoPlayer for the transition start effect.")]
    public VideoPlayer startVideoPlayer;
    [Tooltip("VideoPlayer for the transition end effect.")]
    public VideoPlayer endVideoPlayer;
    [Tooltip("RawImage to display the start transition video.")]
    public RawImage startRawImage;
    [Tooltip("RawImage to display the end transition video.")]
    public RawImage endRawImage;
    [Tooltip("Canvas holding all transition UI elements.")]
    public Canvas transitionEffectCanvas; // Assign the parent Canvas

    [Header("Audio Clips")]
    [Tooltip("Sound played when the transition starts.")]
    public AudioClip startTransitionSound;
    [Tooltip("Sound played when the new scene appears.")]
    public AudioClip endTransitionSound;

    [Header("Transition Timing")]
    [Tooltip("Duration of the start transition video/animation.")]
    public float startVideoDuration = 1.7f;
    [Tooltip("Duration of the end transition video/animation.")]
    public float endVideoDuration = 1.25f;

    private AudioSource audioSource;
    private bool isTransitioning = false; // Flag to prevent overlapping transitions
    private string targetSceneNameToLoad = null; // For loading specific scenes by name/path

    void Awake()
    {
        // --- Singleton Implementation ---
        if (Instance != null && Instance != this)
        {
            // If an instance already exists and it's not this one, destroy this duplicate.
            Debug.LogWarning($"CircleTransition: Duplicate instance detected ({gameObject.name}). Destroying self. Using existing instance: {Instance.gameObject.name}.", gameObject);
            Destroy(gameObject);
            return; // Stop execution for this duplicate instance
        }
        else if (Instance == null)
        {
            // If no instance exists, this becomes the singleton instance.
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist this GameObject across scene loads
            Debug.Log($"CircleTransition: Singleton instance established ({gameObject.name}). Marked DontDestroyOnLoad.", gameObject);

            // Initialize references ONLY for the true singleton instance the first time Awake runs.
            InitializeReferences();
        }
        // If Instance == this, Awake might have been called again (e.g., re-enabling object),
        // but we don't need to re-initialize or DontDestroyOnLoad again.

        // Ensure SceneManager.sceneLoaded is subscribed only once by the singleton instance
        // Always remove first to prevent duplicates if Awake somehow runs again on the singleton
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void InitializeReferences()
    {
        Debug.Log($"CircleTransition ({gameObject.name}): Initializing references.");

        // Ensure AudioSource exists
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // Important!
            audioSource.loop = false;
            // Consider adding spatialBlend = 0 if it should always be 2D UI sound
            audioSource.spatialBlend = 0;
        }

        // Check if essential components are assigned via Inspector (preferred)
        if (transitionEffectCanvas == null)
        {
            Debug.LogError("CircleTransition: TransitionEffectCanvas is not assigned in the Inspector! Transitions will likely fail.", gameObject);
            enabled = false; // Disable script if core component is missing
            return;
        }
         if (startVideoPlayer == null) Debug.LogError("CircleTransition: Start VideoPlayer is not assigned in the Inspector!", gameObject);
         if (endVideoPlayer == null) Debug.LogError("CircleTransition: End VideoPlayer is not assigned in the Inspector!", gameObject);
         if (startRawImage == null) Debug.LogError("CircleTransition: Start RawImage is not assigned in the Inspector!", gameObject);
         if (endRawImage == null) Debug.LogError("CircleTransition: End RawImage is not assigned in the Inspector!", gameObject);


        // Setup video players (even if null, the method handles checks)
        SetupVideoPlayer(startVideoPlayer, startRawImage);
        SetupVideoPlayer(endVideoPlayer, endRawImage);

        // Initial state: Ensure canvas is active, but visual elements are hidden
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
            // Log if the corresponding RawImage exists but player doesn't
            if(rawImage != null) Debug.LogWarning($"CircleTransition: VideoPlayer is null, but RawImage '{rawImage.name}' exists. Image will not be used.", gameObject);
            return; // Nothing to set up
        }

        player.playOnAwake = false;
        player.Stop(); // Ensure it's stopped initially
        player.isLooping = false;
        player.audioOutputMode = VideoAudioOutputMode.None; // Manage audio via AudioSource component

        // Subscribe to know when video is ready to play smoothly
        player.prepareCompleted -= OnVideoPrepared; // Remove first to prevent duplicates
        player.prepareCompleted += OnVideoPrepared;

        // Configure RawImage if it exists and the player uses a RenderTexture
        if (rawImage != null)
        {
            if (player.renderMode == VideoRenderMode.RenderTexture)
            {
                if (player.targetTexture != null)
                {
                    rawImage.texture = player.targetTexture;
                    rawImage.color = Color.white; // Ensure it's visible (alpha = 1)
                    rawImage.gameObject.SetActive(false); // Keep it inactive until needed
                }
                else
                {
                    Debug.LogWarning($"VideoPlayer {player.name} is set to RenderTexture mode but has no target texture assigned. RawImage '{rawImage.name}' will be hidden.", player);
                    rawImage.texture = null; // Clear texture reference
                    rawImage.gameObject.SetActive(false); // Ensure it's hidden
                }
            }
            else // Player doesn't use RenderTexture
            {
                // Debug.Log($"VideoPlayer {player.name} is not using RenderTexture mode. Hiding RawImage '{rawImage.name}'.", player);
                rawImage.texture = null;
                rawImage.gameObject.SetActive(false);
            }
        }
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        // This callback confirms the video is ready internally. Good for debugging.
        // Debug.Log($"{source.name} Prepared and ready to play.");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure this logic only runs on the singleton instance
        if (Instance != this) return;

        Debug.Log($"CircleTransition Instance ({gameObject.name}) - Scene loaded: {scene.name} (Mode: {mode})");

        // Force-hide end transition elements from the *previous* transition,
        // just in case they were somehow left active due to an error or interruption.
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

        // Reset the transitioning flag, ready for the next request.
        isTransitioning = false;
        Debug.Log($"CircleTransition Instance ({gameObject.name}): Ready for next transition.");
    }

    // Public method to start transition to a SPECIFIC scene by name or path
    public void StartTransitionToScene(string sceneName, Action onTransitionComplete = null)
    {
        if (!EnsureReadyForTransition()) return; // Check if already transitioning or invalid scene name

        if (string.IsNullOrEmpty(sceneName))
        {
             Debug.LogError("CircleTransition: Scene name provided to StartTransitionToScene is null or empty.", gameObject);
             return;
        }
        // Optional: Prevent transitioning to the same scene?
        // if (SceneManager.GetActiveScene().name == sceneName || SceneManager.GetActiveScene().path == sceneName)
        // {
        //      Debug.LogWarning($"CircleTransition: Already in scene '{sceneName}'. Transition request ignored.", gameObject);
        //      return;
        // }

        Debug.Log($"CircleTransition: Received request to transition to specific scene '{sceneName}'");
        targetSceneNameToLoad = sceneName; // Store the target scene
        StartCoroutine(TransitionSequence(onTransitionComplete));
    }

    // Public method to start transition to the NEXT scene in build settings
    public void LoadNextSceneWithTransition(Action onTransitionComplete = null)
    {
        if (!EnsureReadyForTransition()) return; // Check if already transitioning

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("CircleTransition: No next scene in build settings. Transition request ignored.", gameObject);
            isTransitioning = false; // Reset flag if we aborted early
            return;
        }

        Debug.Log($"CircleTransition: Received request to transition to next scene (Index: {nextSceneIndex}).");
        targetSceneNameToLoad = null; // Clear specific target name, ensuring next scene logic runs
        StartCoroutine(TransitionSequence(onTransitionComplete));
    }

    // Helper to check if a transition can start
    private bool EnsureReadyForTransition()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("CircleTransition: Transition already in progress. Request ignored.", gameObject);
            return false;
        }
         if (Instance != this) // Should not happen if singleton logic is correct, but good safeguard
         {
             Debug.LogError("CircleTransition: Attempted to start transition from a non-singleton instance! This should not happen.", gameObject);
             return false;
         }
        isTransitioning = true; // Set flag EARLY to prevent race conditions
        return true;
    }


    IEnumerator TransitionSequence(Action onTransitionComplete = null)
    {
        Debug.Log($"CircleTransition Instance ({gameObject.name}): TransitionSequence started.");

        // --- Start Transition ---
        if (startTransitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startTransitionSound);
        }

        bool startTransitionPlayed = false;
        if (startVideoPlayer != null)
        {
            startVideoPlayer.gameObject.SetActive(true); // Activate player object

            // Activate RawImage only if valid setup
             bool useStartRawImage = (startRawImage != null && startVideoPlayer.renderMode == VideoRenderMode.RenderTexture && startVideoPlayer.targetTexture != null);
             if (useStartRawImage)
             {
                // Set texture just in case it was unset
                startRawImage.texture = startVideoPlayer.targetTexture;
                startRawImage.color = Color.white;
             }
             else if (startRawImage != null) // If raw image exists but isn't used, ensure it's off
             {
                  startRawImage.gameObject.SetActive(false);
             }

            startVideoPlayer.Prepare();
            Debug.Log("CircleTransition: Preparing Start Video...");
            yield return new WaitUntil(() => startVideoPlayer.isPrepared);
            Debug.Log("CircleTransition: Start Video Prepared.");

            // Activate the RawImage *just before* playing, only if needed
            if (useStartRawImage)
            {
                startRawImage.gameObject.SetActive(true);
            }

            Debug.Log("CircleTransition: Playing Start Video...");
            startVideoPlayer.Play();
            startTransitionPlayed = true;
        }
        else
        {
            Debug.LogWarning("CircleTransition: Start VideoPlayer is missing or not assigned. Skipping start transition video.", gameObject);
            // If no video, we might still want a minimal delay or potentially show the raw image if configured differently?
            // For now, we just skip the video part but still wait the duration.
        }

        // Wait for the expected duration of the start transition
        yield return new WaitForSeconds(startVideoDuration);
        Debug.Log("CircleTransition: Start transition visual duration elapsed.");

        // --- Scene Loading ---
        AsyncOperation asyncLoad = null;
        bool sceneLoadInitiated = false;
        string sceneToLoadDebugName = "N/A";

        // Decide which scene to load (Specific Target vs Next)
        if (!string.IsNullOrEmpty(targetSceneNameToLoad))
        {
            // Try loading the specific scene stored earlier
            sceneToLoadDebugName = targetSceneNameToLoad;
             // Check if the scene exists in build settings before loading by name/path
            if (Application.CanStreamedLevelBeLoaded(targetSceneNameToLoad)) {
                 Debug.Log($"CircleTransition: Loading target scene by name/path: {sceneToLoadDebugName}");
                 asyncLoad = SceneManager.LoadSceneAsync(targetSceneNameToLoad, LoadSceneMode.Single);
                 sceneLoadInitiated = true;
            } else {
                int sceneIndex = SceneUtility.GetBuildIndexByScenePath(targetSceneNameToLoad);
                if (sceneIndex >= 0) {
                     Debug.Log($"CircleTransition: Loading target scene by index derived from path: {sceneToLoadDebugName} (Index: {sceneIndex})");
                     asyncLoad = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single);
                     sceneLoadInitiated = true;
                } else {
                    Debug.LogError($"CircleTransition: Scene '{targetSceneNameToLoad}' cannot be loaded. Check name/path and ensure it's in Build Settings.", gameObject);
                }
            }
            targetSceneNameToLoad = null; // Clear the target after attempting load
        }
        else // Load the next scene in build order
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
                sceneToLoadDebugName = System.IO.Path.GetFileNameWithoutExtension(scenePath); // Get cleaner name for logging
                Debug.Log($"CircleTransition: Loading next scene: {sceneToLoadDebugName} (Index: {nextSceneIndex})");
                asyncLoad = SceneManager.LoadSceneAsync(nextSceneIndex, LoadSceneMode.Single);
                sceneLoadInitiated = true;
            }
            else
            {
                 Debug.LogError("CircleTransition: Attempted to load next scene, but there are no more scenes in build settings!", gameObject);
            }
        }

        // If loading failed to start, abort the transition
        if (!sceneLoadInitiated || asyncLoad == null)
        {
            Debug.LogError("CircleTransition: Scene load operation could not be initiated. Aborting transition.", gameObject);
            // Clean up start transition visuals immediately
             if (startVideoPlayer != null && startVideoPlayer.gameObject.activeSelf) { startVideoPlayer.Stop(); startVideoPlayer.gameObject.SetActive(false); }
             if (startRawImage != null && startRawImage.gameObject.activeSelf) { startRawImage.gameObject.SetActive(false); }
            isTransitioning = false; // Reset flag on failure
            onTransitionComplete?.Invoke(); // Signal completion (or failure)
            yield break; // Exit coroutine
        }

        // --- Wait for Load, Keep Start Transition Visible ---
        asyncLoad.allowSceneActivation = false;
        Debug.Log("CircleTransition: Waiting for scene load (allowSceneActivation=false)...");
        while (asyncLoad.progress < 0.9f) // 0.9f means loading is done, ready for activation
        {
            // Optional: Update a loading progress UI element here
            yield return null; // Wait for the next frame
        }
        Debug.Log("CircleTransition: Scene loaded to 90%. Ready for activation.");

        // --- Activate Scene ---
        // At this point, the start transition (video/image) is still visible.
        Debug.Log("CircleTransition: Activating scene...");
        asyncLoad.allowSceneActivation = true;
        // Wait until the scene is fully activated (OnSceneLoaded callback will fire during this)
        yield return new WaitUntil(() => asyncLoad.isDone);
        Debug.Log($"CircleTransition: Scene '{sceneToLoadDebugName}' activated.");

        // --- End Transition --- (Plays *after* new scene is active and OnSceneLoaded has run)
        Debug.Log("CircleTransition: Preparing end transition visuals/sound...");

        if (endTransitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(endTransitionSound);
        }

        bool endTransitionWillPlay = false;
        if (endVideoPlayer != null)
        {
            endVideoPlayer.gameObject.SetActive(true);

            // Activate RawImage only if valid setup
            bool useEndRawImage = (endRawImage != null && endVideoPlayer.renderMode == VideoRenderMode.RenderTexture && endVideoPlayer.targetTexture != null);
            if(useEndRawImage)
            {
                endRawImage.texture = endVideoPlayer.targetTexture;
                endRawImage.color = Color.white;
            } else if (endRawImage != null) {
                endRawImage.gameObject.SetActive(false);
            }


            endVideoPlayer.Prepare();
            Debug.Log("CircleTransition: Preparing End Video...");
            yield return new WaitUntil(() => endVideoPlayer.isPrepared);
            Debug.Log("CircleTransition: End Video Prepared.");

            // Activate RawImage just before playing
             if(useEndRawImage)
             {
                  endRawImage.gameObject.SetActive(true);
             }

            endTransitionWillPlay = true;
        }
        else
        {
            Debug.LogWarning("CircleTransition: End VideoPlayer is missing or not assigned. Skipping end transition video.", gameObject);
        }

        // --- Hide Start Transition NOW ---
        // This happens just before the end transition visuals appear
        if (startTransitionPlayed)
        {
            Debug.Log("CircleTransition: Hiding start transition elements.");
            if (startVideoPlayer != null && startVideoPlayer.gameObject.activeSelf) { startVideoPlayer.Stop(); startVideoPlayer.gameObject.SetActive(false); }
            if (startRawImage != null && startRawImage.gameObject.activeSelf) { startRawImage.gameObject.SetActive(false); }
        }

        // --- Play End Transition ---
        if (endTransitionWillPlay)
        {
            Debug.Log("CircleTransition: Playing End Video...");
            endVideoPlayer.Play();
            yield return new WaitForSeconds(endVideoDuration); // Wait for its duration
            Debug.Log("CircleTransition: End transition visual duration elapsed.");
        }
        else
        {
            // If no end video, maybe wait a very brief moment or proceed immediately.
            // yield return null; // Wait one frame
            Debug.Log("CircleTransition: Skipping end transition duration wait.");
        }

        // --- Clean Up End Transition ---
        Debug.Log("CircleTransition: Cleaning up end transition elements.");
        if (endVideoPlayer != null && endVideoPlayer.gameObject.activeSelf)
        {
            endVideoPlayer.Stop();
            endVideoPlayer.gameObject.SetActive(false);
        }
        if (endRawImage != null && endRawImage.gameObject.activeSelf)
        {
            endRawImage.gameObject.SetActive(false);
        }

        // --- Final State Reset ---
        // isTransitioning flag is reset in OnSceneLoaded, which should have fired by now.
        // If OnSceneLoaded didn't reset it (e.g., if the singleton instance was somehow lost), reset it here as a safeguard.
        if (isTransitioning)
        {
            Debug.LogWarning("CircleTransition: isTransitioning flag was still true at the end of TransitionSequence. Resetting now.", gameObject);
            isTransitioning = false;
        }
        Debug.Log($"CircleTransition Instance ({gameObject.name}): TransitionSequence finished successfully.");
        onTransitionComplete?.Invoke(); // Signal completion callback if provided
    }


    void OnDestroy()
    {
        // This method is called when the GameObject is destroyed
        Debug.Log($"CircleTransition: OnDestroy called for {gameObject.name} (InstanceID: {gameObject.GetInstanceID()}).", gameObject);

        // Unsubscribe from events to prevent errors and memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (startVideoPlayer != null) {
             startVideoPlayer.prepareCompleted -= OnVideoPrepared;
             // Optional: Release RenderTexture if needed, although Unity usually manages this.
             // if (startVideoPlayer.targetTexture != null) startVideoPlayer.targetTexture.Release();
        }
        if (endVideoPlayer != null) {
             endVideoPlayer.prepareCompleted -= OnVideoPrepared;
             // if (endVideoPlayer.targetTexture != null) endVideoPlayer.targetTexture.Release();
        }

        // --- Singleton Cleanup ---
        // If the instance being destroyed is the currently active singleton instance,
        // clear the static reference so the pattern works correctly if the game restarts
        // or another instance tries to become the singleton later.
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("CircleTransition: Singleton instance destroyed and static reference cleared.", gameObject);
        }
        // --- End Singleton Cleanup ---
    }
}