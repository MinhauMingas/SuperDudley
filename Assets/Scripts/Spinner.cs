using UnityEngine;

public class Spinner : MonoBehaviour
{
    public Transform spinnerTransform;

    [SerializeField] float rotationSpeed = 100f;
    [SerializeField] Vector3 rotationAngles = Vector3.zero; // Replacing three floats with a single Vector3


    void Start()
    {
        
    }

    void Update()
    {
        spinnerTransform.Rotate(rotationAngles * rotationSpeed * Time.deltaTime);


    }
}
