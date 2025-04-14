using UnityEngine;

public class FloatingPattern : MonoBehaviour
{
    [Header("Pattern Settings")]
    [Tooltip("The type of movement pattern for the platform.")]
    public MovementPattern patternType = MovementPattern.SineWaveVertical;

    public enum MovementPattern
    {
        SineWaveVertical,
        SineWaveHorizontal,
        CircularHorizontal,
        CustomPoints,
        CombinedSineWave
    }

    [Header("Sine Wave Settings (Vertical & Horizontal)")]
    [Tooltip("The amplitude (distance) of the sine wave movement.")]
    public float amplitude = 1f;

    [Tooltip("The speed of the sine wave movement.")]
    public float frequency = 1f;

    [Tooltip("The starting offset in the sine wave cycle.")]
    public float startOffset = 0f;

    [Header("Circular Horizontal Settings")]
    [Tooltip("The radius of the circular movement.")]
    public float radius = 2f;

    [Tooltip("The speed of the circular movement.")]
    public float angularSpeed = 2f;

    [Header("Custom Points Settings")]
    [Tooltip("A list of local positions the platform will move towards.")]
    public Vector3[] targetPoints;

    [Tooltip("The speed at which the platform moves between custom points.")]
    public float moveSpeed = 2f;

    [Tooltip("How long to wait at each custom point before moving to the next.")]
    public float waitTime = 1f;

    [Tooltip("Should the platform loop through the custom points?")]
    public bool loop = true;

    [Header("Rocking Rotation Settings")]
    [Tooltip("The maximum rotation angle around the Z-axis (in degrees).")]
    public float maxRotationAngleZ = 15f;

    [Tooltip("The speed of the rocking rotation around the Z-axis.")]
    public float rockingSpeedZ = 1f;

    [Tooltip("The maximum rotation angle around the X-axis (in degrees).")]
    public float maxRotationAngleX = 10f;

    [Tooltip("The speed of the rocking rotation around the X-axis.")]
    public float rockingSpeedX = 1.2f;

    private Vector3 initialPosition;
    private Quaternion initialRotation; // Store the initial rotation
    private float timer;
    private int currentTargetIndex = 0;
    private float waitTimer = 0f;
    private float rotationTimerZ = 0f;
    private float rotationTimerX = 0f;

    void Start()
    {
        initialPosition = transform.localPosition; // Use localPosition for movement relative to the parent
        initialRotation = transform.localRotation; // Store the initial local rotation
        timer = startOffset;

        // Ensure targetPoints is not empty for the CustomPoints pattern
        if (patternType == MovementPattern.CustomPoints && targetPoints.Length == 0)
        {
            Debug.LogError("FloatingPattern: Target Points array is empty for CustomPoints pattern on " + gameObject.name);
            enabled = false;
        }
    }

    void Update()
    {
        switch (patternType)
        {
            case MovementPattern.SineWaveVertical:
                MoveSineWaveVertical();
                break;
            case MovementPattern.SineWaveHorizontal:
                MoveSineWaveHorizontal();
                break;
            case MovementPattern.CircularHorizontal:
                MoveCircularHorizontal();
                break;
            case MovementPattern.CustomPoints:
                MoveCustomPoints();
                break;
            case MovementPattern.CombinedSineWave:
                MoveCombinedSineWave();
                break;
        }

        // Apply rocking rotation
        ApplyRockingRotation();
    }

    void MoveSineWaveVertical()
    {
        timer += Time.deltaTime * frequency;
        float verticalOffset = Mathf.Sin(timer) * amplitude;
        transform.localPosition = initialPosition + Vector3.up * verticalOffset;
    }

    void MoveSineWaveHorizontal()
    {
        timer += Time.deltaTime * frequency;
        float horizontalOffset = Mathf.Sin(timer) * amplitude;
        transform.localPosition = initialPosition + Vector3.right * horizontalOffset;
    }

    void MoveCircularHorizontal()
    {
        timer += Time.deltaTime * angularSpeed;
        float x = Mathf.Cos(timer) * radius;
        float z = Mathf.Sin(timer) * radius;
        transform.localPosition = initialPosition + new Vector3(x, 0f, z);
    }

    void MoveCustomPoints()
    {
        if (targetPoints.Length == 0) return;

        Vector3 targetLocalPosition = initialPosition + targetPoints[currentTargetIndex];

        if (Vector3.Distance(transform.localPosition, targetLocalPosition) < 0.01f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;
                currentTargetIndex++;

                if (currentTargetIndex >= targetPoints.Length)
                {
                    if (loop)
                    {
                        currentTargetIndex = 0;
                    }
                    else
                    {
                        // Optionally disable movement or hold the last position
                        enabled = false;
                    }
                }
            }
        }
        else
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetLocalPosition, moveSpeed * Time.deltaTime);
        }
    }

    void MoveCombinedSineWave()
    {
        timer += Time.deltaTime * frequency;
        float verticalOffset = Mathf.Sin(timer) * amplitude;
        float horizontalOffset = Mathf.Cos(timer * 0.8f + 1.5f) * amplitude * 0.75f; // Added a slightly different frequency and offset for variation
        transform.localPosition = initialPosition + Vector3.up * verticalOffset + Vector3.right * horizontalOffset;
    }

    void ApplyRockingRotation()
    {
        rotationTimerZ += Time.deltaTime * rockingSpeedZ;
        float angleZ = Mathf.Sin(rotationTimerZ) * maxRotationAngleZ;

        rotationTimerX += Time.deltaTime * rockingSpeedX;
        float angleX = Mathf.Sin(rotationTimerX) * maxRotationAngleX;

        // Apply the rocking rotation around the initial rotation
        transform.localRotation = initialRotation * Quaternion.Euler(angleX, 0f, angleZ);
    }

    // Optional: Visualize the custom points in the editor
    private void OnDrawGizmosSelected()
    {
        if (patternType == MovementPattern.CustomPoints && targetPoints != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < targetPoints.Length; i++)
            {
                Vector3 worldPos = transform.position + targetPoints[i];
                Gizmos.DrawSphere(worldPos, 0.15f);
                if (i < targetPoints.Length - 1)
                {
                    Vector3 nextWorldPos = transform.position + targetPoints[i + 1];
                    Gizmos.DrawLine(worldPos, nextWorldPos);
                }
                else if (loop && targetPoints.Length > 1)
                {
                    Vector3 firstWorldPos = transform.position + targetPoints[0];
                    Gizmos.DrawLine(worldPos, firstWorldPos);
                }
            }
        }
    }
}