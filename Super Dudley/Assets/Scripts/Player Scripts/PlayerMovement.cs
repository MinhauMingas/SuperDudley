using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float groundDrag = 1.5f;
    public float airControl = 0.5f;
    public float rotationSpeed = 20f;
    public float groundCheckDistance = 0.25f;
    public float groundStickForce = 5f;
    public LayerMask groundLayer = ~0;

    [Header("Ground Dash Settings")]
    public float groundDashForce = 15f;
    public float groundDashDuration = 0.3f;
    public float groundDashCooldown = 0.5f;
    public AudioClip[] groundDashSounds;
    [Range(0f, 1f)] public float groundDashVolume = 0.8f;
    public float groundDashRequiredSpeed = 7f;

    [Header("Input Thresholds")]
    public float walkThreshold = 0.2f;
    public float sprintThreshold = 0.3f;
    public float disableInputDuration = 0.3f;
    public float hitKnockbackForce = 10f;

    [Header("Particle Effect")]
    public ParticleSystem footParticleSystem;
    public float particleSpeedThreshold = 7f;
    public float particleInputThreshold = 0.3f;
    public float particleFadeOutDuration = 0.5f;

    [HideInInspector] public Rigidbody rb;
    private AudioSource audioSource;
    private PlayerInput playerInput;
    private float particleFadeOutTimer = 0f;
    private bool particlesShouldBePlaying = false;

    // Input & State Variables
    private Vector2 rawMoveInput;
    private Vector3 movementInput;
    [HideInInspector] public float inputMagnitude;
    private float currentMoveSpeed;
    private bool isGrounded;
    private RaycastHit groundHit;
    private Vector3 groundNormal = Vector3.up;
    private bool inputEnabled = true;
    private float disableInputTimer = 0f;
    [HideInInspector] public bool canMove = true;
    private Transform currentPlatform;
    [HideInInspector] public Transform originalParent;

    // Knockback State
    private Vector3 knockbackDirection;
    private bool isKnockbacking = false;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0.3f;

    // Dash State
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 lastGroundDashDirection = Vector3.zero;

    // Jump State
    private bool isJumping = false;

    // Ground Stick State
    private bool groundStickEnabled = true;

    // Stop threshold
    private float stopThreshold = 0.02f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInput>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearDamping = 0;
        currentMoveSpeed = walkSpeed;
        originalParent = transform.parent;

        // Initialize particle system
        if (footParticleSystem != null)
        {
            var mainModule = footParticleSystem.main;
            mainModule.loop = true;
            mainModule.playOnAwake = false;
            footParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        else
        {
            Debug.LogWarning("Foot particle system not assigned in the inspector!");
        }
    }

    public void OnMove(InputValue value)
    {
        rawMoveInput = value.Get<Vector2>();
    }

    public void OnGroundDash(InputValue value)
    {
        if (value.isPressed && inputEnabled && canMove && isGrounded && !isDashing &&
            dashCooldownTimer <= 0 && rb.linearVelocity.magnitude >= groundDashRequiredSpeed)
        {
            PerformGroundDash();
        }
    }

    void Update()
    {
        HandleTimers();
        if (inputEnabled && canMove && !isDashing && !isKnockbacking)
        {
            ProcessMovementInput();
        }
        else
        {
            movementInput = Vector3.zero;
            inputMagnitude = 0f;
        }
    }

    void HandleTimers()
    {
        if (!inputEnabled)
        {
            disableInputTimer -= Time.deltaTime;
            if (disableInputTimer <= 0)
            {
                inputEnabled = true;
                canMove = true; // Also re-enable canMove when the timer expires
            }
        }

        if (isKnockbacking)
        {
            knockbackTimer += Time.deltaTime;
            if (knockbackTimer >= knockbackDuration)
            {
                isKnockbacking = false;
            }
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }
    }

    void ProcessMovementInput()
    {
        movementInput = new Vector3(rawMoveInput.x, 0, rawMoveInput.y);
        inputMagnitude = Mathf.Clamp01(movementInput.magnitude);

        if (inputMagnitude > 0.01f)
        {
            movementInput.Normalize();
            if (inputMagnitude < walkThreshold)
            {
                float t = Mathf.InverseLerp(0.01f, walkThreshold, inputMagnitude);
                currentMoveSpeed = Mathf.Lerp(walkSpeed * 0.5f, walkSpeed, t);
            }
            else if (inputMagnitude < sprintThreshold)
            {
                currentMoveSpeed = walkSpeed;
            }
            else
            {
                float t = Mathf.InverseLerp(sprintThreshold, 1.0f, inputMagnitude);
                currentMoveSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, t);
            }
        }
        else
        {
            movementInput = Vector3.zero;
            inputMagnitude = 0f;
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        if (isGrounded && !isDashing && !isKnockbacking)
        {
            ApplyGroundDrag();
        }

        if (isKnockbacking)
        {
            groundStickEnabled = false;
        }
        else if (isDashing)
        {
            groundStickEnabled = false;
        }
        else if (canMove && inputEnabled)
        {
            HandleRotation();
            HandleMovementForce();
            groundStickEnabled = true;
        }
        else
        {
            HandleIdleOrDisabled();
            groundStickEnabled = true;
        }

        ApplyGroundStick();

        if (!isDashing && !isKnockbacking)
        {
            StopVelocityIfNeeded();
        }

        HandleParticleEffect();
        isJumping = false;
    }

    void HandleParticleEffect()
    {
        if (footParticleSystem == null) return;
        bool isMovingFastEnough = rb.linearVelocity.magnitude > particleSpeedThreshold;
        bool isActuallyMoving = inputMagnitude > particleInputThreshold;
        bool shouldPlay = isGrounded && isMovingFastEnough && isActuallyMoving;

        if (shouldPlay)
        {
            if (!particlesShouldBePlaying)
            {
                footParticleSystem.Play();
                particlesShouldBePlaying = true;
            }
            particleFadeOutTimer = particleFadeOutDuration;
        }
        else
        {
            if (particlesShouldBePlaying)
            {
                particleFadeOutTimer -= Time.fixedDeltaTime;
               
                if (particleFadeOutTimer <= 0)
                {
                    footParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    particlesShouldBePlaying = false;
                }
            }
        }
    }

    void ApplyGroundDrag()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float dragMultiplier = (inputMagnitude < 0.01f) ? 2.0f : 1.0f;
        rb.AddForce(-horizontalVelocity * groundDrag * dragMultiplier, ForceMode.Acceleration);
    }

    void StopVelocityIfNeeded()
    {
        if (isGrounded && inputMagnitude < 0.01f)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (horizontalVelocity.magnitude < stopThreshold)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
        }
    }

    void HandleRotation()
    {
        if (movementInput.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(movementInput.x, movementInput.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleMovementForce()
    {
        if (inputMagnitude > 0.01f)
        {
            Vector3 moveDirection = transform.forward;
            if (isGrounded)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
                Vector3 targetVelocity = moveDirection * currentMoveSpeed;
                Vector3 currentVelocity = rb.linearVelocity;
                Vector3 groundPlaneVelocity = Vector3.ProjectOnPlane(currentVelocity, groundNormal);
                Vector3 velocityChange = targetVelocity - groundPlaneVelocity;
                float accelRate = Mathf.Lerp(deceleration, acceleration, inputMagnitude);
                velocityChange = Vector3.ClampMagnitude(velocityChange, accelRate * Time.fixedDeltaTime);
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                Vector3 targetVelocity = moveDirection * currentMoveSpeed * airControl;
                Vector3 currentVelocity = rb.linearVelocity;
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                Vector3 targetHorizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);
                Vector3 velocityChange = targetHorizontalVelocity - horizontalVelocity;
                velocityChange = Vector3.ClampMagnitude(velocityChange, acceleration * airControl * Time.fixedDeltaTime);
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }
    }

    void HandleIdleOrDisabled()
    {
        if (isGrounded)
        {
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, groundNormal);
            if (horizontalVelocity.sqrMagnitude > 0.01f)
            {
                float decelerationRate = deceleration * 0.8f;
                Vector3 velocityChange = -horizontalVelocity.normalized *
                                        Mathf.Min(horizontalVelocity.magnitude, decelerationRate * Time.fixedDeltaTime);
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }
    }

    void ApplyGroundStick()
    {
        if (isGrounded && groundStickEnabled && !isJumping)
        {
            if (Vector3.Dot(rb.linearVelocity, groundNormal) < 0.1f)
            {
                rb.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);
            }
        }
    }

    void PerformGroundDash()
    {
        Vector3 dashDirection = movementInput.magnitude > 0.1f ?
            movementInput.normalized : transform.forward;
        lastGroundDashDirection = dashDirection;
        isDashing = true;
        dashTimer = groundDashDuration;
        dashCooldownTimer = groundDashCooldown;
        groundStickEnabled = false;
        PlaySound(groundDashSounds, groundDashVolume);
        rb.AddForce(dashDirection * groundDashForce, ForceMode.VelocityChange);
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out groundHit, groundCheckDistance, groundLayer);
        currentPlatform = null;
        if (isGrounded)
        {
            groundNormal = groundHit.normal;
            HandlePlatformParenting(groundHit.transform);
        }
        else
        {
            groundNormal = Vector3.up;
            HandlePlatformParenting(null);
        }
    }

    void HandlePlatformParenting(Transform detectedPlatform)
    {
        bool isOnPlatform = false;
        if(detectedPlatform != null)
        {
            isOnPlatform = detectedPlatform.CompareTag("Platform") || detectedPlatform.GetComponent<MovingPlatform>() != null;
            if(isOnPlatform) currentPlatform = detectedPlatform;
        }

        if (isOnPlatform)
        {
            if (transform.parent != currentPlatform)
            {
                transform.SetParent(currentPlatform, true);
            }
        }
        else
        {
            if (transform.parent != originalParent)
            {
                transform.SetParent(originalParent, true);
            }
        }
    }

    void PlaySound(AudioClip[] clips, float volume)
    {
        if (audioSource != null && clips != null && clips.Length > 0)
        {
            audioSource.ignoreListenerPause = true;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }
    }

    public void DisableInputForKnockback(Vector3 collisionPoint)
    {
        inputEnabled = false;
        canMove = false;
        isKnockbacking = true;
        isDashing = false;
        disableInputTimer = disableInputDuration;
        knockbackTimer = 0f;
        Vector3 horizontalDirection = (transform.position - collisionPoint);
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();
        knockbackDirection = (horizontalDirection + Vector3.up * 0.3f).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockbackDirection * hitKnockbackForce, ForceMode.VelocityChange);
    }

    public void DisableMovement()
    {
        canMove = false;
        inputEnabled = false;
        rawMoveInput = Vector2.zero;
        movementInput = Vector3.zero;
        inputMagnitude = 0f;
    }

    public void EnableMovement()
    {
        canMove = true;
        inputEnabled = true;
    }

    public bool IsGrounded() => isGrounded;
    public bool IsDashing() => isDashing;

    public void SetIsJumping(bool jumping)
    {
        isJumping = jumping;
        if (jumping)
        {
            groundStickEnabled = false;
        }
    }
}