using UnityEngine;

public class ParticleCollisionAnimation : MonoBehaviour
{
    public Animator animator; // Drag your Animator component here in the Inspector
    public string animationTriggerName = "PlayAnimation"; // The name of the trigger parameter in your Animator

    private void OnParticleCollision(GameObject other)
    {
        // 'other' is the GameObject of the Particle System that entered the trigger

        if (animator != null)
        {
            // Trigger the animation in the Animator
            animator.SetTrigger(animationTriggerName);

            // Optional: You could also play a specific animation state directly
            // animator.Play("YourAnimationStateName");
        }
        else
        {
            Debug.LogWarning("Animator component not assigned to " + gameObject.name);
        }

        // Optional: You can access the colliding particles' data if needed
        // ParticleSystem.CollisionEvent[] collisionEvents = new ParticleSystem.CollisionEvent[16]; // Adjust size if needed
        // int numCollisions = other.GetComponent<ParticleSystem>().GetCollisionEvents(gameObject, collisionEvents);
        // for (int i = 0; i < numCollisions; i++)
        // {
        //     Debug.Log("Particle entered trigger at: " + collisionEvents[i].intersection);
        // }
    }
}