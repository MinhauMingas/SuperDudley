using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public string idleAnimation = "idle";
    public string walkAnimation = "walk";
    public string sprintAnimation = "sprint";
    public string jumpAnimation = "jump";
    public string doubleJumpAnimation = "doubleJump";
    public string fallAnimation = "fall";
    public string hurtAnimationName = "hurt";
    public string deathAnimationName = "die";
    public string starAnimationName = "emote-yes";
    public string dashAnimationName = "dash";
    public string groundDashAnimationName = "groundDash";

    [Header("Animation Timing")]
    [SerializeField] public float jumpAnimationDuration = 0.8f;
    [SerializeField] public float doubleJumpAnimationDuration = 0.6f;
    [SerializeField] public float hurtAnimationDuration = 1.0f;
    [SerializeField] public float starAnimationDuration = 1.0f;
    [SerializeField] public float dashAnimationDuration = 0.2f;
    [SerializeField] public float groundDashAnimationDuration = 0.3f;

    private Animator anim;
    private PlayerMovement playerMovement;
    public PlayerJump playerJump;
    private bool isPlayingHurtAnimation = false;
    private float hurtAnimationEndTime;
    [HideInInspector] public bool isDead = false;
    private bool isPlayingStarAnimation = false;
    private bool isPlayingDashAnimation = false;
    private bool isPlayingGroundDashAnimation = false;
    private float dashAnimationEndTime;
    private float groundDashAnimationEndTime;
    public bool canPlayerMove = true;

    void Start()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        playerJump = GetComponent<PlayerJump>();

        if (anim == null)
        {
            Debug.LogError("Animator component not found on PlayerAnimation!");
        }
    }

    void Update()
    {
        if (isDead) return;

        if (isPlayingHurtAnimation && Time.time >= hurtAnimationEndTime)
        {
            isPlayingHurtAnimation = false;
            canPlayerMove = true;
        }

        if (isPlayingDashAnimation && Time.time >= dashAnimationEndTime)
        {
            isPlayingDashAnimation = false;
        }

        if (isPlayingGroundDashAnimation && Time.time >= groundDashAnimationEndTime)
        {
            isPlayingGroundDashAnimation = false;
        }

        if (isPlayingHurtAnimation || isPlayingGroundDashAnimation)
        {
            return;
        }

        if (!isPlayingStarAnimation && !isPlayingDashAnimation)
        {
            UpdateAnimations();
        }
    }

    void UpdateAnimations()
    {
        if (anim == null) return;

        if (playerMovement.IsDashing())
        {
            if (playerMovement.IsGrounded())
            {
                if (!isPlayingGroundDashAnimation)
                {
                    PlayGroundDashAnimation();
                }
            }
            else
            {
                if (!isPlayingDashAnimation)
                {
                    PlayDashAnimation();
                }
            }
            return;
        }

        if (playerJump.isJumping && playerJump.rb.linearVelocity.y > 0.1f && !playerMovement.IsGrounded() && !playerJump.isDoubleJumping)
        {
            StartJumpAnimation();
        }
        else if (playerJump.isDoubleJumping && playerJump.rb.linearVelocity.y > 0.1f && !playerMovement.IsGrounded())
        {
            StartDoubleJumpAnimation();
        }
        else if (!playerMovement.IsGrounded() && playerJump.rb.linearVelocity.y < -0.1f)
        {
            anim.Play(fallAnimation);
        }
        else if (playerMovement.IsGrounded())
        {
            if (playerMovement.inputMagnitude > playerMovement.sprintThreshold)
            {
                anim.Play(sprintAnimation);
            }
            else if (playerMovement.inputMagnitude > playerMovement.walkThreshold)
            {
                anim.Play(walkAnimation);
            }
            else if (playerMovement.inputMagnitude > 0.1f)
            {
                anim.Play(walkAnimation);
            }
            else
            {
                anim.Play(idleAnimation);
            }
        }
    }

    public void StartJumpAnimation()
    {
        if (anim == null) return;
        anim.Play(jumpAnimation);
    }

    public void StartDoubleJumpAnimation()
    {
        if (anim == null) return;
        anim.Play(doubleJumpAnimation);
    }

    public void PlayDashAnimation()
    {
        if (anim != null && !string.IsNullOrEmpty(dashAnimationName))
        {
            anim.Play(dashAnimationName);
            isPlayingDashAnimation = true;
            dashAnimationEndTime = Time.time + dashAnimationDuration;
        }
        else
        {
            Debug.LogWarning("Animator or dashAnimationName is null or empty!");
        }
    }

    public void PlayGroundDashAnimation()
    {
        if (anim != null && !string.IsNullOrEmpty(groundDashAnimationName))
        {
            anim.Play(groundDashAnimationName);
            isPlayingGroundDashAnimation = true;
            groundDashAnimationEndTime = Time.time + groundDashAnimationDuration;
        }
        else
        {
            Debug.LogWarning("Animator or groundDashAnimationName is null or empty!");
        }
    }

    public void PlayHurtAnimation()
    {
        if (anim != null && !string.IsNullOrEmpty(hurtAnimationName))
        {
            anim.Play(hurtAnimationName);
            isPlayingHurtAnimation = true;
            canPlayerMove = false;
            hurtAnimationEndTime = Time.time + hurtAnimationDuration;
        }
        else
        {
            Debug.LogWarning("Animator or hurtAnimationName is null or empty!");
        }
    }

    public void PlayDeathAnimation()
    {
        if (anim != null && !string.IsNullOrEmpty(deathAnimationName))
        {
            anim.Play(deathAnimationName);
            canPlayerMove = false;
        }
        else
        {
            Debug.LogWarning("Animator or deathAnimationName is null or empty!");
        }
    }

    public void PlayStarCollectedAnimation()
    {
        if (anim != null && !string.IsNullOrEmpty(starAnimationName))
        {
            anim.Play(starAnimationName);
            isPlayingStarAnimation = true;
            canPlayerMove = false;
            Invoke("ResetStarAnimationFlag", starAnimationDuration);
        }
        else
        {
            Debug.LogWarning("Animator or starAnimationName is null or empty!");
        }
    }

    void ResetStarAnimationFlag()
    {
        isPlayingStarAnimation = false;
        canPlayerMove = true;
    }

    public float GetStarAnimationDuration()
    {
        return starAnimationDuration;
    }

    public float GetDashAnimationDuration()
    {
        return dashAnimationDuration;
    }

    public float GetGroundDashAnimationDuration()
    {
        return groundDashAnimationDuration;
    }
}