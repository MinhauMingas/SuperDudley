// ScoreManager.cs
using UnityEngine;
using TMPro; // Use this if using TextMeshPro
// using UnityEngine.UI; // Use this if using standard UI Text

public class ScoreManager : MonoBehaviour
{
    // Singleton pattern: Ensures only one instance exists
    public static ScoreManager Instance { get; private set; }

    private int coinCount = 0;

    // Assign your UI Text element in the Inspector
    [SerializeField] private TextMeshProUGUI coinText;
    // [SerializeField] private Text coinText; // For standard UI Text

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate managers
        }
        else
        {
            Instance = this;
            // Optional: Keep the manager across scene loads
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Initialize the text display
        UpdateCoinText();
    }

    // Public method for coins to call when collected
    public void AddCoin(int amount = 1)
    {
        coinCount += amount;
        UpdateCoinText();
        Debug.Log("Coin collected! Total coins: " + coinCount); // For verification
    }

    // Method to update the UI display
    private void UpdateCoinText()
    {
        if (coinText != null)
        {
            // Customize the format as needed (e.g., "Coins: ")
            coinText.text = " " + coinCount;
        }
        else
        {
            Debug.LogError("Coin Text UI element not assigned to the ScoreManager!");
        }
    }

    // Optional: Method to get the current coin count from other scripts
    public int GetCoinCount()
    {
        return coinCount;
    }
}