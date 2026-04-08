using UnityEngine;
using System.IO;

[System.Serializable]
public class GameConfig
{
    public float fishMinSpeed;
    public float fishMaxSpeed;
    public float detectionRadius;
    public float hungerCooldown;
    public float trashMinSpeed;
    public float trashMaxSpeed;
}

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }
    public GameConfig Config { get; private set; }

    private void Awake()
    {
        // Singleton pattern agar ConfigManager mudah diakses dari mana saja tanpa perlu referensi langsung
        if (Instance == null)
        {
            Instance = this;
            LoadConfig();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadConfig()
    {
        string configPath = Path.Combine(Application.dataPath, "../config.json");

        configPath = Path.GetFullPath(configPath);

        if (File.Exists(configPath))
        {
            string jsonContent = File.ReadAllText(configPath);
            Config = JsonUtility.FromJson<GameConfig>(jsonContent);
            Debug.Log("Config loaded successfully from: " + configPath);
        }
        else
        {
            Debug.LogWarning("Config file not found at: " + configPath);
            Config = new GameConfig
            {
                fishMinSpeed = 1f,
                fishMaxSpeed = 3f,
                detectionRadius = 5f,
                hungerCooldown = 10f,
                trashMinSpeed = 0.5f,
                trashMaxSpeed = 1.5f
            };
        }
    }
}