using UnityEngine;
using UnityEngine.InputSystem; // Required
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerInput))] // Required
[RequireComponent(typeof(PlayerMovement))]
public class PlayerJump : MonoBehaviour
{
    // SECTION: Inspector Variables =============================================
    [Header("Input Action Name")]
    [Tooltip("The exact name of the Jump Action defined in the Input Actions asset.")]
    public string jumpActionName = "Jump";

    [Header("Jump Settings (Cut-off Based)")]
    [Tooltip("The initial upward impulse force applied on jump press. This determines the MAX potential height.")]
    public float jumpForce = 20f;
    [Tooltip("Multiplier applied to upward velocity when jump is released early (e.g., 0.5 = cut velocity in half). 0 = stop instantly.")]
    [Range(0f, 1f)] public float jumpCutOffMultiplier = 0.5f;
    [Tooltip("Maximum duration the jump can continue ascending at full force before velocity is cut off, even if held.")]
    public float maxJumpAscentDuration = 0.4f;

    [Header("Jump Apex Smoothing (On Max Duration)")]
    [Tooltip("Enable temporary gravity reduction when jump hits max duration?")]
    public bool applyApexSmoothingOnMaxDuration = true;
    [Tooltip("Gravity multiplier near the apex (e.g., 0.5 = half gravity). 1 = no effect.")]
    [Range(0f, 1f)] public float apexGravityMultiplier = 0.5f;


    [Header("Jump Settings - General")]
    public float doubleJumpForce = 8f;
    public float doubleJumpTimeWindow = 0.2f;

    [Header("Coyote Time")]
    public float coyoteTimeDuration = 0.1f;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.15f;

    [Header("Platform Inertia")]
    public float platformInertiaMultiplier = 1.0f;
    public float inertiaDelay = 0.05f;

    [Header("Air Dash Settings")]
    public float airDashForce = 12f;
    public float airDashDuration = 0.2f;
    public bool overrideVerticalVelocity = false;
    public float dashVerticalBoost = 0f;

    [Header("Sound Arrays")]
    public AudioClip[] jumpSounds;
    public AudioClip[] doubleJumpSounds;
    public AudioClip[] airDashSounds;

    [Header("Sound Volumes")]
    [Range(0f, 1f)] public float jumpVolume = 1f;
    [Range(0f, 1f)] public float doubleJumpVolume = 1f;
    [Range(0f, 1f)] public float airDashVolume = 0.8f;

    // SECTION: Components & State Variables ====================================
    [HideInInspector] public Rigidbody rb;
    private AudioSource audioSource;
    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;

    // Input Action Reference
    private InputAction jumpAction;

    // Internal State
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool isDoubleJumping;
    [HideInInspector] public bool isAirDashing;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    private float timeSinceJumpStarted;
    private bool hasUsedDoubleJump = false;
    private bool hasAirDashed;

    // State for Velocity Cut-off
    private bool jumpCutOffApplied = false;

    // State for Timed Apex Smoothing
    private bool shouldApplyTimedApexSmoothing = false;

    // Input Action states
    private bool canJumpInput = true;

    private bool justJumped = false;
    private Coroutine resetJustJumpedCoroutine;


    // SECTION: Unity Lifecycle Methods =========================================
    void Awake()
    {
        // Get Components & Error Checks
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        if (rb == null) Debug.LogError("Rigidbody component missing!", this);
        if (audioSource == null) Debug.LogError("AudioSource component missing!", this);
        if (playerInput == null) Debug.LogError("PlayerInput component missing!", this);
        if (playerMovement == null) Debug.LogError("PlayerMovement component missing!", this);
        if (playerInput != null && playerInput.actions != null) {
            jumpAction = playerInput.actions.FindAction(jumpActionName);
            if (jumpAction == null) Debug.LogError($"Jump action '{jumpActionName}' not found!", this);
        } else { Debug.LogError("PlayerInput or Actions asset not assigned/found!", this); }
        rb.freezeRotation = true;
    }

    void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed += HandleJumpAction;
            jumpAction.canceled += HandleJumpAction;
            jumpAction.Enable();
        }
    }

    void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= HandleJumpAction;
            jumpAction.canceled -= HandleJumpAction;
        }
    }


    void Update()
    {
        if (playerMovement == null) return;
        bool isGrounded = playerMovement.IsGrounded();

        if (isGrounded && !justJumped) { HandleGroundedState(); }
        else { HandleAirborneState(); }

        // Manage counters
        if (!isGrounded) coyoteTimeCounter -= Time.deltaTime;
        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;
        coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter);
        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter);

        // Track jump time AND check for max ascent duration cut-off
        if (isJumping && !isGrounded && !isDoubleJumping && rb.linearVelocity.y > 0)
        {
             timeSinceJumpStarted += Time.deltaTime;

             // Check for Max Duration
             if(!jumpCutOffApplied && timeSinceJumpStarted >= maxJumpAscentDuration)
             {
                // Trigger Smoothing
                if (applyApexSmoothingOnMaxDuration)
                {
                    shouldApplyTimedApexSmoothing = true;
                }
                ApplyJumpCutoff();
             }
        }
    }

    void FixedUpdate()
    {
        // Apply Apex Smoothing - ONLY if flag is set
        if (shouldApplyTimedApexSmoothing && !playerMovement.IsGrounded() && !isAirDashing)
        {
            float counterGravityForce = Physics.gravity.magnitude * (1f - apexGravityMultiplier);
            rb.AddForce(Vector3.up * counterGravityForce, ForceMode.Acceleration);

            // Stop smoothing when falling
            if (rb.linearVelocity.y <= 0)
            {
                shouldApplyTimedApexSmoothing = false;
            }
        }
    }


    // SECTION: Input System Message Handlers ==================================

    private void HandleJumpAction(InputAction.CallbackContext context)
    {
        if (!canJumpInput || playerMovement == null || isAirDashing) return;

        bool isGrounded = playerMovement.IsGrounded();

        // Handle Press
        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;
            if ((isGrounded || coyoteTimeCounter > 0f) && !isJumping && !justJumped) {
                PerformJump();
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            } else if (!isGrounded && isJumping && !hasUsedDoubleJump && timeSinceJumpStarted >= doubleJumpTimeWindow) {
                PerformDoubleJump();
                jumpBufferCounter = 0f;
            }
        }

        // Handle Release -> Apply Velocity Cut-off
        if (context.canceled)
        {
            ApplyJumpCutoff();
        }
    }

    public void OnAirDash(InputValue value)
    {
         if (!canJumpInput || playerMovement == null) return;
        bool isGrounded = playerMovement.IsGrounded();
        if (value.isPressed && !isGrounded && !hasAirDashed && !isAirDashing)
        {
             ApplyJumpCutoff();
             PerformAirDash();
        }
    }

    // SECTION: State Handling Methods ==========================================
    private void HandleGroundedState() {
        coyoteTimeCounter = coyoteTimeDuration;
        // Check if landing
        if ((isJumping || isDoubleJumping || rb.linearVelocity.y < -0.1f ) && rb.linearVelocity.y <= 0.1f ) {
            isJumping = false;
            isDoubleJumping = false;
            hasUsedDoubleJump = false;
            hasAirDashed = false;
            timeSinceJumpStarted = 0f;
            jumpCutOffApplied = false;
            shouldApplyTimedApexSmoothing = false; // Reset flag
            if (playerMovement != null) playerMovement.SetIsJumping(false);
        }
        // Buffer check
        if (jumpBufferCounter > 0f && canJumpInput && !isJumping) {
            PerformJump();
            jumpBufferCounter = 0f;
        }
     }

    private void HandleAirborneState() { /* Currently empty */ }

    // SECTION: Core Action Methods ==============================================

    void PerformJump() {
        if (!canJumpInput) return;
        SetJustJumpedFlag();
        Vector3 pVel = GetCurrentPlatformVelocity();

        // Apply initial jump force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        StartCoroutine(DelayedInertia(pVel));

        // Update State
        isJumping = true;
        isDoubleJumping = false;
        hasUsedDoubleJump = false;
        timeSinceJumpStarted = 0f;
        jumpCutOffApplied = false;
        shouldApplyTimedApexSmoothing = false; // Reset flag
        hasAirDashed = false;

        // FX
        if (playerAnimation != null) playerAnimation.StartJumpAnimation();
        PlaySound(jumpSounds, jumpVolume);
        if (playerMovement != null) playerMovement.SetIsJumping(true);
     }

    void ApplyJumpCutoff()
    {
        if (!jumpCutOffApplied && isJumping && !isDoubleJumping && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutOffMultiplier, rb.linearVelocity.z);
            jumpCutOffApplied = true;
        }
    }

    void PerformDoubleJump() {
        if (!canJumpInput) return;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.VelocityChange);

        // Update State
        isJumping = true;
        isDoubleJumping = true;
        hasUsedDoubleJump = true;
        jumpCutOffApplied = true;
        shouldApplyTimedApexSmoothing = false; // Reset flag

        // FX
        if (playerAnimation != null) playerAnimation.StartDoubleJumpAnimation();
        PlaySound(doubleJumpSounds, doubleJumpVolume);
     }

    void PerformAirDash()
    {
        if (!canJumpInput || hasAirDashed || isAirDashing) return;

        shouldApplyTimedApexSmoothing = false; // Reset flag

        // Calculate Dash Direction (Same as before, simplified here for brevity)
        InputAction moveAction = playerInput.actions.FindAction("Move");
        Vector2 moveVector = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 inputDirection = new Vector3(moveVector.x, 0, moveVector.y);
        Vector3 dashDirection;
        if (inputDirection.magnitude > 0.1f) {
             Transform cameraTransform = Camera.main?.transform;
             if (cameraTransform != null) {
                 Vector3 cameraForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
                 Vector3 cameraRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
                 dashDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
             } else { dashDirection = transform.forward; } // Fallback if no camera
        } else { dashDirection = transform.forward.normalized; } // Use normalized forward if no input

        // Ensure dashDirection is normalized if needed (it should be from calculations above)
        if (dashDirection == Vector3.zero) dashDirection = transform.forward; // Final fallback


        StartCoroutine(ExecuteAirDash(dashDirection));

        // State & FX
        hasAirDashed = true;
        isAirDashing = true;
        if (playerAnimation != null) playerAnimation.PlayDashAnimation();
        PlaySound(airDashSounds, airDashVolume);
    }

    // ***** FIXED: ExecuteAirDash ensures dashVelocity is assigned *****
    IEnumerator ExecuteAirDash(Vector3 direction)
    {
        float currentYVelocity = rb.linearVelocity.y;
        Vector3 dashVelocity; // Declare here

        if (overrideVerticalVelocity) {
            // Calculate velocity with vertical override
            dashVelocity = direction.normalized * airDashForce; // Use normalized direction
            dashVelocity.y = dashVerticalBoost;
        } else {
            // Calculate velocity preserving some vertical momentum
            dashVelocity = direction.normalized * airDashForce; // Use normalized direction
            dashVelocity.y = currentYVelocity + dashVerticalBoost;
        }

        // Now assign the calculated velocity
        rb.linearVelocity = dashVelocity;

        yield return new WaitForSeconds(airDashDuration);
        isAirDashing = false;
    }

    // --- Utility and Other Methods ---

    // NOTE: This method is IEnumerator, it yields execution.
    // The "not all code paths return a value" warning is likely an IDE error for coroutines.
    IEnumerator DelayedInertia(Vector3 platformVelocityAtJump) {
        yield return new WaitForSeconds(inertiaDelay);
        if (playerMovement != null && !playerMovement.IsGrounded()) {
             Vector3 iF = new Vector3(platformVelocityAtJump.x * platformInertiaMultiplier, 0, platformVelocityAtJump.z * platformInertiaMultiplier);
             rb.AddForce(iF, ForceMode.VelocityChange);
        }
        // No return statement needed here for IEnumerator after yield
    }

    // ***** FIXED: GetCurrentPlatformVelocity ensures a return *****
    private Vector3 GetCurrentPlatformVelocity() {
        Vector3 platformVel = Vector3.zero; // Default value
        if (playerMovement != null && transform.parent != null && transform.parent != playerMovement.originalParent) {
            MovingPlatform p = transform.parent.GetComponent<MovingPlatform>();
            if (p != null) {
                 // Ensure MovingPlatform script has a public GetVelocity() method
                 platformVel = p.GetVelocity();
            }
        }
        return platformVel; // Always return the calculated or default value
    }

    private void PlaySound(AudioClip[] clips, float volume) {
        if (audioSource != null && clips != null && clips.Length > 0) {
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }
    }

    private void SetJustJumpedFlag() {
        justJumped = true;
        if (resetJustJumpedCoroutine != null) StopCoroutine(resetJustJumpedCoroutine);
        // Start the coroutine that resets the flag
        resetJustJumpedCoroutine = StartCoroutine(ResetJustJumpedFlag());
    }

    // NOTE: ResetJustJumpedFlag IS used by SetJustJumpedFlag above.
    // The "unused" warning (IDE0051) is likely an IDE error.
    private IEnumerator ResetJustJumpedFlag() {
        yield return new WaitForSeconds(0.1f);
        justJumped = false;
        resetJustJumpedCoroutine = null;
    }

    public bool IsAirDashing() => isAirDashing;

    public void DisableJump() { canJumpInput = false; }
    public void EnableJump() { canJumpInput = true; }
}