using UnityEngine;

public class SpinStar : MonoBehaviour
{
    public float spinSpeed = 30f; // Degrees per second

    void Update()
    {
        // Rotate the object around the Y-axis.
        transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);
    }
}