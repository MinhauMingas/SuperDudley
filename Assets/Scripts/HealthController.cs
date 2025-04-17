// HealthController.cs
using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Transform heartsParent;
    public GameObject heartContainerPrefab;

    [Header("Health Values")]
    [SerializeField] private float health;
    [SerializeField] private float maxHealth = 3;
    [SerializeField] private float maxTotalHealth = 3;

    private GameObject[] heartContainers;
    private Image[] heartFills;

    public float Health { get { return health; } }
    public float MaxHealth { get { return maxHealth; } }
    public float MaxTotalHealth { get { return maxTotalHealth; } }

    public delegate void OnHealthChangedDelegate();
    public OnHealthChangedDelegate onHealthChangedCallback;

    void Start()
    {
        health = maxHealth;
        ClampHealth();

        heartContainers = new GameObject[(int)maxTotalHealth];
        heartFills = new Image[(int)maxTotalHealth];

        onHealthChangedCallback += UpdateHeartsHUD;
        InstantiateHeartContainers();
        UpdateHeartsHUD();
    }

    public void UpdateHeartsHUD()
    {
        SetHeartContainers();
        SetFilledHearts();
    }

    void SetHeartContainers()
    {
        for (int i = 0; i < heartContainers.Length; i++)
        {
            if (i < maxHealth)
            {
                heartContainers[i].SetActive(true);
            }
            else
            {
                heartContainers[i].SetActive(false);
            }
        }
    }

    void SetFilledHearts()
    {
        for (int i = 0; i < heartFills.Length; i++)
        {
            if (i < health)
            {
                heartFills[i].fillAmount = 1;
            }
            else
            {
                heartFills[i].fillAmount = 0;
            }
        }

        if (health % 1 != 0)
        {
            int lastPos = Mathf.FloorToInt(health);
            heartFills[lastPos].fillAmount = health % 1;
        }
    }

    void InstantiateHeartContainers()
    {
        for (int i = 0; i < maxTotalHealth; i++)
        {
            GameObject temp = Instantiate(heartContainerPrefab);
            temp.transform.SetParent(heartsParent, false);
            heartContainers[i] = temp;
            heartFills[i] = temp.transform.Find("HeartFill").GetComponent<Image>();
        }
    }

    public void Heal(float amount)
    {
        this.health += amount;
        ClampHealth();
        Debug.Log($"Player healed by {amount}. Current Health: {health}");
    }

    public void AddMaxHealth(int amount = 1)
    {
        if (maxHealth < maxTotalHealth)
        {
            maxHealth = Mathf.Min(maxHealth + amount, maxTotalHealth);
            health = maxHealth;
            ClampHealth();
            Debug.Log($"Player max health increased. Max Health: {maxHealth}, Current Health: {health}");
        }
    }

    void ClampHealth()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        onHealthChangedCallback?.Invoke();
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        ClampHealth();
    }
}

