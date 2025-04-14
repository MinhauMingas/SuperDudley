using UnityEngine;

public class DropperTimer : MonoBehaviour
{
    public Rigidbody rb;
    public MeshRenderer meshRenderer;
    [SerializeField] float timeToDrop = 5.0f;

    void Start()
    {
        meshRenderer.enabled = false;
        rb.useGravity = false;
    }


    void Update()
    {
        if (Time.time >= timeToDrop)
        {
            Drop();
        }
        else
        {
            Debug.Log(Time.time);
        }
    }

    void Drop()
    {
        rb.useGravity = true;
        meshRenderer.enabled = true;


    }
}
