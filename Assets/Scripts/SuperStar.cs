using UnityEngine;
using System.Collections;

public class SuperStar : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The exact name of the SFX track in AudioManager to play upon collection.")]
    public string collectionSfxName = "StarCollected";
    [Tooltip("Duration (in seconds) for the music to fade out.")]
    public float musicFadeOutDuration = 1.0f; // Added
    [Tooltip("Duration (in seconds) for the ambience to fade out.")]
    public float ambienceFadeOutDuration = 1.5f; // Added

    [Header("Gameplay Settings")]
    [Tooltip("Delay before scene transition starts. Should be >= longest fade-out duration.")]
    public float transitionDelay = 1.5f; // Keep or adjust based on fades
    public ParticleSystem starParticleEffect;

    private bool played = false;
    private CircleTransition transitionCanvas;
    private PlayerController playerController;
    private Animator anim;
    private Rigidbody rb;
    private GameObject player;

    void Start()
    {
        transitionCanvas = Object.FindFirstObjectByType<CircleTransition>();
        if (transitionCanvas == null) Debug.LogWarning("CircleTransitionCanvas not found!");
        player = null;
        if (starParticleEffect != null) starParticleEffect.Stop();
        else Debug.LogWarning("Star Particle Effect is not assigned!", this);

        // Ensure transition delay makes sense
        if (transitionDelay < Mathf.Max(musicFadeOutDuration, ambienceFadeOutDuration))
        {
            Debug.LogWarning($"SuperStar '{gameObject.name}': Transition Delay ({transitionDelay}s) is shorter than the longest fade-out duration ({Mathf.Max(musicFadeOutDuration, ambienceFadeOutDuration)}s). Sounds may be cut off.", this);
        }
    }

    void Update()
    {
        // Find player logic remains the same
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
                anim = player.GetComponent<Animator>();
                rb = player.GetComponent<Rigidbody>();
                if (playerController == null) { Debug.LogError("SuperStar could not find PlayerController!", player); player = null; }
                // Optional null checks for anim/rb
            }
        }
    }

    void OnCollisionEnter(Collision collision) { HandleCollision(collision.gameObject); }
    void OnTriggerEnter(Collider other) { HandleCollision(other.gameObject); }

    void HandleCollision(GameObject otherGameObject)
    {
        if (otherGameObject.CompareTag("Player") && !played && playerController != null)
        {
            played = true;

            // Player Control
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            playerController.DisableCharacterMovement();
            playerController.TriggerEmoteAnimation();

            // Effects
            if (starParticleEffect != null) starParticleEffect.Play();

            // --- Audio ---
            if (AudioManager.Instance != null)
            {
                // Play the SFX
                if (!string.IsNullOrEmpty(collectionSfxName))
                {
                    AudioManager.Instance.PlaySFX(collectionSfxName);
                }
                else Debug.LogWarning($"SuperStar on {gameObject.name} has no 'Collection Sfx Name' set.", this);

                // --- Fade Out Music and Ambience ---
                AudioManager.Instance.FadeOutMusic(musicFadeOutDuration);
                AudioManager.Instance.FadeOutAmbience(ambienceFadeOutDuration);
                // Ducking via SFX is no longer needed as ambience is fading out anyway.
            }
            else
            {
                Debug.LogWarning("AudioManager Instance is null! Cannot play SFX or fade audio.", this);
            }

            // --- Transition ---
            StartCoroutine(TransitionAfterDelay());

            // Optional: Disable visuals/collider immediately
            Collider col = GetComponent<Collider>(); if (col != null) col.enabled = false;
            Renderer ren = GetComponent<Renderer>(); if (ren != null) ren.enabled = false;
        }
    }

    IEnumerator TransitionAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);
        TriggerTransition();
        // Optional: Destroy(gameObject);
    }

    void TriggerTransition()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.LoadNextSceneWithTransition();
        }
        else Debug.LogError("Failed to transition: CircleTransitionCanvas not found", this);
    }
}