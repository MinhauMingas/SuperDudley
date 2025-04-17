using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerSpawn : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    [Header("Fade Settings")]
    public float fadeOutDuration = 0.5f;
    public float fadeInDuration = 0.3f;
    public float fadeHoldDuration = 0.2f;

    [Header("Cinemachine Settings")]
    public CinemachineCamera virtualCamera;
    public CinemachineOffsetYModifier cameraOffsetModifier; // Add this line

    private Image fadeImage;
    private static Image staticFadeImage;
    private static bool sceneReloaded = false;
    public GameObject currentPlayer;
    private GameObject canvasHP; // Reference to Canva-HP

    private static Transform lastCheckpoint = null;

    void Awake()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            lastCheckpoint = null;
        }
    }

    void Start()
    {
        if (staticFadeImage == null)
        {
            staticFadeImage = CreateFadeImage();
        }

        fadeImage = staticFadeImage;

        // Find Canva-HP
        canvasHP = GameObject.Find("Canva-HP");
        if (canvasHP == null)
        {
            Debug.LogError("Canva-HP GameObject not found!");
        }

        SpawnPlayer();

        if (sceneReloaded)
        {
            StartCoroutine(FadeIn());
            sceneReloaded = false;
        }
    }

    public void RespawnPlayer()
    {
        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        yield return StartCoroutine(FadeToBlack(fadeOutDuration));

        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        // Call ResetOffsetImmediate *before* spawning the new player
        if (cameraOffsetModifier != null)
        {
            cameraOffsetModifier.ResetOffsetImmediate(); // Reset immediately
        }

        SpawnPlayer();

        if (virtualCamera != null && currentPlayer != null)
        {
            virtualCamera.Follow = currentPlayer.transform;
            virtualCamera.LookAt = currentPlayer.transform;
        }
        else
        {
            Debug.LogWarning("Cinemachine Virtual Camera or currentPlayer is null. Camera tracking may not work.");
        }

        yield return new WaitForSeconds(fadeHoldDuration);

        yield return StartCoroutine(FadeIn());
    }

    void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            Transform spawn = lastCheckpoint != null ? lastCheckpoint : spawnPoint;

            if (spawn != null)
            {
                currentPlayer = Instantiate(playerPrefab, spawn.position, spawn.rotation);

                // Reset Player Health Directly on Canva-HP HealthController
                if (canvasHP != null)
                {
                    HealthController playerHealth = canvasHP.GetComponent<HealthController>();
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(playerHealth.MaxHealth); // Set current health to max health
                        Debug.Log("Player health reset to max.");
                    }
                    else
                    {
                        Debug.LogError("HealthController component not found on Canva-HP.");
                    }
                }
            }
            else
            {
                Debug.LogError("Spawn point or last checkpoint is not assigned.");
            }
        }
        else
        {
            Debug.LogError("Player prefab is not assigned.");
        }
    }

    IEnumerator FadeToBlack(float duration)
    {
        if (fadeImage != null)
        {
            Color startColor = new Color(0, 0, 0, 0);
            Color endColor = new Color(0, 0, 0, 1);
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                fadeImage.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
                yield return null;
            }

            fadeImage.color = endColor;
        }
        else
        {
            Debug.LogWarning("Fade image is null. Fade effect will not occur.");
        }
    }

    IEnumerator FadeIn()
    {
        if (fadeImage != null)
        {
            Color startColor = new Color(0, 0, 0, 1);
            Color endColor = new Color(0, 0, 0, 0);
            float elapsedTime = 0;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeImage.color = Color.Lerp(startColor, endColor, elapsedTime / fadeInDuration);
                yield return null;
            }

            fadeImage.color = endColor;
        }
        else
        {
            Debug.LogWarning("Fade image is null. Fade effect will not occur.");
        }
    }

    private Image CreateFadeImage()
    {
        GameObject fadeObject = new GameObject("FadeImage");
        GameObject canvasObject = GameObject.Find("Canva-HP");
        if (canvasObject != null)
        {
            Debug.Log("Canvas Found");
        }
        else
        {
            Debug.Log("Canvas Not Found");
        }
        fadeObject.transform.SetParent(canvasObject.transform, false);

        Image image = fadeObject.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0);

        RectTransform rectTransform = fadeObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(0, 0);
        rectTransform.offsetMax = new Vector2(0, 0);

        return image;
    }

    public static void SetCheckpoint(Transform checkpoint)
    {
        lastCheckpoint = checkpoint;
    }
}