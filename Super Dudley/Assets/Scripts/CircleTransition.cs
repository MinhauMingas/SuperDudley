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
    private int nextSceneIndex;
    private Canvas transitionEffectCanvas;

    void Awake()
    {
        // Prevent duplicate instances
        var existingTransitions = FindObjectsByType<CircleTransition>(FindObjectsSortMode.None);
        if (existingTransitions.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        InitializeReferences();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void InitializeReferences()
    {
        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Find canvas and components
        transitionEffectCanvas = GetComponentInChildren<Canvas>(true);
        if (transitionEffectCanvas == null)
        {
            Debug.LogError("TransitionEffectCanvas not found as a child!");
            return;
        }

        // Initialize video players
        startVideoPlayer = transitionEffectCanvas.transform.Find("Transition Start - VideoPlayer")?.GetComponent<VideoPlayer>();
        endVideoPlayer = transitionEffectCanvas.transform.Find("Transition End - VideoPlayer")?.GetComponent<VideoPlayer>();
        
        // Initialize raw images
        startRawImage = transitionEffectCanvas.transform.Find("Transition Start - RawImage")?.GetComponent<RawImage>();
        endRawImage = transitionEffectCanvas.transform.Find("Transition End - RawImage")?.GetComponent<RawImage>();

        // Verify all components
        if (startVideoPlayer == null || endVideoPlayer == null || 
            startRawImage == null || endRawImage == null)
        {
            Debug.LogError("One or more transition components are missing!");
            return;
        }

        // Setup video players
        SetupVideoPlayer(startVideoPlayer);
        SetupVideoPlayer(endVideoPlayer);

        // Initial state
        startVideoPlayer.gameObject.SetActive(false);
        startRawImage.gameObject.SetActive(false);
        endVideoPlayer.gameObject.SetActive(false);
        endRawImage.gameObject.SetActive(false);
    }

    void SetupVideoPlayer(VideoPlayer player)
    {
        if (player == null) return;
        
        player.playOnAwake = false;
        player.isLooping = false;
        player.Stop();
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.EnableAudioTrack(0, true);
        player.prepareCompleted += OnVideoPrepared;
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        // Optional: Add any preparation logic if needed
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        
        // Reinitialize references in case they were lost
        InitializeReferences();
        
        // Reset transition state
        if (endVideoPlayer != null)
        {
            endVideoPlayer.Stop();
            endVideoPlayer.gameObject.SetActive(false);
        }
        if (endRawImage != null)
        {
            endRawImage.gameObject.SetActive(false);
        }
        
        isTransitioning = false;
    }

    public void StartTransition(Action onTransitionComplete = null)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionSequence(onTransitionComplete));
    }

    IEnumerator TransitionSequence(Action onTransitionComplete = null)
    {
        isTransitioning = true;

        // Play start sound
        if (startTransitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startTransitionSound);
        }

        // Show and play start transition
        if (startVideoPlayer != null && startRawImage != null)
        {
            startVideoPlayer.gameObject.SetActive(true);
            startRawImage.gameObject.SetActive(true);
            startVideoPlayer.Prepare();
            yield return new WaitUntil(() => startVideoPlayer.isPrepared);
            startVideoPlayer.Play();
        }

        yield return new WaitForSeconds(startVideoDuration);

        // Determine next scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        nextSceneIndex = currentSceneIndex + 1;

        // Load next scene if available
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneIndex);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        else
        {
            Debug.Log("Reached the end of the scene list.");
            isTransitioning = false;
            onTransitionComplete?.Invoke();
            yield break;
        }

        // Play end sound
        if (endTransitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(endTransitionSound);
        }

        // Hide start transition, show end transition
        if (startVideoPlayer != null && startRawImage != null)
        {
            startVideoPlayer.gameObject.SetActive(false);
            startRawImage.gameObject.SetActive(false);
        }

        if (endVideoPlayer != null && endRawImage != null)
        {
            endVideoPlayer.gameObject.SetActive(true);
            endRawImage.gameObject.SetActive(true);
            endVideoPlayer.Prepare();
            yield return new WaitUntil(() => endVideoPlayer.isPrepared);
            endVideoPlayer.Play();
        }

        yield return new WaitForSeconds(endVideoDuration);

        // Clean up
        if (endVideoPlayer != null)
        {
            endVideoPlayer.Stop();
            endVideoPlayer.gameObject.SetActive(false);
        }
        if (endRawImage != null)
        {
            endRawImage.gameObject.SetActive(false);
        }

        isTransitioning = false;
        onTransitionComplete?.Invoke();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (startVideoPlayer != null)
            startVideoPlayer.prepareCompleted -= OnVideoPrepared;
        if (endVideoPlayer != null)
            endVideoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    public void LoadNextSceneWithTransition()
    {
        StartTransition();
    }

    public void LoadNextSceneWithTransition(Action onTransitionComplete)
    {
        StartTransition(onTransitionComplete);
    }
}