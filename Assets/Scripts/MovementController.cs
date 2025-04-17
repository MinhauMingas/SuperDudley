using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 7f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float airControl = 0.5f;
    public float jumpForce = 7f;
    public float doubleJumpForce = 5f;
    public float rotationSpeed = 20f;
    public float groundCheckDistance = 0.25f;
    public float groundStickForce = 5f;
    public LayerMask groundLayer = ~0;

    [Header("Animation Settings")]
    public string idleAnimation = "idle";
    public string walkAnimation = "walk";
    public string sprintAnimation = "sprint";
    public string jumpAnimation = "jump";
    public string doubleJumpAnimation = "doubleJump";
    public string fallAnimation = "fall";
    public string hurtAnimationName = "hurt";
    public string deathAnimationName = "die";

    [Header("Animation Timing")]
    public float jumpAnimationDuration = 0.8f;
    public float doubleJumpAnimationDuration = 0.6f;
    public float hurtAnimationDuration = 1.0f;

    [Header("Input Thresholds")]
    public float walkThreshold = 0.2f;
    public float sprintThreshold = 0.3f;

    [Header("Random Sound Arrays")]
    public AudioClip[] jumpSounds;
    public AudioClip[] doubleJumpSounds;
    public AudioClip[] hurtSounds;
    public AudioClip[] dieSounds;

    [Header("Sound Volumes")]
    [Range(0f, 1f)] public float jumpVolume = 1f;
    [Range(0f, 1f)] public float doubleJumpVolume = 1f;
    [Range(0f, 1f)] public float hurtVolume = 1f;
    [Range(0f, 1f)] public float dieVolume = 1f;

    [Header("Health Settings")]
    public float invulnerabilityDuration = 1.5f;
    public float blinkInterval = 0.1f;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    private Renderer bodyRenderer;
    private Renderer headRenderer;

    [Header("Hazard Settings")]
    public int hazardDamage = 10;
    public int hazardKnockbackThreshold = 10;

    private Rigidbody rb;
    private Animator anim;
    private Vector3 movementInput;
    private bool isJumping;
    private bool canDoubleJump;
    private bool isDoubleJumping;
    private bool isGrounded;
    private bool wasGrounded;
    private RaycastHit groundHit;
    private float jumpStartTime;
    private const float minJumpTime = 0.3f;
    private bool isPlayingJumpAnimation;
    private bool isPlayingDoubleJumpAnimation;
    private float jumpAnimationEndTime;
    private float doubleJumpAnimationEndTime;
    private Vector3 groundNormal = Vector3.up;
    private float currentMoveSpeed;
    private float inputMagnitude;
    private bool inputEnabled = true;
    public float disableInputDuration = 0.3f;
    public float hitKnockbackForce = 5f;
    private float disableInputTimer = 0f;
    private Vector3 knockbackDirection;
    private bool isKnockbacking = false;
    private bool isPlayingHurtAnimation = false;
    private float hurtAnimationEndTime;
    private bool isDead = false;
    private AudioSource audioSource;

    [Header("Star Collision Settings")]
    public string starAnimationName = "starCollected";
    private bool isStarCollected = false;
    private float starAnimationEndTime;
    public float starAnimationDuration = 2f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hurtEffectPrefab;
    [SerializeField] private float hurtEffectDuration = 1f;

    public float kinematicDelay = 0.2f; // Delay before kinematic activation

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        currentMoveSpeed = walkSpeed;

        bodyRenderer = transform.Find("body-mesh").GetComponent<Renderer>();
        headRenderer = transform.Find("head-mesh").GetComponent<Renderer>();

        if (bodyRenderer == null || headRenderer == null)
        {
            Debug.LogError("Renderers (body-mesh or head-mesh) not found!");
        }

        isInvulnerable = false;
        if (bodyRenderer != null) bodyRenderer.enabled = true;
        if (headRenderer != null) headRenderer.enabled = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        if (isStarCollected)
        {
            if (Time.time >= starAnimationEndTime)
            {
                isStarCollected = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }

        if (isDead) return;

        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;

            float blinkRemainder = invulnerabilityTimer % (blinkInterval * 2);
            bool shouldBeVisible = blinkRemainder > blinkInterval;

            if (bodyRenderer != null) bodyRenderer.enabled = shouldBeVisible;
            if (headRenderer != null) headRenderer.enabled = shouldBeVisible;

            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
                if (bodyRenderer != null) bodyRenderer.enabled = true;
                if (headRenderer != null) headRenderer.enabled = true;
            }
        }
        if (!inputEnabled)
        {
            disableInputTimer -= Time.deltaTime;
            if (disableInputTimer <= 0)
            {
                inputEnabled = true;
                isKnockbacking = false;
            }
        }

        if (isPlayingHurtAnimation && Time.time >= hurtAnimationEndTime)
        {
            isPlayingHurtAnimation = false;
        }

        if (isPlayingHurtAnimation)
        {
            return;
        }

        if (inputEnabled)
        {
            HandleInput();
        }

        UpdateAnimations();
    }

    void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetButtonDown("Jump");

        inputMagnitude = new Vector2(horizontal, vertical).magnitude;
        movementInput = new Vector3(horizontal, 0, vertical).normalized * inputMagnitude;

        if (inputMagnitude > sprintThreshold)
        {
            currentMoveSpeed = sprintSpeed;
        }
        else if (inputMagnitude > walkThreshold)
        {
            float t = (inputMagnitude - walkThreshold) / (sprintThreshold - walkThreshold);
            currentMoveSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, t);
        }
        else
        {
            currentMoveSpeed = walkSpeed;
        }

        if (jumpPressed && isGrounded && !isJumping)
        {
            Jump();
        }
        else if (jumpPressed && canDoubleJump && !isDoubleJumping)
        {
            DoubleJump();
        }
    }

    void FixedUpdate()
    {
        if (isStarCollected || isDead) return;
        wasGrounded = isGrounded;
        CheckGround();
        if (inputEnabled)
        {
            HandleMovement();
        }
        else if (isKnockbacking)
        {
            rb.AddForce(knockbackDirection * hitKnockbackForce, ForceMode.Impulse);
        }

        if (isGrounded && !isJumping && !isDoubleJumping)
        {
            rb.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out groundHit, groundCheckDistance, groundLayer);

        if (!isGrounded)
        {
            Vector3 frontRight = transform.position + new Vector3(0.3f, 0.1f, 0.3f);
            Vector3 frontLeft = transform.position + new Vector3(-0.3f, 0.1f, 0.3f);
            Vector3 backRight = transform.position + new Vector3(0.3f, 0.1f, -0.3f);
            Vector3 backLeft = transform.position + new Vector3(-0.3f, 0.1f, -0.3f);

            if (Physics.Raycast(frontRight, Vector3.down, out groundHit, groundCheckDistance, groundLayer) ||
                Physics.Raycast(frontLeft, Vector3.down, out groundHit, groundCheckDistance, groundLayer) ||
                Physics.Raycast(backRight, Vector3.down, out groundHit, groundCheckDistance, groundLayer) ||
                Physics.Raycast(backLeft, Vector3.down, out groundHit, groundCheckDistance, groundLayer))
            {
                isGrounded = true;
            }
        }

        if (isGrounded)
        {
            groundNormal = groundHit.normal;
        }
        else
        {
            groundNormal = Vector3.up;
        }

        if (isGrounded && !wasGrounded)
        {
            isPlayingJumpAnimation = false;
            isPlayingDoubleJumpAnimation = false;
        }

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            isJumping = false;
            isDoubleJumping = false;

            if (!canDoubleJump && !isPlayingJumpAnimation && !isPlayingDoubleJumpAnimation)
            {
                canDoubleJump = true;
            }
        }
    }

    void HandleMovement()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = Vector3.zero;

        if (movementInput.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(movementInput.x, movementInput.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            Vector3 moveDirection = transform.forward;

            if (isGrounded)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
                targetVelocity = moveDirection * currentMoveSpeed * inputMagnitude;

                rb.linearVelocity = Vector3.Lerp(
                    new Vector3(currentVelocity.x, 0, currentVelocity.z),
                    targetVelocity,
                    acceleration * Time.fixedDeltaTime
                );
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, currentVelocity.y, rb.linearVelocity.z);
            }
            else
            {
                targetVelocity = moveDirection * currentMoveSpeed * inputMagnitude * airControl;

                rb.linearVelocity = Vector3.Lerp(
                    new Vector3(currentVelocity.x, 0, currentVelocity.z),
                    targetVelocity,
                    acceleration * airControl * Time.fixedDeltaTime
                );
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, currentVelocity.y, rb.linearVelocity.z);
            }
        }
        else if (isGrounded)
        {
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, groundNormal);
            Vector3 desiredVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);

            Vector3 verticalVelocity = Vector3.Project(currentVelocity, groundNormal);
            rb.linearVelocity = desiredVelocity + verticalVelocity;
        }
    }

    void UpdateAnimations()
    {
        if (isStarCollected)
        {
            anim.Play(starAnimationName);
            return;
        }

        float currentTime = Time.time;

        if ((isPlayingJumpAnimation || isPlayingDoubleJumpAnimation) && isGrounded)
        {
            isPlayingJumpAnimation = false;
            isPlayingDoubleJumpAnimation = false;
        }

        if (isPlayingJumpAnimation && currentTime < jumpAnimationEndTime && !isGrounded)
        {
            return;
        }

        if (isPlayingDoubleJumpAnimation && currentTime < doubleJumpAnimationEndTime && !isGrounded)
        {
            return;
        }

        if (isPlayingJumpAnimation && currentTime >= jumpAnimationEndTime)
        {
            isPlayingJumpAnimation = false;
        }

        if (isPlayingDoubleJumpAnimation && currentTime >= doubleJumpAnimationEndTime)
        {
            isPlayingDoubleJumpAnimation = false;
        }

        if (!isPlayingJumpAnimation && !isPlayingDoubleJumpAnimation)
        {
            if (isJumping && rb.linearVelocity.y > 0.1f && !isGrounded)
            {
                StartJumpAnimation();
            }
            else if (isDoubleJumping && rb.linearVelocity.y > 0.1f && !isGrounded)
            {
                StartDoubleJumpAnimation();
            }
            else if (!isGrounded && rb.linearVelocity.y < -0.1f && currentTime - jumpStartTime > minJumpTime)
            {
                anim.Play(fallAnimation);
            }
            else if (isGrounded)
            {
                if (inputMagnitude > sprintThreshold)
                {
                    anim.Play(sprintAnimation);
                }
                else if (inputMagnitude > walkThreshold)
                {
                    anim.Play(walkAnimation);
                }
                else if (inputMagnitude > 0.1f)
                {
                    anim.Play(walkAnimation);
                }
                else
                {
                    anim.Play(idleAnimation);
                }
            }
        }
    }

    void StartJumpAnimation()
    {
        anim.Play(jumpAnimation);
        isPlayingJumpAnimation = true;
        jumpAnimationEndTime = Time.time + jumpAnimationDuration;
    }

    void StartDoubleJumpAnimation()
    {
        anim.Play(doubleJumpAnimation);
        isPlayingDoubleJumpAnimation = true;
        doubleJumpAnimationEndTime = Time.time + doubleJumpAnimationDuration;
    }

    void Jump()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
        isJumping = true;
        canDoubleJump = true;
        jumpStartTime = Time.time;
        StartJumpAnimation();

        if (jumpSounds != null && jumpSounds.Length > 0)
        {
            audioSource.PlayOneShot(jumpSounds[Random.Range(0, jumpSounds.Length)], jumpVolume);
        }
    }

    void DoubleJump()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVelocity.x, doubleJumpForce, currentVelocity.z);
        isDoubleJumping = true;
        canDoubleJump = false;
        jumpStartTime = Time.time;
        StartDoubleJumpAnimation();

        if (doubleJumpSounds != null && doubleJumpSounds.Length > 0)
        {
            audioSource.PlayOneShot(doubleJumpSounds[Random.Range(0, doubleJumpSounds.Length)], doubleJumpVolume);
        }
    }

    public void DisableInput(Vector3 collisionPoint)
    {
        inputEnabled = false;
        disableInputTimer = disableInputDuration;
        knockbackDirection = (transform.position - collisionPoint).normalized;
        isKnockbacking = true;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvulnerable || isDead) return;

        PlayerStats.Instance.TakeDamage(damageAmount);

        Debug.Log($"Jogador tomou {damageAmount} de dano. Vida atual: {PlayerStats.Instance.Health}");

        if (PlayerStats.Instance.Health <= 0)
        {
            Die();
            return;
        }

        if (hurtSounds != null && hurtSounds.Length > 0)
        {
            audioSource.PlayOneShot(hurtSounds[Random.Range(0, hurtSounds.Length)], hurtVolume);
        }

        if (anim != null && !string.IsNullOrEmpty(hurtAnimationName))
        {
            Debug.Log("Attempting to play hurt animation: " + hurtAnimationName);
            anim.Play(hurtAnimationName);
            isPlayingHurtAnimation = true;
            hurtAnimationEndTime = Time.time + hurtAnimationDuration;
            Debug.Log("Current Animator State: " + anim.GetCurrentAnimatorStateInfo(0).shortNameHash);
        }
        else
        {
            Debug.LogWarning("Animator or hurtAnimationName is null or empty!");
        }

        StartInvulnerability();
        DisableInput(transform.position);

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
        Debug.Log("Jogador está invulnerável por " + invulnerabilityDuration + " segundos.");
    }

    void Die(bool fromDeathbox = false)
    {
        Debug.Log("Die() function called");

        Debug.Log("Jogador Morreu!");
        isInvulnerable = false;
        isDead = true;

        if (dieSounds != null && dieSounds.Length > 0)
        {
            audioSource.PlayOneShot(dieSounds[Random.Range(0, dieSounds.Length)], dieVolume);
        }

        if (bodyRenderer != null) bodyRenderer.enabled = true;
        if (headRenderer != null) headRenderer.enabled = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (fromDeathbox)
        {
            StartCoroutine(EnableKinematicWithDelay());
        }

        if (anim != null && !string.IsNullOrEmpty(deathAnimationName))
        {
            Debug.Log("Attempting to play death animation: " + deathAnimationName);
            anim.Play(deathAnimationName);
            Debug.Log("Current Animator State: " + anim.GetCurrentAnimatorStateInfo(0).shortNameHash);
        }
        else
        {
            Debug.LogWarning("Animator or deathAnimationName is null or empty!");
        }

        inputEnabled = false;
        this.enabled = false;

        StartCoroutine(RestartLevelAfterDelay(1f));
    }

    IEnumerator EnableKinematicWithDelay()
    {
        yield return new WaitForSeconds(kinematicDelay);
        rb.isKinematic = true;
    }

    IEnumerator RestartLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("hazard") && !isInvulnerable)
        {
            Debug.Log("Colidiu com Hazard!");

            if (PlayerStats.Instance.Health > hazardKnockbackThreshold)
            {
                Vector3 knockbackDirection = (transform.position - collision.contacts[0].point).normalized;
                knockbackDirection.y = Mathf.Clamp(knockbackDirection.y, 0.2f, 1.0f);

                rb.linearVelocity = Vector3.zero;
                rb.AddForce(knockbackDirection * hitKnockbackForce, ForceMode.Impulse);
            }

            TakeDamage(hazardDamage);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("star") && !isStarCollected)
        {
            HandleStarCollision();
        }
        else if (other.CompareTag("deathbox"))
        {
            Die(true);
        }
    }

    void HandleStarCollision()
    {
        isStarCollected = true;
        starAnimationEndTime = Time.time + starAnimationDuration;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        inputEnabled = false;

        if (anim != null && !string.IsNullOrEmpty(starAnimationName))
        {
            anim.Play(starAnimationName);
        }
        else
        {
            Debug.LogWarning("Animator or starAnimationName is null or empty!");
        }
    }
}