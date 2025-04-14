using UnityEngine;

public class ObjectHit : MonoBehaviour
{
    [SerializeField] private Material redColorMaterial; // Assign this in the Inspector
    [SerializeField] private LayerMask ignoreLayers; // Assign floor or any objects to ignore
    [SerializeField] private GameObject destructionEffectPrefab; // Explosion effect prefab
    [SerializeField] private float effectDuration = 1f; // Duration for effect before destroying

    int health = 3;
    private Material originalMaterial;

    void OnCollisionEnter(Collision collision)
    {
        health -= 1;
        Debug.Log("Your health: " + health);

        // Check if the collision is not with the floor (or any object in the ignoreLayers)
        if ((ignoreLayers.value & (1 << collision.gameObject.layer)) == 0)
        {
            // Only change the material of objects that have a MeshRenderer
            MeshRenderer obstacleRenderer = collision.gameObject.GetComponent<MeshRenderer>();
            if (obstacleRenderer != null && redColorMaterial != null)
            {
                // Store the original material and change to Red Color
                originalMaterial = obstacleRenderer.material;
                obstacleRenderer.material = redColorMaterial;
            }

            // Trigger explosion effect
            TriggerExplosionEffect(collision.contacts[0].point);
        }

        Debug.Log("Player collided with " + collision.gameObject.name);
    }

    void OnCollisionExit(Collision collision)
    {
        // Check if the object is not in the ignore layers and has a MeshRenderer
        if ((ignoreLayers.value & (1 << collision.gameObject.layer)) == 0)
        {
            MeshRenderer obstacleRenderer = collision.gameObject.GetComponent<MeshRenderer>();
            if (obstacleRenderer != null)
            {
                // Restore the original material when exiting the collision
                obstacleRenderer.material = originalMaterial;
            }
        }
    }

    // Function to trigger explosion effect at the point of collision
    void TriggerExplosionEffect(Vector3 collisionPoint)
    {
        if (destructionEffectPrefab != null)
        {
            // Instantiate the destruction effect at the collision point
            GameObject explosion = Instantiate(destructionEffectPrefab, collisionPoint, Quaternion.identity);
            Destroy(explosion, effectDuration); // Destroy the effect after its duration
        }
    }
}
