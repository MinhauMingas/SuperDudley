using UnityEngine;
using System.Collections;

public class PlayerStarCollision : MonoBehaviour
{
    private Animator anim;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;
    private Rigidbody rb;
    private bool isStarCollected = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("star") && !isStarCollected)
        {
            HandleStarCollision();
        }
    }

    void HandleStarCollision()
    {
        isStarCollected = true;

        // Stop the player's movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Disable player movement
        playerMovement.enabled = false;

        // Play the star collected animation
        playerAnimation.PlayStarCollectedAnimation();
        
    }
}