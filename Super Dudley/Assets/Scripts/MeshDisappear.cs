using UnityEngine;

public class MeshDisappear : MonoBehaviour
{
    void Start()
    {
        DisappearMesh();
    }

    public void DisappearMesh()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
        else
        {
            Debug.LogWarning("MeshRenderer not found on this GameObject. It won't be disabled.");
        }
    }
}