using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right;
    public float moveSpeed = 1f;
    public float moveDistance = 10f;
    public float easeDuration = 0.5f;
    
    [Header("Pause Settings")]
    public float startPointPauseDuration = 0.5f;
    public float endPointPauseDuration = 0.5f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float moveProgress = 0f;
    private bool movingForward = true;
    private float currentSpeed;
    private float pauseTimer = 0f;
    private bool isPaused = false;

    public Vector3 platformVelocity; // Store the velocity
    private Vector3 lastPosition;

    private void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + moveDirection.normalized * moveDistance;
        currentSpeed = moveSpeed;
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Store the last position before updating
        lastPosition = transform.position;

        // Handle pausing at endpoints
        if (isPaused)
        {
            pauseTimer -= Time.fixedDeltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
            }
            else
            {
                // While paused, velocity is zero
                platformVelocity = Vector3.zero;
                return;
            }
        }

        // Regular movement
        if (movingForward)
        {
            moveProgress += currentSpeed * Time.fixedDeltaTime;
            if (moveProgress >= 1f)
            {
                moveProgress = 1f;
                movingForward = false;
                
                // Start pause at endpoint if duration > 0
                if (endPointPauseDuration > 0f)
                {
                    isPaused = true;
                    pauseTimer = endPointPauseDuration;
                }
            }
        }
        else
        {
            moveProgress -= currentSpeed * Time.fixedDeltaTime;
            if (moveProgress <= 0f)
            {
                moveProgress = 0f;
                movingForward = true;
                
                // Start pause at start point if duration > 0
                if (startPointPauseDuration > 0f)
                {
                    isPaused = true;
                    pauseTimer = startPointPauseDuration;
                }
            }
        }

        // Apply smooth step for easing
        float easedProgress = Mathf.SmoothStep(0f, 1f, moveProgress);

        // Adjust speed based on position
        if (moveProgress < easeDuration || moveProgress > 1f - easeDuration)
        {
            currentSpeed = moveSpeed * 0.5f;
        }
        else
        {
            currentSpeed = moveSpeed;
        }

        transform.position = Vector3.Lerp(startPosition, endPosition, easedProgress);

        // Calculate actual velocity based on position change
        platformVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;

        Vector3 startGizmoPos = transform.position;
        Vector3 endGizmoPos = transform.position + moveDirection.normalized * moveDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startGizmoPos, endGizmoPos);
        Gizmos.DrawSphere(startGizmoPos, 0.2f);
        Gizmos.DrawSphere(endGizmoPos, 0.2f);
    }

    public Vector3 GetVelocity()
    {
        return platformVelocity;
    }
}