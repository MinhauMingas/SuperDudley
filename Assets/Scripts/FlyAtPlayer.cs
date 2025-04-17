using UnityEngine;

public class FlyAtPlayer : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] GameObject destructionEffectPrefab;
    [SerializeField] float projectileSpeed = 1f;
    [SerializeField] float destroyDelay = 0.1f;
    [SerializeField] float followDuration = 3f;
    [SerializeField] float postFollowDuration = 2f;
    [SerializeField] Vector3 initialOffset = new Vector3(0.5f, 0.3f, 0f); // Initial height offset
    [SerializeField] float descendSpeed = 0.5f; // How quickly it loses height after following

    private Transform playerTransform;
    private Vector3 targetPosition;
    private float followTimer;
    private float postFollowTimer;
    private bool isFollowing = true;
    private int floorLayer;
    private bool isDestroying = false;
    private float currentHeightOffset; // Track current height offset

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player not found. Make sure the player has the correct tag.");
        }

        followTimer = followDuration;
        postFollowTimer = postFollowDuration;
        currentHeightOffset = initialOffset.y;
        floorLayer = LayerMask.NameToLayer("Floor");
        UpdateTargetPosition();
    }

    void Update()
    {
        if (!isDestroying)
        {
            UpdateTargetPosition();
            MoveTowards();
            DestroyWhenReached();
            CheckPlayerReached();
        }
    }

    void UpdateTargetPosition()
    {
        if (isFollowing && playerTransform != null)
        {
            if (followTimer > 0)
            {
                // Apply X/Z offset but reduce Y offset over time
                targetPosition = playerTransform.position + 
                               new Vector3(initialOffset.x, currentHeightOffset, initialOffset.z);
                followTimer -= Time.deltaTime;
                
                // Gradually reduce height offset during follow phase
                currentHeightOffset = Mathf.Lerp(0, initialOffset.y, followTimer / followDuration);
            }
            else
            {
                isFollowing = false;
            }
        }
        else if (!isFollowing)
        {
            // After following, gradually descend
            currentHeightOffset = Mathf.Max(0, currentHeightOffset - descendSpeed * Time.deltaTime);
            targetPosition.y -= descendSpeed * Time.deltaTime;
        }
    }

    void MoveTowards()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, projectileSpeed * Time.deltaTime);
    }

    void CheckPlayerReached()
    {
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < 0.5f)
        {
            DestroyWithEffect();
        }
    }

    void DestroyWhenReached()
    {
        if (!isFollowing)
        {
            if (postFollowTimer > 0)
            {
                postFollowTimer -= Time.deltaTime;
            }
            else
            {
                DestroyWithEffect();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Destroy when colliding with anything (except maybe the shooter if needed)
        if (!isDestroying && other.gameObject.tag != "IgnoreProjectile") // Add any tags you want to ignore
        {
            DestroyWithEffect();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Destroy when colliding with anything (except maybe the shooter if needed)
        if (!isDestroying && collision.gameObject.tag != "IgnoreProjectile") // Add any tags you want to ignore
        {
            DestroyWithEffect();
        }
    }

    void DestroyWithEffect()
    {
        if (!isDestroying)
        {
            isDestroying = true;

            if (destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }

            StartCoroutine(ScaleDownAndDestroy());
        }
    }

    System.Collections.IEnumerator ScaleDownAndDestroy()
    {
        float scaleDuration = destroyDelay;
        Vector3 initialScale = transform.localScale;
        float timer = 0f;

        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, timer / scaleDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}