using UnityEngine;
using System.Collections.Generic;

public class BackgroundPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float semiMajorAxis = 10f;
    public float semiMinorAxis = 5f;
    public float speed = 5f; // Approximate linear speed
    public Vector3 centerPoint = Vector3.zero;
    [Space]
    [Header("Multiple Copies")]
    public int numberOfCopies = 1;
    [Range(0f, 360f)]
    public float angleOffsetPerCopy = 30f; // Angle difference between each copy
    public GameObject[] objectsToCopy; // Array of GameObjects to instantiate

    [Space]
    [Header("Object Scaling")]
    public float uniformScaleValue = 1f;
    public bool useUniformScale = false;

    [Space]
    [Header("Ellipse Rotation")]
    public Vector3 ellipseRotationAxis = Vector3.up;
    public float ellipseRotationAngle = 0f;
    public float ellipseRotationSpeed = 0f;

    private List<Transform> copies = new List<Transform>();
    private List<float> copyAngles = new List<float>();

    void Start()
    {
        if (objectsToCopy == null || objectsToCopy.Length == 0)
        {
            Debug.LogError("Objects to Copy array is empty or not assigned in the Inspector!");
            enabled = false;
            return;
        }

        if (numberOfCopies <= 0)
        {
            Debug.LogWarning("Number of Copies is set to 0 or less. No copies will be created.");
            return;
        }

        for (int i = 0; i < numberOfCopies; i++)
        {
            float startAngle = i * angleOffsetPerCopy * Mathf.Deg2Rad;
            copyAngles.Add(startAngle);

            // Select an object to copy from the array (cycle through the array)
            GameObject objectToInstantiate = objectsToCopy[i % objectsToCopy.Length];

            // Calculate the starting position on the rotated ellipse
            float x = semiMajorAxis * Mathf.Cos(startAngle);
            float z = semiMinorAxis * Mathf.Sin(startAngle);
            Vector3 ellipsePoint = new Vector3(x, 0f, z);
            Quaternion ellipseRotation = Quaternion.AngleAxis(ellipseRotationAngle, ellipseRotationAxis);
            Vector3 startPosition = centerPoint + ellipseRotation * ellipsePoint;

            // Instantiate the copy
            GameObject newCopy = Instantiate(objectToInstantiate, startPosition, Quaternion.identity);
            Transform copyTransform = newCopy.transform;
            copies.Add(copyTransform);

            // Apply uniform scaling
            if (useUniformScale)
            {
                copyTransform.localScale = new Vector3(uniformScaleValue, uniformScaleValue, uniformScaleValue);
            }

            // Calculate the initial tangent and set the initial rotation
            float tangentX = -semiMajorAxis * Mathf.Sin(startAngle);
            float tangentZ = semiMinorAxis * Mathf.Cos(startAngle);
            Vector3 tangent = new Vector3(tangentX, 0f, tangentZ).normalized;
            Vector3 rotatedTangent = ellipseRotation * tangent;
            Quaternion initialRotation = Quaternion.LookRotation(rotatedTangent, Vector3.up);
            copyTransform.rotation = initialRotation;

            // If this is the original object, disable its movement
            if (objectsToCopy.Length == 1 && i == 0 && gameObject == objectsToCopy[0])
            {
                Destroy(this);
                break;
            }
        }

        if (numberOfCopies > 0 && objectsToCopy.Length == 1 && gameObject == objectsToCopy[0] && copies.Count == 0)
        {
            copies.Add(transform);
            copyAngles.Add(0f);
            float startAngle = 0f;
            float x = semiMajorAxis * Mathf.Cos(startAngle);
            float z = semiMinorAxis * Mathf.Sin(startAngle);
            Vector3 ellipsePoint = new Vector3(x, 0f, z);
            Quaternion ellipseRotation = Quaternion.AngleAxis(ellipseRotationAngle, ellipseRotationAxis);
            transform.position = centerPoint + ellipseRotation * ellipsePoint;
            float tangentX = -semiMajorAxis * Mathf.Sin(startAngle);
            float tangentZ = semiMinorAxis * Mathf.Cos(startAngle);
            Vector3 tangent = new Vector3(tangentX, 0f, tangentZ).normalized;
            Vector3 rotatedTangent = ellipseRotation * tangent;
            transform.rotation = Quaternion.LookRotation(rotatedTangent, Vector3.up);
            if (useUniformScale) transform.localScale = new Vector3(uniformScaleValue, uniformScaleValue, uniformScaleValue);
        }
        else if (objectsToCopy.Length == 1 && gameObject == objectsToCopy[0] && numberOfCopies > 0)
        {
            enabled = false;
        }
    }

    void Update()
    {
        ellipseRotationAngle += ellipseRotationSpeed * Time.deltaTime;
        Quaternion currentEllipseRotation = Quaternion.AngleAxis(ellipseRotationAngle, ellipseRotationAxis);

        for (int i = 0; i < copies.Count; i++)
        {
            if (copies[i] == null) continue;

            float currentGlobalAngle = copyAngles[i] + (ellipseRotationAngle * Mathf.Deg2Rad);
            float x = semiMajorAxis * Mathf.Cos(currentGlobalAngle);
            float z = semiMinorAxis * Mathf.Sin(currentGlobalAngle);
            Vector3 ellipsePoint = new Vector3(x, 0f, z);
            Vector3 targetPosition = centerPoint + currentEllipseRotation * ellipsePoint;

            float tangentX = -semiMajorAxis * Mathf.Sin(currentGlobalAngle);
            float tangentZ = semiMinorAxis * Mathf.Cos(currentGlobalAngle);
            Vector3 tangent = new Vector3(tangentX, 0f, tangentZ).normalized;
            Vector3 rotatedTangent = currentEllipseRotation * tangent;

            Quaternion targetRotation = Quaternion.LookRotation(rotatedTangent, Vector3.up);
            copies[i].rotation = Quaternion.Slerp(copies[i].rotation, targetRotation, Time.deltaTime * speed);

            Vector3 moveDirection = targetPosition - copies[i].position;
            if (moveDirection.magnitude > 0.001f)
            {
                copies[i].position += moveDirection.normalized * speed * Time.deltaTime;
            }

            float circumferenceApproximation = Mathf.PI * (3 * (semiMajorAxis + semiMinorAxis) - Mathf.Sqrt((3 * semiMajorAxis + semiMinorAxis) * (semiMajorAxis + 3 * semiMinorAxis)));
            float angularSpeed = (speed / circumferenceApproximation) * 2 * Mathf.PI;

            copyAngles[i] += angularSpeed * Time.deltaTime;
            if (copyAngles[i] > 2 * Mathf.PI) copyAngles[i] -= 2 * Mathf.PI;
            else if (copyAngles[i] < 0) copyAngles[i] += 2 * Mathf.PI;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Quaternion ellipseRotationGizmo = Quaternion.AngleAxis(ellipseRotationAngle, ellipseRotationAxis);
        float steps = 100;
        for (int i = 0; i <= steps; i++)
        {
            float angle = 2 * Mathf.PI * i / steps;
            float xOffset = semiMajorAxis * Mathf.Cos(angle);
            float zOffset = semiMinorAxis * Mathf.Sin(angle);
            Vector3 ellipsePoint = new Vector3(xOffset, 0f, zOffset);
            Vector3 rotatedPoint = centerPoint + ellipseRotationGizmo * ellipsePoint;

            float prevAngle = 2 * Mathf.PI * (i - 1) / steps;
            float prevXOffset = semiMajorAxis * Mathf.Cos(prevAngle);
            float prevZOffset = semiMinorAxis * Mathf.Sin(prevAngle);
            Vector3 prevEllipsePoint = new Vector3(prevXOffset, 0f, prevZOffset);
            Vector3 prevRotatedPoint = centerPoint + ellipseRotationGizmo * prevEllipsePoint;

            if (i > 0) Gizmos.DrawLine(prevRotatedPoint, rotatedPoint);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPoint, 0.2f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(centerPoint, ellipseRotationGizmo * ellipseRotationAxis * 2f);

        Gizmos.color = Color.magenta;
        if (objectsToCopy != null)
        {
            for (int i = 0; i < numberOfCopies; i++)
            {
                float startAngleRad = i * angleOffsetPerCopy * Mathf.Deg2Rad;
                float currentAngleForCopy = startAngleRad + (ellipseRotationAngle * Mathf.Deg2Rad);
                float copyX = semiMajorAxis * Mathf.Cos(currentAngleForCopy);
                float copyZ = semiMinorAxis * Mathf.Sin(currentAngleForCopy);
                Vector3 copyEllipsePoint = new Vector3(copyX, 0f, copyZ);
                Vector3 rotatedCopyPoint = centerPoint + ellipseRotationGizmo * copyEllipsePoint;
                Gizmos.DrawSphere(rotatedCopyPoint, 0.3f);
            }
        }
    }
}