using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalState : MonoBehaviour
{
    [Header("Player Stats")]
    public int power = 100;
    
    [Header("Scoring")]
    [SerializeField] public int baseTimePoints = 100;
    [SerializeField] private int pointsPerGlorp = 50;
    
    [Header("Current Level Stats")]
    private float levelStartTime;
    private int glorpsCollected;
    private int totalScore;
    
    public static GlobalState Instance;


    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
    
    private void Start()
    {
        StartLevel();
    }
    
        
    // Add these event subscription methods
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartLevel();
    }
    
    private void StartLevel()
    {
        levelStartTime = Time.time;
        glorpsCollected = 0;
        power = 100;
        Debug.Log("Level started - timer reset");
    }

    private void RestartLevel()
    {
        Debug.Log("Restarting Level");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        levelStartTime = Time.time;
        glorpsCollected = 0;
        power = 100;
    }
    
    public bool hasEnoughPower(int amount)
    {
        return power >= amount;
    }
    
    public void DecreasePower(int amount)
    {
        power = power - amount;
    }
    
    public void CollectGlorp()
    {
        glorpsCollected++;
        Debug.Log($"Glorp collected! Total: {glorpsCollected}");
    }

    public int CalculateLevelScore()
    {
        float completionTime = Time.time - levelStartTime;
        
        // Option 1: Inverse time scoring
        int timeBonus = Mathf.RoundToInt(baseTimePoints / completionTime);
        
        // Linear energy bonus
        int energyBonus = power;
        
        // Glorp points
        int glorpPoints = glorpsCollected * pointsPerGlorp;
        
        int levelScore = timeBonus + energyBonus + glorpPoints;
        totalScore += levelScore;
        
        Debug.Log($"Level completed in {completionTime:F1}s");
        Debug.Log($"Time Bonus: {timeBonus}, Energy Bonus: {energyBonus}, Glorp Points: {glorpPoints}");
        Debug.Log($"Level Score: {levelScore}");
        Debug.Log($"total score: {totalScore}");

        return levelScore;
    }
    
    public int GetTotalScore()
    {
        return totalScore;
    }
}
