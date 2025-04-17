using UnityEngine;

using System.Collections;



public class PlayerHealth : MonoBehaviour

{

    [Header("Invulnerability Settings")]

    public float invulnerabilityDuration = 1.5f;

    public float blinkInterval = 0.1f;



    [Header("Hazard Settings")]

    public int hazardDamage = 1;



    [Header("Visual Effects")]

    [SerializeField] private GameObject hurtEffectPrefab;

    [SerializeField] private float hurtEffectDuration = 1f;



    [Header("Sound Effects")]

    public AudioClip[] hurtSounds;

    public AudioClip[] dieSounds;

    [Range(0f, 1f)] public float hurtVolume = 1f;

    [Range(0f, 1f)] public float dieVolume = 1f;



    [Header("Death Kinematic Delay")]

    public float deathKinematicDelay = 0.2f;



    private bool isInvulnerable = false;

    private float invulnerabilityTimer = 0f;

    private Renderer bodyRenderer;

    private Renderer headRenderer;

    private PlayerMovement playerMovement;

    private PlayerAnimation playerAnimation;

    private PlayerJump playerJump;

    private AudioSource audioSource;

    private HealthController healthController;



    void Start()

    {

        playerMovement = GetComponent<PlayerMovement>();

        playerAnimation = GetComponent<PlayerAnimation>();

        playerJump = GetComponent<PlayerJump>();

        audioSource = GetComponent<AudioSource>();



        GameObject canvasHP = GameObject.Find("Canva-HP");

        if (canvasHP != null)

        {

            healthController = canvasHP.GetComponent<HealthController>();

            if (healthController == null)

            {

                Debug.LogError("HealthController component not found on Canva-HP!");

            }

        }

        else

        {

            Debug.LogError("Canva-HP GameObject not found!");

        }



        bodyRenderer = transform.Find("body-mesh")?.GetComponent<Renderer>();

        headRenderer = transform.Find("head-mesh")?.GetComponent<Renderer>();



        if (audioSource == null)

        {

            audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0f;

        }



        isInvulnerable = false;

        EnableRenderers(true);

    }



    void Update()

    {

        if (isInvulnerable)

        {

            invulnerabilityTimer -= Time.deltaTime;

            float blinkRemainder = invulnerabilityTimer % (blinkInterval * 2);

            bool shouldBeVisible = blinkRemainder > blinkInterval;

            EnableRenderers(shouldBeVisible);



            if (invulnerabilityTimer <= 0f)

            {

                isInvulnerable = false;

                EnableRenderers(true);

            }

        }

    }



    public void TakeDamage(Vector3 damageSourcePosition)

    {

        if (isInvulnerable || (playerAnimation != null && playerAnimation.isDead)) return;



        if (healthController != null)

        {

            healthController.TakeDamage(hazardDamage);

            Debug.Log($"Player took {hazardDamage} damage.");



            if (healthController.Health <= 0)

            {

                Die();

                return;

            }

        }



        PlaySound(hurtSounds, hurtVolume);

        if (playerAnimation != null) playerAnimation.PlayHurtAnimation();

        StartInvulnerability();



        if (playerMovement != null)

        {

            playerMovement.DisableInputForKnockback(damageSourcePosition);

        }



        TriggerHurtEffect(transform.position);

    }



    void TriggerHurtEffect(Vector3 hurtPosition)

    {

        if (hurtEffectPrefab != null)

        {

            GameObject hurtEffect = Instantiate(hurtEffectPrefab, hurtPosition, Quaternion.identity);

            Destroy(hurtEffect, hurtEffectDuration);

        }

    }



    void StartInvulnerability()

    {

        isInvulnerable = true;

        invulnerabilityTimer = invulnerabilityDuration;

        Debug.Log("Player is invulnerable for " + invulnerabilityDuration + " seconds.");

    }



    void Die()

    {

        if (playerAnimation != null && playerAnimation.isDead) return;



        Debug.Log("Die() called. Player Died!");

        isInvulnerable = false; // Ensure invulnerability is turned off

        if (playerAnimation != null) playerAnimation.isDead = true;



        PlaySound(dieSounds, dieVolume);

        EnableRenderers(true);



        if (playerMovement != null && playerMovement.rb != null)

        {

            playerMovement.rb.linearVelocity = Vector3.zero;

            playerMovement.rb.angularVelocity = Vector3.zero;

            StartCoroutine(EnableKinematicWithDelay()); // Apply the delay on every death

        }



        if (playerAnimation != null) playerAnimation.PlayDeathAnimation();

        if (playerMovement != null) playerMovement.DisableMovement();

        if (playerJump != null) playerJump.DisableJump();

        this.enabled = false;



        PlayerSpawn playerSpawn = Object.FindFirstObjectByType<PlayerSpawn>();

        if (playerSpawn != null)

        {

            playerSpawn.RespawnPlayer();

        }

        else

        {

            Debug.LogError("PlayerSpawn object not found!");

        }

    }



    IEnumerator EnableKinematicWithDelay()

    {

        yield return new WaitForSeconds(deathKinematicDelay);

        if (playerMovement != null && playerMovement.rb != null)

        {

            playerMovement.rb.isKinematic = true;

        }

    }



    void OnCollisionEnter(Collision collision)

    {

        if ((playerAnimation != null && playerAnimation.isDead) || isInvulnerable) return;



        if (collision.gameObject.CompareTag("hazard"))

        {

            Debug.Log("Collided with Hazard!");

            Vector3 knockbackDirection = (transform.position - collision.contacts[0].point).normalized;

            knockbackDirection.y = Mathf.Clamp(knockbackDirection.y, 0.2f, 1.0f);



            if (healthController != null && healthController.Health > 1 && playerMovement != null && playerMovement.rb != null)

            {

                playerMovement.rb.linearVelocity = Vector3.zero;

                playerMovement.rb.AddForce(knockbackDirection * playerMovement.hitKnockbackForce, ForceMode.Impulse);

            }

            TakeDamage(collision.contacts[0].point);

        }

    }



    void OnTriggerEnter(Collider other)

    {

        if (other.CompareTag("deathbox"))

        {

            Debug.Log("Entered Deathbox!");

            Die(); // Call Die() directly, bypassing invulnerability check

            return;

        }



        if ((playerAnimation != null && playerAnimation.isDead) || isInvulnerable) return;



        if (other.CompareTag("hazard"))

        {

            Debug.Log("Triggered Hazard!");

            TakeDamage(other.ClosestPoint(transform.position)); // Use ClosestPoint for a general trigger point

        }

    }



    void EnableRenderers(bool enabled)

    {

        if (bodyRenderer != null) bodyRenderer.enabled = enabled;

        if (headRenderer != null) headRenderer.enabled = enabled;

    }



    void PlaySound(AudioClip[] clips, float volume)

    {

        if (audioSource != null && clips != null && clips.Length > 0)

        {

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            if (clip != null)

            {

                audioSource.PlayOneShot(clip, volume);

            }

        }

    }

}