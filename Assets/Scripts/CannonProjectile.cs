// CannonProjectile.cs
using UnityEngine;

public class CannonProjectile : MonoBehaviour
{
    public float lifetime = 5f;
    public GameObject explosionPrefab;
    public float explosionLifetime = 2f;
    public GameObject explosionAreaObject;
    public float explosionAreaDuration = 1f;

    [Header("Audio - Explosion")]
    public AudioClip[] explosionSounds; // Array to hold multiple explosion sounds
    [Range(0f, 1f)] public float explosionVolumeMultiplier = 1f;
    [Tooltip("Sets how much the explosion sound is affected by distance (0 = 2D, 1 = 3D).")]
    [Range(0f, 1f)] public float explosionSpatialBlend = 1f;
    [Tooltip("The minimum distance for the explosion sound.")]
    public float explosionMinDistance = 1f;
    [Tooltip("The maximum distance for the explosion sound.")]
    public float explosionMaxDistance = 10f;
    [Tooltip("The rolloff mode for the explosion sound.")]
    public AudioRolloffMode explosionRolloffMode = AudioRolloffMode.Linear;

    private Rigidbody rb;
    private bool hasExploded = false;

    void Awake() // Use Awake to ensure Rigidbody is found even if Start isn't called immediately
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component NOT FOUND on this projectile!", this);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("CannonProjectile found Rigidbody: " + rb);
        }

        // Destroy the projectile after its lifetime
        Destroy(gameObject, lifetime);

        if (explosionAreaObject != null)
        {
            explosionAreaObject.SetActive(false);
        }
    }

    public void Launch(Vector3 direction, float force)
    {
        if (rb != null)
        {
            Debug.Log($"CannonProjectile Launching with direction: {direction}, force: {force}, ForceMode: Impulse");
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("Rigidbody is NULL in Launch method!", this);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        Explode(collision.contacts[0].point);
    }

    void Explode(Vector3 explosionPosition)
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionAreaObject != null)
        {
            explosionAreaObject.transform.parent = null;
            explosionAreaObject.transform.position = explosionPosition;
            explosionAreaObject.SetActive(true);
            Destroy(explosionAreaObject, explosionAreaDuration);
        }

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
            Destroy(explosion, explosionLifetime);
        }

        // Play random explosion sound
        if (explosionSounds != null && explosionSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, explosionSounds.Length);

            // Create a new GameObject to hold the AudioSource for the sound
            GameObject soundObject = new GameObject("ExplosionSound");
            soundObject.transform.position = explosionPosition;
            AudioSource tempSource = soundObject.AddComponent<AudioSource>();
            tempSource.clip = explosionSounds[randomIndex];
            tempSource.volume = explosionVolumeMultiplier;
            ConfigureExplosionAudio(tempSource);
            tempSource.Play();

            // Destroy the temporary GameObject after the sound finishes
            Destroy(soundObject, explosionSounds[randomIndex].length);
        }

        Destroy(gameObject);
    }

    void ConfigureExplosionAudio(AudioSource source)
    {
        source.spatialBlend = explosionSpatialBlend;
        source.minDistance = explosionMinDistance;
        source.maxDistance = explosionMaxDistance;
        source.rolloffMode = explosionRolloffMode;
    }

    private void OnValidate()
    {
        // Ensure spatial audio properties are valid in the editor
        explosionSpatialBlend = Mathf.Clamp01(explosionSpatialBlend);
        explosionMinDistance = Mathf.Max(0f, explosionMinDistance);
        explosionMaxDistance = Mathf.Max(explosionMinDistance, explosionMaxDistance);
        explosionVolumeMultiplier = Mathf.Max(0f, explosionVolumeMultiplier);
    }
}