using UnityEngine;

public class BackgroundMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Distance to move along the X-axis.")]
    public float moveDistanceX = 5f;
    [Tooltip("Distance to move along the Z-axis.")]
    public float moveDistanceZ = 3f;
    [Tooltip("Speed of the movement.")]
    public float moveSpeed = 2f;

    [Header("Direction")]
    [Tooltip("Should the object move along the positive X-axis initially?")]
    public bool movePositiveX = true;
    [Tooltip("Should the object move along the positive Z-axis initially?")]
    public bool movePositiveZ = true;

    private Vector3 initialPosition;
    private bool movingForwardX;
    private bool movingForwardZ;

    void Start()
    {
        initialPosition = transform.position;
        movingForwardX = movePositiveX;
        movingForwardZ = movePositiveZ;
    }

    void Update()
    {
        float targetX = initialPosition.x + (movingForwardX ? moveDistanceX : -moveDistanceX);
        float targetZ = initialPosition.z + (movingForwardZ ? moveDistanceZ : -moveDistanceZ);
        Vector3 targetPosition = new Vector3(targetX, initialPosition.y, targetZ);

        // Move towards the target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Check if we've reached the target X position
        if (Mathf.Abs(transform.position.x - targetX) < 0.01f)
        {
            movingForwardX = !movingForwardX;
        }

        // Check if we've reached the target Z position
        if (Mathf.Abs(transform.position.z - targetZ) < 0.01f)
        {
            movingForwardZ = !movingForwardZ;
        }
    }
}