using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System
using System.Collections;

/// <summary>
/// Controls player movement, jumping, dashing, health, animation, and effects.
/// Combines logic from multiple previous components into a single controller.
/// Requires Rigidbody, AudioSource, PlayerInput, and Animator components.
/// </summary>
// SECTION: Required Components =============================================
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerInput))] // Required for Input System
[RequireComponent(typeof(Animator))]     // Required for combined animation logic
public class PlayerController : MonoBehaviour
{
    #region Constants
    private const string HAZARD_TAG = "hazard";
    private const string DEATHBOX_TAG = "deathbox";
    private const string PLATFORM_TAG = "Platform"; // Assuming "Platform" is the correct tag
    #endregion

    #region Inspector Variables - Movement
    [Header("Movement Settings")]
    [Tooltip("Base speed when walking.")]
    [SerializeField] private float characterWalkSpeed = 1.5f;
    [Tooltip("Maximum speed when sprinting.")]
    [SerializeField] private float characterSprintSpeed = 8f;
    [Tooltip("Rate at which the player reaches target speed.")]
    [SerializeField] private float movementAcceleration = 30f;
    [Tooltip("Rate at which the player slows down when stopping.")]
    [SerializeField] private float movementDeceleration = 30f;
    [Tooltip("Friction applied when grounded and not actively moving.")]
    [SerializeField] private float groundDragFactor = 4f;
    [Tooltip("How much control the player has while airborne.")]
    [SerializeField] private float airControlFactor = 0.6f;
    [Tooltip("How quickly the player rotates to face the movement direction.")]
    [SerializeField] private float characterRotationSpeed = 40f;
    [Tooltip("Distance downwards to check for ground.")]
    [SerializeField] private float groundCheckRayDistance = 0.35f;
    [Tooltip("Downward force applied to help stick to slopes.")]
    [SerializeField] private float groundStickinessForce = 3f;
    [Tooltip("Layers considered as ground.")]
    [SerializeField] private LayerMask groundDetectionLayer = ~0; // Default to everything

    [Header("Movement - Input Thresholds")]
    [Tooltip("Minimum input magnitude to start walking.")]
    [SerializeField] private float walkInputThreshold = 0.01f;
    [Tooltip("Minimum input magnitude to start sprinting.")]
    [SerializeField] private float sprintInputThreshold = 0.55f;
    [Tooltip("How long input is disabled after taking knockback.")]
    [SerializeField] private float postKnockbackInputDisableDuration = 0.1f;
    [Tooltip("Force applied when hit by a hazard.")]
    [SerializeField] private float hazardKnockbackForce = 4f;
    [Tooltip("Velocity magnitude below which the player snaps to zero horizontal speed when grounded and stopping.")]
    [SerializeField] private float stopVelocityThreshold = 0.02f;
    #endregion

    #region Inspector Variables - Jump & Air Actions
    [Header("Jump Settings (Cut-off Based)")]
    [Tooltip("The initial upward impulse force applied on jump press. Determines MAX potential height.")]
    [SerializeField] private float initialJumpForce = 7f;
    [Tooltip("Multiplier applied to upward velocity when jump is released early (e.g., 0.5 = cut velocity in half). 0 = stop instantly.")]
    [Range(0f, 1f)] [SerializeField] private float jumpEarlyReleaseCutoffMultiplier = 0.5f;
    [Tooltip("Maximum duration the jump can continue ascending at full force before velocity is cut off, even if held.")]
    [SerializeField] private float maxJumpAscentTime = 0.5f;

    [Header("Jump Apex Smoothing (On Max Duration)")]
    [Tooltip("Enable temporary gravity reduction when jump hits max duration?")]
    [SerializeField] private bool enableApexSmoothingOnMaxDuration = true;
    [Tooltip("Gravity multiplier near the apex (e.g., 0.5 = half gravity). 1 = no effect.")]
    [Range(0f, 1f)] [SerializeField] private float jumpApexGravityScale = 0.5f;

    [Header("Jump Settings - General")]
    [Tooltip("Upward force applied for a double jump.")]
    [SerializeField] private float doubleJumpImpulse = 4f;
    [Tooltip("Minimum time after initial jump before double jump is allowed.")]
    [SerializeField] private float doubleJumpActivationDelay = 0.12f;

    [Header("Jump Assistance - Coyote Time")]
    [Tooltip("Duration after leaving ground where jump is still possible.")]
    [SerializeField] private float coyoteTimeWindow = 0.3f;

    [Header("Jump Assistance - Jump Buffer")]
    [Tooltip("Duration before landing where jump input is registered.")]
    [SerializeField] private float jumpInputBufferWindow = 0.15f;

    [Header("Platform Interaction")]
    [Tooltip("Multiplier for inheriting horizontal velocity from moving platforms when jumping.")]
    [SerializeField] private float platformVelocityInheritanceMultiplier = 1.0f;
    [Tooltip("Delay before applying inherited platform velocity after jumping.")]
    [SerializeField] private float platformInertiaApplicationDelay = 0.05f;

    [Header("Air Dash Settings")]
    [Tooltip("Force applied during the air dash.")]
    [SerializeField] private float airDashImpulse = 10f;
    [Tooltip("Duration of the air dash.")]
    [SerializeField] private float airDashActiveDuration = 0.2f;
    [Tooltip("Should air dash set vertical velocity to 'Dash Vertical Boost'?")]
    [SerializeField] private bool airDashOverridesVerticalVelocity = true;
    [Tooltip("Vertical velocity applied during air dash (if overriding or added).")]
    [SerializeField] private float airDashVerticalVelocityBoost = 0.5f;
    [Tooltip("Minimum time the player must be airborne before an air dash can be initiated (general case).")]
    [SerializeField] private float minimumAirTimeForAirDash = 0.1f;
    // *** NEW VARIABLE ADDED ***
    [Tooltip("Minimum time the player must be airborne *specifically after finishing a ground dash* before an air dash can be initiated.")]
    [SerializeField] private float minAirTimeAfterGroundDashForAirDash = 0.15f; // Example value, adjust as needed

    [Header("Ground Dash Settings")]
    [Tooltip("Force applied during the ground dash.")]
    [SerializeField] private float groundDashImpulse = 6f;
    [Tooltip("Duration of the ground dash.")]
    [SerializeField] private float groundDashActiveDuration = 0.3f;
    [Tooltip("Cooldown time between ground dashes.")]
    [SerializeField] private float groundDashCooldownTime = 0.4f;
    [Tooltip("Minimum ground speed required to initiate a ground dash.")]
    [SerializeField] private float groundDashMinSpeedRequirement = 3.5f;
    #endregion

    #region Inspector Variables - Health & Damage
    [Header("Health & Invulnerability")]
    [Tooltip("Duration of invulnerability after taking damage.")]
    [SerializeField] private float damageInvulnerabilityTime = 1.5f;
    [Tooltip("How fast the player model blinks while invulnerable.")]
    [SerializeField] private float invulnerabilityBlinkRate = 0.1f;
    [Tooltip("Amount of damage taken from standard hazards.")]
    [SerializeField] private int standardHazardDamageAmount = 1;
    [Tooltip("Delay after death before Rigidbody becomes kinematic.")]
    [SerializeField] private float deathKinematicActivationDelay = 0.7f;
    #endregion

    #region Inspector Variables - Animation
    [Header("Animation State Names")]
    [SerializeField] private string animStateIdle = "idle";
    [SerializeField] private string animStateWalk = "walk";
    [SerializeField] private string animStateSprint = "sprint";
    [SerializeField] private string animStateJump = "jump";
    [SerializeField] private string animStateDoubleJump = "doublejump";
    [SerializeField] private string animStateFall = "fall";
    [SerializeField] private string animStateHurt = "hurt";
    [SerializeField] private string animStateDie = "die";
    [SerializeField] private string animStateEmote = "emote-yes";
    [SerializeField] private string animStateAirDash = "drive";
    [SerializeField] private string animStateGroundDash = "wheelchair-move-back";

    [Header("Animation Durations (Used for State Control)")]
    [Tooltip("Duration override for the hurt animation (used for disabling controls).")]
    [SerializeField] private float hurtAnimationLockoutDuration = 0.5f;
    [Tooltip("Duration override for the emote animation (used for disabling controls).")]
    [SerializeField] private float emoteAnimationLockoutDuration = 1.0f;
    #endregion

    #region Inspector Variables - Effects & Sound
    [Header("Visual Effects")]
    [Tooltip("Prefab instantiated when the player gets hurt.")]
    [SerializeField] private GameObject hurtEffectObjectPrefab = null;
    [Tooltip("Lifetime of the hurt effect prefab.")]
    [SerializeField] private float hurtEffectObjectDuration = 1f;
    [Tooltip("Particle system for foot movement (e.g., dust).")]
    [SerializeField] private ParticleSystem footstepParticleEffect = null;
    [Tooltip("Minimum speed to trigger footstep particles.")]
    [SerializeField] private float footstepParticleMinSpeed = 6.8f;
    [Tooltip("Minimum input magnitude to trigger footstep particles (prevents playing while sliding).")]
    [SerializeField] private float footstepParticleMinInput = 0.3f;
    [Tooltip("How quickly footstep particles fade out when stopping.")]
    [SerializeField] private float footstepParticleFadeTime = 0.5f;

    [Header("Sound Effects - Jump & Air")]
    [SerializeField] private AudioClip[] jumpSoundClips;
    [SerializeField] private AudioClip[] doubleJumpSoundClips;
    [SerializeField] private AudioClip[] airDashSoundClips;
    [Range(0f, 1f)] [SerializeField] private float jumpSoundVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float doubleJumpSoundVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float airDashSoundVolume = 0.8f;

    [Header("Sound Effects - Movement")]
    [SerializeField] private AudioClip[] groundDashSoundClips;
    [Range(0f, 1f)] [SerializeField] private float groundDashSoundVolume = 0.8f;

    [Header("Sound Effects - Health")]
    [SerializeField] private AudioClip[] hurtSoundClips;
    [SerializeField] private AudioClip[] dieSoundClips;
    [Range(0f, 1f)] [SerializeField] private float hurtSoundVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float dieSoundVolume = 1f;

    [Header("Input Action Names")]
    [Tooltip("The exact name of the Jump Action defined in the Input Actions asset.")]
    [SerializeField] private string jumpActionIdentifier = "Jump";
    [Tooltip("The exact name of the Move Action defined in the Input Actions asset.")]
    [SerializeField] private string moveActionIdentifier = "Move";
    [Tooltip("The exact name of the Air Dash Action defined in the Input Actions asset.")]
    [SerializeField] private string airDashActionIdentifier = "AirDash";
    [Tooltip("The exact name of the Ground Dash Action defined in the Input Actions asset.")]
    [SerializeField] private string groundDashActionIdentifier = "GroundDash";
    #endregion


    #region Components & External References
    [HideInInspector] public Rigidbody controllerRigidbody;
    private AudioSource controllerAudioSource;
    private PlayerInput controllerPlayerInput;
    private Animator characterAnimator;
    private Renderer bodyMeshRenderer;
    private Renderer headMeshRenderer;
    private Camera mainCamera;
    private HealthController sceneHealthController;
    private PlayerSpawn scenePlayerSpawn;
    #endregion

    #region Input Action References
    private InputAction moveInputAction;
    private InputAction jumpInputAction;
    private InputAction airDashInputAction;
    private InputAction groundDashInputAction;
    #endregion

    #region Internal State Variables - Core
    [HideInInspector] public bool isCharacterGrounded { get; private set; }
    private bool isCharacterDead = false;
    private bool canProcessGeneralInput = true;
    private bool canCharacterMove = true;
    private bool canProcessJumpInput = true;
    private bool isInputTemporarilyDisabled = false;
    private float temporaryInputDisableTimer = 0f;
    #endregion

    #region Internal State Variables - Movement
    private Vector2 currentRawMoveInput;
    private Vector3 calculatedMovementInputVector;
    [HideInInspector] public float currentInputMagnitude { get; private set; }
    private float currentTargetMoveSpeed;
    private RaycastHit currentGroundHitInfo;
    private Vector3 currentGroundNormal = Vector3.up;
    private Transform currentAttachedPlatform;
    [HideInInspector] public Transform originalCharacterParent { get; private set; }
    private bool applyGroundStickiness = true;
    private bool isPerformingGroundDash = false;
    private float groundDashActiveTimer = 0f;
    private float groundDashCooldownTimer = 0f;
    private bool recentlyFinishedGroundDash = false; // Flag set when ground dash timer ends, reset on landing
    #endregion

    #region Internal State Variables - Jump & Air
    [HideInInspector] public bool isPerformingJump { get; private set; }
    [HideInInspector] public bool isPerformingDoubleJump { get; private set; }
    [HideInInspector] public bool isPerformingAirDash { get; private set; }
    private float coyoteTimeRemaining = 0f;
    private float jumpBufferRemaining = 0f;
    private float timeSinceJumpButtonPress;
    private bool jumpHasBeenCutOff = false;
    private bool hasPerformedDoubleJumpThisAirborne = false;
    private bool hasPerformedAirDashThisAirborne = false;
    private bool applyApexSmoothing = false;
    private bool recentlyPerformedJump = false;
    private Coroutine resetRecentlyPerformedJumpCoroutine;
    private Coroutine airDashCoroutine;
    private Coroutine makeKinematicCoroutine;
    private Coroutine platformInertiaCoroutine;
    private float timeSinceLastGrounded = 0f; // Tracks total time airborne
    #endregion

    #region Internal State Variables - Health
    private bool isDamageImmune = false;
    private float damageImmunityTimer = 0f;
    private bool isBeingKnockedBack = false;
    private float knockbackActiveTimer = 0f;
    private float baseKnockbackDuration = 0.3f;
    #endregion

    #region Internal State Variables - Animation & Effects
    private bool isPlayingHurtAnim = false;
    private float hurtAnimEndTime;
    private bool isPlayingEmoteAnim = false;
    private float emoteAnimEndTime;
    private bool footstepParticlesActive = false;
    private float footstepParticleFadeOutCounter = 0f;
    #endregion

    #region Unity Lifecycle Methods
    void Awake()
    {
        controllerRigidbody = GetComponent<Rigidbody>();
        controllerAudioSource = GetComponent<AudioSource>();
        controllerPlayerInput = GetComponent<PlayerInput>();
        characterAnimator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (controllerRigidbody == null) Debug.LogError("PlayerController: Rigidbody component missing!", this);
        if (controllerAudioSource == null) Debug.LogError("PlayerController: AudioSource component missing!", this);
        if (controllerPlayerInput == null) Debug.LogError("PlayerController: PlayerInput component missing!", this);
        if (characterAnimator == null) Debug.LogError("PlayerController: Animator component missing!", this);
        if (mainCamera == null) Debug.LogError("PlayerController: Main Camera not found in scene!", this);

        controllerRigidbody.freezeRotation = true;
        controllerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        controllerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        bodyMeshRenderer = transform.Find("body-mesh")?.GetComponent<Renderer>();
        headMeshRenderer = transform.Find("head-mesh")?.GetComponent<Renderer>();
        if (bodyMeshRenderer == null || headMeshRenderer == null) Debug.LogWarning("PlayerController: Could not find body-mesh or head-mesh Renderers for blinking.", this);

        originalCharacterParent = transform.parent;

        GameObject healthCanvasObject = GameObject.Find("Canva-HP");
        if (healthCanvasObject != null) { sceneHealthController = healthCanvasObject.GetComponent<HealthController>(); if (sceneHealthController == null) Debug.LogError("PlayerController: HealthController component not found on Canva-HP object!", this); }
        else { Debug.LogWarning("PlayerController: Could not find Canva-HP object in scene! Health system inactive.", this); }

        InitializeInputActions();
        InitializeFootstepParticles();
    }

    void OnEnable() { SubscribeInputActions(); ResetCharacterStateOnEnable(); }
    void OnDisable() { UnsubscribeInputActions(); StopAllCoroutines(); }

    void Update()
    { if (isCharacterDead) return; HandleTimers(); DetermineInputAvailability(); ProcessBufferedInputs(); UpdateNonPhysicsStates(); }

    void FixedUpdate()
    { if (isCharacterDead) return; HandleGrounding(); UpdateTimeSinceLastGrounded(); ApplyPhysicsBasedForces(); HandleMovementPhysics(); }
    #endregion

    #region Initialization & Setup
    private void InitializeInputActions()
    { if (controllerPlayerInput == null || controllerPlayerInput.actions == null) { Debug.LogError("PlayerController: PlayerInput or Actions asset not assigned/found!", this); this.enabled = false; return; } moveInputAction = controllerPlayerInput.actions.FindAction(moveActionIdentifier); jumpInputAction = controllerPlayerInput.actions.FindAction(jumpActionIdentifier); airDashInputAction = controllerPlayerInput.actions.FindAction(airDashActionIdentifier); groundDashInputAction = controllerPlayerInput.actions.FindAction(groundDashActionIdentifier); if (moveInputAction == null) Debug.LogError($"PlayerController: Move action '{moveActionIdentifier}' not found!", this); if (jumpInputAction == null) Debug.LogError($"PlayerController: Jump action '{jumpActionIdentifier}' not found!", this); if (airDashInputAction == null) Debug.LogWarning($"PlayerController: AirDash action '{airDashActionIdentifier}' not found (optional).", this); if (groundDashInputAction == null) Debug.LogWarning($"PlayerController: GroundDash action '{groundDashActionIdentifier}' not found (optional).", this); }
    private void SubscribeInputActions()
    { if (jumpInputAction != null) { jumpInputAction.performed += HandleJumpInput; jumpInputAction.canceled += HandleJumpInput; jumpInputAction.Enable(); } if (moveInputAction != null) { moveInputAction.performed += ProcessMoveInputCallback; moveInputAction.canceled += ProcessMoveInputCallback; moveInputAction.Enable(); } if (airDashInputAction != null) { airDashInputAction.performed += HandleAirDashInput; airDashInputAction.Enable(); } if (groundDashInputAction != null) { groundDashInputAction.performed += HandleGroundDashInput; groundDashInputAction.Enable(); } }
    private void UnsubscribeInputActions()
    { if (jumpInputAction != null) { jumpInputAction.performed -= HandleJumpInput; jumpInputAction.canceled -= HandleJumpInput; } if (moveInputAction != null) { moveInputAction.performed -= ProcessMoveInputCallback; moveInputAction.canceled -= ProcessMoveInputCallback; } if (airDashInputAction != null) { airDashInputAction.performed -= HandleAirDashInput; } if (groundDashInputAction != null) { groundDashInputAction.performed -= HandleGroundDashInput; } }
    private void ResetCharacterStateOnEnable()
    { SetCharacterRenderersEnabled(true); isCharacterDead = false; isDamageImmune = false; canProcessGeneralInput = true; canCharacterMove = true; canProcessJumpInput = true; isPlayingHurtAnim = false; isPlayingEmoteAnim = false; isPerformingAirDash = false; isPerformingGroundDash = false; isBeingKnockedBack = false; isInputTemporarilyDisabled = false; timeSinceLastGrounded = 0f; recentlyFinishedGroundDash = false; // Reset this flag
        if (controllerRigidbody != null) { controllerRigidbody.isKinematic = false; controllerRigidbody.linearVelocity = Vector3.zero; controllerRigidbody.angularVelocity = Vector3.zero; } ResetAirborneStateFlags(); currentRawMoveInput = Vector2.zero; calculatedMovementInputVector = Vector3.zero; currentInputMagnitude = 0f; }
    private void InitializeFootstepParticles()
    { if (footstepParticleEffect != null) { var mainModule = footstepParticleEffect.main; mainModule.loop = true; mainModule.playOnAwake = false; footstepParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); footstepParticlesActive = false; } else { Debug.LogWarning("PlayerController: Footstep particle system not assigned!", this); } }
    #endregion

    #region Update Logic Methods
    private void HandleTimers()
    {
        float dt = Time.deltaTime;
        if (isPerformingGroundDash)
        {
            groundDashActiveTimer -= dt;
            if (groundDashActiveTimer <= 0)
            {
                isPerformingGroundDash = false;
                applyGroundStickiness = true;
                recentlyFinishedGroundDash = true; // Set flag when ground dash ends
                // IMPORTANT: timeSinceLastGrounded starts counting AFTER this frame in FixedUpdate if player leaves ground
            }
        }
        if (groundDashCooldownTimer > 0) { groundDashCooldownTimer -= dt; }
        if (isDamageImmune) { damageImmunityTimer -= dt; }
        if (isBeingKnockedBack) { knockbackActiveTimer += dt; if (knockbackActiveTimer >= baseKnockbackDuration) { isBeingKnockedBack = false; } }
        /* Emote timer handled by Invoke */
        if (isPlayingHurtAnim && Time.time >= hurtAnimEndTime) { isPlayingHurtAnim = false; }
        if (isInputTemporarilyDisabled) { temporaryInputDisableTimer -= dt; if (temporaryInputDisableTimer <= 0) { isInputTemporarilyDisabled = false; } }
        if (jumpBufferRemaining > 0) { jumpBufferRemaining -= dt; }
    }
    private void DetermineInputAvailability()
    { bool blockAllInput = PauseSystem.IsGamePaused || isCharacterDead || isPlayingHurtAnim || isPlayingEmoteAnim || isInputTemporarilyDisabled || isBeingKnockedBack; canProcessGeneralInput = !blockAllInput; canCharacterMove = !blockAllInput; canProcessJumpInput = canProcessGeneralInput && !isPerformingAirDash; }
    private void ProcessBufferedInputs() { /* Jump buffer used in TransitionToGroundedState */ }
    private void UpdateNonPhysicsStates() { UpdateJumpAscentState(); UpdateInvulnerabilityVisuals(); UpdateCharacterAnimations(); UpdateFootstepParticles(); }
    #endregion

    #region FixedUpdate Logic Methods
    private void HandleGrounding()
    {
        bool wasGrounded = isCharacterGrounded;
        CheckCharacterGrounded();

        if (isCharacterGrounded)
        {
            if (!wasGrounded && !recentlyPerformedJump)
            {
                TransitionToGroundedState();
            }
            HandlePlatformParenting(currentGroundHitInfo.transform);
        }
        else // Is Airborne
        {
            if (wasGrounded) // Just became airborne
            {
                TransitionToAirborneState(); // This primarily handles unparenting now
                // timeSinceLastGrounded starts incrementing in UpdateTimeSinceLastGrounded
            }
            HandlePlatformParenting(null); // Ensure unparented if airborne
        }
    }

    private void UpdateTimeSinceLastGrounded()
    {
        if (!isCharacterGrounded)
        {
            timeSinceLastGrounded += Time.fixedDeltaTime; // Increment air timer
            if (coyoteTimeRemaining > 0f)
            {
                coyoteTimeRemaining -= Time.fixedDeltaTime;
            }
            else
            {
                coyoteTimeRemaining = 0f;
            }
        }
        // No 'else' needed, timer automatically stays at value when grounded
        // It gets reset to 0 explicitly in TransitionToGroundedState
    }
    private void ApplyPhysicsBasedForces() { ApplyGroundStickinessForce(); ApplyJumpApexSmoothingForce(); }
    private void HandleMovementPhysics() { if (isBeingKnockedBack) { } else if (isPerformingGroundDash) { ApplyGroundDragDuringDash(); } else if (canCharacterMove) { CalculateMovementVectorFromInput(); ApplyCharacterRotation(); ApplyMovementForce(); ApplyIdleDecelerationAndDrag(); SnapToZeroVelocityWhenStopping(); } else { ApplyIdleDecelerationAndDrag(); SnapToZeroVelocityWhenStopping(); } }
    #endregion

    #region Input System Callback Handlers
    private void ProcessMoveInputCallback(InputAction.CallbackContext context)
    { if (isCharacterDead) { currentRawMoveInput = Vector2.zero; return; } currentRawMoveInput = context.ReadValue<Vector2>(); }
    private void HandleJumpInput(InputAction.CallbackContext context)
    { if (isCharacterDead || !canProcessGeneralInput || !canProcessJumpInput) return; if (context.performed) { jumpBufferRemaining = jumpInputBufferWindow; if ((isCharacterGrounded || coyoteTimeRemaining > 0f) && !isPerformingJump && !recentlyPerformedJump) { ExecutePrimaryJump(); jumpBufferRemaining = 0f; coyoteTimeRemaining = 0f; } else if (!isCharacterGrounded && isPerformingJump && !hasPerformedDoubleJumpThisAirborne && timeSinceJumpButtonPress >= doubleJumpActivationDelay) { ExecuteDoubleJump(); jumpBufferRemaining = 0f; } } else if (context.canceled) { ApplyJumpCutoffVelocity(); } }

    // *** MODIFIED Air Dash Input Handling ***
    private void HandleAirDashInput(InputAction.CallbackContext context)
    {
        if (!context.performed || isCharacterDead || !canProcessGeneralInput || !canProcessJumpInput) return;

        // Check base conditions first
        bool baseConditionsMet = !isCharacterGrounded &&
                                !hasPerformedAirDashThisAirborne &&
                                !isPerformingAirDash;

        if (baseConditionsMet)
        {
            // Determine the required air time based on whether we just finished a ground dash
            float requiredAirTime;
            if (recentlyFinishedGroundDash)
            {
                // Use the specific threshold for post-ground dash air dash
                requiredAirTime = minAirTimeAfterGroundDashForAirDash;
            }
            else
            {
                // Use the general minimum air time for regular jumps/falls
                requiredAirTime = minimumAirTimeForAirDash;
            }

            // Check if enough time has passed since leaving the ground
            bool timeConditionMet = timeSinceLastGrounded >= requiredAirTime;

            if (timeConditionMet)
            {
                // All conditions met, execute the dash
                ExecuteAirDash();
            }
            else // Time condition failed
            {
                LogAirDashFailureReason(requiredAirTime);
            }
        }
        else // Base conditions failed
        {
            LogAirDashFailureReason(recentlyFinishedGroundDash ? minAirTimeAfterGroundDashForAirDash : minimumAirTimeForAirDash); // Pass potential required time for logging consistency
        }
    }

    // Helper method for logging air dash failures
    private void LogAirDashFailureReason(float requiredAirTime)
    {
        if (!isCharacterGrounded) // Only log failures if we are actually airborne
        {
            string reason = "";
            if (hasPerformedAirDashThisAirborne) reason += "Already dashed this airtime. ";
            if (isPerformingAirDash) reason += "Currently dashing. ";

            // Check the specific time condition failure
            if (timeSinceLastGrounded < requiredAirTime)
            {
                string context = recentlyFinishedGroundDash ? " (after ground dash)" : " (general)";
                reason += $"Air time too short{context} ({timeSinceLastGrounded:F2}s < {requiredAirTime:F2}s). ";
            }
            // Check if base conditions (checked before time) might also be the cause if reason is still empty
            if (string.IsNullOrEmpty(reason) && isCharacterGrounded) reason += "Character is grounded. "; // Should not happen if we only log when airborne, but as safety check.
            // Note: !canProcessJumpInput covers other general input locks (hurt, emote etc.)

            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log($"HandleAirDashInput: Dash conditions NOT MET. Reason(s): {reason} Time: {Time.time}");
            }
            // If no specific reason logged but conditions failed, it might be due to general input locks
            // (canProcessJumpInput being false), which are checked at the very start. No need to log these explicitly.
        }
    }
    // *** END MODIFIED Air Dash Handling ***

    private void HandleGroundDashInput(InputAction.CallbackContext context)
    { if (!context.performed || isCharacterDead || !canProcessGeneralInput || !canCharacterMove) return; if (isCharacterGrounded && !isPerformingGroundDash && groundDashCooldownTimer <= 0f && controllerRigidbody.linearVelocity.magnitude >= groundDashMinSpeedRequirement) { ExecuteGroundDash(); } }
    #endregion

    #region State Handling & Transitions
    private void TransitionToGroundedState()
    {
        coyoteTimeRemaining = coyoteTimeWindow;
        timeSinceLastGrounded = 0f; // Reset air time upon landing
        recentlyFinishedGroundDash = false; // Clear the flag upon landing

        if (controllerRigidbody.linearVelocity.y <= 0.1f)
        {
            ResetAirborneStateFlags(); // Reset jumps, air dash availability etc.
        }

        // Check for buffered jump
        if (jumpBufferRemaining > 0f && canProcessJumpInput && !isPerformingJump && !recentlyPerformedJump)
        {
            ExecutePrimaryJump();
            jumpBufferRemaining = 0f;
        }
    }
    private void TransitionToAirborneState()
    {
        // This function now primarily handles unparenting from platforms
        HandlePlatformParenting(null);
        // The timeSinceLastGrounded timer starts incrementing in UpdateTimeSinceLastGrounded()
    }
    private void ResetAirborneStateFlags()
    { isPerformingJump = false; isPerformingDoubleJump = false; hasPerformedDoubleJumpThisAirborne = false; hasPerformedAirDashThisAirborne = false; timeSinceJumpButtonPress = 0f; jumpHasBeenCutOff = false; applyApexSmoothing = false; isPerformingAirDash = false; applyGroundStickiness = true; if (airDashCoroutine != null) { StopCoroutine(airDashCoroutine); airDashCoroutine = null; if(controllerRigidbody != null) controllerRigidbody.useGravity = true; } }
    #endregion

    #region Core Action Execution Methods
    void ExecutePrimaryJump()
    { if (!canProcessJumpInput) return; FlagRecentlyPerformedJump(); Vector3 pVel = GetCurrentAttachedPlatformVelocity(); controllerRigidbody.linearVelocity = new Vector3(controllerRigidbody.linearVelocity.x, 0, controllerRigidbody.linearVelocity.z); controllerRigidbody.AddForce(Vector3.up * initialJumpForce, ForceMode.VelocityChange); if (platformInertiaCoroutine != null) StopCoroutine(platformInertiaCoroutine); platformInertiaCoroutine = StartCoroutine(ApplyDelayedPlatformInertia(pVel)); isPerformingJump = true; isPerformingDoubleJump = false; hasPerformedDoubleJumpThisAirborne = false; timeSinceJumpButtonPress = 0f; jumpHasBeenCutOff = false; applyApexSmoothing = false; hasPerformedAirDashThisAirborne = false; applyGroundStickiness = false;
        // recentlyFinishedGroundDash = false; // No, don't reset this here, it's reset on LANDING. A jump *during* the recentlyFinished period is still subject to the special air dash timer.
        PlayRandomSound(jumpSoundClips, jumpSoundVolume);
    }
    void ApplyJumpCutoffVelocity()
    { if (!jumpHasBeenCutOff && isPerformingJump && !isPerformingDoubleJump && controllerRigidbody.linearVelocity.y > 0) { controllerRigidbody.linearVelocity = new Vector3(controllerRigidbody.linearVelocity.x, controllerRigidbody.linearVelocity.y * jumpEarlyReleaseCutoffMultiplier, controllerRigidbody.linearVelocity.z); jumpHasBeenCutOff = true; applyApexSmoothing = false; } }
    void ExecuteDoubleJump()
    { if (!canProcessJumpInput) return; controllerRigidbody.linearVelocity = new Vector3(controllerRigidbody.linearVelocity.x, 0, controllerRigidbody.linearVelocity.z); controllerRigidbody.AddForce(Vector3.up * doubleJumpImpulse, ForceMode.VelocityChange); isPerformingDoubleJump = true; hasPerformedDoubleJumpThisAirborne = true; jumpHasBeenCutOff = true; applyApexSmoothing = false; isPerformingAirDash = false;
        // recentlyFinishedGroundDash = false; // No, don't reset this here either.
        PlayRandomSound(doubleJumpSoundClips, doubleJumpSoundVolume);
    }
    void ExecuteAirDash()
    { if (!canProcessJumpInput || isPerformingAirDash) return; applyApexSmoothing = false; ApplyJumpCutoffVelocity(); Vector3 dashDirection = CalculateAirDashDirection(); if (airDashCoroutine != null) StopCoroutine(airDashCoroutine); airDashCoroutine = StartCoroutine(AirDashCoroutine(dashDirection)); hasPerformedAirDashThisAirborne = true; isPerformingAirDash = true; isPerformingJump = true; // Treat air dash as ending the initial jump phase
        isPerformingDoubleJump = false; // Cannot double jump after air dash usually
        jumpHasBeenCutOff = true; // Air dash overrides vertical momentum
        PlayRandomSound(airDashSoundClips, airDashSoundVolume);
    }
    private IEnumerator AirDashCoroutine(Vector3 direction)
    { float currentYVelocity = controllerRigidbody.linearVelocity.y; Vector3 dashVelocity; if (airDashOverridesVerticalVelocity) { dashVelocity = direction.normalized * airDashImpulse; dashVelocity.y = airDashVerticalVelocityBoost; } else { dashVelocity = direction.normalized * airDashImpulse; dashVelocity.y = Mathf.Max(currentYVelocity, 0) + airDashVerticalVelocityBoost; } controllerRigidbody.linearVelocity = dashVelocity; controllerRigidbody.useGravity = false; yield return new WaitForSeconds(airDashActiveDuration); if (controllerRigidbody != null) { controllerRigidbody.useGravity = true; } isPerformingAirDash = false; airDashCoroutine = null; }
    void ExecuteGroundDash()
    { CalculateMovementVectorFromInput(); Vector3 dashDirection = calculatedMovementInputVector.magnitude > 0.1f ? calculatedMovementInputVector.normalized : transform.forward; isPerformingGroundDash = true; recentlyFinishedGroundDash = false; // Starting a new ground dash clears the flag
        groundDashActiveTimer = groundDashActiveDuration; groundDashCooldownTimer = groundDashCooldownTime; applyGroundStickiness = false; Vector3 dashVelocity = dashDirection * groundDashImpulse; dashVelocity.y = controllerRigidbody.linearVelocity.y; controllerRigidbody.linearVelocity = dashVelocity; PlayRandomSound(groundDashSoundClips, groundDashSoundVolume);
    }
    public void ProcessDamageTaken(Vector3 damageSourcePosition)
    { if (isDamageImmune || isCharacterDead) return; if (sceneHealthController != null) { bool wasAlive = sceneHealthController.Health > 0; sceneHealthController.TakeDamage(standardHazardDamageAmount); Debug.Log($"PlayerController: Took {standardHazardDamageAmount} damage. Health: {sceneHealthController.Health}"); if (wasAlive && sceneHealthController.Health <= 0) { TriggerCharacterDeath(); return; } } else { Debug.LogWarning("PlayerController: Scene Health Controller not found, cannot apply damage.", this); } PlayRandomSound(hurtSoundClips, hurtSoundVolume); TriggerHurtAnimation(); ActivateDamageImmunity(); ApplyKnockbackForce(damageSourcePosition); SpawnHurtEffect(transform.position); }
    void TriggerCharacterDeath()
    { if (isCharacterDead) return; Debug.Log("PlayerController: TriggerCharacterDeath() called."); isCharacterDead = true; isDamageImmune = false; canProcessGeneralInput = false; canCharacterMove = false; canProcessJumpInput = false; isPerformingAirDash = false; isPerformingGroundDash = false; applyApexSmoothing = false; applyGroundStickiness = false; isBeingKnockedBack = false; isInputTemporarilyDisabled = false; recentlyFinishedGroundDash = false; if (airDashCoroutine != null) StopCoroutine(airDashCoroutine); if (platformInertiaCoroutine != null) StopCoroutine(platformInertiaCoroutine); if (resetRecentlyPerformedJumpCoroutine != null) StopCoroutine(resetRecentlyPerformedJumpCoroutine); if (makeKinematicCoroutine != null) StopCoroutine(makeKinematicCoroutine); isPlayingHurtAnim = false; isPlayingEmoteAnim = false; CancelInvoke(nameof(ResetEmoteAnimationFlag)); PlayAnimationState(animStateDie); if (controllerRigidbody != null) { controllerRigidbody.linearVelocity = Vector3.zero; controllerRigidbody.angularVelocity = Vector3.zero; controllerRigidbody.useGravity = true; makeKinematicCoroutine = StartCoroutine(MakeRigidbodyKinematicAfterDelay(deathKinematicActivationDelay)); } PlayRandomSound(dieSoundClips, dieSoundVolume); SetCharacterRenderersEnabled(true); StopFootstepParticles(); if (scenePlayerSpawn == null) { scenePlayerSpawn = FindFirstObjectByType<PlayerSpawn>(); } if (scenePlayerSpawn != null) { scenePlayerSpawn.RespawnPlayer(); } else { Debug.LogError("PlayerController: PlayerSpawn object not found in scene! Cannot respawn."); } }
    #endregion

    #region Movement & Physics Logic
    void CheckCharacterGrounded()
    { isCharacterGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out currentGroundHitInfo, groundCheckRayDistance, groundDetectionLayer, QueryTriggerInteraction.Ignore); currentGroundNormal = isCharacterGrounded ? currentGroundHitInfo.normal : Vector3.up; }
    void ApplyGroundStickinessForce()
    { if (isCharacterGrounded && applyGroundStickiness && !isPerformingJump && !isPerformingDoubleJump && !recentlyPerformedJump && !isPerformingGroundDash) { if (Vector3.Dot(controllerRigidbody.linearVelocity, currentGroundNormal) < 0.1f) { controllerRigidbody.AddForce(-currentGroundNormal * groundStickinessForce, ForceMode.Acceleration); } } }
    void ApplyJumpApexSmoothingForce()
    { if (applyApexSmoothing && !isCharacterGrounded && !isPerformingAirDash && controllerRigidbody.linearVelocity.y > 0) { float counterGravity = Physics.gravity.magnitude * (1f - jumpApexGravityScale); controllerRigidbody.AddForce(Vector3.up * counterGravity, ForceMode.Acceleration); } else if (controllerRigidbody.linearVelocity.y <= 0) { applyApexSmoothing = false; } }
    void CalculateMovementVectorFromInput()
    { if (mainCamera == null) { calculatedMovementInputVector = Vector3.zero; currentInputMagnitude = 0f; currentTargetMoveSpeed = 0f; return; } Vector3 camF = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized; Vector3 camR = Vector3.Scale(mainCamera.transform.right, new Vector3(1, 0, 1)).normalized; Vector3 worldInputDir = (camF * currentRawMoveInput.y + camR * currentRawMoveInput.x); calculatedMovementInputVector = worldInputDir.normalized; currentInputMagnitude = Mathf.Clamp01(worldInputDir.magnitude); if (currentInputMagnitude > 0.01f) { if (currentInputMagnitude < walkInputThreshold) { float i = Mathf.InverseLerp(0.01f, walkInputThreshold, currentInputMagnitude); currentTargetMoveSpeed = Mathf.Lerp(characterWalkSpeed * 0.5f, characterWalkSpeed, i); } else if (currentInputMagnitude < sprintInputThreshold) { currentTargetMoveSpeed = characterWalkSpeed; } else { float i = Mathf.InverseLerp(sprintInputThreshold, 1.0f, currentInputMagnitude); currentTargetMoveSpeed = Mathf.Lerp(characterWalkSpeed, characterSprintSpeed, i); } } else { calculatedMovementInputVector = Vector3.zero; currentInputMagnitude = 0f; currentTargetMoveSpeed = 0f; } }
    void ApplyCharacterRotation()
    { if (currentInputMagnitude > 0.1f && calculatedMovementInputVector != Vector3.zero) { Quaternion targetRot = Quaternion.LookRotation(calculatedMovementInputVector, Vector3.up); transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, characterRotationSpeed * Time.fixedDeltaTime); } }
    void ApplyMovementForce()
    { if (currentInputMagnitude <= 0.01f) return; Vector3 forceDirection = transform.forward; if (isCharacterGrounded) { forceDirection = Vector3.ProjectOnPlane(forceDirection, currentGroundNormal).normalized; Vector3 targetVelocity = forceDirection * currentTargetMoveSpeed; Vector3 currentVelocity = controllerRigidbody.linearVelocity; Vector3 groundPlaneVelocity = Vector3.ProjectOnPlane(currentVelocity, currentGroundNormal); Vector3 velocityChange = targetVelocity - groundPlaneVelocity; velocityChange = Vector3.ClampMagnitude(velocityChange, movementAcceleration * Time.fixedDeltaTime); controllerRigidbody.AddForce(velocityChange, ForceMode.VelocityChange); } else { Vector3 targetVelocity = forceDirection * currentTargetMoveSpeed * airControlFactor; Vector3 currentVelocity = controllerRigidbody.linearVelocity; Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z); Vector3 targetHorizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z); Vector3 velocityChange = targetHorizontalVelocity - horizontalVelocity; float airAccel = movementAcceleration * airControlFactor; velocityChange = Vector3.ClampMagnitude(velocityChange, airAccel * Time.fixedDeltaTime); controllerRigidbody.AddForce(velocityChange, ForceMode.VelocityChange); } }
    void ApplyIdleDecelerationAndDrag()
    { if (isCharacterGrounded) { Vector3 currentVelocity = controllerRigidbody.linearVelocity; Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, currentGroundNormal); if (currentInputMagnitude < 0.01f && horizontalVelocity.sqrMagnitude > 0.01f) { controllerRigidbody.AddForce(-horizontalVelocity * groundDragFactor, ForceMode.Acceleration); float decelRate = movementDeceleration; Vector3 decelerationForce = -horizontalVelocity.normalized * decelRate; if (Vector3.Dot(horizontalVelocity + decelerationForce * Time.fixedDeltaTime, horizontalVelocity) <= 0) { controllerRigidbody.linearVelocity = Vector3.Project(currentVelocity, currentGroundNormal); } else { controllerRigidbody.AddForce(decelerationForce, ForceMode.Acceleration); } } } }
    void ApplyGroundDragDuringDash() { /* No drag */ }
    void SnapToZeroVelocityWhenStopping()
    { if (isCharacterGrounded && currentInputMagnitude < 0.01f && !isBeingKnockedBack && !isPerformingGroundDash) { Vector3 currentVelocity = controllerRigidbody.linearVelocity; Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, currentGroundNormal); if (horizontalVelocity.magnitude < stopVelocityThreshold) { Vector3 verticalVelocity = Vector3.Project(currentVelocity, currentGroundNormal); controllerRigidbody.linearVelocity = verticalVelocity; } } }
    void HandlePlatformParenting(Transform detectedTransform)
    { Transform platformToAttach = null; if (detectedTransform != null) { bool isTagged = detectedTransform.CompareTag(PLATFORM_TAG); bool hasScript = detectedTransform.GetComponent<MovingPlatform>() != null; if (isTagged || hasScript) { platformToAttach = detectedTransform; } } if (platformToAttach != null) { if (transform.parent != platformToAttach) { transform.SetParent(platformToAttach, true); currentAttachedPlatform = platformToAttach; } } else { if (transform.parent != originalCharacterParent) { transform.SetParent(originalCharacterParent, true); currentAttachedPlatform = null; } } }
    Vector3 GetCurrentAttachedPlatformVelocity()
    { if (currentAttachedPlatform != null) { MovingPlatform movingPlatform = currentAttachedPlatform.GetComponent<MovingPlatform>(); if (movingPlatform != null) { return movingPlatform.GetVelocity(); } Rigidbody platformRb = currentAttachedPlatform.GetComponent<Rigidbody>(); if (platformRb != null && !platformRb.isKinematic) return platformRb.linearVelocity; } return Vector3.zero; }
    private IEnumerator ApplyDelayedPlatformInertia(Vector3 platformVelocityAtJump)
    { yield return new WaitForSeconds(platformInertiaApplicationDelay); if (!isCharacterGrounded && controllerRigidbody != null) { Vector3 inertiaForce = new Vector3(platformVelocityAtJump.x * platformVelocityInheritanceMultiplier, 0, platformVelocityAtJump.z * platformVelocityInheritanceMultiplier); controllerRigidbody.AddForce(inertiaForce, ForceMode.VelocityChange); } platformInertiaCoroutine = null; }
    Vector3 CalculateAirDashDirection()
    { CalculateMovementVectorFromInput(); Vector3 inputBasedDirection = calculatedMovementInputVector; Vector3 dashDirection; if (inputBasedDirection.magnitude > 0.1f) { dashDirection = inputBasedDirection.normalized; } else { dashDirection = transform.forward.normalized; } if (dashDirection == Vector3.zero) { dashDirection = transform.up; if (transform.up.y < 0) dashDirection *= -1; dashDirection.y = 0; dashDirection.Normalize(); if (dashDirection == Vector3.zero) dashDirection = Vector3.forward; } return dashDirection; }
    #endregion

    #region Health & Damage Logic
    void ActivateDamageImmunity()
    { if (isDamageImmune) return; isDamageImmune = true; damageImmunityTimer = damageInvulnerabilityTime; Debug.Log($"PlayerController: Activated damage immunity for {damageInvulnerabilityTime} seconds."); }
    void UpdateInvulnerabilityVisuals()
    { if (isDamageImmune) { if (damageImmunityTimer <= 0f) { isDamageImmune = false; SetCharacterRenderersEnabled(true); } else { float blinkCycleTime = invulnerabilityBlinkRate * 2f; float timeInCycle = Mathf.Repeat(damageImmunityTimer, blinkCycleTime); bool isVisible = timeInCycle > invulnerabilityBlinkRate; SetCharacterRenderersEnabled(isVisible); } } }
    void ApplyKnockbackForce(Vector3 damageSourcePosition)
    { if (isCharacterDead || isBeingKnockedBack) return; isPerformingGroundDash = false; if (airDashCoroutine != null) { StopCoroutine(airDashCoroutine); airDashCoroutine = null; if(controllerRigidbody != null) controllerRigidbody.useGravity = true; } isPerformingAirDash = false; recentlyFinishedGroundDash = false; // Knockback cancels this state
        isBeingKnockedBack = true; knockbackActiveTimer = 0f; isInputTemporarilyDisabled = true; temporaryInputDisableTimer = baseKnockbackDuration + postKnockbackInputDisableDuration; canCharacterMove = false; Vector3 horizontalDirection = transform.position - damageSourcePosition; horizontalDirection.y = 0; horizontalDirection.Normalize(); if (horizontalDirection == Vector3.zero) { horizontalDirection = -transform.forward; if (horizontalDirection == Vector3.zero) horizontalDirection = Vector3.back; } Vector3 knockbackDirection = (horizontalDirection + Vector3.up * 0.4f).normalized; controllerRigidbody.linearVelocity = Vector3.zero; controllerRigidbody.angularVelocity = Vector3.zero; controllerRigidbody.AddForce(knockbackDirection * hazardKnockbackForce, ForceMode.VelocityChange);
    }
    private IEnumerator MakeRigidbodyKinematicAfterDelay(float delay)
    { yield return new WaitForSeconds(delay); if (controllerRigidbody != null && isCharacterDead) { controllerRigidbody.isKinematic = true; } makeKinematicCoroutine = null; }
    void SpawnHurtEffect(Vector3 position)
    { if (hurtEffectObjectPrefab != null) { GameObject effect = Instantiate(hurtEffectObjectPrefab, position, Quaternion.identity); Destroy(effect, hurtEffectObjectDuration); } }
    #endregion

    #region Animation Logic
    void UpdateCharacterAnimations()
    { if (characterAnimator == null) return; string targetState = DetermineTargetAnimationState(); PlayAnimationState(targetState); }
    private string DetermineTargetAnimationState()
    { if (isCharacterDead) return animStateDie; if (isPlayingHurtAnim) return animStateHurt; if (isPlayingEmoteAnim) return animStateEmote; if (isPerformingAirDash) return animStateAirDash; if (isPerformingGroundDash) return animStateGroundDash; if (!isCharacterGrounded) { if (controllerRigidbody.linearVelocity.y > 0.1f) { return isPerformingDoubleJump ? animStateDoubleJump : animStateJump; } else { return animStateFall; } } if (isCharacterGrounded) { if (currentInputMagnitude > sprintInputThreshold) return animStateSprint; if (currentInputMagnitude > 0.05f) return animStateWalk; return animStateIdle; } return animStateIdle; }
    void PlayAnimationState(string stateName)
    { if (string.IsNullOrEmpty(stateName) || characterAnimator == null) return; if (!characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) { characterAnimator.Play(stateName, 0); } }
    public void TriggerEmoteAnimation()
    { if (isCharacterDead || isPlayingEmoteAnim || isPlayingHurtAnim) return; if (characterAnimator != null && !string.IsNullOrEmpty(animStateEmote)) { PlayAnimationState(animStateEmote); isPlayingEmoteAnim = true; emoteAnimEndTime = Time.time + emoteAnimationLockoutDuration; Invoke(nameof(ResetEmoteAnimationFlag), emoteAnimationLockoutDuration); } else { Debug.LogWarning("PlayerController: Animator or emote animation name is missing!", this); } }
    private void ResetEmoteAnimationFlag()
    { isPlayingEmoteAnim = false; if (!isCharacterDead && !isPlayingHurtAnim && !isInputTemporarilyDisabled && !isBeingKnockedBack) { EnableCharacterMovement(); } }
    void TriggerHurtAnimation()
    { if (isCharacterDead || isPlayingHurtAnim) return; if (characterAnimator != null && !string.IsNullOrEmpty(animStateHurt)) { isPlayingHurtAnim = true; hurtAnimEndTime = Time.time + hurtAnimationLockoutDuration; } else { Debug.LogWarning("PlayerController: Animator or hurt animation name is missing!", this); } }
    #endregion

    #region Particle Effect Logic
    void UpdateFootstepParticles()
    { if (footstepParticleEffect == null) return; bool shouldPlay = isCharacterGrounded && controllerRigidbody.linearVelocity.magnitude > footstepParticleMinSpeed && currentInputMagnitude > footstepParticleMinInput && !isPerformingGroundDash && !isCharacterDead; if (shouldPlay) { if (!footstepParticlesActive) { footstepParticleEffect.Play(); footstepParticlesActive = true; } footstepParticleFadeOutCounter = footstepParticleFadeTime; } else { if (footstepParticlesActive) { footstepParticleFadeOutCounter -= Time.deltaTime; if (footstepParticleFadeOutCounter <= 0) { StopFootstepParticles(); } } } }
    void StopFootstepParticles()
    { if (footstepParticleEffect != null && footstepParticlesActive) { footstepParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting); footstepParticlesActive = false; footstepParticleFadeOutCounter = 0f; } }
    #endregion

    #region Collision & Trigger Handlers
    void OnCollisionEnter(Collision collision)
    { if (isCharacterDead || isDamageImmune) return; if (collision.gameObject.CompareTag(HAZARD_TAG)) { Debug.Log("PlayerController: Collided with Hazard!"); ContactPoint c = collision.contacts[0]; ProcessDamageTaken(c.point); } }
    void OnTriggerEnter(Collider other)
    { if (other.CompareTag(DEATHBOX_TAG)) { Debug.Log("PlayerController: Entered Deathbox!"); TriggerCharacterDeath(); return; } if (isCharacterDead || isDamageImmune) return; if (other.CompareTag(HAZARD_TAG)) { Debug.Log("PlayerController: Triggered Hazard!"); Vector3 p = other.ClosestPoint(transform.position); ProcessDamageTaken(p); } }
    #endregion

    #region Utility & Helper Methods
    void UpdateJumpAscentState()
    { if (isPerformingJump && !isCharacterGrounded && !isPerformingDoubleJump && !isPerformingAirDash && controllerRigidbody.linearVelocity.y > 0) { timeSinceJumpButtonPress += Time.deltaTime; if (!jumpHasBeenCutOff && timeSinceJumpButtonPress >= maxJumpAscentTime) { if (enableApexSmoothingOnMaxDuration) { applyApexSmoothing = true; } ApplyJumpCutoffVelocity(); } } else { if (!isCharacterGrounded) { applyApexSmoothing = false; } } }
    void SetCharacterRenderersEnabled(bool enabled)
    { if (bodyMeshRenderer != null) bodyMeshRenderer.enabled = enabled; if (headMeshRenderer != null) headMeshRenderer.enabled = enabled; }
    void PlayRandomSound(AudioClip[] clips, float volume)
    { if (controllerAudioSource != null && clips != null && clips.Length > 0) { AudioClip clipToPlay = clips[Random.Range(0, clips.Length)]; if (clipToPlay != null) { controllerAudioSource.PlayOneShot(clipToPlay, volume); } } }
    private void FlagRecentlyPerformedJump()
    { recentlyPerformedJump = true; if (resetRecentlyPerformedJumpCoroutine != null) { StopCoroutine(resetRecentlyPerformedJumpCoroutine); } resetRecentlyPerformedJumpCoroutine = StartCoroutine(ResetRecentlyPerformedJumpFlagCoroutine()); }
    private IEnumerator ResetRecentlyPerformedJumpFlagCoroutine()
    { yield return new WaitForSeconds(0.1f); recentlyPerformedJump = false; resetRecentlyPerformedJumpCoroutine = null; }
    public void DisableCharacterMovement()
    { canCharacterMove = false; canProcessGeneralInput = false; currentRawMoveInput = Vector2.zero; calculatedMovementInputVector = Vector3.zero; currentInputMagnitude = 0f; if (controllerRigidbody != null) { controllerRigidbody.linearVelocity = Vector3.zero; controllerRigidbody.angularVelocity = Vector3.zero; } }
    public void EnableCharacterMovement()
    { if (!isCharacterDead && !isPlayingHurtAnim && !isPlayingEmoteAnim && !isInputTemporarilyDisabled) { canCharacterMove = true; canProcessGeneralInput = true; DetermineInputAvailability(); } }
    public void DisableJumpAbility() { canProcessJumpInput = false; }
    public void EnableJumpAbility()
    { if (!isCharacterDead && !isPlayingHurtAnim && !isPlayingEmoteAnim && !isInputTemporarilyDisabled) { canProcessJumpInput = true; DetermineInputAvailability(); } }
    #endregion

    #region Public Query Methods
    public bool QueryIsAirDashing() => isPerformingAirDash;
    public bool QueryIsGrounded() => isCharacterGrounded;
    public bool QueryIsGroundDashing() => isPerformingGroundDash;
    public bool QueryIsDead() => isCharacterDead;
    public bool QueryIsDamageImmune() => isDamageImmune;
    #endregion

} // End of PlayerController class