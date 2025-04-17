using UnityEngine;

public class MovingSpike : MonoBehaviour
{
    [Header("Motion Settings")]
    public Vector3 motionDirection = Vector3.right;
    public float motionSpeed = 1f;
    public float motionDistance = 10f;
    public float smoothTransitionDuration = 0.5f; // Duration of the ease-in/ease-out effect

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private float motionProgress = 0f;
    private bool movingForward = true;
    private float currentMotionSpeed; // Variable to store the adjusted speed

    private void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + motionDirection.normalized * motionDistance;
        currentMotionSpeed = motionSpeed; // Initialize current speed
    }

    public void PerformSmoothMotion() // Public method for motion execution
    {
        if (movingForward)
        {
            motionProgress += currentMotionSpeed * Time.fixedDeltaTime;
            if (motionProgress >= 1f)
            {
                motionProgress = 1f;
                movingForward = false;
            }
        }
        else
        {
            motionProgress -= currentMotionSpeed * Time.fixedDeltaTime;
            if (motionProgress <= 0f)
            {
                motionProgress = 0f;
                movingForward = true;
            }
        }

        // Apply ease-in/ease-out effect
        float easedProgress = Mathf.SmoothStep(0f, 1f, motionProgress);

        // Adjust speed based on smoothTransitionDuration
        if (motionProgress < smoothTransitionDuration || motionProgress > 1f - smoothTransitionDuration)
        {
            currentMotionSpeed = motionSpeed * 0.5f; // Reduce speed during ease-in/ease-out
        }
        else
        {
            currentMotionSpeed = motionSpeed; // Restore normal speed
        }

        transform.position = Vector3.Lerp(initialPosition, targetPosition, easedProgress);
    }

    private void FixedUpdate()
    {
        PerformSmoothMotion(); // Call the public method in FixedUpdate
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;

        Vector3 startGizmoPos = transform.position;
        Vector3 endGizmoPos = transform.position + motionDirection.normalized * motionDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startGizmoPos, endGizmoPos);
        Gizmos.DrawSphere(startGizmoPos, 0.2f);
        Gizmos.DrawSphere(endGizmoPos, 0.2f);
    }
}