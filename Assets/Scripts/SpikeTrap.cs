using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    [Header("Animation Names")]
    [Tooltip("The name of the 'hide' animation state (within this GameObject's Animator).")]
    public string hideAnimationName = "hide";
    [Tooltip("The name of the 'show' animation state (within this GameObject's Animator).")]
    public string showAnimationName = "show";

    [Header("Damage Object")]
    [Tooltip("The nested GameObject responsible for dealing damage when the spike is showing.")]
    public GameObject damageObject;

    [Header("Speed Control")]
    [Tooltip("Controls the speed of the hide/show cycle (0 to 10). Higher values mean faster cycles.")]
    [Range(0f, 10f)]
    public float speed = 1f;
    [Tooltip("A small delay (in seconds) after the 'show' animation starts before the damage object is enabled.")]
    public float enableDamageDelay = 0.2f;

    [Header("Alternating Behavior")]
    [Tooltip("If true, this spike's initial state will be 'showing' instead of 'hiding', effectively alternating with spikes that have this set to false.")]
    public bool startShowing = false;

    [Header("Sound Settings")]
    public AudioClip showSoundClip;
    public AudioClip hideSoundClip;
    [Range(0f, 1f)] public float soundVolume = 1f;
    private AudioSource audioSource;
    [Tooltip("Sets how much the sound is affected by distance (0 = 2D, 1 = 3D).")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Tooltip("The minimum distance the listener can be from the source before volume starts to decrease.")]
    public float minDistance = 1f;
    [Tooltip("The distance at which the sound will be inaudible.")]
    public float maxDistance = 10f;
    [Tooltip("The type of volume rolloff to use over distance.")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    private Animator animator;
    private bool isHidden = true;
    private float cycleDuration;
    private float timer;

    void Awake()
    {
        // Get the Animator component attached to this GameObject
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("SpikeTrap: Animator component not found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Ensure the Damage Object is assigned
        if (damageObject == null)
        {
            Debug.LogError("SpikeTrap: Damage Object not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        // Initially disable the damage object
        damageObject.SetActive(false);

        // Set initial state based on the bool
        isHidden = !startShowing;

        // Calculate cycle duration based on speed
        UpdateCycleDuration();

        // Set initial timer
        timer = cycleDuration / 2f; // Start halfway through a cycle for better visual distribution

        // Add AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // Configure spatial audio settings
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = rolloffMode;

        // Play initial animation (sound is handled separately in PlayInitialSound)
        PlayInitialAnimation();
        PlayInitialSound();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ToggleState();
            UpdateCycleDuration();
            timer = cycleDuration / 2f;
            PlayAnimation();
            // Play sound is now called directly in ToggleState
        }
    }

    void ToggleState()
    {
        isHidden = !isHidden;
        damageObject.SetActive(!isHidden); // Enable damage object when showing
        PlaySound(); // Play sound immediately when the state toggles
        if (isHidden)
        {
            CancelInvoke("EnableDamageObject");
            damageObject.SetActive(false);
        }
        else
        {
            Invoke("EnableDamageObject", enableDamageDelay);
        }
    }

    void UpdateCycleDuration()
    {
        // Map the speed (0-10) to a reasonable cycle duration
        float baseDuration = Mathf.Lerp(5f, 0.5f, speed / 10f);
        cycleDuration = baseDuration;
    }

    void PlayInitialAnimation()
    {
        animator.Play(isHidden ? hideAnimationName : showAnimationName);
    }

    void PlayAnimation()
    {
        animator.Play(isHidden ? hideAnimationName : showAnimationName);
    }

    void PlayInitialSound()
    {
        if (startShowing && showSoundClip != null)
        {
            audioSource.PlayOneShot(showSoundClip, soundVolume);
        }
        else if (!startShowing && hideSoundClip != null)
        {
            audioSource.PlayOneShot(hideSoundClip, soundVolume);
        }
    }

    void PlaySound()
    {
        if (!isHidden && showSoundClip != null)
        {
            audioSource.PlayOneShot(showSoundClip, soundVolume);
        }
        else if (isHidden && hideSoundClip != null)
        {
            audioSource.PlayOneShot(hideSoundClip, soundVolume);
        }
    }

    void EnableDamageObject()
    {
        damageObject.SetActive(true);
    }

    // Editor helper to ensure animation names and delay are not negative
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(hideAnimationName))
        {
            hideAnimationName = "hide";
        }
        if (string.IsNullOrEmpty(showAnimationName))
        {
            showAnimationName = "show";
        }
        if (speed < 0f)
        {
            speed = 0f;
        }
        if (speed > 10f)
        {
            speed = 10f;
        }
        if (enableDamageDelay < 0f)
        {
            enableDamageDelay = 0f;
        }
        if (soundVolume < 0f)
        {
            soundVolume = 0f;
        }
        if (soundVolume > 1f)
        {
            soundVolume = 1f;
        }
        if (spatialBlend < 0f)
        {
            spatialBlend = 0f;
        }
        if (spatialBlend > 1f)
        {
            spatialBlend = 1f;
        }
        if (minDistance < 0f)
        {
            minDistance = 0f;
        }
        if (maxDistance < minDistance)
        {
            maxDistance = minDistance;
        }
    }
}